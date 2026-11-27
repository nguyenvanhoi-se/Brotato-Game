using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Standalone prototype for GunSkillTest. Secondary attacks deal damage without generating skill hits.
internal sealed class GunLevel11 : MonoBehaviour
{
    internal sealed class Volley
    {
        public int Generation;
        public int Size;
        public bool Charged;
        public readonly Dictionary<EnemyHealth, int> Hits = new Dictionary<EnemyHealth, int>();
    }

    private sealed class TargetState
    {
        public int Charge;
        public float LastHit = float.NegativeInfinity;
        public float SunderUntil;
    }

    private enum Art { Sweep, Sunder, Charge, Fuse, Echo, Rift, Recall, Sky, Orbit, Satellite }
    private readonly Dictionary<EnemyHealth, TargetState> states = new Dictionary<EnemyHealth, TargetState>();
    private readonly Sprite[][] frames = new Sprite[10][];
    private readonly Sprite[] lance = new Sprite[2];
    private Texture2D[] sheets;
    private Transform visuals;
    private Transform player;
    private EnemyHealth[] targets;
    private GunSkillWeapon weapon;
    private SpriteRenderer orbit;
    private float orbitStarted;
    private GunBranch branch;
    private Vector3Int ranks;
    private int slot;
    private int shotNumber;
    private int generation;
    private bool inBossMode;

    internal void Setup(GunSkillWeapon owner, Transform playerRoot, Transform effects,
        EnemyHealth[] arenaTargets, Texture2D[] atlases)
    {
        weapon = owner;
        player = playerRoot;
        targets = arenaTargets;
        sheets = atlases;
        visuals = new GameObject("Level 11 effects").transform;
        visuals.SetParent(effects.parent, false);
    }

    internal void Select(GunBranch mode, int selection, Vector3Int upgradeRanks, bool bossMode)
    {
        if (branch != mode || slot != selection || ranks != upgradeRanks || inBossMode != bossMode) ResetEffects();
        branch = mode;
        slot = selection;
        ranks = upgradeRanks;
        inBossMode = bossMode;
    }

    internal void ResetEffects(bool restartCycle = true)
    {
        StopAllCoroutines();
        if (visuals != null)
        {
            foreach (Transform child in visuals)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }
        orbit = null;
        states.Clear();
        if (restartCycle) shotNumber = 0;
        generation++;
    }

    private void OnDestroy()
    {
        if (visuals != null) Destroy(visuals.gameObject);
        foreach (Sprite[] clip in frames)
        {
            if (clip == null) continue;
            foreach (Sprite frame in clip) Destroy(frame);
        }
        foreach (Sprite sprite in lance)
            if (sprite != null) Destroy(sprite);
    }

    internal Volley BeginVolley(Vector2 origin, Vector2 heading, int size)
    {
        if (slot == 0 || branch == GunBranch.Basic) return null;
        shotNumber++;

        if (branch == GunBranch.Spread && slot == 1 && shotNumber % 2 == 0)
            StartCoroutine(Sweep(origin, heading));
        if (branch == GunBranch.Piercing && shotNumber % 4 == 0)
        {
            if (slot == 1) StartCoroutine(Rift(origin, heading));
            if (slot == 2) StartCoroutine(Recall(origin, heading));
        }
        return new Volley
        {
            Generation = generation,
            Size = size,
            Charged = branch == GunBranch.Spread && slot == 3 && shotNumber % 2 == 0
        };
    }

    internal int DamageAgainst(EnemyHealth enemy, int damage)
    {
        return states.TryGetValue(enemy, out var state) && state.SunderUntil > Time.time
            ? Mathf.CeilToInt(damage * 1.25f) : damage;
    }

    internal void Hit(Volley volley, EnemyHealth enemy)
    {
        if (volley == null || volley.Generation != generation || !Alive(enemy)) return;
        volley.Hits.TryGetValue(enemy, out int hits);
        volley.Hits[enemy] = ++hits;

        switch (branch)
        {
            case GunBranch.Spread:
                if (slot == 2 && hits == 1) Sunder(enemy);
                if (slot == 3 && volley.Charged && hits == 1)
                    StartCoroutine(ChargedShot(enemy, volley));
                break;
            case GunBranch.Burst:
                if (slot == 1 && Charge(enemy, 5))
                    StartCoroutine(Detonate(enemy));
                if (slot == 2 && hits == Mathf.CeilToInt(volley.Size * 0.6f))
                    StartCoroutine(Echo(enemy));
                if (slot == 3)
                {
                    if (orbit == null)
                    {
                        orbitStarted = Time.time;
                        orbit = CreateVisual("Orbit charge", player.position, Vector2.right, 2.5f, 2.5f);
                        StartCoroutine(OrbitCharge(orbit));
                    }
                    if (Charge(enemy, 8))
                    {
                        float phase = (Time.time - orbitStarted) * Mathf.PI * 2f;
                        Destroy(orbit.gameObject);
                        orbit = null;
                        StartCoroutine(LaunchOrbit(enemy, phase));
                    }
                }
                break;
            case GunBranch.Piercing:
                if (slot == 3 && Charge(enemy, 3))
                    StartCoroutine(SkyLance(enemy.transform.position));
                break;
        }
    }

