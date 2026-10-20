#if BROTATO_E2E && (UNITY_EDITOR || DEVELOPMENT_BUILD)
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Brotato.E2E
{
    // Copied into an isolated test project only. These hooks set up simulated
    // component scenarios; they do not represent a player journey or assert success.
    [Preserve]
    public sealed class E2EHarness : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private string sourceFingerprint;
        [SerializeField] private string testBuildFingerprint;

        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly object ErrorLock = new object();
        private static readonly List<string> Errors = new List<string>();
        private readonly HashSet<int> observedSpawns = new HashSet<int>();
        private readonly List<Position> spawnSamples = new List<Position>();
        private GameObject enemy;
        private Transform target;
        private EnemySpawner spawner;
        private Transform enemiesParent;
        private GameObject templateContainer;
        private GameObject contactActor;
        private Rigidbody2D contactBody;
        private Collider2D contactCollider;
        private PlayerHealth contactHealth;
        private EnemyHealth contactOtherHealth;
        private E2EContactProbe contactProbe;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CaptureErrors()
        {
            // Static state deliberately survives LoadScene resets. Errors from an
            // earlier scenario must remain visible to teardown and the report.
            Application.logMessageReceivedThreaded -= OnLog;
            Application.logMessageReceivedThreaded += OnLog;
        }

        private static void OnLog(string message, string trace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            lock (ErrorLock) Errors.Add(type + ": " + message + "\n" + trace);
        }

        private void Update()
        {
            ObserveSpawns();
        }

        private void EnsureTarget()
        {
            if (target != null) return;
            target = new GameObject("__E2ETarget").transform;
            target.SetParent(transform, false);
            target.position = Vector3.zero;
        }

        private void RequireEnemy()
        {
            if (enemy == null) throw new InvalidOperationException("CreateEnemy must run first and enemy must still exist.");
        }

        private void RequireSpawner()
        {
            if (spawner == null) throw new InvalidOperationException("CreateSpawner must run first.");
        }

        private static FieldInfo Field(Type type, string name)
        {
            FieldInfo field = type.GetField(name, Fields);
            if (field == null) throw new MissingFieldException(type.FullName, name);
            return field;
        }

        private void SetSpawnerField(string name, object value)
        {
            RequireSpawner();
            Field(typeof(EnemySpawner), name).SetValue(spawner, value);
        }

        private bool HasSpawnerReference(string name)
        {
            if (spawner == null) return false;
            object value = Field(typeof(EnemySpawner), name).GetValue(spawner);
            if (value is UnityEngine.Object unityObject) return unityObject != null;
            return value != null;
        }

        [Preserve]
        public void CreateEnemy(bool withHealth = true)
        {
            if (enemy != null || spawner != null) throw new InvalidOperationException("Reload the scene before creating a new scenario.");
            if (enemyPrefab == null) throw new InvalidOperationException("Test build has no serialized Enemy prefab reference.");
            EnsureTarget();
            enemy = Instantiate(enemyPrefab, new Vector3(6f, 0f, 0f), Quaternion.identity, transform);
            enemy.name = "__E2EEnemy";
            EnemyController controller = enemy.GetComponent<EnemyController>();
            if (controller == null) throw new MissingComponentException("Saved Enemy prefab has no EnemyController.");
            controller.SetTarget(target); // Explicit existing API before Start's tag fallback.
            if (withHealth && enemy.GetComponent<EnemyHealth>() == null) enemy.AddComponent<EnemyHealth>();
        }

        [Preserve]
        public void SetTarget(bool enabled)
        {
            RequireEnemy();
            if (enabled) EnsureTarget();
            enemy.GetComponent<EnemyController>().SetTarget(enabled ? target : null);
        }

        [Preserve]
        public void SetSpeed(float speed)
        {
            RequireEnemy();
            enemy.GetComponent<EnemyController>().SetMoveSpeed(speed);
        }

        [Preserve]
        public void SetEnemyPosition(float x, float y)
        {
            RequireEnemy();
            enemy.transform.position = new Vector3(x, y, enemy.transform.position.z);
            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
            if (body != null) body.position = new Vector2(x, y);
        }

        [Preserve]
        public void MoveTarget(float x, float y)
        {
            EnsureTarget();
            target.position = new Vector3(x, y, target.position.z);
        }

        [Preserve]
        public void Damage(int amount)
        {
            RequireEnemy();
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health == null) throw new MissingComponentException("Scenario enemy has no EnemyHealth.");
            health.TakeDamage(amount);
        }

        [Preserve]
        public void InitializeHealth(int amount)
        {
            RequireEnemy();
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health == null) throw new MissingComponentException("Scenario enemy has no EnemyHealth.");
            health.InitializeHealth(amount);
        }

        [Preserve]
        public void RemoveTarget()
        {
            if (target != null) Destroy(target.gameObject);
        }

        [Preserve]
        public void CreateSpawner(bool withHealth)
        {
            if (enemy != null || spawner != null) throw new InvalidOperationException("Reload the scene before creating a new scenario.");
            if (enemyPrefab == null) throw new InvalidOperationException("Test build has no serialized Enemy prefab reference.");
            EnsureTarget();
            enemiesParent = new GameObject("__E2EEnemies").transform;
            enemiesParent.SetParent(transform, false);

            // Keep the source template under an inactive parent so it never runs
            // EnemyController.Start or contributes to the spawner's alive count.
            // Instantiation into the active enemies parent activates actual clones.
            templateContainer = new GameObject("__E2ETemplateContainer");
            templateContainer.transform.SetParent(transform, false);
            templateContainer.SetActive(false);
            GameObject template = Instantiate(enemyPrefab, templateContainer.transform);
            template.name = "__E2ESpawnTemplate";
            template.SetActive(true);
            if (withHealth && template.GetComponent<EnemyHealth>() == null) template.AddComponent<EnemyHealth>();

            GameObject owner = new GameObject("__E2ESpawner");
            owner.transform.SetParent(transform, false);
            spawner = owner.AddComponent<EnemySpawner>();
            spawner.enabled = false;
            SetSpawnerField("enemyPrefab", template);
            SetSpawnerField("player", target);
            SetSpawnerField("enemiesParent", enemiesParent);
            SetSpawnerField("gameplayCamera", Camera.main);
        }

        [Preserve]
        public void SetupContact(bool withPlayerHealth)
        {
            // Simulated fixture only: production prefab and production damage/HP
            // components, with actual physics callbacks instead of invoking them.
            CreateEnemy(false);
            enemy.GetComponent<EnemyController>().enabled = false;
            SetEnemyPosition(0f, 0f);
            Rigidbody2D enemyBody = enemy.GetComponent<Rigidbody2D>();
            enemyBody.gravityScale = 0f;
            enemyBody.sleepMode = RigidbodySleepMode2D.NeverSleep;
            enemy.AddComponent<EnemyContactDamage>();
            contactProbe = enemy.AddComponent<E2EContactProbe>();

            contactActor = new GameObject("__E2EContactActor");
            contactActor.transform.SetParent(transform, false);
            contactActor.transform.position = new Vector3(10f, 0f, 0f);
            contactBody = contactActor.AddComponent<Rigidbody2D>();
            contactBody.gravityScale = 0f;
            contactBody.freezeRotation = true;
            contactBody.sleepMode = RigidbodySleepMode2D.NeverSleep;
            if (withPlayerHealth) contactHealth = contactActor.AddComponent<PlayerHealth>();
            else contactOtherHealth = contactActor.AddComponent<EnemyHealth>();

            GameObject child = new GameObject("__E2EContactCollider");
            child.transform.SetParent(contactActor.transform, false);
            contactCollider = child.AddComponent<CircleCollider2D>();
            contactProbe.Other = contactCollider;
            Physics2D.SyncTransforms();
        }

        [Preserve]
        public void SetContact(bool overlapping)
        {
            if (contactBody == null) throw new InvalidOperationException("SetupContact must run first.");
            contactBody.position = overlapping ? Vector2.zero : new Vector2(10f, 0f);
            contactBody.WakeUp();
            Physics2D.SyncTransforms();
        }

        [Preserve]
        public void SetWave(float interval, int cap, float speed, int health)
        {
            RequireSpawner();
            // Original production contract recovered from upstream WaveManager.cs.
            // It has a five-argument constructor, not a parameterless constructor.
            spawner.ConfigureForWave(new WaveSettings(30f, cap, health, interval, speed));
            spawner.enabled = true;
        }

        [Preserve]
        public void DropReference(string name)
        {
            switch (name)
            {
                case "enemyPrefab": case "player": case "enemiesParent": case "gameplayCamera": case "settings":
                    SetSpawnerField(name, null);
                    break;
                default: throw new ArgumentException("Unknown EnemySpawner reference: " + name);
            }
        }

        [Preserve]
        public void SetMinimumDistance(float distance)
        {
            SetSpawnerField("minimumPlayerDistance", distance);
        }

        [Preserve]
        public void SetSpawnMargin(float margin)
        {
            SetSpawnerField("spawnMargin", margin);
        }

        [Preserve]
        public void SetRandomSeed(int seed)
        {
            // Explicit simulated scenario setup: this changes the global Unity
            // random stream; it is not input from a real player journey.
            UnityEngine.Random.InitState(seed);
        }

        [Preserve]
        public void ClearEnemies()
        {
            RequireSpawner();
            ObserveSpawns();
            spawner.ClearEnemies();
        }

        [Preserve]
        public void SetSpawning(bool enabled)
        {
            RequireSpawner();
            spawner.enabled = enabled;
        }

        private void ObserveSpawns()
        {
            if (enemiesParent == null) return;
            foreach (Transform child in enemiesParent)
                if (observedSpawns.Add(child.gameObject.GetInstanceID()))
                    // First observed frame, not exact Instantiate time/position.
                    // Assertions need a frame-time and movement tolerance.
                    spawnSamples.Add(new Position(child));
        }

        [Preserve]
        public string Snapshot()
        {
            ObserveSpawns();
            EnemyController controller = enemy == null ? null : enemy.GetComponent<EnemyController>();
            EnemyHealth health = enemy == null ? null : enemy.GetComponent<EnemyHealth>();
            Camera camera = Camera.main;
            var positions = new List<Position>();
            if (enemiesParent != null)
                foreach (Transform child in enemiesParent)
                    if (child.gameObject.activeInHierarchy) positions.Add(new Position(child));
            string[] errors;
            lock (ErrorLock) errors = Errors.ToArray();
            return JsonUtility.ToJson(new State
            {
                scene = SceneManager.GetActiveScene().name,
                processId = System.Diagnostics.Process.GetCurrentProcess().Id,
                sessionNonce = Environment.GetEnvironmentVariable("BROTATO_E2E_SESSION_NONCE") ?? "",
                sourceFingerprint = sourceFingerprint,
                testBuildFingerprint = testBuildFingerprint,
                enemyExists = enemy != null,
                enemyX = enemy == null ? 0f : enemy.transform.position.x,
                enemyY = enemy == null ? 0f : enemy.transform.position.y,
                targetX = target == null ? 0f : target.position.x,
                targetY = target == null ? 0f : target.position.y,
                targetExists = target != null,
                speed = controller == null ? 0f : controller.MoveSpeed,
                currentHealth = health == null ? -1 : health.CurrentHealth,
                maxHealth = health == null ? -1 : health.MaxHealth,
                aliveCount = spawner != null ? spawner.AliveCount : health != null && health.CurrentHealth > 0 ? 1 : 0,
                spawnedCount = observedSpawns.Count,
                positions = positions.ToArray(),
                spawnSamples = spawnSamples.ToArray(),
                cameraX = camera == null ? 0f : camera.transform.position.x,
                cameraY = camera == null ? 0f : camera.transform.position.y,
                halfHeight = camera == null ? 0f : camera.orthographicSize,
                halfWidth = camera == null ? 0f : camera.orthographicSize * camera.aspect,
                minimumDistance = spawner == null ? 0f : (float)Field(typeof(EnemySpawner), "minimumPlayerDistance").GetValue(spawner),
                spawnMargin = spawner == null ? 0f : (float)Field(typeof(EnemySpawner), "spawnMargin").GetValue(spawner),
                errorCount = errors.Length,
                errors = errors,
                sourceEnemyHasHealth = enemyPrefab != null && enemyPrefab.GetComponent<EnemyHealth>() != null,
                hasEnemyPrefab = HasSpawnerReference("enemyPrefab"),
                hasPlayer = HasSpawnerReference("player"),
                hasEnemiesParent = HasSpawnerReference("enemiesParent"),
                hasCamera = HasSpawnerReference("gameplayCamera"),
                hasSettings = HasSpawnerReference("settings"),
                spawning = spawner != null && spawner.enabled,
                contactExists = contactActor != null && contactCollider != null && contactProbe != null,
                contactHasPlayerHealth = contactCollider != null && contactCollider.GetComponentInParent<PlayerHealth>() != null,
                contactColliderIsChild = contactCollider != null && contactCollider.transform.parent == contactActor.transform,
                contactHealth = contactHealth == null ? -1 : contactHealth.CurrentHealth,
                contactMaxHealth = contactHealth == null ? -1 : contactHealth.MaxHealth,
                contactOtherHealth = contactOtherHealth == null ? -1 : contactOtherHealth.CurrentHealth,
                contactDamageCooldown = contactHealth == null ? -1f : (float)Field(typeof(PlayerHealth), "damageCooldown").GetValue(contactHealth),
                contactEnterCount = contactProbe == null ? 0 : contactProbe.EnterCount,
                contactStayCount = contactProbe == null ? 0 : contactProbe.StayCount,
                contactExitCount = contactProbe == null ? 0 : contactProbe.ExitCount,
                contactEnterTime = contactProbe == null ? -1f : contactProbe.EnterTime,
                contactTime = Time.time
            });
        }

        [Serializable]
        private sealed class Position
        {
            public float x, y, z, time, speed;
            public int currentHealth, maxHealth;

            public Position(Transform child)
            {
                Vector3 value = child.position;
                x = value.x;
                y = value.y;
                z = value.z;
                time = Time.time;
                EnemyController controller = child.GetComponent<EnemyController>();
                EnemyHealth health = child.GetComponent<EnemyHealth>();
                speed = controller == null ? 0f : controller.MoveSpeed;
                currentHealth = health == null ? -1 : health.CurrentHealth;
                maxHealth = health == null ? -1 : health.MaxHealth;
            }
        }

        [Serializable]
        private sealed class State
        {
            public string scene, sessionNonce, sourceFingerprint, testBuildFingerprint;
            public int processId;
            public bool enemyExists, targetExists, sourceEnemyHasHealth;
            public float enemyX, enemyY, targetX, targetY, speed;
            public float cameraX, cameraY, halfHeight, halfWidth, minimumDistance, spawnMargin;
            public int currentHealth, maxHealth, aliveCount, spawnedCount, errorCount;
            public Position[] positions, spawnSamples;
            public string[] errors;
            public bool hasEnemyPrefab, hasPlayer, hasEnemiesParent, hasCamera, hasSettings, spawning;
            public bool contactExists, contactHasPlayerHealth, contactColliderIsChild;
            public int contactHealth, contactMaxHealth, contactOtherHealth, contactEnterCount, contactStayCount, contactExitCount;
            public float contactDamageCooldown, contactEnterTime, contactTime;
        }
    }

    // Observation only. This does not implement damage or call gameplay callbacks.
    [Preserve]
    public sealed class E2EContactProbe : MonoBehaviour
    {
        public Collider2D Other { get; set; }
        public int EnterCount { get; private set; }
        public int StayCount { get; private set; }
        public int ExitCount { get; private set; }
        public float EnterTime { get; private set; } = -1f;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other != Other) return;
            EnterCount++;
            EnterTime = Time.time;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other == Other) StayCount++;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other == Other) ExitCount++;
        }
    }
}
#endif
