using UnityEngine;
using System.Collections.Generic;

namespace Stonehold
{
    public class HeroAttack : MonoBehaviour
    {
        [SerializeField] private HeroDefinition definition;
        [SerializeField] private float targetRefreshInterval = 0.2f;
        [SerializeField] private Vector3 projectileLaunchOffset = new Vector3(0f, 0.6f, 0f);

        private Enemy currentTarget;
        private Enemy priorityTarget;
        private int priorityTargetActivationId;
        private float targetRefreshTimer;
        private float fireCooldown;
        private float abilityCooldown;
        private float abilityBuffTimer; // Tracks Archer rapid multishot buff duration
        private ProceduralAnimator animator;
        private TargetingMode currentTargetingMode;
        private readonly Enemy[] cachedTargets = new Enemy[8];
        private readonly int[] cachedChainHitIds = new int[16];

        public HeroDefinition Definition => definition;
        public bool HasSignatureAbility => definition != null && definition.abilityType != HeroAbilityType.None;
        public bool IsAbilityReady => HasSignatureAbility && abilityCooldown <= 0f;
        public float AbilityCharge01
        {
            get
            {
                if (!HasSignatureAbility)
                {
                    return 0f;
                }

                float duration = Mathf.Max(1f, GetModifiedAbilityCooldown());
                return 1f - Mathf.Clamp01(abilityCooldown / duration);
            }
        }
        public TargetingMode CurrentTargetingMode
        {
            get => currentTargetingMode;
            set
            {
                currentTargetingMode = value;
                currentTarget = null;
                targetRefreshTimer = 0f;
                ClearPriorityTarget();
            }
        }
        public Enemy PriorityTarget => IsPriorityTargetValid(priorityTarget) ? priorityTarget : null;

        private void Awake()
        {
            animator = GetComponent<ProceduralAnimator>();
        }

        public void Configure(HeroDefinition heroDefinition)
        {
            definition = heroDefinition;
            currentTargetingMode = definition != null
                ? definition.defaultTargetingMode
                : TargetingMode.ClosestToGoal;
            abilityCooldown = definition != null ? GetModifiedAbilityCooldown() * 0.5f : 0f;
            abilityBuffTimer = 0f;
            ClearPriorityTarget();
            enabled = definition != null && definition.weapon != null;
        }

        public bool TrySetPriorityTarget(Enemy target)
        {
            if (target == null || !target.IsTargetable)
            {
                return false;
            }

            float range = GetModifiedRange();
            if ((target.transform.position - transform.position).sqrMagnitude > range * range)
            {
                return false;
            }

            priorityTarget = target;
            priorityTargetActivationId = target.ActivationId;
            currentTarget = target;
            targetRefreshTimer = Mathf.Max(0.05f, targetRefreshInterval);
            return true;
        }

        public void ClearPriorityTarget()
        {
            priorityTarget = null;
            priorityTargetActivationId = 0;
        }

        private bool IsPriorityTargetValid(Enemy target)
        {
            if (target == null || !target.IsTargetable || !target.MatchesActivation(priorityTargetActivationId))
            {
                return false;
            }

            float range = GetModifiedRange();
            return (target.transform.position - transform.position).sqrMagnitude <= range * range;
        }

        public float GetAbilityCooldownRemaining()
        {
            return Mathf.Max(0f, abilityCooldown);
        }

        public float GetModifiedAbilityCooldown()
        {
            float cd = definition != null ? definition.abilityCooldown : 10f;
            if (RunModifierManager.Instance != null && definition != null)
            {
                cd *= RunModifierManager.Instance.GetAbilityCooldownMultiplier(definition.id);
            }
            if (RelicManager.Instance != null)
            {
                cd *= RelicManager.Instance.GetCooldownMultiplier();
            }
            return cd;
        }

        public float GetModifiedAbilityRadius()
        {
            float radius = definition != null ? definition.abilityRadius : 3f;
            if (RunModifierManager.Instance != null && definition != null)
            {
                radius *= RunModifierManager.Instance.GetAbilityRadiusMultiplier(definition.id);
            }
            return radius;
        }

        public float GetModifiedBurnDuration()
        {
            float dur = 4f;
            if (RunModifierManager.Instance != null && definition != null)
            {
                dur += RunModifierManager.Instance.GetBurnDurationAdd(definition.id);
            }
            return dur;
        }

        public float GetModifiedSlowDuration()
        {
            float dur = 4f;
            if (RunModifierManager.Instance != null && definition != null)
            {
                dur += RunModifierManager.Instance.GetSlowDurationAdd(definition.id);
            }
            return dur;
        }