    private TargetState State(EnemyHealth enemy)
    {
        if (!states.TryGetValue(enemy, out var state))
        {
            state = new TargetState();
            states.Add(enemy, state);
        }
        return state;
    }

    private bool Charge(EnemyHealth enemy, int threshold)
    {
        var state = State(enemy);
        if (++state.Charge < threshold) return false;
        state.Charge = 0;
        return true;
    }

    private void Sunder(EnemyHealth enemy)
    {
        var state = State(enemy);
        if (state.SunderUntil > Time.time) return;
        if (Time.time - state.LastHit > 6f) state.Charge = 0;
        state.LastHit = Time.time;
        state.Charge += 2 + ranks.z;
        if (state.Charge < 6) return;
        state.Charge = 0;
        var fx = CreateVisual("Sunder armor", enemy.transform.position, Vector2.right, 2.5f, 2.5f);
        StartCoroutine(Follow(fx, enemy));
        StartCoroutine(Play(fx, Art.Sunder, 32));
        state.SunderUntil = Time.time + 5f;
    }

    private IEnumerator ChargedShot(EnemyHealth enemy, Volley volley)
    {
        var fx = CreateVisual("Charged shot marks", enemy.transform.position, Vector2.right, 3f, 3f);
        StartCoroutine(Follow(fx, enemy));
        StartCoroutine(Play(fx, Art.Charge, 24));
        yield return new WaitForSeconds(21f / 24f);
        if (Alive(enemy)) Area(enemy.transform.position, 0.7f, 10 + 8 * volley.Hits[enemy]);
    }

    private IEnumerator Detonate(EnemyHealth enemy)
    {
        var fx = CreateVisual("Chain detonator", enemy.transform.position, Vector2.right, 2.9f, 2.9f);
        StartCoroutine(Follow(fx, enemy));
        StartCoroutine(Play(fx, Art.Fuse, 32));
        yield return new WaitForSeconds(21f / 32f);
        if (Alive(enemy)) Area(enemy.transform.position, 0.8f, 25 + 5 * ranks.z);
    }

    private IEnumerator Echo(EnemyHealth enemy)
    {
        Vector2 heading = ((Vector2)enemy.transform.position - (Vector2)player.position).normalized;
        Vector2 offset = new Vector2(-heading.y, heading.x) * 0.62f;
        Vector2 position = (Vector2)player.position + offset;
        var fx = CreateVisual("Echo gun", position, heading, 2.1f, 2.1f);
        StartCoroutine(Play(fx, Art.Echo, 32));
        yield return new WaitForSeconds(0.30f);
        for (int i = 0; i < 3; i++)
        {
            if (!Alive(enemy)) yield break;
            heading = ((Vector2)enemy.transform.position - position).normalized;
            fx.transform.rotation = Rotation(heading);
            Vector2 firePoint = position + heading * 0.34f;
            weapon.FireEcho(firePoint, heading);
            if (i < ranks.x)
            {
                yield return new WaitForSeconds(0.07f);
                weapon.FireEcho(firePoint, heading);
                yield return new WaitForSeconds(0.10f);
            }
            else yield return new WaitForSeconds(0.17f);
        }
    }

    private IEnumerator OrbitCharge(SpriteRenderer fx)
    {
        Sprite[] clip = GetFrames(Art.Orbit);
        while (fx != null && orbit == fx)
        {
            fx.transform.position = player.position;
            fx.sprite = clip[Mathf.FloorToInt((Time.time - orbitStarted) * 32f) % 32];
            yield return null;
        }
    }

    private IEnumerator LaunchOrbit(EnemyHealth enemy, float phase)
    {
        int count = 6 + ranks.x;
        for (int i = 0; i < count && Alive(enemy); i++)
        {
            float angle = phase + (i * Mathf.PI * 2f / count) + i * 0.07f * Mathf.PI * 2f;
            Vector2 from = (Vector2)player.position + new Vector2(Mathf.Cos(angle), 0.82f * Mathf.Sin(angle)) * 0.48f;
            StartCoroutine(Satellite(enemy, from, i % 2 == 0 ? 1f : -1f));
            yield return new WaitForSeconds(0.07f);
        }
    }

