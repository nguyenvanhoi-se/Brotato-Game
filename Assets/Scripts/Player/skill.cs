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
        public bool FireBreathReady;
        public bool BallLightningReady;
        public Vector2 Heading;
        public readonly Dictionary<EnemyHealth, int> Hits = new Dictionary<EnemyHealth, int>();
    }

    private sealed class TargetState
    {
        public int Charge;
        public float LastHit = float.NegativeInfinity;
        public float SunderUntil;
        public bool SunderPending;
    }

    private readonly Dictionary<EnemyHealth, TargetState> states = new Dictionary<EnemyHealth, TargetState>();
    private Sprite echoGun;
    private bool echoActive;
    private GunNewSkillArt newArt;
    private Transform visuals;
    private Transform player;
    private EnemyHealth[] targets;
    private GunSkillWeapon weapon;
    private GunSkillTestArena arena;
    private SpriteRenderer playerView;
    private bool healActive;
    private GunBranch branch;
    private Vector3Int ranks;
    private int slot;
    private int shotNumber;
    private int generation;
    private bool inBossMode;

    internal void Setup(GunSkillWeapon owner, Transform playerRoot, Transform effects,
        EnemyHealth[] arenaTargets, GunNewSkillArt importedArt, Sprite echoGunSprite)
    {
        weapon = owner;
        player = playerRoot;
        arena = GetComponent<GunSkillTestArena>();
        playerView = player.GetComponentInChildren<SpriteRenderer>();
        targets = arenaTargets;
        newArt = importedArt;
        echoGun = echoGunSprite;
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
        healActive = false;
        echoActive = false;
        states.Clear();
        if (restartCycle) shotNumber = 0;
        generation++;
    }

    private void OnDestroy()
    {
        if (visuals != null) Destroy(visuals.gameObject);
    }

    internal Volley BeginVolley(Vector2 origin, Vector2 heading, int size)
    {
        if (slot == 0 || branch == GunBranch.Basic) return null;
        shotNumber++;

        if (branch == GunBranch.Spread && slot == 1 && shotNumber % 2 == 0)
            StartCoroutine(Sweep(origin, heading));
        if (branch == GunBranch.Piercing && slot == 1 && shotNumber % 4 == 0)
            StartCoroutine(Rift(origin, heading));
        return new Volley
        {
            Generation = generation,
            Size = size,
            FireBreathReady = branch == GunBranch.Spread && slot == 3 && shotNumber % 2 == 0,
            BallLightningReady = branch == GunBranch.Piercing && slot == 2 && shotNumber % 4 == 0,
            Heading = heading
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
                if (slot == 3 && volley.FireBreathReady)
                {
                    volley.FireBreathReady = false;
                    StartCoroutine(FireBreath(enemy, volley));
                }
                break;
            case GunBranch.Burst:
                if (slot == 1 && Charge(enemy, 5))
                    StartCoroutine(Detonate(enemy));
                if (slot == 2 && !echoActive && hits == Mathf.CeilToInt(volley.Size * 0.6f))
                    StartCoroutine(Echo(enemy));
                if (slot == 3 && !healActive && Charge(enemy, 8))
                    StartCoroutine(HealingCompanion());
                break;
            case GunBranch.Piercing:
                if (slot == 2 && volley.BallLightningReady)
                {
                    volley.BallLightningReady = false;
                    StartCoroutine(BallLightning(enemy, volley.Heading));
                }
                if (slot == 3 && Charge(enemy, 3))
                    StartCoroutine(SkyLance(EnemyFootPosition(enemy)));
                break;
        }
    }

    internal bool TryIntegratedCast(GunBranch mode, int choice, EnemyHealth enemy, Vector2 origin, Vector2 heading)
    {
        if (mode == GunBranch.Spread && choice == 0) StartCoroutine(Sweep(origin, heading));
        else if (mode == GunBranch.Piercing && choice == 0) StartCoroutine(Rift(origin, heading));
        else if (mode == GunBranch.Burst && choice == 2)
        {
            if (healActive) return false;
            StartCoroutine(HealingCompanion());
        }
        else
        {
            if (!Alive(enemy)) return false;
            switch (mode)
            {
                case GunBranch.Spread:
                    if (choice == 1)
                    {
                        var state = State(enemy);
                        if (state.SunderPending) return false;
                        StartCoroutine(Fireball(enemy, state));
                    }
                    else
                    {
                        var volley = new Volley { Heading = heading, Size = 1 };
                        volley.Hits[enemy] = 1;
                        StartCoroutine(FireBreath(enemy, volley));
                    }
                    break;
                case GunBranch.Burst:
                    if (choice == 0) StartCoroutine(Detonate(enemy));
                    else
                    {
                        if (echoActive) return false;
                        StartCoroutine(Echo(enemy));
                    }
                    break;
                case GunBranch.Piercing:
                    if (choice == 1) StartCoroutine(BallLightning(enemy, heading));
                    else StartCoroutine(SkyLance(EnemyFootPosition(enemy)));
                    break;
                default: return false;
            }
        }
        return true;
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
        if (state.SunderPending || state.SunderUntil > Time.time) return;
        if (Time.time - state.LastHit > 6f) state.Charge = 0;
        state.LastHit = Time.time;
        state.Charge += 2 + ranks.z;
        if (state.Charge < 6) return;
        state.Charge = 0;
        StartCoroutine(Fireball(enemy, state));
    }

    private const float FireballChargeDuration = 0.30f;

    private IEnumerator Fireball(EnemyHealth enemy, TargetState state)
    {
        state.SunderPending = true;
        const float spacing = 0.16f;
        StartCoroutine(LaunchFireball(enemy, state));
        yield return new WaitForSeconds(FireballChargeDuration + spacing);
        StartCoroutine(LaunchFireball(enemy, state, charge: false));
        yield return new WaitForSeconds(spacing);
        yield return LaunchFireball(enemy, state, charge: false);
        state.SunderPending = false;
    }

    private IEnumerator FireBreath(EnemyHealth firstTarget, Volley volley)
    {
        // Let the remaining pellets contribute before the flame starts.
        yield return new WaitForSeconds(0.12f);
        const float reach = 5.4f;
        const float halfAngle = 30f;
        float sideGrowth = Mathf.Tan(halfAngle * Mathf.Deg2Rad);
        const float tickInterval = 0.20f;
        const int ticks = 8;
        const float duration = tickInterval * ticks;
        int damage = 10 + 8 * volley.Hits[firstTarget];
        Vector2 heading = volley.Heading;
        Vector2 side = new Vector2(-heading.y, heading.x);
        const float wingAngle = 21f;
        var centerFlame = CreateFlameStream("Shotgun fire breath", heading, 0f, 1.65f, 1.65f);
        var leftFlame = CreateFlameStream("Shotgun flame left", heading, wingAngle, 1.72f, 1.35f);
        var rightFlame = CreateFlameStream("Shotgun flame right", heading, -wingAngle, 1.72f, 1.35f);
        centerFlame.sortingOrder = leftFlame.sortingOrder + 1;
        int nextTick = 0;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            Vector2 origin = weapon.MuzzlePosition;
            float fade = Mathf.Clamp01((duration - elapsed) / 0.16f);
            ShowFlameStream(centerFlame, origin, elapsed, fade);
            ShowFlameStream(leftFlame, origin, elapsed + 0.20f, fade * 0.88f);
            ShowFlameStream(rightFlame, origin, elapsed + 0.43f, fade * 0.88f);
            while (nextTick < ticks && elapsed >= nextTick * tickInterval)
            {
                int tickDamage = damage / ticks + (nextTick < damage % ticks ? 1 : 0);
                nextTick++;
                foreach (EnemyHealth enemy in targets)
                {
                    if (!Alive(enemy)) continue;
                    Vector2 delta = (Vector2)enemy.transform.position - origin;
                    float along = Vector2.Dot(delta, heading);
                    if (along >= 0f && along <= reach && Mathf.Abs(Vector2.Dot(delta, side)) <= along * sideGrowth)
                        enemy.TakeDamage(tickDamage);
                }
            }
            yield return null;
        }
        Destroy(centerFlame.gameObject);
        Destroy(leftFlame.gameObject);
        Destroy(rightFlame.gameObject);
    }

    private SpriteRenderer CreateFlameStream(string label, Vector2 heading, float angle, float length, float width)
    {
        // The flame in the atlas rises about 0.15 units for every unit forward.
        float correction = Mathf.Atan2(0.15f * width, length) * Mathf.Rad2Deg;
        Vector2 spriteHeading = Quaternion.Euler(0f, 0f, angle - correction) * heading;
        var renderer = CreateVisual(label, weapon.MuzzlePosition, spriteHeading, length, width);
        renderer.sharedMaterial = newArt.NoGlowMaterial;
        return renderer;
    }

    private void ShowFlameStream(SpriteRenderer renderer, Vector2 origin, float elapsed, float opacity)
    {
        renderer.transform.position = origin;
        renderer.sprite = newArt.Loop(GunNewSkillArt.Clip.Breath, elapsed, 30f, 0, 45);
        renderer.color = new Color(1f, 1f, 1f, opacity);
    }

    private IEnumerator LaunchFireball(EnemyHealth target, TargetState sunder, bool charge = true)
    {
        if (!Alive(target)) yield break;
        const GunNewSkillArt.Clip clip = GunNewSkillArt.Clip.Fireball;
        Vector2 origin = player.position;
        Vector2 destination = target.transform.position;
        Vector2 heading = (destination - origin).normalized;
        var fx = CreateVisual("Công phá fireball", origin, heading, 1f, 1f);
        if (charge)
        {
            for (float elapsed = 0f; elapsed < FireballChargeDuration; elapsed += Time.deltaTime)
            {
                fx.transform.position = player.position;
                fx.sprite = newArt.Progress(clip, elapsed / FireballChargeDuration, 7, 28);
                yield return null;
            }
            origin = fx.transform.position;
        }

        const float duration = 0.55f;
        Vector2 previous = origin;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            if (Alive(target)) destination = target.transform.position;
            Vector2 next = Vector2.Lerp(origin, destination, Mathf.Clamp01(elapsed / duration));
            Vector2 delta = next - previous;
            if (delta.sqrMagnitude > 0.0001f) heading = delta.normalized;
            fx.transform.SetPositionAndRotation(next, Rotation(heading));
            fx.sprite = newArt.Progress(clip, elapsed / duration, 29, 42);
            previous = next;
            yield return null;
        }
        fx.transform.SetPositionAndRotation(destination, Quaternion.identity);
        Area(destination, 1.0f, 18 + ranks.z * 3);
        if (Alive(target)) sunder.SunderUntil = Time.time + 5f;
        yield return NewAnimation(fx, clip, 43, 175, 70f);
        Destroy(fx.gameObject);
    }

    private IEnumerator NewAnimation(SpriteRenderer renderer, GunNewSkillArt.Clip clip, int first, int last, float fps)
    {
        float duration = (last - first + 1) / fps;
        for (float elapsed = 0f; renderer != null && elapsed < duration; elapsed += Time.deltaTime)
        {
            renderer.sprite = newArt.Frame(clip, Mathf.Min(last, first + Mathf.FloorToInt(elapsed * fps)));
            float fade = Mathf.Clamp01((duration - elapsed) / 0.18f);
            renderer.color = new Color(1f, 1f, 1f, fade);
            yield return null;
        }
    }

    private IEnumerator Detonate(EnemyHealth enemy)
    {
        const GunNewSkillArt.Clip clip = GunNewSkillArt.Clip.Fuse;
        const float fuseDelay = 21f / 32f;
        const float visualScale = 2f;
        const int impactFrame = 24;
        Vector2 launch = player.position;
        Vector2 destination = enemy.transform.position;
        Vector2 heading = (destination - launch).normalized;
        var fx = CreateVisual("Chain detonator", launch, heading, visualScale, visualScale);
        Sprite launchFrame = newArt.Frame(clip, 0);
        // Frame zero's bolt starts at x=42; the atlas pivot is at the impact point.
        float launchOffset = (launchFrame.pivot.x - 42f) / launchFrame.pixelsPerUnit * visualScale;
        for (float elapsed = 0f; elapsed < fuseDelay && Alive(enemy); elapsed += Time.deltaTime)
        {
            destination = enemy.transform.position;
            heading = (destination - launch).normalized;
            float progress = elapsed / fuseDelay;
            Vector2 pivot = Vector2.Lerp(launch + heading * launchOffset, destination, progress);
            fx.transform.SetPositionAndRotation(pivot, Rotation(heading));
            fx.sprite = newArt.Progress(clip, progress, 0, impactFrame - 1);
            yield return null;
        }

        if (Alive(enemy))
        {
            destination = enemy.transform.position;
            fx.transform.position = destination;
            Area(destination, 0.8f, 25 + 5 * ranks.y);
            yield return NewAnimation(fx, clip, impactFrame, 53, impactFrame / fuseDelay);
        }
        Destroy(fx.gameObject);
    }

    private IEnumerator Echo(EnemyHealth enemy)
    {
        if (echoGun == null) yield break;
        echoActive = true;
        const float duration = 3f, pairSpacing = 0.88f, wingOffset = 0.62f;
        const int pairCount = 3;
        const float spriteWidth = 0.72f, muzzlePixel = 62f;
        float gunScale = spriteWidth * echoGun.pixelsPerUnit / echoGun.rect.width;
        float muzzleOffset = (muzzlePixel - echoGun.pivot.x) / echoGun.pixelsPerUnit * gunScale;
        Vector2 axis = ((Vector2)enemy.transform.position - (Vector2)player.position).normalized;
        if (axis == Vector2.zero) axis = Vector2.right;
        var left = CreateVisual("Echo gun left", player.position, axis, gunScale, gunScale);
        var right = CreateVisual("Echo gun right", player.position, axis, gunScale, gunScale);
        left.sprite = right.sprite = echoGun;
        Color idle = Color.white;
        Color flash = new Color(1f, 0.82f, 0.84f);
        float nextPair = 0.30f;
        float bonusAt = float.PositiveInfinity;
        float flashUntil = 0f;
        int pairs = 0;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            Vector2 origin = player.position;
            bool targetAlive = Alive(enemy);
            Vector2 target = targetAlive ? (Vector2)enemy.transform.position : origin + axis;
            if ((target - origin).sqrMagnitude > 0.0001f) axis = (target - origin).normalized;
            Vector2 offset = new Vector2(-axis.y, axis.x) * wingOffset;
            Vector2 leftPosition = origin + offset;
            Vector2 rightPosition = origin - offset;
            Vector2 leftAim = (target - leftPosition).normalized;
            Vector2 rightAim = (target - rightPosition).normalized;
            left.transform.SetPositionAndRotation(leftPosition, Rotation(leftAim));
            right.transform.SetPositionAndRotation(rightPosition, Rotation(rightAim));
            left.flipY = leftAim.x < 0f;
            right.flipY = rightAim.x < 0f;

            bool fire = targetAlive && elapsed >= bonusAt;
            if (fire) bonusAt = float.PositiveInfinity;
            if (targetAlive && pairs < pairCount && elapsed >= nextPair)
            {
                fire = true;
                if (pairs < ranks.z) bonusAt = elapsed + 0.07f;
                pairs++;
                nextPair += pairSpacing;
            }
            if (fire)
            {
                weapon.FireEcho(leftPosition + leftAim * muzzleOffset, leftAim);
                weapon.FireEcho(rightPosition + rightAim * muzzleOffset, rightAim);
                flashUntil = elapsed + 0.09f;
            }

            Color color = elapsed < flashUntil ? flash : idle;
            color.a = Mathf.Clamp01((duration - elapsed) / 0.24f);
            left.color = right.color = color;
            yield return null;
        }
        Destroy(left.gameObject);
        Destroy(right.gameObject);
        echoActive = false;
    }

    private IEnumerator HealingCompanion()
    {
        healActive = true;
        const float duration = 5f;
        const int healingPerSecond = 4;
        var ring = CreateVisual("Healing ground ring", player.position, Vector2.right, 1f, 1f);
        var slime = CreateVisual("Healing slime", player.position, Vector2.right, 1f, 1f);
        var hearts = CreateVisual("Healing hearts", player.position, Vector2.right, 1f, 1f);
        int playerOrder = playerView != null ? playerView.sortingOrder : 0;
        ring.sortingOrder = playerOrder - 1;
        slime.sortingOrder = playerOrder + 1;
        hearts.sortingOrder = playerOrder + 8;
        SetHealingPalette(ring, 0.035f, 0.65f);
        SetHealingPalette(slime, 0.11f, 0.90f);
        SetHealingPalette(hearts, 0.065f, 0.85f);
        Vector2 lastPosition = player.position;
        Vector2 moveDirection = Vector2.right;
        Vector2 slimePosition = lastPosition + new Vector2(-0.70f, -0.30f);
        int nextHeal = 1;
        for (float elapsed = 0f; arena != null && arena.PlayerStanding; elapsed += Time.deltaTime)
        {
            while (nextHeal <= duration && elapsed >= nextHeal)
            {
                arena.RestorePracticeHealth(healingPerSecond);
                nextHeal++;
            }
            if (elapsed >= duration) break;

            Vector2 position = player.position;
            Vector2 movement = position - lastPosition;
            if (movement.sqrMagnitude > 0.00001f) moveDirection = movement.normalized;
            lastPosition = position;
            Bounds body = playerView != null ? playerView.bounds : new Bounds(player.position, Vector3.one * 0.7f);
            var foot = new Vector2(position.x, body.min.y + 0.04f);
            Vector2 desired = foot - moveDirection * 0.72f + Vector2.down * 0.12f;
            Vector2 follow = desired - slimePosition;
            bool moving = follow.sqrMagnitude > 0.015f;
            slimePosition = Vector2.MoveTowards(slimePosition, desired,
                (3f + follow.magnitude * 3f) * Time.deltaTime);
            slime.transform.position = slimePosition;
            if (Mathf.Abs(follow.x) > 0.035f) slime.flipX = follow.x < 0f;

            float appear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.28f));
            float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((duration - elapsed) / 0.38f));
            float pulse = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(elapsed * 2f * Mathf.PI)), 5f);
            float opacity = appear * fade;
            ring.transform.position = foot;
            ring.transform.localScale = new Vector3(1.50f, 1.24f, 1f) * (1f + 0.045f * pulse);
            ring.sprite = elapsed < 0.56f
                ? newArt.Progress(GunNewSkillArt.Clip.HealRing, elapsed / 0.56f, 10, 28)
                : newArt.Frame(GunNewSkillArt.Clip.HealRing, 28 + Mathf.RoundToInt(Mathf.PingPong((elapsed - 0.56f) * 18f, 11f)));
            ring.color = new Color(1f, 1f, 1f, opacity * (0.72f + 0.18f * pulse));

            slime.transform.localScale = Vector3.one * Mathf.Lerp(0.74f, 1f, appear);
            slime.sprite = moving
                ? newArt.Loop(GunNewSkillArt.Clip.HealSlime, elapsed, 32f, 21, 28)
                : newArt.Loop(GunNewSkillArt.Clip.HealSlime, elapsed, 20f, 0, 21);
            slime.color = new Color(1f, 1f, 1f, opacity);

            hearts.transform.position = new Vector2(position.x, Mathf.Lerp(body.center.y, body.max.y, 0.65f));
            hearts.transform.localScale = Vector3.one * (0.84f + 0.08f * pulse);
            hearts.sprite = newArt.Loop(GunNewSkillArt.Clip.HealHearts, elapsed, 28f, 18, 42);
            hearts.color = new Color(1f, 1f, 1f, opacity * (0.59f + 0.28f * pulse));
            yield return null;
        }
        Destroy(ring.gameObject);
        Destroy(slime.gameObject);
        Destroy(hearts.gameObject);
        healActive = false;
    }

    private void SetHealingPalette(SpriteRenderer renderer, float cutoff, float strength)
    {
        if (newArt.NoGlowMaterial == null) return;
        renderer.sharedMaterial = newArt.NoGlowMaterial;
        var properties = new MaterialPropertyBlock();
        properties.SetFloat("_GlowCutoff", cutoff);
        properties.SetFloat("_HealPalette", strength);
        renderer.SetPropertyBlock(properties);
    }

    private IEnumerator Sweep(Vector2 origin, Vector2 heading)
    {
        const float reach = 5.2f;
        const float lengthScale = 1.33f;
        float halfWidth = 3.4f - ranks.y * 0.46f;
        float width = 1.6f * halfWidth / 2.6f;
        var fx = CreateVisual("Sweeping blade", origin, heading, lengthScale, width);
        // Straighten the atlas's diagonal: its bright arc is 59 px forward and 13 px up from the pivot.
        float atlasTilt = Mathf.Atan2(13f * width, 59f * lengthScale) * Mathf.Rad2Deg;
        fx.transform.rotation *= Quaternion.Euler(0f, 0f, -atlasTilt);
        if (newArt.NoGlowMaterial != null)
        {
            fx.sharedMaterial = newArt.NoGlowMaterial;
            var properties = new MaterialPropertyBlock();
            properties.SetFloat("_GlowCutoff", 0.47f);
            fx.SetPropertyBlock(properties);
        }
        var hit = new HashSet<EnemyHealth>();
        yield return new WaitForSeconds(0.19f);
        StartCoroutine(PixelPlay(fx, GunNewSkillArt.Clip.Sweep, 18, 16f, firstFrame: 1));
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
                if (along < -0.2f || along > front || across > halfWidth * front / reach + 0.25f) continue;
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
        const float duration = 1.35f;
        origin.y = playerView != null && playerView.sprite != null
            ? playerView.bounds.min.y + 0.035f
            : origin.y - 0.4f;
        Vector2 end = origin + heading * length;
        var fx = CreateVisual("Spatial rift", (origin + end) / 2f, heading, length / 2.6f, 2.0f);
        float nextTick = 0.28f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            fx.sprite = newArt.Progress(GunNewSkillArt.Clip.Quake, elapsed / duration, 2, 95);
            if (elapsed >= nextTick)
            {
                foreach (EnemyHealth enemy in targets)
                {
                    if (!Alive(enemy) || DistanceSquared(EnemyFootPosition(enemy), origin, end) > 0.50f * 0.50f) continue;
                    enemy.TakeDamage(5 + 2 * ranks.x);
                }
                nextTick += 0.23f;
            }
            yield return null;
        }
        Destroy(fx.gameObject);
    }

    private IEnumerator BallLightning(EnemyHealth enemy, Vector2 heading)
    {
        const float lifetime = 2.4f, tickInterval = 0.3f, flashDuration = 0.18f;
        const int tickCount = 8;
        int damage = 5 + ranks.x;
        var view = enemy.GetComponentInChildren<SpriteRenderer>();
        var orb = CreateVisual("Ball lightning", enemy.transform.position, Vector2.right, 1f, 1f);
        var discharge = new GameObject("Ball lightning discharge");
        discharge.transform.SetParent(visuals, false);
        var glow = CreateLightningLine(discharge.transform, "Glow", orb.sharedMaterial, 9, 0.16f, orb.sortingOrder);
        var bolt = CreateLightningLine(discharge.transform, "Bolt", orb.sharedMaterial, 9, 0.045f, orb.sortingOrder + 1);
        var branches = new LineRenderer[3];
        for (int i = 0; i < branches.Length; i++)
            branches[i] = CreateLightningLine(discharge.transform, "Branch", orb.sharedMaterial, 4, 0.06f, orb.sortingOrder);
        var impact = CreateVisual("Ball lightning sparks", enemy.transform.position, Vector2.right, 0.50f, 0.74f);
        impact.transform.SetParent(discharge.transform, true);
        impact.sharedMaterial = newArt.NoGlowMaterial;
        impact.sortingOrder = orb.sortingOrder;
        discharge.SetActive(false);

        var offsets = new float[bolt.positionCount];
        var points = new Vector3[bolt.positionCount];
        var forks = new Vector2[branches.Length];
        Vector2 wander = new Vector2(Vector2.SignedAngle(Vector2.right, heading) + Random.Range(-45f, 45f), 0.18f);
        Vector2 from = wander, destination = wander;
        float legStart = 0f, legEnd = 0f;
        float lastStrike = -flashDuration, elapsed = 0f;
        int ticks = 0;
        while (Alive(enemy))
        {
            int previousTicks = ticks;
            while (ticks < tickCount && elapsed >= (ticks + 0.5f) * tickInterval && Alive(enemy))
            {
                ticks++;
                enemy.TakeDamage(damage);
            }
            if (elapsed >= lifetime || !Alive(enemy)) break;

            Bounds bounds = view != null && view.sprite != null
                ? view.bounds : new Bounds(enemy.transform.position, Vector3.one * 0.65f);
            Vector2 center = bounds.center;
            if (elapsed >= legEnd)
            {
                from = destination;
                float turn = Random.Range(30f, 70f) * (Random.value < 0.5f ? -1f : 1f);
                destination = new Vector2(from.x + turn, Random.Range(0f, 0.65f));
                legStart = elapsed;
                legEnd = elapsed + Random.Range(0.70f, 1.05f);
            }
            float blend = Mathf.SmoothStep(0f, 1f, (elapsed - legStart) / (legEnd - legStart));
            wander = Vector2.Lerp(from, destination, blend);
            float clearance = Mathf.Max(0.9f, Mathf.Max(bounds.extents.x, bounds.extents.y) + 0.42f);
            Vector2 position = center + (Vector2)(Quaternion.Euler(0f, 0f, wander.x) * Vector2.right) * (clearance + wander.y);
            orb.transform.position = position;
            orb.sprite = newArt.Loop(GunNewSkillArt.Clip.BallLightning, elapsed, 22f, 40, 10);

            if (ticks != previousTicks)
            {
                lastStrike = elapsed;
                for (int i = 1; i < offsets.Length - 1; i++)
                    offsets[i] = (i % 2 == 0 ? 1f : -1f) * Random.Range(0.12f, 0.29f);
                for (int i = 0; i < forks.Length; i++)
                    forks[i] = new Vector2(Random.Range(0.18f, 0.42f),
                        Random.Range(0.28f, 0.58f) * (i % 2 == 0 ? -1f : 1f));
            }
            float flashAge = elapsed - lastStrike;
            discharge.SetActive(flashAge < flashDuration);
            if (discharge.activeSelf)
            {
                float fade = 1f - flashAge / flashDuration;
                Vector2 axis = (center - position).normalized;
                Vector2 side = new Vector2(-axis.y, axis.x);
                for (int i = 0; i < points.Length; i++)
                {
                    float t = i / (points.Length - 1f);
                    float flicker = 0.85f + 0.17f * Mathf.Sin(flashAge * 110f + i);
                    points[i] = Vector2.Lerp(position, center, t) + side * (offsets[i] * flicker);
                }
                glow.SetPositions(points);
                bolt.SetPositions(points);
                TintLightning(glow, new Color(0.20f, 0.42f, 1f, 0.65f * fade), 0.85f);
                TintLightning(bolt, new Color(0.83f, 0.96f, 1f, fade), 1f);
                for (int i = 0; i < branches.Length; i++)
                {
                    Vector2 root = points[2 + i * 2];
                    Vector2 forward = axis * forks[i].x;
                    Vector2 across = side * (forks[i].y * (0.8f + 0.2f * fade));
                    var fork = branches[i];
                    fork.SetPosition(0, root);
                    fork.SetPosition(1, root + forward * 0.35f + across * 0.28f);
                    fork.SetPosition(2, root + forward * 0.44f + across * 0.70f);
                    fork.SetPosition(3, root + forward + across);
                    TintLightning(fork, new Color(0.52f, 0.83f, 1f, 0.9f * fade), 0.08f);
                }
                impact.transform.SetPositionAndRotation(center, Rotation(axis) * Quaternion.Euler(0f, 0f, 60f));
                impact.sprite = newArt.Loop(GunNewSkillArt.Clip.ChainLightning, flashAge, 40f, 10, 12);
                impact.color = new Color(0.75f, 0.91f, 1f, 0.74f * fade);
            }
            yield return null;
            elapsed += Time.deltaTime;
        }
        Destroy(orb.gameObject);
        Destroy(discharge);
    }

    private static LineRenderer CreateLightningLine(Transform parent, string label, Material material,
        int pointCount, float width, int order)
    {
        var obj = new GameObject(label);
        obj.transform.SetParent(parent, false);
        var line = obj.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.positionCount = pointCount;
        line.startWidth = width;
        line.endWidth = width * 0.7f;
        line.numCornerVertices = 1;
        line.sortingOrder = order;
        return line;
    }

    private static void TintLightning(LineRenderer line, Color color, float endAlpha)
    {
        line.startColor = color;
        color.a *= endAlpha;
        line.endColor = color;
    }

    private IEnumerator SkyLance(Vector2 position)
    {
        float radius = 0.85f + 0.17f * ranks.z;
        var fx = CreateVisual("Sky lance", position, Vector2.right, radius / 0.95f, radius / 0.95f);
        bool struck = false;
        const GunNewSkillArt.Clip clip = GunNewSkillArt.Clip.SkyCircle;
        const float duration = 1.6f;
        const float impactTime = duration * 17f / 88f;
        const int impactFrame = 46, lastFrame = 105;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            fx.sprite = elapsed < impactTime
                ? newArt.Progress(clip, elapsed / impactTime, 0, impactFrame - 1)
                : newArt.Progress(clip, (elapsed - impactTime) / (duration - impactTime), impactFrame, lastFrame);
            if (!struck && elapsed >= impactTime)
            {
                struck = true;
                Area(position, radius, 38 + 6 * ranks.x, useFeet: true);
            }
            yield return null;
        }
        if (!struck) Area(position, radius, 38 + 6 * ranks.x, useFeet: true);
        Destroy(fx.gameObject);
    }

    private static Vector2 EnemyFootPosition(EnemyHealth enemy)
    {
        var view = enemy.GetComponentInChildren<SpriteRenderer>();
        if (view == null || view.sprite == null) return enemy.transform.position;
        Bounds bounds = view.bounds;
        return new Vector2(bounds.center.x, bounds.min.y + 0.035f);
    }

    private void Area(Vector2 center, float radius, int damage, bool useFeet = false)
    {
        foreach (EnemyHealth enemy in targets)
        {
            if (!Alive(enemy)) continue;
            Vector2 position = useFeet ? EnemyFootPosition(enemy) : (Vector2)enemy.transform.position;
            if ((position - center).sqrMagnitude <= radius * radius) enemy.TakeDamage(damage);
        }
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

    private IEnumerator PixelPlay(SpriteRenderer renderer, GunNewSkillArt.Clip clip, int count, float fps, int firstFrame = 0)
    {
        for (float elapsed = 0f; renderer != null && elapsed < (count - firstFrame) / fps; elapsed += Time.deltaTime)
        {
            renderer.sprite = newArt.Frame(clip, Mathf.Min(count - 1, firstFrame + Mathf.FloorToInt(elapsed * fps)));
            yield return null;
        }
        if (renderer != null) Destroy(renderer.gameObject);
    }

}