        private bool IsNearPaladinAura(out float auraFireRateBuff, out float auraCritBuff)
        {
            auraFireRateBuff = 0f;
            auraCritBuff = 0f;
            if (HeroRosterManager.Instance == null) return false;

            var slots = HeroRosterManager.Instance.Slots;
            if (slots == null) return false;

            for (int i = 0; i < slots.Count; i++)
            {
                HeroSlot slot = slots[i];
                if (slot != null && slot.IsOccupied && slot.CurrentHero != null)
                {
                    HeroAttack paladin = slot.CurrentHero;
                    if (paladin != null && paladin.Definition != null && paladin.Definition.id == "radiant_paladin")
                    {
                        float auraRange = 7.0f;
                        float fireRateBonus = 0.15f;
                        float critBonus = 0.10f;

                        if (RunModifierManager.Instance != null && RunModifierManager.Instance.HasBehavior("radiant_paladin", HeroBehaviorEffectType.SanctuaryAura))
                        {
                            auraRange = 11.0f;
                            fireRateBonus = 0.25f;
                            critBonus = 0.15f;
                        }

                        if (paladin == this || Vector3.Distance(transform.position, paladin.transform.position) <= auraRange)
                        {
                            auraFireRateBuff = Mathf.Max(auraFireRateBuff, fireRateBonus);
                            auraCritBuff = Mathf.Max(auraCritBuff, critBonus);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public float GetModifiedCritChance()
        {
            float chance = 0.05f;
            if (definition != null)
            {
                if (definition.id == "sniper") chance = 0.20f;
                else if (definition.id == "shadow_assassin") chance = 0.25f;
            }
            if (RunModifierManager.Instance != null && definition != null)
            {
                chance += RunModifierManager.Instance.GetCritChanceAdd(definition.id);
            }
            if (IsNearPaladinAura(out _, out float auraCrit))
            {
                chance += auraCrit;
            }
            return Mathf.Clamp01(chance);
        }

        public float GetModifiedCritMultiplier()
        {
            float mult = (definition != null && definition.id == "shadow_assassin") ? 2.5f : 2.0f;
            if (RunModifierManager.Instance != null && definition != null)
            {
                mult += RunModifierManager.Instance.GetCritMultiplierAdd(definition.id);
            }
            return mult;
        }

        public float GetModifiedDamage()
        {
            float damage = definition != null ? definition.baseDamage : 0f;
            if (MetaUpgradeManager.Instance != null)
            {
                damage *= MetaUpgradeManager.Instance.GetGlobalDamageMultiplier();
            }
            if (RunModifierManager.Instance != null && definition != null)
            {
                damage *= RunModifierManager.Instance.GetDamageMultiplier(definition.id);
            }
            return damage;
        }

        public float GetModifiedFireRate()
        {
            float fireRate = definition != null ? definition.baseFireRate : 1f;
            if (MetaUpgradeManager.Instance != null)
            {
                fireRate *= MetaUpgradeManager.Instance.GetGlobalFireRateMultiplier();
            }
            if (RunModifierManager.Instance != null && definition != null)
            {
                fireRate *= RunModifierManager.Instance.GetFireRateMultiplier(definition.id);
            }
            if (IsNearPaladinAura(out float auraFR, out _))
            {
                fireRate *= (1f + auraFR);
            }
            if (definition != null && definition.id == "archer" && abilityBuffTimer > 0f)
            {
                fireRate *= 3.0f; // Triple fire rate during rapid multishot
            }
            return fireRate;
        }

        public float GetModifiedRange()
        {
            float range = definition != null ? definition.baseRange : 0f;
            if (MetaUpgradeManager.Instance != null)
            {
                range *= MetaUpgradeManager.Instance.GetGlobalRangeMultiplier();
            }
            if (RunModifierManager.Instance != null && definition != null)
            {
                range *= RunModifierManager.Instance.GetRangeMultiplier(definition.id);
            }
            return range;
        }

        public static bool AutoCastSkills = true;
        public bool manualCastMode = false;

        public float AbilityCooldownRemaining => Mathf.Max(0f, abilityCooldown);
        public float AbilityTotalCooldown => Mathf.Max(0.1f, GetModifiedAbilityCooldown());
        public float AbilityCooldownNormalized => Mathf.Clamp01(abilityCooldown / AbilityTotalCooldown);
        public void ResetAbilityCooldown() { abilityCooldown = 0f; }

        public bool TriggerManualAbility()
        {
            if (!IsAbilityReady) return false;

            Enemy target = currentTarget != null && currentTarget.gameObject.activeInHierarchy
                ? currentTarget
                : EnemyManager.FindTarget(transform.position, GetModifiedRange(), currentTargetingMode);

            if (target == null)
            {
                var all = EnemyManager.All;
                for (int i = 0; i < all.Count; i++)
                {
                    if (all[i] != null && !all[i].IsDead && all[i].IsTargetable)
                    {
                        target = all[i];
                        break;
                    }
                }
            }

            if (target == null) return false;

            UseSignatureAbility(target);
            abilityCooldown = GetModifiedAbilityCooldown();
            return true;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            if (definition == null || definition.weapon == null)
            {
                return;
            }

            targetRefreshTimer -= Time.deltaTime;
            fireCooldown -= Time.deltaTime;
            abilityCooldown -= Time.deltaTime;
            if (abilityBuffTimer > 0f)
            {
                abilityBuffTimer -= Time.deltaTime;
            }

            if (priorityTarget != null && !IsPriorityTargetValid(priorityTarget))
            {
                ClearPriorityTarget();
                currentTarget = null;
                targetRefreshTimer = 0f;
            }

            if (priorityTarget != null)
            {
                currentTarget = priorityTarget;
            }
            else if (targetRefreshTimer <= 0f)
            {
                currentTarget = EnemyManager.FindTarget(transform.position, GetModifiedRange(), currentTargetingMode);
                targetRefreshTimer = Mathf.Max(0.05f, targetRefreshInterval);
            }

            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                return;
            }

            if (definition.abilityType != HeroAbilityType.None && abilityCooldown <= 0f && (AutoCastSkills && !manualCastMode))
            {
                UseSignatureAbility(currentTarget);
                abilityCooldown = GetModifiedAbilityCooldown();
            }

            Vector3 direction = currentTarget.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 12f * Time.deltaTime);
            }

            if (fireCooldown <= 0f)
            {
                Fire(currentTarget);
                float rate = GetModifiedFireRate();
                fireCooldown = rate > 0f ? 1f / rate : 1f;
            }
        }

        private void UseSignatureAbility(Enemy primaryTarget)
        {
            if (animator != null)
            {
                animator.PlayAbility();
            }

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayHeroAbilityCast(GetAbilityOriginPosition(), definition.id);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayAbilityCast(definition.id);
            }

            float delay = 0f;
            switch (definition.id)
            {
                case "frost_mage": delay = 0.25f; break;
                case "fire_mage": delay = 0.25f; break;
                case "bombardier": delay = 0.3f; break;
                case "sniper": delay = 0.35f; break;
                case "electric_engineer": delay = 0.2f; break;
                case "plague_doctor": delay = 0.25f; break;
                case "radiant_paladin": delay = 0.2f; break;
                case "shadow_assassin": delay = 0.12f; break;
                case "storm_druid": delay = 0.22f; break;
                default: delay = 0.15f; break;
            }

            int targetActivationId = primaryTarget != null ? primaryTarget.ActivationId : 0;
            StartCoroutine(ExecuteSignatureAbilityDelayed(primaryTarget, targetActivationId, delay));
        }

        private System.Collections.IEnumerator ExecuteSignatureAbilityDelayed(Enemy primaryTarget, int targetActivationId, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (primaryTarget == null || !primaryTarget.MatchesActivation(targetActivationId))
            {
                primaryTarget = EnemyManager.FindTarget(transform.position, GetModifiedRange(), currentTargetingMode);
                if (primaryTarget == null)
                {
                    yield break;
                }
                targetActivationId = primaryTarget.ActivationId;
            }

            float abilityDamage = GetModifiedDamage() * Mathf.Max(1f, definition.abilityPowerMultiplier);
            if (RunModifierManager.Instance != null)
            {
                abilityDamage *= RunModifierManager.Instance.GetAbilityDamageMultiplier(definition.id);
            }

            int extraProjOrChain = 0;
            if (RunModifierManager.Instance != null)
            {
                extraProjOrChain = RunModifierManager.Instance.GetAbilityExtraProjOrChain(definition.id);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayHeroImpact(definition.id, true);
            }

            switch (definition.abilityType)
            {
                case HeroAbilityType.PowerShot:
                    UsePowerShotAbility(primaryTarget, abilityDamage);
                    break;
                case HeroAbilityType.FrostNova:
                    HitEnemiesInRadius(primaryTarget.transform.position, abilityDamage, StatusEffectType.Slow, 0.0f, GetModifiedSlowDuration(), false);
                    if (VfxManager.Instance != null)
                    {
                        VfxManager.Instance.PlayHeroProjectileImpact(primaryTarget.transform.position, "frost_mage", false);
                        if (CameraRig.Instance != null)
                        {
                            CameraRig.Instance.Shake(0.5f);
                        }
                    }
                    if (definition.id == "frost_mage" && RunModifierManager.Instance != null && RunModifierManager.Instance.HasBehavior("frost_mage", HeroBehaviorEffectType.ExtraCast))
                    {
                        int modifierRevision = RunModifierManager.Instance.Revision;
                        StartCoroutine(ExecuteEchoingNova(primaryTarget.transform.position, abilityDamage * 0.5f, GetModifiedAbilityRadius() * 0.7f, modifierRevision));
                    }
                    break;
                case HeroAbilityType.FlameWave:
                    SpawnMeteor(primaryTarget, abilityDamage);
                    break;
                case HeroAbilityType.ArtilleryBarrage:
                    FireArtilleryBarrageBomb(primaryTarget, abilityDamage);
                    break;
                case HeroAbilityType.MultiShot:
                    abilityBuffTimer = 5f;
                    break;
                case HeroAbilityType.ChainStorm:
                    UseChainLightningAbility(primaryTarget, abilityDamage, definition.abilityTargetCount + extraProjOrChain);
                    break;
                case HeroAbilityType.PlagueFlask:
                    UsePlagueFlaskAbility(primaryTarget, abilityDamage);
                    break;
                case HeroAbilityType.Consecration:
                    UseConsecrationAbility(primaryTarget, abilityDamage);
                    break;
                case HeroAbilityType.ShadowStep:
                    UseShadowStepAbility(primaryTarget, abilityDamage);
                    break;
                case HeroAbilityType.TempestCyclone:
                    UseTempestCycloneAbility(primaryTarget, abilityDamage);
                    break;
            }
        }

        private void UseShadowStepAbility(Enemy primaryTarget, float damage)
        {
            if (primaryTarget == null || primaryTarget.IsDead) return;

            Vector3 targetPos = primaryTarget.transform.position;
            bool isCrit = true; // Guaranteed lethal critical strike
            float finalDamage = damage * 2.0f; // Eviscerate multiplier

            if (RunModifierManager.Instance != null && RunModifierManager.Instance.HasBehavior("shadow_assassin", HeroBehaviorEffectType.LethalPrecisionCrit))
            {
                finalDamage *= 1.5f;
            }

            float applied = primaryTarget.TakeDamage(finalDamage, true, isCrit, definition.id);
            DamageTracker.RecordDamage(definition.id, applied);

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayHeroProjectileImpact(targetPos, "shadow_assassin", true);
                if (CameraRig.Instance != null) CameraRig.Instance.Shake(0.5f);
            }

            // If target died and ShadowDanceReset is active, reset cooldown!
            if (primaryTarget.IsDead && RunModifierManager.Instance != null && RunModifierManager.Instance.HasBehavior("shadow_assassin", HeroBehaviorEffectType.ShadowDanceReset))
            {
                abilityCooldown = 0f;
                Debug.Log("[HeroAttack] 🗡️ SHADOW DANCE: Cooldown instantly refreshed on kill!");
            }
        }