    private IEnumerator Satellite(EnemyHealth enemy, Vector2 from, float side)
    {
        var fx = CreateVisual("Orbit satellite", from, Vector2.right, 1.35f, 1.35f);
        Sprite[] clip = GetFrames(Art.Satellite);
        Vector2 previous = from;
        for (float elapsed = 0f; elapsed < 0.32f && Alive(enemy); elapsed += Time.deltaTime)
        {
            float t = elapsed / 0.32f;
            Vector2 destination = enemy.transform.position;
            Vector2 axis = (destination - from).normalized;
            Vector2 position = Vector2.Lerp(from, destination, t)
                + new Vector2(-axis.y, axis.x) * (side * Mathf.Sin(t * Mathf.PI) * 0.65f);
            fx.transform.SetPositionAndRotation(position, Rotation(position - previous));
            fx.sprite = clip[Mathf.FloorToInt(elapsed * 32f) % 32];
            previous = position;
            yield return null;
        }
        if (Alive(enemy))
        {
            weapon.ShowImpact(enemy.transform.position, Color.cyan);
            enemy.TakeDamage(5 + ranks.z);
        }
        Destroy(fx.gameObject);
    }

    private IEnumerator Sweep(Vector2 origin, Vector2 heading)
    {
        const float reach = 3.9f;
        var fx = CreateVisual("Sweeping blade", origin, heading, 9.6f, 8.2f - 1.4f * ranks.y);
        StartCoroutine(Play(fx, Art.Sweep, 32));
        var hit = new HashSet<EnemyHealth>();
        yield return new WaitForSeconds(0.19f);
        for (float elapsed = 0.19f; elapsed < 0.65f; elapsed += Time.deltaTime)
        {
            float t = Mathf.InverseLerp(0.19f, 0.65f, elapsed);
            float front = Mathf.Lerp(0.7f, reach, 1f - Mathf.Pow(1f - t, 3f));
            foreach (EnemyHealth enemy in targets)
            {
                if (!Alive(enemy) || hit.Contains(enemy)) continue;
                Vector2 delta = (Vector2)enemy.transform.position - origin;
                float along = Vector2.Dot(delta, heading);
                float across = Mathf.Abs(Vector2.Dot(delta, new Vector2(-heading.y, heading.x)));
                if (along < -0.2f || along > front || across > (2.6f - ranks.y * 0.46f) * front / reach + 0.25f) continue;
                hit.Add(enemy);
                enemy.TakeDamage(22 + 5 * ranks.x);
                if (Alive(enemy) && ranks.z > 0 && enemy.TryGetComponent<GunSkillTargetEffects>(out var status))
                    status.Push(heading * (0.36f * ranks.z));
            }
            yield return null;
        }
    }

    private IEnumerator Rift(Vector2 origin, Vector2 heading)
    {
        const float length = 8f;
        Vector2 end = origin + heading * length;
        var fx = CreateVisual("Spatial rift", (origin + end) / 2f, heading, length * 256f / 210f, 2.8f);
        StartCoroutine(Play(fx, Art.Rift, 24));
        yield return new WaitForSeconds(0.28f);
        for (int i = 0; i < 4; i++)
        {
            foreach (EnemyHealth enemy in targets)
            {
                if (!Alive(enemy) || DistanceSquared(enemy.transform.position, origin, end) > 0.50f * 0.50f) continue;
                enemy.TakeDamage(5 + 2 * ranks.x);
            }
            yield return new WaitForSeconds(0.23f);
        }
    }

    private IEnumerator Recall(Vector2 origin, Vector2 heading)
    {
        const float length = 8f;
        Vector2 end = origin + heading * length;
        var fx = CreateVisual("Returning lance", origin, heading, 1.6f, 0.85f);
        var hit = new HashSet<EnemyHealth>();
        for (int leg = 0; leg < 2; leg++)
        {
            Vector2 from = leg == 0 ? origin : end;
            Vector2 to = leg == 0 ? end : origin;
            fx.sprite = GetLance(leg);
            fx.flipX = leg == 1;
            fx.transform.SetPositionAndRotation(from, Rotation(heading * (leg == 0 ? 1f : -1f)));
            hit.Clear();
            Vector2 previous = from;
            float elapsed = 0f;
            while (elapsed < 0.48f)
            {
                yield return null;
                elapsed += Time.deltaTime;
                Vector2 next = Vector2.Lerp(from, to, Mathf.Clamp01(elapsed / 0.48f));
                foreach (EnemyHealth enemy in targets)
                {
                    if (!Alive(enemy) || hit.Contains(enemy)
                        || DistanceSquared(enemy.transform.position, previous, next) > 0.42f * 0.42f) continue;
                    hit.Add(enemy);
                    enemy.TakeDamage(leg == 0 ? 15 : 19 + 4 * ranks.x);
                }
                fx.transform.position = previous = next;
            }
            weapon.ShowImpact(to, Color.cyan);
        }
        Destroy(fx.gameObject);
    }

