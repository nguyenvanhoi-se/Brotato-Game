#if BROTATO_E2E && (UNITY_EDITOR || DEVELOPMENT_BUILD)
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Brotato.E2E
{
    // This transport is compiled only into an isolated Development test build.
    // Network workers never call Unity APIs: Update dispatches every operation.
    [Preserve]
    public sealed class E2EBridge : MonoBehaviour
    {
        private const int MaxFrameBytes = 1024 * 1024;
        private const int MaxQueuedRequests = 64;
        private const int RequestTimeoutMs = 30000;
        private const string TimeoutResponse = "{\"id\":0,\"ok\":false,\"result\":\"null\",\"error\":\"Request timed out or bridge stopped.\"}";
        private static readonly HashSet<string> HookMethods = new HashSet<string>(StringComparer.Ordinal)
        {
            "CreateEnemy", "SetTarget", "SetSpeed", "SetEnemyPosition", "MoveTarget",
            "Damage", "InitializeHealth", "RemoveTarget", "CreateSpawner", "SetWave",
            "DropReference", "SetMinimumDistance", "SetSpawnMargin", "SetRandomSeed",
            "ClearEnemies", "SetSpawning", "SetupContact", "SetContact"
        };
        private readonly ConcurrentQueue<Pending> requests = new ConcurrentQueue<Pending>();
        private readonly ConcurrentDictionary<TcpClient, byte> clients = new ConcurrentDictionary<TcpClient, byte>();
        private TcpListener listener;
        private string token;
        private string artifactsRoot;
        private volatile bool stopping;
        private int queuedCount;
        private int stopped;
        private Keyboard virtualKeyboard;
        private Keyboard originalKeyboard;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (UnityEngine.Object.FindFirstObjectByType<E2EBridge>() != null) return;
            var owner = new GameObject("__UnityE2EDriver");
            DontDestroyOnLoad(owner);
            owner.AddComponent<E2EBridge>();
        }

        private void Awake()
        {
            token = Environment.GetEnvironmentVariable("UNITY_E2E_TOKEN");
            string portText = Environment.GetEnvironmentVariable("UNITY_E2E_PORT");
            string root = Environment.GetEnvironmentVariable("UNITY_E2E_ARTIFACTS_ROOT");
            if (string.IsNullOrEmpty(token) || token.Length < 32 ||
                !int.TryParse(portText, out int port) || port < 1 || port > 65535 || string.IsNullOrEmpty(root))
                throw new InvalidOperationException("The test runner must supply a token, loopback port and artifact root.");
            artifactsRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(artifactsRoot);
            listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start(4);
            Task.Run(AcceptClients);
        }

        private async Task AcceptClients()
        {
            try
            {
                while (!stopping)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    if (stopping || clients.Count >= 2)
                    {
                        client.Close();
                        continue;
                    }
                    clients.TryAdd(client, 0);
                    Task.Run(() => ServeClient(client));
                }
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
        }

        private void ServeClient(TcpClient client)
        {
            try
            {
                client.NoDelay = true;
                client.ReceiveTimeout = RequestTimeoutMs + 5000;
                client.SendTimeout = RequestTimeoutMs + 5000;
                using (NetworkStream stream = client.GetStream())
                {
                    while (!stopping)
                    {
                        string frame = ReadFrame(stream);
                        if (frame == null) break;
                        if (Interlocked.Increment(ref queuedCount) > MaxQueuedRequests)
                        {
                            Interlocked.Decrement(ref queuedCount);
                            WriteFrame(stream, TimeoutResponse);
                            break;
                        }
                        var pending = new Pending { Frame = frame };
                        requests.Enqueue(pending);
                        if (!pending.Completion.Task.Wait(RequestTimeoutMs))
                        {
                            pending.Expired = true;
                            WriteFrame(stream, TimeoutResponse);
                            break;
                        }
                        WriteFrame(stream, pending.Completion.Task.Result);
                    }
                }
            }
            catch (IOException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            catch (AggregateException) { }
            finally
            {
                clients.TryRemove(client, out _);
                client.Close();
            }
        }

        private static string ReadFrame(Stream stream)
        {
            using (var frame = new MemoryStream())
            {
                while (frame.Length <= MaxFrameBytes)
                {
                    int next = stream.ReadByte();
                    if (next < 0) return frame.Length == 0 ? null : throw new IOException("Incomplete JSON frame.");
                    if (next == '\n') return Encoding.UTF8.GetString(frame.ToArray());
                    if (next != '\r') frame.WriteByte((byte)next);
                }
            }
            throw new IOException("Request exceeds one megabyte.");
        }

        private static void WriteFrame(Stream stream, string frame)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(frame + "\n");
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }

        private void Update()
        {
            for (int count = 0; count < 16 && requests.TryDequeue(out Pending pending); count++)
            {
                Interlocked.Decrement(ref queuedCount);
                if (pending.Expired) continue;
                Request request = null;
                Exception failure = null;
                try
                {
                    request = JsonUtility.FromJson<Request>(pending.Frame);
                    if (request == null || !string.Equals(request.token, token, StringComparison.Ordinal))
                        throw new UnauthorizedAccessException("Invalid test session token.");
                    if (string.IsNullOrEmpty(request.command)) throw new ArgumentException("A command is required.");
                }
                catch (Exception exception) { failure = exception; }
                if (failure != null) Complete(pending, request == null ? 0 : request.id, null, failure);
                else StartCoroutine(Execute(request, pending));
            }
        }

        private IEnumerator Execute(Request request, Pending pending)
        {
            var result = new Result();
            IEnumerator operation = Operation(request, result);
            try
            {
                while (!pending.Expired && !stopping)
                {
                    bool more = false;
                    object yielded = null;
                    Exception failure = null;
                    try
                    {
                        more = operation.MoveNext();
                        if (more) yielded = operation.Current;
                    }
                    catch (Exception exception) { failure = exception; }
                    if (failure != null)
                    {
                        Complete(pending, request.id, null, failure);
                        yield break;
                    }
                    if (!more)
                    {
                        Complete(pending, request.id, result.Json, null);
                        yield break;
                    }
                    yield return yielded;
                }
            }
            finally { (operation as IDisposable)?.Dispose(); }
        }

        private IEnumerator Operation(Request request, Result result)
        {
            switch (request.command)
            {
                case "hello":
                    Identity identity = JsonUtility.FromJson<Identity>(Harness().Snapshot());
                    identity.protocolVersion = "1";
                    result.Json = JsonUtility.ToJson(identity);
                    break;
                case "scene": result.Json = Quote(SceneManager.GetActiveScene().name); break;
                case "snapshot": result.Json = Harness().Snapshot(); break;
                case "call":
                    CallHook(request);
                    result.Json = "null";
                    break;
                case "find":
                    var found = new List<ObjectInfo>();
                    foreach (GameObject owner in Objects(request)) found.Add(Describe(owner));
                    result.Json = JsonUtility.ToJson(new ObjectList { objects = found.ToArray() });
                    break;
                case "property": result.Json = ReadProperty(request); break;
                case "click": Click(request); break;
                case "load_scene":
                    if (string.IsNullOrEmpty(request.scene) || !Application.CanStreamedLevelBeLoaded(request.scene))
                        throw new ArgumentException("Scene is not in the test build: " + request.scene);
                    AsyncOperation loading = SceneManager.LoadSceneAsync(request.scene, LoadSceneMode.Single);
                    if (loading == null) throw new InvalidOperationException("Unity did not create a scene load operation.");
                    while (!loading.isDone) yield return null;
                    yield return null;
                    result.Json = Quote(SceneManager.GetActiveScene().name);
                    break;
                case "screenshot":
                    string destination = ArtifactPath(request.path);
                    if (File.Exists(destination)) throw new IOException("Screenshot already exists; use a new artifact name.");
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(destination);
                    yield return new WaitForEndOfFrame();
                    float deadline = Time.realtimeSinceStartup + 5f;
                    while (!ScreenshotReady(destination) && Time.realtimeSinceStartup < deadline) yield return null;
                    if (!ScreenshotReady(destination))
                        throw new IOException("Unity did not produce a screenshot.");
                    result.Json = Quote(destination);
                    break;
                case "input":
                    if (!Enum.TryParse(request.key, true, out Key key) || !Enum.IsDefined(typeof(Key), key) || key == Key.None)
                        throw new ArgumentException("Unknown Input System key: " + request.key);
                    if (float.IsNaN(request.duration) || float.IsInfinity(request.duration) || request.duration < 0f || request.duration > 10f)
                        throw new ArgumentOutOfRangeException("duration", "Use a duration from zero to ten seconds.");
                    if (virtualKeyboard == null)
                    {
                        originalKeyboard = Keyboard.current;
                        virtualKeyboard = InputSystem.AddDevice<Keyboard>("UnityE2EKeyboard");
                    }
                    virtualKeyboard.MakeCurrent();
                    InputSystem.QueueStateEvent(virtualKeyboard, new KeyboardState(key));
                    try
                    {
                        yield return null;
                        if (request.duration > 0f) yield return new WaitForSecondsRealtime(request.duration);
                    }
                    finally
                    {
                        if (virtualKeyboard != null && virtualKeyboard.added)
                            InputSystem.QueueStateEvent(virtualKeyboard, new KeyboardState());
                    }
                    yield return null;
                    if (originalKeyboard != null && originalKeyboard.added) originalKeyboard.MakeCurrent();
                    break;
                default: throw new ArgumentException("Unknown test command: " + request.command);
            }
        }

        private static E2EHarness Harness()
        {
            E2EHarness[] matches = UnityEngine.Object.FindObjectsByType<E2EHarness>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (matches.Length != 1) throw new InvalidOperationException("Expected exactly one active E2EHarness.");
            return matches[0];
        }

        private static void CallHook(Request request)
        {
            if (!HookMethods.Contains(request.method ?? "")) throw new ArgumentException("Hook is not allowed: " + request.method);
            MethodInfo method = typeof(E2EHarness).GetMethod(request.method, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (method == null) throw new MissingMethodException(typeof(E2EHarness).FullName, request.method);
            ParameterInfo[] parameters = method.GetParameters();
            string[] supplied = request.args ?? Array.Empty<string>();
            if (parameters.Length != supplied.Length) throw new ArgumentException("Hook argument count does not match.");
            var values = new object[parameters.Length];
            for (int index = 0; index < parameters.Length; index++)
                values[index] = Convert.ChangeType(supplied[index], parameters[index].ParameterType, CultureInfo.InvariantCulture);
            method.Invoke(Harness(), values);
        }

        private static List<GameObject> Objects(Request request)
        {
            var matches = new List<GameObject>();
            var scanned = new HashSet<int>();
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (!scene.isLoaded) continue;
                scanned.Add(scene.handle);
                foreach (GameObject root in scene.GetRootGameObjects()) FindChildren(root.transform, request, matches);
            }
            // Traverse the persistent scene too; user-owned DontDestroyOnLoad UI
            // is not exposed by SceneManager's ordinary sceneCount enumeration.
            foreach (E2EBridge bridge in UnityEngine.Object.FindObjectsByType<E2EBridge>(FindObjectsSortMode.None))
            {
                Scene persistent = bridge.gameObject.scene;
                if (!scanned.Add(persistent.handle)) continue;
                foreach (GameObject root in persistent.GetRootGameObjects()) FindChildren(root.transform, request, matches);
            }
            return matches;
        }

        private static void FindChildren(Transform node, Request request, List<GameObject> matches)
        {
            if (!node.gameObject.activeInHierarchy) return;
            bool match = request.objectId != 0 ? node.gameObject.GetInstanceID() == request.objectId :
                !string.IsNullOrEmpty(request.path) ? string.Equals(ObjectPath(node), request.path, StringComparison.Ordinal) :
                string.IsNullOrEmpty(request.name) || string.Equals(node.name, request.name, StringComparison.Ordinal);
            if (match) matches.Add(node.gameObject);
            foreach (Transform child in node) FindChildren(child, request, matches);
        }

        private static GameObject SingleObject(Request request)
        {
            List<GameObject> matches = Objects(request);
            if (matches.Count != 1) throw new InvalidOperationException("Expected one matching active object; found " + matches.Count);
            return matches[0];
        }

        private static string ObjectPath(Transform owner)
        {
            string path = "/" + owner.name;
            while (owner.parent != null) { owner = owner.parent; path = "/" + owner.name + path; }
            return path;
        }

        private static ObjectInfo Describe(GameObject owner)
        {
            return new ObjectInfo { id = owner.GetInstanceID(), name = owner.name, path = ObjectPath(owner.transform), active = owner.activeInHierarchy };
        }

        private static string ReadProperty(Request request)
        {
            GameObject owner = SingleObject(request);
            object component = owner;
            if (request.component != "UnityEngine.GameObject" && request.component != "GameObject")
            {
                Type type = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    if ((type = assembly.GetType(request.component ?? "", false)) != null) break;
                if (type == null || !typeof(Component).IsAssignableFrom(type))
                    throw new ArgumentException("Unknown Unity component type: " + request.component);
                component = owner.GetComponent(type);
                if (component == null) throw new MissingComponentException(request.component);
            }
            const BindingFlags publicInstance = BindingFlags.Public | BindingFlags.Instance;
            Type componentType = component.GetType();
            PropertyInfo property = componentType.GetProperty(request.property ?? "", publicInstance);
            object value;
            if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
                value = property.GetValue(component);
            else
            {
                FieldInfo field = componentType.GetField(request.property ?? "", publicInstance);
                if (field == null) throw new MissingMemberException(componentType.FullName, request.property);
                value = field.GetValue(component);
            }
            return SerializeValue(value);
        }

        private static string SerializeValue(object value)
        {
            if (value == null) return "null";
            if (value is string text) return Quote(text);
            if (value is bool flag) return flag ? "true" : "false";
            if (value is float single)
            {
                if (float.IsNaN(single) || float.IsInfinity(single)) throw new InvalidOperationException("Non-finite property value.");
                return single.ToString("R", CultureInfo.InvariantCulture);
            }
            if (value is double number)
            {
                if (double.IsNaN(number) || double.IsInfinity(number)) throw new InvalidOperationException("Non-finite property value.");
                return number.ToString("R", CultureInfo.InvariantCulture);
            }
            if (value is UnityEngine.Object unityObject)
            {
                GameObject owner = unityObject as GameObject;
                if (unityObject is Component component) owner = component.gameObject;
                return owner != null ? JsonUtility.ToJson(Describe(owner)) : Quote(unityObject.name);
            }
            Type type = value.GetType();
            if (type.IsEnum) return Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            if (value is IConvertible) return Convert.ToString(value, CultureInfo.InvariantCulture);
            string json = JsonUtility.ToJson(value);
            if (json == "{}") throw new NotSupportedException("Property value cannot be represented by the test protocol: " + type.FullName);
            return json;
        }

        private static void Click(Request request)
        {
            GameObject owner = SingleObject(request);
            Button button = owner.GetComponent<Button>();
            if (button == null || !button.isActiveAndEnabled || !button.IsInteractable())
                throw new InvalidOperationException("Click requires an active interactable UI Button.");
            if (EventSystem.current == null) throw new InvalidOperationException("No active EventSystem for UI click.");
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            Canvas canvas = owner.GetComponentInParent<Canvas>();
            Camera camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            pointer.position = RectTransformUtility.WorldToScreenPoint(camera, owner.transform.position);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<Button>() != button)
                throw new InvalidOperationException("The UI Button is covered or not raycastable at its center.");
            ExecuteEvents.Execute(owner, pointer, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(owner, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(owner, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(owner, pointer, ExecuteEvents.pointerClickHandler);
        }

        private string ArtifactPath(string requested)
        {
            if (string.IsNullOrEmpty(requested)) throw new ArgumentException("Screenshot path is required.");
            string absolute = Path.GetFullPath(requested);
            StringComparison comparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!absolute.StartsWith(artifactsRoot, comparison) || !string.Equals(Path.GetExtension(absolute), ".png", comparison))
                throw new UnauthorizedAccessException("Screenshots must be PNG files beneath the runner artifact root.");
            return absolute;
        }

        private static bool ScreenshotReady(string path)
        {
            if (!File.Exists(path)) return false;
            try
            {
                using (var image = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (image.Length < 20) return false;
                    image.Seek(-12, SeekOrigin.End);
                    byte[] tail = new byte[12];
                    if (image.Read(tail, 0, tail.Length) != tail.Length) return false;
                    byte[] expected = { 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 };
                    for (int index = 0; index < tail.Length; index++)
                        if (tail[index] != expected[index]) return false;
                    return true;
                }
            }
            catch (IOException) { return false; }
        }

        private static string Quote(string text)
        {
            string wrapper = JsonUtility.ToJson(new StringValue { value = text });
            return wrapper.Substring(9, wrapper.Length - 10);
        }

        private static void Complete(Pending pending, int id, string result, Exception exception)
        {
            if (exception is TargetInvocationException invocation && invocation.InnerException != null) exception = invocation.InnerException;
            var response = new Response
            {
                id = id, ok = exception == null, result = result ?? "null",
                error = exception == null ? "" : exception.GetType().Name + ": " + exception.Message
            };
            pending.Completion.TrySetResult(JsonUtility.ToJson(response));
        }

        private void OnApplicationQuit() { Shutdown(); }
        private void OnDestroy() { Shutdown(); }

        private void Shutdown()
        {
            if (Interlocked.Exchange(ref stopped, 1) != 0) return;
            stopping = true;
            listener?.Stop();
            foreach (TcpClient client in clients.Keys) client.Close();
            while (requests.TryDequeue(out Pending pending)) pending.Completion.TrySetResult(TimeoutResponse);
            if (virtualKeyboard != null && virtualKeyboard.added)
            {
                InputSystem.QueueStateEvent(virtualKeyboard, new KeyboardState());
                InputSystem.RemoveDevice(virtualKeyboard);
            }
            if (originalKeyboard != null && originalKeyboard.added) originalKeyboard.MakeCurrent();
        }

        private sealed class Pending
        {
            public string Frame;
            public volatile bool Expired;
            public readonly TaskCompletionSource<string> Completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        private sealed class Result { public string Json = "null"; }
        [Serializable] private sealed class StringValue { public string value; }
        [Serializable] private sealed class Request
        {
            public int id, objectId, frames;
            public string token, command, name, path, component, property, method, scene, key;
            public string[] args;
            public float duration;
        }
        [Serializable] private sealed class Response { public int id; public bool ok; public string result, error; }
        [Serializable] private sealed class ObjectInfo { public int id; public string name, path; public bool active; }
        [Serializable] private sealed class ObjectList { public ObjectInfo[] objects; }
        [Serializable] private sealed class Identity
        {
            public int processId;
            public string sessionNonce, protocolVersion, scene, sourceFingerprint, testBuildFingerprint;
        }
    }
}
#endif