        private void UseTempestCycloneAbility(Enemy primaryTarget, float damage)
        {
            Vector3 center = primaryTarget != null ? primaryTarget.transform.position : transform.position;
            float radius = GetModifiedAbilityRadius();

            if (RunModifierManager.Instance != null && RunModifierManager.Instance.HasBehavior("storm_druid", HeroBehaviorEffectType.VortexMaelstrom))
            {
                radius *= 1.5f;
            }

            StartCoroutine(ExecuteTempestCycloneRoutine(center, damage, radius));
        }

        private System.Collections.IEnumerator ExecuteTempestCycloneRoutine(Vector3 center, float totalDamage, float radius)
        {
            float duration = 2.5f;
            float tickInterval = 0.5f;
            float tickDamage = totalDamage / (duration / tickInterval);
            float elapsed = 0f;

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayHeroProjectileImpact(center, "storm_druid", true);
                if (CameraRig.Instance != null) CameraRig.Instance.Shake(0.4f);
            }

            while (elapsed < duration)
            {
                if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
                {
                    yield return null;
                    continue;
                }

                var enemies = EnemyManager.All;
                if (enemies != null)
                {
                    float radiusSqr = radius * radius;
                    for (int i = 0; i < enemies.Count; i++)
                    {
                        var e = enemies[i];
                        if (e != null && !e.IsDead && e.IsTargetable)
                        {
                            Vector3 diff = center - e.transform.position;
                            if (diff.sqrMagnitude <= radiusSqr)
                            {
                                e.transform.position = Vector3.MoveTowards(e.transform.position, center, 2.5f * Time.deltaTime);
                            }
                        }
                    }
                }

                HitEnemiesInRadius(center, tickDamage, StatusEffectType.Shock, 1.0f, 2.0f, false, radius);
                yield return new WaitForSeconds(tickInterval);
                elapsed += tickInterval;
            }
        }