    private IEnumerator SkyLance(Vector2 position)
    {
        float radius = 0.85f + 0.17f * ranks.z;
        var fx = CreateVisual("Sky lance", position, Vector2.right, radius * 2.8f, radius * 3.4f);
        StartCoroutine(Play(fx, Art.Sky, 30));
        yield return new WaitForSeconds(19f / 30f);
        Area(position, radius, 38 + 6 * ranks.x);
    }

    private void Area(Vector2 center, float radius, int damage)
    {
        foreach (EnemyHealth enemy in targets)
            if (Alive(enemy) && ((Vector2)enemy.transform.position - center).sqrMagnitude <= radius * radius)
                enemy.TakeDamage(damage);
    }

    private static bool Alive(EnemyHealth enemy) => enemy != null && enemy.CurrentHealth > 0;

    private static float DistanceSquared(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / Mathf.Max(segment.sqrMagnitude, 0.0001f));
        return (point - (start + segment * t)).sqrMagnitude;
    }

    private SpriteRenderer CreateVisual(string label, Vector2 position, Vector2 heading, float width, float height)
    {
        var obj = new GameObject(label);
        obj.transform.SetParent(visuals, false);
        obj.transform.SetPositionAndRotation(position, Rotation(heading));
        obj.transform.localScale = new Vector3(width, height, 1f);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 8;
        return renderer;
    }

    private static Quaternion Rotation(Vector2 heading) =>
        Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.right, heading));

    private IEnumerator Follow(SpriteRenderer fx, EnemyHealth target)
    {
        while (fx != null && Alive(target))
        {
            fx.transform.position = target.transform.position;
            yield return null;
        }
        if (fx != null) Destroy(fx.gameObject);
    }

    private IEnumerator Play(SpriteRenderer renderer, Art art, int fps)
    {
        Sprite[] clip = GetFrames(art);
        for (float elapsed = 0f; renderer != null && elapsed < clip.Length / (float)fps; elapsed += Time.deltaTime)
        {
            renderer.sprite = clip[Mathf.FloorToInt(elapsed * fps)];
            yield return null;
        }
        if (renderer != null) Destroy(renderer.gameObject);
    }

    private Sprite[] GetFrames(Art art)
    {
        int index = (int)art;
        if (frames[index] != null) return frames[index];
        var clip = new Sprite[32];
        for (int i = 0; i < clip.Length; i++)
            clip[i] = Sprite.Create(sheets[index], FrameRect(i), Vector2.one * 0.5f, 256f, 0, SpriteMeshType.FullRect);
        return frames[index] = clip;
    }

    private Sprite GetLance(int leg)
    {
        if (lance[leg] != null) return lance[leg];
        // Crop the spear from each direction of the approved animation so the flight can use world coordinates.
        Rect cell = FrameRect(leg == 0 ? 9 : 21);
        var crop = new Rect(cell.x + (leg == 0 ? 127 : 142), cell.y + 109f, 72f, 38f);
        lance[leg] = Sprite.Create(sheets[(int)Art.Recall], crop, Vector2.one * 0.5f, 72f, 0, SpriteMeshType.FullRect);
        return lance[leg];
    }

    private static Rect FrameRect(int index) =>
        new Rect(index % 8 * 256, (3 - index / 8) * 256, 256, 256);
}


internal enum HexcoreUltimate { None, Tornado, Meteors, Turret, Cage }

internal sealed class GunHexcoreUltimate : MonoBehaviour
{
    // Texture order matches GunSkillTest's serialized references.
    private enum Art { Grenade, Blast, Helicopter, AirstrikeMark, AirstrikeImpact,
        Charge, Beam, CageField, CageShot, CageCollapse }

