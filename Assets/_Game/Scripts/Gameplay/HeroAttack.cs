using UnityEngine;

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
                animator.PlayAttack();
            }

            if (VfxManager.Instance != null)
            {
                VfxManager.Instance.PlayHeroAbilityCast(transform.position + projectileLaunchOffset, definition.id);
            }

            float abilityDamage = GetModifiedDamage() * Mathf.Max(1f, definition.abilityPowerMultiplier);
            switch (definition.abilityType)
            {
                case HeroAbilityType.PowerShot:
                    if (VfxManager.Instance != null)
                    {
                        VfxManager.Instance.PlayAbilityTrace(
                            transform.position + projectileLaunchOffset,
                            primaryTarget.transform.position + Vector3.up * 0.25f,
                            definition.id,
                            0.16f);
                        VfxManager.Instance.PlaySniperImpact(primaryTarget.transform.position);
                    }
                    ApplyAbilityHit(primaryTarget, abilityDamage, StatusEffectType.None, 0f, 0f);
                    break;
                case HeroAbilityType.FrostNova:
                    HitEnemiesInRadius(primaryTarget.transform.position, abilityDamage, StatusEffectType.Slow, 0.25f, 3.5f);
                    if (VfxManager.Instance != null) VfxManager.Instance.PlayFrost(primaryTarget.transform.position);
                    break;
                case HeroAbilityType.FlameWave:
                    HitEnemiesInRadius(primaryTarget.transform.position, abilityDamage, StatusEffectType.Burn, abilityDamage * 0.2f, 4f);
                    if (VfxManager.Instance != null) VfxManager.Instance.PlayFireImpact(primaryTarget.transform.position);
                    break;
                case HeroAbilityType.ArtilleryBarrage:
                    HitEnemiesInRadius(primaryTarget.transform.position, abilityDamage, StatusEffectType.None, 0f, 0f);
                    if (VfxManager.Instance != null) VfxManager.Instance.PlayExplosion(primaryTarget.transform.position);
                    break;
                case HeroAbilityType.MultiShot:
                case HeroAbilityType.ChainStorm:
                    HitMultipleEnemies(
                        abilityDamage,
                        definition.abilityType == HeroAbilityType.ChainStorm ? StatusEffectType.Shock : StatusEffectType.None);
                    break;
            }
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

        private void HitMultipleEnemies(float damage, StatusEffectType effectType)
        {
            int hitsRemaining = Mathf.Max(1, definition.abilityTargetCount);
            float rangeSqr = GetModifiedRange() * GetModifiedRange();
            var enemies = EnemyManager.All;
            for (int i = 0; i < enemies.Count && hitsRemaining > 0; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy == null || enemy.IsDead || (enemy.transform.position - transform.position).sqrMagnitude > rangeSqr)
                {
                    continue;
                }

                ApplyAbilityHit(enemy, damage, effectType, effectType == StatusEffectType.Shock ? 1f : 0f, effectType == StatusEffectType.Shock ? 3f : 0f);
                if (VfxManager.Instance != null)
                {
                    VfxManager.Instance.PlayAbilityTrace(
                        transform.position + projectileLaunchOffset,
                        enemy.transform.position + Vector3.up * 0.25f,
                        definition.id,
                        effectType == StatusEffectType.Shock ? 0.11f : 0.07f);
                    if (effectType == StatusEffectType.Shock)
                    {
                        VfxManager.Instance.PlayShockImpact(enemy.transform.position);
                    }
                    else
                    {
                        VfxManager.Instance.PlayHit(enemy.transform.position, VfxManager.GetHeroColor(definition.id));
                    }
                }
                hitsRemaining--;
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
