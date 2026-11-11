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