    internal const int TurretLimit = 3;
    internal const int TornadoCapacity = 3;
    private const float TornadoRechargeSeconds = 2.4f;
    private int tornadoCharges = TornadoCapacity;
    private float nextTornadoCharge;
    private float nextTornadoCast;
    private Texture2D grenadeAtlas;
    private Texture2D meteorAtlas;
    private Sprite[] meteorFrames;
    private Sprite[] grenadeFrames;
    private static readonly Vector2[] GrenadeCenters =
    {
        new Vector2(19.0575f, 100.6092f),
        new Vector2(25.2333f, 95.9778f),
        new Vector2(31.0213f, 92.7128f),
        new Vector2(36.8710f, 90.3226f),
        new Vector2(42.5870f, 88.6630f),
        new Vector2(48.5889f, 87.9889f),
        new Vector2(54.1868f, 88.3187f),
        new Vector2(59.7912f, 89.6044f),
        new Vector2(65.1778f, 91.9889f),
        new Vector2(70.5957f, 95.3298f),
        new Vector2(75.8804f, 99.7717f),
        new Vector2(81.2283f, 105.3913f),
        new Vector2(86.6023f, 112.0341f),
        new Vector2(91.9880f, 119.7229f),
        new Vector2(97.2747f, 128.5165f),
        new Vector2(102.4886f, 138.2841f),
        new Vector2(108.2778f, 149.3000f),
        new Vector2(113.5955f, 161.2809f),
        new Vector2(118.5294f, 160.5412f),
        new Vector2(123.1494f, 158.0575f),
        new Vector2(128.0112f, 156.1798f),
        new Vector2(132.5824f, 155.2527f),
        new Vector2(137.3563f, 155.3563f),
        new Vector2(142.0899f, 156.1798f),
        new Vector2(146.9674f, 157.9674f),
        new Vector2(151.6923f, 160.8352f),
        new Vector2(156.3810f, 163.4881f),
        new Vector2(159.9091f, 162.5682f),
        new Vector2(163.3763f, 162.0860f),
        new Vector2(166.8913f, 162.7826f),
        new Vector2(169.9889f, 162.4778f),
        new Vector2(172.6111f, 162.2222f),
        new Vector2(174.8556f, 162.3889f),
        new Vector2(177.3933f, 162.6966f),
        new Vector2(179.6559f, 163.1398f),
        new Vector2(181.9655f, 163.3793f),
        new Vector2(184.0562f, 163.8652f),
        new Vector2(186.2921f, 163.8539f),
        new Vector2(188.1978f, 164.2857f),
        new Vector2(190.1613f, 164.2903f),
        new Vector2(192.1222f, 164.3889f),
        new Vector2(193.3793f, 164.3103f),
        new Vector2(195.0667f, 164.1556f),
        new Vector2(196.2889f, 164.2000f),
        new Vector2(197.5730f, 163.9888f),
        new Vector2(198.7111f, 163.8556f),
        new Vector2(199.6374f, 163.7912f),
        new Vector2(200.8864f, 163.8182f),
        new Vector2(201.1667f, 163.6310f),
    };
    internal int TornadoCharges { get { RefreshTornado(); return tornadoCharges; } }
    internal float TornadoRechargeLeft
    {
        get
        {
            RefreshTornado();
            return tornadoCharges < TornadoCapacity ? Mathf.Max(0f, nextTornadoCharge - Time.time) : 0f;
        }
    }

    private void RefreshTornado()
    {
        while (tornadoCharges < TornadoCapacity && Time.time >= nextTornadoCharge)
        {
            tornadoCharges++;
            nextTornadoCharge += TornadoRechargeSeconds;
        }
    }

    private const int FramesPerClip = 32;
    private readonly Sprite[][] frames = new Sprite[10][];
    private readonly List<RaycastHit2D> lineHits = new List<RaycastHit2D>();
    private readonly HashSet<EnemyHealth> damaged = new HashSet<EnemyHealth>();
    private Texture2D[] sheets;
    private GunNewSkillArt newArt;
    private EnemyHealth[] targets;
    private Transform muzzle;
    private Transform visuals;
    private Material material;
    private float readyAt;
    private int turretCount;

    internal float CooldownLeft => Mathf.Max(0f, readyAt - Time.time);
    internal int TurretCount => turretCount;

    internal void Setup(Transform firePoint, Material spriteMaterial, EnemyHealth[] practiceTargets,
        Texture2D[] textures, Texture2D grenadeTexture, Texture2D meteorTexture, GunNewSkillArt importedArt)
    {
        muzzle = firePoint;
        material = spriteMaterial;
        targets = practiceTargets;
        sheets = textures;
        newArt = importedArt;
        grenadeAtlas = grenadeTexture;
        meteorAtlas = meteorTexture;
        visuals = new GameObject("Hexcore ultimate effects").transform;
        visuals.SetParent(transform, false);
    }

