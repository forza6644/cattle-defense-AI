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
        private ProceduralAnimator animator;

        public HeroDefinition Definition => definition;

        private void Awake()
        {
            animator = GetComponent<ProceduralAnimator>();
        }

        public void Configure(HeroDefinition heroDefinition)
        {
            definition = heroDefinition;
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

            if (targetRefreshTimer <= 0f)
            {
                currentTarget = EnemyManager.FindNearest(transform.position, GetModifiedRange());
                targetRefreshTimer = Mathf.Max(0.05f, targetRefreshInterval);
            }

            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                return;
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

        private void Fire(Enemy target)
        {
            WeaponDefinition weapon = definition.weapon;
            if (animator != null)
            {
                animator.PlayAttack();
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
                        GetTrailColor(weapon, appliedEffectType),
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

        private static Color GetTrailColor(WeaponDefinition weapon, StatusEffectType resolvedEffectType)
        {
            if (weapon.attackType == AttackType.Splash)
            {
                return new Color(1f, 0.55f, 0.2f, 1f);
            }

            switch (resolvedEffectType)
            {
                case StatusEffectType.Slow:
                    return new Color(0.5f, 0.85f, 1f, 1f);
                case StatusEffectType.Burn:
                    return new Color(1f, 0.3f, 0.1f, 1f);
                case StatusEffectType.Shock:
                    return new Color(0.9f, 0.9f, 0.2f, 1f);
                default:
                    return new Color(1f, 0.95f, 0.55f, 1f);
            }
        }
    }
}