        private void UsePlagueFlaskAbility(Enemy primaryTarget, float damage)
        {
            Vector3 center = primaryTarget != null ? primaryTarget.transform.position : transform.position;
            float radius = GetModifiedAbilityRadius();

            HitEnemiesInRadius(center, damage, StatusEffectType.Poison, damage * 0.5f, 6.0f, true, radius);

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayHeroProjectileImpact(center, "plague_doctor", false);
                if (CameraRig.Instance != null)
                {
                    CameraRig.Instance.Shake(0.4f);
                }
            }

            SpectralMinion.Spawn(center, damage * 0.4f, 12f, 60f, "plague_doctor");
        }

        private void UseConsecrationAbility(Enemy primaryTarget, float damage)
        {
            Vector3 center = primaryTarget != null ? primaryTarget.transform.position : transform.position;
            float radius = GetModifiedAbilityRadius() * 1.25f;

            var enemies = EnemyManager.All;
            if (enemies != null)
            {
                float radiusSqr = radius * radius;
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    if (i >= enemies.Count) continue;
                    Enemy e = enemies[i];
                    if (e != null && !e.IsDead && (e.transform.position - center).sqrMagnitude <= radiusSqr)
                    {
                        float finalDmg = damage;
                        if (e.CurrentShield > 0f || (e.Data != null && e.Data.classification == EnemyClassification.Boss))
                        {
                            finalDmg *= 2.0f;
                        }
                        float applied = e.TakeDamage(finalDmg, false, true, definition.id);
                        DamageTracker.RecordDamage(definition.id, applied);
                    }
                }
            }

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayHeroProjectileImpact(center, "radiant_paladin", true);
                if (CameraRig.Instance != null)
                {
                    CameraRig.Instance.Shake(0.6f);
                }
            }
        }

        private System.Collections.IEnumerator ExecuteEchoingNova(Vector3 center, float damage, float radius, int modifierRevision)
        {
            float elapsed = 0f;
            while (elapsed < 1.0f)
            {
                if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
                {
                    if (GameManager.Instance.State == GameState.Victory || GameManager.Instance.State == GameState.Defeat)
                    {
                        yield break;
                    }
                    yield return null;
                    continue;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            {
                yield break;
            }

            if (RunModifierManager.Instance == null ||
                RunModifierManager.Instance.Revision != modifierRevision ||
                !RunModifierManager.Instance.HasBehavior("frost_mage", HeroBehaviorEffectType.ExtraCast))
            {
                yield break;
            }

            HitEnemiesInRadius(center, damage, StatusEffectType.Slow, 0.0f, GetModifiedSlowDuration(), false, radius);
            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayHeroProjectileImpact(center, "frost_mage", false);
            }
        }

        private void SpawnMeteor(Enemy target, float damage)
        {
            WeaponDefinition weapon = definition.weapon;
            if (weapon != null && weapon.projectilePrefab != null)
            {
                Vector3 spawnPos = target.transform.position + new Vector3(-4f, 12f, 0f);
                Projectile projectile = Projectile.Spawn(weapon.projectilePrefab, spawnPos);
                if (projectile != null)
                {
                    projectile.IsAbility = true;
                    projectile.transform.localScale = Vector3.one * 2.5f;
                    projectile.InitWithStatusEffect(
                        target,
                        damage,
                        GetModifiedAbilityRadius(),
                        new Color(1f, 0.25f, 0.05f, 1f),
                        definition.id,
                        StatusEffectType.Burn,
                        damage * 0.35f,
                        GetModifiedBurnDuration()
                    );
                }
            }
        }

        private void UsePowerShotAbility(Enemy primaryTarget, float damage)
        {
            Vector3 start = GetAbilityOriginPosition();
            Vector3 direction = (primaryTarget.transform.position - start).normalized;
            direction.y = 0f;
            Vector3 end = start + direction * GetModifiedRange();

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayHeroAttackTrace(start, end, definition.id, 0.18f);
            }

            var all = EnemyManager.All;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                Enemy enemy = all[i];
                if (enemy == null || enemy.IsDead) continue;

                float distToLine = DistanceToLineSegment(enemy.transform.position, start, end);
                if (distToLine <= 1.2f)
                {
                    ApplyAbilityHit(enemy, damage, StatusEffectType.None, 0f, 0f, true); // Ignore armor
                    if (VfxManager.Instance != null)
                    {
                        VfxManager.Instance.PlayHeroProjectileImpact(enemy.transform.position, "sniper", true);
                    }
                }
            }
        }

        private static float DistanceToLineSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector3 lineVec = end - start;
            Vector3 pointVec = point - start;
            float lineLenSqr = lineVec.sqrMagnitude;
            if (lineLenSqr < 0.0001f) return Vector3.Distance(point, start);

            float t = Mathf.Clamp01(Vector3.Dot(pointVec, lineVec) / lineLenSqr);
            Vector3 projection = start + t * lineVec;
            return Vector3.Distance(point, projection);
        }

        private void FireArtilleryBarrageBomb(Enemy target, float damage)
        {
            WeaponDefinition weapon = definition.weapon;
            if (weapon != null && weapon.projectilePrefab != null)
            {
                Projectile projectile = Projectile.Spawn(weapon.projectilePrefab, GetAbilityOriginPosition());
                if (projectile != null)
                {
                    projectile.IsAbility = true;
                    projectile.transform.localScale = Vector3.one * 2.2f;
                    projectile.InitWithStatusEffect(
                        target,
                        damage,
                        GetModifiedAbilityRadius(),
                        new Color(1f, 0.45f, 0.1f, 1f),
                        definition.id,
                        StatusEffectType.Stun,
                        0f,
                        1.5f
                    );
                }
            }
        }

        private void FireMultiShotVolley(float damage, bool isCritical = false)
        {
            int extraProjOrChain = 0;
            if (RunModifierManager.Instance != null)
            {
                extraProjOrChain = RunModifierManager.Instance.GetAbilityExtraProjOrChain(definition.id);
            }
            int targetsCount = Mathf.Max(1, definition.abilityTargetCount + extraProjOrChain);
            if (targetsCount > cachedTargets.Length)
            {
                targetsCount = cachedTargets.Length;
            }

            float rangeSqr = GetModifiedRange() * GetModifiedRange();
            var enemies = EnemyManager.All;

            for (int i = 0; i < cachedTargets.Length; i++)
            {
                cachedTargets[i] = null;
            }

            int targetsFound = 0;
            for (int i = 0; i < enemies.Count && targetsFound < targetsCount; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy != null && !enemy.IsDead && enemy.IsTargetable && (enemy.transform.position - transform.position).sqrMagnitude <= rangeSqr)
                {
                    cachedTargets[targetsFound] = enemy;
                    targetsFound++;
                }
            }

            WeaponDefinition weapon = definition.weapon;
            if (weapon != null && weapon.projectilePrefab != null)
            {
                for (int i = 0; i < targetsFound; i++)
                {
                    Enemy targetEnemy = cachedTargets[i];
                    if (targetEnemy == null) continue;

                    Projectile projectile = Projectile.Spawn(weapon.projectilePrefab, GetMuzzlePosition());
                    if (projectile != null)
                    {
                        projectile.InitWithStatusEffect(
                            targetEnemy,
                            damage,
                            0f,
                            GetTrailColor(weapon, StatusEffectType.None, definition.id),
                            definition.id,
                            StatusEffectType.None,
                            0f,
                            0f,
                            isCritical
                        );
                    }
                }
            }

            for (int i = 0; i < cachedTargets.Length; i++)
            {
                cachedTargets[i] = null;
            }
        }

        private void UseChainLightningAbility(Enemy primaryTarget, float damage, int bounceCount)
        {
            Vector3 startSource = GetAbilityOriginPosition();
            Enemy current = primaryTarget;

            for (int i = 0; i < cachedChainHitIds.Length; i++)
            {
                cachedChainHitIds[i] = 0;
            }

            for (int b = 0; b < bounceCount && current != null; b++)
            {
                cachedChainHitIds[b] = current.ActivationId;
                ApplyAbilityHit(current, damage, StatusEffectType.Shock, 1f, 4f, false);

                Vector3 endPos = current.transform.position + Vector3.up * 0.25f;
                if (VfxManager.Instance != null)
                {
                    VfxManager.Instance.PlayHeroAttackTrace(startSource, endPos, "electric_engineer", 0.12f);
                    VfxManager.Instance.PlayHeroProjectileImpact(current.transform.position, "electric_engineer", false);
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayHeroImpact("electric_engineer", true);
                }

                startSource = endPos;
                current = FindNextChainTargetNonAlloc(current.transform.position, cachedChainHitIds, b + 1, 5.0f);
            }
        }

        private void UseChainLightningBasic(Enemy primaryTarget, float damage)
        {
            Vector3 startSource = GetMuzzlePosition();
            Enemy current = primaryTarget;

            int extraBounces = 0;
            if (RunModifierManager.Instance != null)
            {
                extraBounces = RunModifierManager.Instance.GetBehaviorStacks("electric_engineer", HeroBehaviorEffectType.ExtraChain);
            }
            int totalBounces = 2 + extraBounces;

            for (int i = 0; i < cachedChainHitIds.Length; i++)
            {
                cachedChainHitIds[i] = 0;
            }

            int hitCount = 0;

            // Forked Current behavior check
            int forkStacks = 0;
            if (RunModifierManager.Instance != null)
            {
                forkStacks = RunModifierManager.Instance.GetBehaviorStacks("electric_engineer", HeroBehaviorEffectType.Ricochet);
            }

            // Primary hit setup
            cachedChainHitIds[hitCount++] = current.ActivationId;

            bool primaryCrit = Random.value < GetModifiedCritChance();
            float primaryFinalDamage = damage;
            if (primaryCrit) primaryFinalDamage *= GetModifiedCritMultiplier();

            float primaryAppliedDamage = current.TakeDamage(primaryFinalDamage, false, primaryCrit, definition.id);
            DamageTracker.RecordDamage(definition.id, primaryAppliedDamage);

            Vector3 startEndPos = current.transform.position + Vector3.up * 0.25f;
            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayHeroAttackTrace(startSource, startEndPos, "electric_engineer", 0.08f);
                VfxManager.Instance.PlayHeroProjectileImpact(current.transform.position, "electric_engineer", false);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayHeroImpact("electric_engineer", false);
            }

            if (!current.IsDead)
            {
                current.ApplyStatusEffect(new StatusEffect(StatusEffectType.Shock, 1f, 2.5f, definition.id));
            }

            // Perform forks off the primary target if Forked Current is active
            if (forkStacks > 0)
            {
                int forksFound = 0;
                var allEnemies = EnemyManager.All;
                for (int i = 0; i < allEnemies.Count && forksFound < forkStacks; i++)
                {
                    Enemy candidate = allEnemies[i];
                    if (candidate == null || candidate == current || candidate.IsDead || !candidate.IsTargetable) continue;

                    float distSqr = (candidate.transform.position - current.transform.position).sqrMagnitude;
                    if (distSqr <= 4.5f * 4.5f)
                    {
                        float forkDamage = damage * 0.5f; // documented 50% damage multiplier
                        bool forkCrit = Random.value < GetModifiedCritChance();
                        float finalForkDamage = forkDamage;
                        if (forkCrit) finalForkDamage *= GetModifiedCritMultiplier();

                        float appliedForkDamage = candidate.TakeDamage(finalForkDamage, false, forkCrit, definition.id);
                        DamageTracker.RecordDamage(definition.id, appliedForkDamage);

                        if (VfxManager.Instance != null)
                        {
                            Vector3 forkStart = current.transform.position + Vector3.up * 0.25f;
                            Vector3 forkEnd = candidate.transform.position + Vector3.up * 0.25f;
                            VfxManager.Instance.PlayHeroAttackTrace(forkStart, forkEnd, "electric_engineer", 0.08f);
                            VfxManager.Instance.PlayHeroProjectileImpact(candidate.transform.position, "electric_engineer", false);
                        }

                        if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlayHeroImpact("electric_engineer", false);
                        }

                        if (!candidate.IsDead)
                        {
                            candidate.ApplyStatusEffect(new StatusEffect(StatusEffectType.Shock, 1f, 2.5f, definition.id));
                        }

                        cachedChainHitIds[totalBounces + forksFound] = candidate.ActivationId;
                        forksFound++;
                    }
                }
            }

            Vector3 nextStartSource = startEndPos;
            Enemy nextCurrent = FindNextChainTargetNonAlloc(current.transform.position, cachedChainHitIds, hitCount, 4.5f);

            // Remaining bounces
            for (int b = 1; b < totalBounces && nextCurrent != null; b++)
            {
                cachedChainHitIds[hitCount++] = nextCurrent.ActivationId;

                bool isCrit = Random.value < GetModifiedCritChance();
                float finalDamage = damage;
                if (isCrit) finalDamage *= GetModifiedCritMultiplier();

                float appliedDamage = nextCurrent.TakeDamage(finalDamage, false, isCrit, definition.id);
                DamageTracker.RecordDamage(definition.id, appliedDamage);

                Vector3 endPos = nextCurrent.transform.position + Vector3.up * 0.25f;
                if (VfxManager.Instance != null)
                {
                    VfxManager.Instance.PlayHeroAttackTrace(nextStartSource, endPos, "electric_engineer", 0.08f);
                    VfxManager.Instance.PlayHeroProjectileImpact(nextCurrent.transform.position, "electric_engineer", false);
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayHeroImpact("electric_engineer", false);
                }

                if (!nextCurrent.IsDead)
                {
                    nextCurrent.ApplyStatusEffect(new StatusEffect(StatusEffectType.Shock, 1f, 2.5f, definition.id));
                }

                nextStartSource = endPos;
                nextCurrent = FindNextChainTargetNonAlloc(nextCurrent.transform.position, cachedChainHitIds, hitCount, 4.5f);
            }
        }

        private Enemy FindNextChainTargetNonAlloc(Vector3 sourcePos, int[] hitList, int hitCount, float bounceRange)
        {
            Enemy best = null;
            float bestDistSqr = bounceRange * bounceRange;
            var all = EnemyManager.All;
            for (int i = 0; i < all.Count; i++)
            {
                Enemy enemy = all[i];
                if (enemy == null || enemy.IsDead || !enemy.IsTargetable) continue;

                bool alreadyHit = false;
                for (int j = 0; j < hitList.Length; j++)
                {
                    if (hitList[j] != 0 && hitList[j] == enemy.ActivationId)
                    {
                        alreadyHit = true;
                        break;
                    }
                }
                if (alreadyHit) continue;

                float distSqr = (enemy.transform.position - sourcePos).sqrMagnitude;
                if (distSqr <= bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    best = enemy;
                }
            }
            return best;
        }

        private void HitEnemiesInRadius(Vector3 center, float damage, StatusEffectType effectType, float effectValue, float effectDuration, bool ignoreArmor = false, float customRadius = -1f)
        {
            float radius = customRadius > 0f ? customRadius : GetModifiedAbilityRadius();
            float radiusSqr = radius * radius;
            var enemies = EnemyManager.All;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i >= enemies.Count) continue;
                Enemy enemy = enemies[i];
                if (enemy != null && !enemy.IsDead && (enemy.transform.position - center).sqrMagnitude <= radiusSqr)
                {
                    ApplyAbilityHit(enemy, damage, effectType, effectValue, effectDuration, ignoreArmor);
                }
            }
        }

        private void ApplyAbilityHit(Enemy enemy, float damage, StatusEffectType effectType, float effectValue, float effectDuration, bool ignoreArmor = false)
        {
            if (enemy == null || enemy.IsDead)
            {
                return;
            }

            bool isCrit = Random.value < GetModifiedCritChance();
            float finalDamage = damage;
            if (isCrit)
            {
                finalDamage *= GetModifiedCritMultiplier();
            }

            float appliedDamage = enemy.TakeDamage(finalDamage, ignoreArmor, isCrit, definition.id);
            DamageTracker.RecordDamage(definition.id, appliedDamage);
            if (effectType != StatusEffectType.None && effectDuration > 0f && !enemy.IsDead)
            {
                enemy.ApplyStatusEffect(new StatusEffect(effectType, effectValue, effectDuration, definition.id));
            }
        }

        private void Fire(Enemy target)
        {
            float delay = 0.15f;
            switch (definition.id)
            {
                case "archer": delay = 0.15f; break;
                case "bombardier": delay = 0.22f; break;
                case "frost_mage": delay = 0.18f; break;
                case "fire_mage": delay = 0.22f; break;
                case "electric_engineer": delay = 0.12f; break;
                case "sniper": delay = 0.28f; break;
                case "plague_doctor": delay = 0.2f; break;
                case "radiant_paladin": delay = 0.18f; break;
                case "shadow_assassin": delay = 0.10f; break;
                case "storm_druid": delay = 0.18f; break;
            }

            if (animator != null)
            {
                animator.PlayAttack();
            }

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayHeroAttackMuzzle(GetMuzzlePosition(), definition.id);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayHeroShot(definition.id);
            }

            WeaponDefinition weapon = definition.weapon;
            float damage = GetModifiedDamage();
            float splashRadius = (definition.id == "archer" && abilityBuffTimer > 0f) ? 0f : (weapon.attackType == AttackType.Splash ? weapon.splashRadius : 0f);

            // Check for Bombardier Wide Blast behavior upgrade
            if (definition.id == "bombardier" && RunModifierManager.Instance != null)
            {
                int wideStacks = RunModifierManager.Instance.GetBehaviorStacks("bombardier", HeroBehaviorEffectType.ExplosionRadius);
                if (wideStacks > 0)
                {
                    // Cap at safe maximum of 5.0m
                    splashRadius = Mathf.Min(5.0f, splashRadius + (wideStacks * 0.75f));
                }
            }

            StatusEffectType effectType = weapon.statusEffectType;
            float effectValue = weapon.statusEffectValue;
            float effectDuration = weapon.statusEffectDuration;

            if (effectType == StatusEffectType.Slow)
            {
                if (RunModifierManager.Instance != null)
                {
                    float slowStrength = RunModifierManager.Instance.GetSlowStrengthAdd(definition.id);
                    effectValue = Mathf.Clamp(effectValue - slowStrength, 0.05f, 0.95f);
                    effectDuration += RunModifierManager.Instance.GetSlowDurationAdd(definition.id);
                }
            }
            else if (effectType == StatusEffectType.Burn)
            {
                if (RunModifierManager.Instance != null)
                {
                    effectValue += RunModifierManager.Instance.GetBurnDamageAdd(definition.id);
                    effectDuration += RunModifierManager.Instance.GetBurnDurationAdd(definition.id);
                }
            }
            else if (effectType == StatusEffectType.Poison)
            {
                if (RunModifierManager.Instance != null)
                {
                    effectValue += RunModifierManager.Instance.GetPoisonDamageAdd(definition.id);
                    effectDuration += RunModifierManager.Instance.GetPoisonDurationAdd(definition.id);
                }
            }
            else if (effectType == StatusEffectType.None)
            {
                if (RunModifierManager.Instance != null)
                {
                    if (RunModifierManager.Instance.IsShockEnabled(definition.id))
                    {
                        effectType = StatusEffectType.Shock;
                        effectValue = 1f;
                        effectDuration = 3f;
                    }
                    else
                    {
                        float burnAdd = RunModifierManager.Instance.GetBurnDamageAdd(definition.id);
                        if (burnAdd > 0f)
                        {
                            effectType = StatusEffectType.Burn;
                            effectValue = burnAdd;
                            effectDuration = 3f + RunModifierManager.Instance.GetBurnDurationAdd(definition.id);
                        }
                    }
                }
            }

            if (definition.id == "radiant_paladin" && target != null)
            {
                if (target.CurrentShield > 0f || (target.Data != null && target.Data.classification == EnemyClassification.Boss))
                {
                    damage *= 2.0f;
                }
            }

            bool isCrit = Random.value < GetModifiedCritChance();
            if (isCrit)
            {
                damage *= GetModifiedCritMultiplier();
            }

            int targetActivationId = target != null ? target.ActivationId : 0;
            StartCoroutine(ExecuteFireDelayed(target, targetActivationId, weapon, damage, splashRadius, effectType, effectValue, effectDuration, isCrit, delay));
        }

        private System.Collections.IEnumerator ExecuteFireDelayed(Enemy target, int targetActivationId, WeaponDefinition weapon, float damage, float splashRadius, StatusEffectType effectType, float effectValue, float effectDuration, bool isCrit, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (target == null || !target.MatchesActivation(targetActivationId))
            {
                target = EnemyManager.FindTarget(transform.position, GetModifiedRange(), currentTargetingMode);
                if (target == null)
                {
                    yield break;
                }
                targetActivationId = target.ActivationId;
            }

            if (definition.id == "archer" && abilityBuffTimer > 0f)
            {
                FireMultiShotVolley(damage, isCrit);
                yield break;
            }

            if (definition.id == "electric_engineer")
            {
                UseChainLightningBasic(target, damage);
                yield break;
            }

            // Archer Twin Volley
            if (definition.id == "archer" && RunModifierManager.Instance != null && RunModifierManager.Instance.HasBehavior("archer", HeroBehaviorEffectType.ExtraProjectile))
            {
                int stacks = RunModifierManager.Instance.GetBehaviorStacks("archer", HeroBehaviorEffectType.ExtraProjectile);
                for (int i = 0; i < cachedTargets.Length; i++) cachedTargets[i] = null;

                cachedTargets[0] = target;
                int targetsFound = 1;
                float rangeSqr = GetModifiedRange() * GetModifiedRange();
                var enemiesList = EnemyManager.All;
                for (int i = 0; i < enemiesList.Count && targetsFound < 1 + stacks; i++)
                {
                    Enemy enemy = enemiesList[i];
                    if (enemy == null || enemy == target || enemy.IsDead || !enemy.IsTargetable) continue;

                    float distSqr = (enemy.transform.position - transform.position).sqrMagnitude;
                    if (distSqr <= rangeSqr)
                    {
                        cachedTargets[targetsFound] = enemy;
                        targetsFound++;
                    }
                }
                while (targetsFound < 1 + stacks)
                {
                    cachedTargets[targetsFound] = target;
                    targetsFound++;
                }

                if (weapon.projectilePrefab != null)
                {
                    for (int i = 0; i < 1 + stacks; i++)
                    {
                        Enemy t = cachedTargets[i];
                        if (t == null || t.IsDead || !t.IsTargetable) continue;

                        Projectile projectile = Projectile.Spawn(weapon.projectilePrefab, GetMuzzlePosition());
                        if (projectile != null)
                        {
                            projectile.InitWithStatusEffect(
                                t,
                                damage,
                                splashRadius,
                                GetTrailColor(weapon, effectType, definition.id),
                                definition.id,
                                effectType,
                                effectValue,
                                effectDuration,
                                isCrit
                            );
                            ConfigurePiercingIfActive(projectile, t);
                        }
                    }
                }

                for (int i = 0; i < cachedTargets.Length; i++) cachedTargets[i] = null;
                yield break;
            }

            // Frost Mage Shard Volley
            if (definition.id == "frost_mage" && RunModifierManager.Instance != null && RunModifierManager.Instance.HasBehavior("frost_mage", HeroBehaviorEffectType.ExtraProjectile))
            {
                int stacks = RunModifierManager.Instance.GetBehaviorStacks("frost_mage", HeroBehaviorEffectType.ExtraProjectile);
                for (int i = 0; i < cachedTargets.Length; i++) cachedTargets[i] = null;

                cachedTargets[0] = target;
                int targetsFound = 1;
                float rangeSqr = GetModifiedRange() * GetModifiedRange();
                var enemiesList = EnemyManager.All;
                for (int i = 0; i < enemiesList.Count && targetsFound < 1 + stacks; i++)
                {
                    Enemy enemy = enemiesList[i];
                    if (enemy == null || enemy == target || enemy.IsDead || !enemy.IsTargetable) continue;

                    float distSqr = (enemy.transform.position - transform.position).sqrMagnitude;
                    if (distSqr <= rangeSqr)
                    {
                        cachedTargets[targetsFound] = enemy;
                        targetsFound++;
                    }
                }
                while (targetsFound < 1 + stacks)
                {
                    cachedTargets[targetsFound] = target;
                    targetsFound++;
                }

                if (weapon.projectilePrefab != null)
                {
                    for (int i = 0; i < 1 + stacks; i++)
                    {
                        Enemy t = cachedTargets[i];
                        if (t == null || t.IsDead || !t.IsTargetable) continue;

                        Projectile projectile = Projectile.Spawn(weapon.projectilePrefab, GetMuzzlePosition());
                        if (projectile != null)
                        {
                            projectile.InitWithStatusEffect(
                                t,
                                damage,
                                splashRadius,
                                GetTrailColor(weapon, effectType, definition.id),
                                definition.id,
                                effectType,
                                effectValue,
                                effectDuration,
                                isCrit
                            );
                        }
                    }
                }

                for (int i = 0; i < cachedTargets.Length; i++) cachedTargets[i] = null;
                yield break;
            }

            if (weapon.projectilePrefab != null)
            {
                Projectile projectile = Projectile.Spawn(weapon.projectilePrefab, GetMuzzlePosition());
                if (projectile != null)
                {
                    projectile.InitWithStatusEffect(
                        target,
                        damage,
                        splashRadius,
                        GetTrailColor(weapon, effectType, definition.id),
                        definition.id,
                        effectType,
                        effectValue,
                        effectDuration,
                        isCrit
                    );
                    ConfigurePiercingIfActive(projectile, target);
                }
                yield break;
            }

            float appliedDamage = target.TakeDamage(damage, false, isCrit, definition.id);
            DamageTracker.RecordDamage(definition.id, appliedDamage);

            if (isCrit && RelicManager.Instance != null && Castle.Instance != null)
            {
                float vamp = RelicManager.Instance.GetCastleVampirismPercent();
                if (vamp > 0f)
                {
                    int heal = Mathf.Max(1, Mathf.RoundToInt(appliedDamage * vamp));
                    Castle.Instance.Repair(heal);
                }
            }

            if (effectType != StatusEffectType.None && effectDuration > 0f && !target.IsDead)
            {
                target.ApplyStatusEffect(new StatusEffect(effectType, effectValue, effectDuration, definition.id));
            }
        }

        private void ConfigurePiercingIfActive(Projectile projectile, Enemy target)
        {
            if (projectile == null || target == null || definition.id != "archer" || RunModifierManager.Instance == null)
            {
                return;
            }

            int stacks = RunModifierManager.Instance.GetBehaviorStacks("archer", HeroBehaviorEffectType.Piercing);
            if (stacks <= 0)
            {
                return;
            }

            float reduction = RunModifierManager.Instance.GetBehaviorSecondaryValue("archer", HeroBehaviorEffectType.Piercing) / stacks;
            Vector3 direction = target.transform.position - GetMuzzlePosition();
            direction.y = 0f;
            projectile.ConfigurePiercing(stacks, direction, reduction);
        }


        private static Color GetTrailColor(WeaponDefinition weapon, StatusEffectType resolvedEffectType, string heroId)
        {
            if (weapon.attackType == AttackType.Splash)
            {
                return new Color(1f, 0.5f, 0.15f, 1f);
            }

            switch (resolvedEffectType)
            {
                case StatusEffectType.Slow:
                    return new Color(0.3f, 0.9f, 1f, 1f);
                case StatusEffectType.Burn:
                    return new Color(1f, 0.25f, 0.05f, 1f);
                case StatusEffectType.Shock:
                    return new Color(1f, 0.95f, 0.15f, 1f);
                case StatusEffectType.Poison:
                    return new Color(0.25f, 0.95f, 0.35f, 1f);
                default:
                    if (heroId == "sniper") return new Color(0.85f, 0.3f, 1f, 1f);
                    if (heroId == "archer") return new Color(0.45f, 0.95f, 0.3f, 1f);
                    if (heroId == "plague_doctor") return new Color(0.25f, 0.95f, 0.35f, 1f);
                    if (heroId == "radiant_paladin") return new Color(1f, 0.88f, 0.2f, 1f);
                    return new Color(1f, 0.88f, 0.4f, 1f);
            }
        }

        private Vector3 GetMuzzlePosition()
        {
            ArtAdapter adapter = GetComponent<ArtAdapter>();
            if (adapter != null && adapter.muzzleTransform != null)
            {
                return adapter.muzzleTransform.position;
            }
            return transform.position + projectileLaunchOffset;
        }

        private Vector3 GetAbilityOriginPosition()
        {
            ArtAdapter adapter = GetComponent<ArtAdapter>();
            if (adapter != null && adapter.abilityOrigin != null)
            {
                return adapter.abilityOrigin.position;
            }
            return transform.position + projectileLaunchOffset;
        }
    }
}