    internal bool CanCast(HexcoreUltimate selected)
    {
        RefreshTornado();
        if (selected == HexcoreUltimate.Tornado)
            return tornadoCharges > 0 && Time.time >= nextTornadoCast && CooldownLeft <= 0f;
        return selected != HexcoreUltimate.None && CooldownLeft <= 0f &&
            (selected != HexcoreUltimate.Turret || turretCount < TurretLimit);
    }

    internal bool Cast(HexcoreUltimate selected, Vector2 aimPoint)
    {
        if (!CanCast(selected)) return false;

        Vector2 origin = muzzle.position;
        Vector2 center = GroundAim(aimPoint);
        Vector2 line = center - origin;
        Vector2 heading = line.normalized;
        switch (selected)
        {
            case HexcoreUltimate.Tornado:
                if (tornadoCharges == TornadoCapacity) nextTornadoCharge = Time.time + TornadoRechargeSeconds;
                tornadoCharges--;
                nextTornadoCast = Time.time + 0.18f;
                StartCoroutine(Tornado(origin, heading));
                break;
            case HexcoreUltimate.Meteors:
                readyAt = Time.time + 5f;
                StartCoroutine(MeteorShower(center));
                break;
            case HexcoreUltimate.Turret:
                readyAt = Time.time + 2.4f;
                turretCount++;
                StartCoroutine(Turret(center));
                break;
            case HexcoreUltimate.Cage:
                readyAt = Time.time + 5f;
                StartCoroutine(Cage(center, heading));
                break;
            default:
                return false;
        }
        return true;
    }

    internal Vector2 GroundAim(Vector2 point)
    {
        Vector2 origin = muzzle.position;
        Vector2 delta = point - origin;
        Vector2 heading = delta.sqrMagnitude > 0.01f ? delta.normalized : (Vector2)muzzle.right;
        return origin + heading * Mathf.Clamp(delta.magnitude, 2f, 8f);
    }

    internal void ResetEffects()
    {
        StopAllCoroutines();
        foreach (Transform child in visuals)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        readyAt = 0f;
        tornadoCharges = TornadoCapacity;
        nextTornadoCharge = nextTornadoCast = 0f;
        turretCount = 0;
        damaged.Clear();
        lineHits.Clear();
    }

    private IEnumerator Tornado(Vector2 origin, Vector2 heading)
    {
        const float lifetime = 7f, travelSpeed = 1.1f, radius = 1.8f, pullSpeed = 2.8f, tickInterval = 0.5f;
        const int damage = 6;
        var column = Visual("Hexcore blizzard vortex", origin, Vector2.right, 2f, 2f, 9);
        column.sharedMaterial = newArt.NoGlowMaterial;
        float start = Time.time;
        float nextTick = 0f;
        for (float elapsed = 0f; elapsed < lifetime; elapsed = Time.time - start)
        {
            Vector2 center = origin + heading * (travelSpeed * elapsed);
            column.transform.position = center;
            column.sprite = newArt.Loop(GunNewSkillArt.Clip.BlizzardVortex, elapsed, 30f, 0, 60);
            column.color = new Color(1f, 1f, 1f, Mathf.Min(1f, (lifetime - elapsed) / 0.25f));

            foreach (EnemyHealth enemy in targets)
            {
                if (Alive(enemy) && enemy.TryGetComponent<GunSkillTargetEffects>(out var effects))
                    effects.PullToward(center, radius, pullSpeed * Time.deltaTime);
            }
            while (nextTick <= elapsed)
            {
                DamageArea(center, radius, damage);
                nextTick += tickInterval;
            }
            yield return null;
        }
        Destroy(column.gameObject);
    }

    internal Sprite[] GetGrenadeFrames()
    {
        if (grenadeFrames != null) return grenadeFrames;
        grenadeFrames = new Sprite[118];
        for (int i = 0; i < grenadeFrames.Length; i++)
        {
            var rect = new Rect(i % 10 * 300, grenadeAtlas.height - (i / 10 + 1) * 200, 300, 200);
            Vector2 center = i < GrenadeCenters.Length ? GrenadeCenters[i] : new Vector2(200f, 162f);
            var pivot = new Vector2(center.x / 300f, 1f - center.y / 200f);
            grenadeFrames[i] = Sprite.Create(grenadeAtlas, rect, pivot, 40f, 0, SpriteMeshType.FullRect);
        }
        return grenadeFrames;
    }

    private IEnumerator MeteorShower(Vector2 center)
    {
        // Keep every strike within one region around the point selected when R was pressed.
        Vector2[] offsets =
        {
            Vector2.zero, new Vector2(-0.72f, 0.48f), new Vector2(0.68f, -0.44f),
            new Vector2(-0.54f, -0.66f), new Vector2(0.62f, 0.70f)
        };
        for (int i = 0; i < offsets.Length; i++)
        {
            StartCoroutine(MeteorStrike(center + offsets[i]));
            yield return new WaitForSeconds(0.22f);
        }
    }

