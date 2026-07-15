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
        private float targetRefreshTimer;
        private float fireCooldown;
        private float abilityCooldown;
        private ProceduralAnimator animator;
        private TargetingMode currentTargetingMode;

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

                float duration = Mathf.Max(1f, definition.abilityCooldown);
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
            }
        }

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
            abilityCooldown = definition != null ? definition.abilityCooldown * 0.5f : 0f;
            enabled = definition != null && definition.weapon != null;
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

            if (targetRefreshTimer <= 0f)
            {
                currentTarget = EnemyManager.FindTarget(transform.position, GetModifiedRange(), currentTargetingMode);
                targetRefreshTimer = Mathf.Max(0.05f, targetRefreshInterval);
            }

            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                return;
            }

            if (definition.abilityType != HeroAbilityType.None && abilityCooldown <= 0f)
            {
                UseSignatureAbility(currentTarget);
                abilityCooldown = Mathf.Max(1f, definition.abilityCooldown);
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
                VfxManager.Instance.PlayHeroAbilityCast(transform.position + projectileLaunchOffset, definition.id);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayHeroImpact(definition.id, true);
            }

            float abilityDamage = GetModifiedDamage() * Mathf.Max(1f, definition.abilityPowerMultiplier);
            switch (definition.abilityType)
            {
                case HeroAbilityType.PowerShot:
                    UsePowerShotAbility(primaryTarget, abilityDamage);
                    break;
                case HeroAbilityType.FrostNova:
                    HitEnemiesInRadius(primaryTarget.transform.position, abilityDamage, StatusEffectType.Slow, 0.2f, 4f);
                    if (VfxManager.Instance != null) VfxManager.Instance.PlayFrost(primaryTarget.transform.position);
                    break;
                case HeroAbilityType.FlameWave:
                    HitEnemiesInRadius(primaryTarget.transform.position, abilityDamage, StatusEffectType.Burn, abilityDamage * 0.22f, 4f);
                    if (VfxManager.Instance != null) VfxManager.Instance.PlayFireImpact(primaryTarget.transform.position);
                    break;
                case HeroAbilityType.ArtilleryBarrage:
                    FireArtilleryBarrageBomb(primaryTarget, abilityDamage);
                    break;
                case HeroAbilityType.MultiShot:
                    FireMultiShotVolley(abilityDamage);
                    break;
                case HeroAbilityType.ChainStorm:
                    UseChainLightningAbility(primaryTarget, abilityDamage, definition.abilityTargetCount);
                    break;
            }
        }

        private void UsePowerShotAbility(Enemy primaryTarget, float damage)
        {
            Vector3 start = transform.position + projectileLaunchOffset;
            Vector3 direction = (primaryTarget.transform.position - start).normalized;
            direction.y = 0f;
            Vector3 end = start + direction * GetModifiedRange();

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayAbilityTrace(start, end, definition.id, 0.18f);
            }

            var all = EnemyManager.All;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                Enemy enemy = all[i];
                if (enemy == null || enemy.IsDead) continue;

                float distToLine = DistanceToLineSegment(enemy.transform.position, start, end);
                if (distToLine <= 1.2f)
                {
                    ApplyAbilityHit(enemy, damage, StatusEffectType.None, 0f, 0f);
                    if (VfxManager.Instance != null)
                    {
                        VfxManager.Instance.PlaySniperImpact(enemy.transform.position);
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
                Projectile projectile = Projectile.Spawn(weapon.projectilePrefab, transform.position + projectileLaunchOffset);
                if (projectile != null)
                {
                    projectile.transform.localScale = Vector3.one * 2.2f;
                    projectile.InitWithStatusEffect(
                        target,
                        damage,
                        definition.abilityRadius,
                        new Color(1f, 0.45f, 0.1f, 1f),
                        definition.id,
                        StatusEffectType.None,
                        0f,
                        0f
                    );
                }
            }
        }

        private void FireMultiShotVolley(float damage)
        {
            int targetsCount = Mathf.Max(1, definition.abilityTargetCount);
            float rangeSqr = GetModifiedRange() * GetModifiedRange();
            var enemies = EnemyManager.All;
            List<Enemy> targetList = new List<Enemy>();

            for (int i = 0; i < enemies.Count && targetList.Count < targetsCount; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy != null && !enemy.IsDead && (enemy.transform.position - transform.position).sqrMagnitude <= rangeSqr)
                {
                    targetList.Add(enemy);
                }
            }

            WeaponDefinition weapon = definition.weapon;
            if (weapon != null && weapon.projectilePrefab != null)
            {
                foreach (Enemy targetEnemy in targetList)
                {
                    Projectile projectile = Projectile.Spawn(weapon.projectilePrefab, transform.position + projectileLaunchOffset);
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
                            0f
                        );
                    }
                }
            }
        }

        private void UseChainLightningAbility(Enemy primaryTarget, float damage, int bounceCount)
        {
            Vector3 startSource = transform.position + projectileLaunchOffset;
            Enemy current = primaryTarget;
            List<Enemy> hitList = new List<Enemy>();

            for (int b = 0; b < bounceCount && current != null; b++)
            {
                hitList.Add(current);
                ApplyAbilityHit(current, damage, StatusEffectType.Shock, 1f, 4f);

                Vector3 endPos = current.transform.position + Vector3.up * 0.25f;
                if (VfxManager.Instance != null)
                {
                    VfxManager.Instance.PlayAbilityTrace(startSource, endPos, "electric_engineer", 0.12f);
                    VfxManager.Instance.PlayShockImpact(current.transform.position);
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayHeroImpact("electric_engineer", true);
                }

                startSource = endPos;
                current = FindNextChainTarget(current.transform.position, hitList, 5.0f);
            }
        }

        private void UseChainLightningBasic(Enemy primaryTarget, float damage)
        {
            Vector3 startSource = transform.position + projectileLaunchOffset;
            Enemy current = primaryTarget;
            List<Enemy> hitList = new List<Enemy>();

            // Bounces up to 2 times (primary + 1 extra)
            for (int b = 0; b < 2 && current != null; b++)
            {
                hitList.Add(current);
                float appliedDamage = current.TakeDamage(damage);
                DamageTracker.RecordDamage(definition.id, appliedDamage);

                Vector3 endPos = current.transform.position + Vector3.up * 0.25f;
                if (VfxManager.Instance != null)
                {
                    VfxManager.Instance.PlayAbilityTrace(startSource, endPos, "electric_engineer", 0.08f);
                    VfxManager.Instance.PlayShockImpact(current.transform.position);
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayHeroImpact("electric_engineer", false);
                }

                if (!current.IsDead)
                {
                    current.ApplyStatusEffect(new StatusEffect(StatusEffectType.Shock, 1f, 2.5f, definition.id));
                }

                startSource = endPos;
                current = FindNextChainTarget(current.transform.position, hitList, 4.5f);
            }
        }

        private Enemy FindNextChainTarget(Vector3 sourcePos, List<Enemy> hitList, float bounceRange)
        {
            Enemy best = null;
            float bestDistSqr = bounceRange * bounceRange;
            var all = EnemyManager.All;
            for (int i = 0; i < all.Count; i++)
            {
                Enemy enemy = all[i];
                if (enemy == null || enemy.IsDead || hitList.Contains(enemy)) continue;
                float distSqr = (enemy.transform.position - sourcePos).sqrMagnitude;
                if (distSqr <= bestDistSqr)
                {
                    bestDistSqr = distSqr;
                    best = enemy;
                }
            }
            return best;
        }

        private void HitEnemiesInRadius(Vector3 center, float damage, StatusEffectType effectType, float effectValue, float effectDuration)
        {
            float radiusSqr = definition.abilityRadius * definition.abilityRadius;
            var enemies = EnemyManager.All;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                if (enemy != null && !enemy.IsDead && (enemy.transform.position - center).sqrMagnitude <= radiusSqr)
                {
                    ApplyAbilityHit(enemy, damage, effectType, effectValue, effectDuration);
                }
            }
        }

        private void ApplyAbilityHit(Enemy enemy, float damage, StatusEffectType effectType, float effectValue, float effectDuration)
        {
            if (enemy == null || enemy.IsDead)
            {
                return;
            }

            float appliedDamage = enemy.TakeDamage(damage);
            DamageTracker.RecordDamage(definition.id, appliedDamage);
            if (effectType != StatusEffectType.None && effectDuration > 0f && !enemy.IsDead)
            {
                enemy.ApplyStatusEffect(new StatusEffect(effectType, effectValue, effectDuration, definition.id));
            }
        }

        private void Fire(Enemy target)
        {
            if (definition.id == "electric_engineer")
            {
                if (animator != null)
                {
                    animator.PlayAttack();
                }
                if (VfxManager.Instance != null)
                {
                    VfxManager.Instance.PlayHeroMuzzle(transform.position + projectileLaunchOffset, definition.id);
                }
                UseChainLightningBasic(target, GetModifiedDamage());
                return;
            }

            WeaponDefinition weapon = definition.weapon;
            if (animator != null)
            {
                animator.PlayAttack();
            }

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayHeroMuzzle(transform.position + projectileLaunchOffset, definition.id);
            }

            float splashRadius = weapon.attackType == AttackType.Splash ? weapon.splashRadius : 0f;

            StatusEffectType appliedEffectType = weapon.statusEffectType;
            float appliedEffectValue = weapon.statusEffectValue;
            float appliedEffectDuration = weapon.statusEffectDuration;

            if (appliedEffectType == StatusEffectType.Slow)
            {
                if (RunModifierManager.Instance != null)
                {
                    float slowStrength = RunModifierManager.Instance.GetSlowStrengthAdd(definition.id);
                    appliedEffectValue = Mathf.Clamp(appliedEffectValue - slowStrength, 0.05f, 0.95f);
                }
            }

            if (appliedEffectType == StatusEffectType.Burn)
            {
                if (RunModifierManager.Instance != null)
                {
                    appliedEffectValue += RunModifierManager.Instance.GetBurnDamageAdd(definition.id);
                }
            }

            if (appliedEffectType == StatusEffectType.None)
            {
                if (RunModifierManager.Instance != null)
                {
                    if (RunModifierManager.Instance.IsShockEnabled(definition.id))
                    {
                        appliedEffectType = StatusEffectType.Shock;
                        appliedEffectValue = 1f;
                        appliedEffectDuration = 3f;
                    }
                    else
                    {
                        float burnAdd = RunModifierManager.Instance.GetBurnDamageAdd(definition.id);
                        if (burnAdd > 0f)
                        {
                            appliedEffectType = StatusEffectType.Burn;
                            appliedEffectValue = burnAdd;
                            appliedEffectDuration = 3f;
                        }
                    }
                }
            }

            if (weapon.projectilePrefab != null)
            {
                Projectile projectile = Projectile.Spawn(weapon.projectilePrefab, transform.position + projectileLaunchOffset);
                if (projectile != null)
                {
                    projectile.InitWithStatusEffect(
                        target,
                        GetModifiedDamage(),
                        splashRadius,
                        GetTrailColor(weapon, appliedEffectType, definition.id),
                        definition.id,
                        appliedEffectType,
                        appliedEffectValue,
                        appliedEffectDuration
                    );
                }
                return;
            }

            float appliedDamage = target.TakeDamage(GetModifiedDamage());
            DamageTracker.RecordDamage(definition.id, appliedDamage);

            if (appliedEffectType != StatusEffectType.None && appliedEffectDuration > 0f)
            {
                target.ApplyStatusEffect(new StatusEffect(appliedEffectType, appliedEffectValue, appliedEffectDuration, definition.id));
            }
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
                default:
                    return heroId == "sniper"
                        ? new Color(0.85f, 0.3f, 1f, 1f)
                        : new Color(1f, 0.88f, 0.4f, 1f);
            }
        }
    }
}