    private IEnumerator MeteorStrike(Vector2 impact)
    {
        const float fps = 40f;
        const int impactFrame = 19;
        const int count = 144;
        Sprite[] clip = GetMeteorFrames();
        var sprite = Visual("Hexcore meteor", impact, Vector2.right, 1f, 1f, 10);
        bool hit = false;
        for (float elapsed = 0f; elapsed < count / fps; elapsed += Time.deltaTime)
        {
            int frame = Mathf.Min(count - 1, Mathf.FloorToInt(elapsed * fps));
            sprite.sprite = clip[frame];
            if (!hit && frame >= impactFrame)
            {
                hit = true;
                sprite.name = "Hexcore meteor impact";
                DamageArea(impact, 1.36f, 47);
            }
            sprite.color = new Color(1f, 1f, 1f, Mathf.Clamp01((count - frame) / 12f));
            yield return null;
        }
        // Preserve damage even if a long frame skips the contact frame.
        if (!hit) DamageArea(impact, 1.36f, 47);
        Destroy(sprite.gameObject);
    }

    private Sprite[] GetMeteorFrames()
    {
        if (meteorFrames != null) return meteorFrames;
        meteorFrames = new Sprite[144];
        for (int i = 0; i < meteorFrames.Length; i++)
        {
            var rect = new Rect(i % 12 * 272, meteorAtlas.height - (i / 12 + 1) * 232, 272, 232);
            // Ground contact stays fixed while the artwork moves from the upper left.
            var pivot = new Vector2(150f / 272f, 1f - 208f / 232f);
            meteorFrames[i] = Sprite.Create(meteorAtlas, rect, pivot, 60f, 0, SpriteMeshType.FullRect);
        }
        return meteorFrames;
    }

    private IEnumerator Turret(Vector2 position)
    {
        const float lifetime = 15f, interval = 1f, waveDuration = 0.46f, radius = 4f;
        const int damage = 12;
        var core = Visual("Hexcore energy turret", position, Vector2.right, 1f, 1f, 7);
        var wave = Visual("Hexcore plasma wave", position, Vector2.right, 1f, 1f, 6);
        float start = Time.time;
        float nextPulse = 0f;
        while (true)
        {
            float elapsed = Time.time - start;
            while (nextPulse < lifetime && elapsed >= nextPulse)
            {
                DamageArea(position, radius, damage);
                nextPulse += interval;
            }
            if (elapsed >= lifetime) break;

            core.sprite = newArt.Loop(GunNewSkillArt.Clip.EnergyCore, elapsed, 30f, 0, 72);
            float fade = Mathf.Clamp01((lifetime - elapsed) / 0.3f);
            core.color = new Color(1f, 1f, 1f, fade);
            float age = elapsed - (nextPulse - interval);
            wave.enabled = age < waveDuration;
            if (wave.enabled)
            {
                float progress = age / waveDuration;
                wave.sprite = newArt.Progress(GunNewSkillArt.Clip.PlasmaExplosion, progress, 0, 23);
                wave.color = new Color(1f, 1f, 1f, Mathf.Min(1f, (1f - progress) * 4f) * fade);
            }
            yield return null;
        }
        Destroy(wave.gameObject);
        Destroy(core.gameObject);
        turretCount--;
    }

    private IEnumerator Cage(Vector2 center, Vector2 heading)
    {
        const float radius = 2.6f;
        const float radiusToSpriteSize = 1f;
        var field = Visual("Hexcore cage", center, heading, radiusToSpriteSize, radiusToSpriteSize, 3);
        float start = Time.time;
        float nextPulse = start + 0.44f;
        int pulses = 0;
        while (pulses < 6)
        {
            field.sprite = newArt.Loop(GunNewSkillArt.Clip.CageField, Time.time - start, 30f, 0, 48);
            if (Time.time >= nextPulse)
            {
                damaged.Clear();
                float angle = (pulses % 3) * 60f;
                Vector2 edge = Quaternion.Euler(0f, 0f, angle) * heading * radius;
                CagePulse(center + edge, center);
                CagePulse(center - edge, center);
                pulses++;
                nextPulse = Time.time + 0.28f;
            }
            yield return null;
        }

        yield return new WaitForSeconds(0.30f);
        Destroy(field.gameObject);
        StartCoroutine(PixelAt("Hexcore cage collapse", GunNewSkillArt.Clip.CageCollapse,
            center, Vector2.right, 48, 30f));
    }

    private void CagePulse(Vector2 node, Vector2 center)
    {
        Vector2 heading = (center - node).normalized;
        StartCoroutine(PixelAt("Hexcore cage node", GunNewSkillArt.Clip.CagePulse, node, heading, 14, 48f));
        StartCoroutine(PixelAt("Hexcore cage impact", GunNewSkillArt.Clip.Impact, center, Vector2.right, 36, 48f));
        DamageLine(node, center, 0.33f, 27);
    }

    private IEnumerator PixelAt(string label, GunNewSkillArt.Clip clip, Vector2 position,
        Vector2 heading, int count, float fps)
    {
        var sprite = Visual(label, position, heading, 1f, 1f, 9);
        for (float elapsed = 0f; elapsed < count / fps; elapsed += Time.deltaTime)
        {
            sprite.sprite = newArt.Frame(clip, Mathf.Min(count - 1, Mathf.FloorToInt(elapsed * fps)));
            yield return null;
        }
        Destroy(sprite.gameObject);
    }

    private void DamageArea(Vector2 center, float radius, int damage)
    {
        foreach (EnemyHealth enemy in targets)
        {
            if (!Alive(enemy)) continue;
            Vector2 closest = enemy.TryGetComponent<Collider2D>(out var hitbox)
                ? hitbox.ClosestPoint(center) : (Vector2)enemy.transform.position;
            if ((closest - center).sqrMagnitude <= radius * radius)
                enemy.TakeDamage(damage);
        }
    }

    private void DamageLine(Vector2 start, Vector2 end, float radius, int damage)
    {
        Vector2 segment = end - start;
        var filter = new ContactFilter2D { useTriggers = true };
        lineHits.Clear();
        Physics2D.CircleCast(start, radius, segment.normalized, filter, lineHits, segment.magnitude);
        foreach (var hit in lineHits)
        {
            EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (Alive(enemy) && damaged.Add(enemy)) enemy.TakeDamage(damage);
        }
    }

    private SpriteRenderer Visual(string label, Vector2 position, Vector2 heading, float width, float height, int layer)
    {
        var obj = new GameObject(label);
        obj.transform.SetParent(visuals, false);
        obj.transform.SetPositionAndRotation(position, Rotation(heading));
        obj.transform.localScale = new Vector3(width, height, 1f);
        var sprite = obj.AddComponent<SpriteRenderer>();
        sprite.sharedMaterial = material;
        sprite.sortingOrder = layer;
        return sprite;
    }

    private IEnumerator PlayAt(string label, Art art, Vector2 position, Vector2 heading, float width, float height, int fps)
    {
        var sprite = Visual(label, position, heading, width, height, 9);
        yield return Animate(sprite, art, FramesPerClip / (float)fps);
        Destroy(sprite.gameObject);
    }

    private IEnumerator Animate(SpriteRenderer sprite, Art art, float duration)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            sprite.sprite = ClipProgress(art, elapsed / duration);
            yield return null;
        }
        sprite.sprite = ClipProgress(art, 1f);
    }

    private Sprite Frame(Art art, float elapsed, float fps) =>
        GetFrame(art, Mathf.FloorToInt(elapsed * fps) % FramesPerClip);

    private Sprite ClipProgress(Art art, float progress) =>
        GetFrame(art, Mathf.Min(FramesPerClip - 1, Mathf.FloorToInt(Mathf.Clamp01(progress) * FramesPerClip)));

    private Sprite GetFrame(Art art, int frame)
    {
        int index = (int)art;
        if (frames[index] == null)
        {
            var clip = new Sprite[FramesPerClip];
            for (int i = 0; i < clip.Length; i++)
            {
                var rect = new Rect(i % 8 * 256, (3 - i / 8) * 256, 256, 256);
                clip[i] = Sprite.Create(sheets[index], rect, Vector2.one * 0.5f, 256f, 0, SpriteMeshType.FullRect);
            }
            frames[index] = clip;
        }
        return frames[index][frame];
    }

    private static bool Alive(EnemyHealth enemy) => enemy != null && enemy.CurrentHealth > 0;
    private static Quaternion Rotation(Vector2 heading) =>
        Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.right, heading));

    private void OnDestroy()
    {
        if (meteorFrames != null)
            foreach (Sprite sprite in meteorFrames) Destroy(sprite);
        if (grenadeFrames != null)
            foreach (Sprite sprite in grenadeFrames) Destroy(sprite);
        foreach (Sprite[] clip in frames)
            if (clip != null)
                foreach (Sprite sprite in clip) Destroy(sprite);
    }
}
