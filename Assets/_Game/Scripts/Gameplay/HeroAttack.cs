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
                currentTarget = EnemyManager.FindNearest(transform.position, definition.baseRange);
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
                fireCooldown = definition.baseFireRate > 0f ? 1f / definition.baseFireRate : 1f;
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

            if (weapon.projectilePrefab != null)
            {
                Projectile projectile = Projectile.Spawn(weapon.projectilePrefab, transform.position + projectileLaunchOffset);
                if (projectile != null)
                {
                    projectile.InitWithStatusEffect(
                        target,
                        definition.baseDamage,
                        splashRadius,
                        GetTrailColor(weapon),
                        definition.id,
                        weapon.statusEffectType,
                        weapon.statusEffectValue,
                        weapon.statusEffectDuration
                    );
                }
                return;
            }

            if (weapon.statusEffectType != StatusEffectType.None && weapon.statusEffectDuration > 0f)
            {
                target.ApplyStatusEffect(new StatusEffect(weapon.statusEffectType, weapon.statusEffectValue, weapon.statusEffectDuration, definition.id));
            }

            float appliedDamage = target.TakeDamage(definition.baseDamage);
            DamageTracker.RecordDamage(definition.id, appliedDamage);
        }

        private static Color GetTrailColor(WeaponDefinition weapon)
        {
            if (weapon.attackType == AttackType.Splash)
            {
                return new Color(1f, 0.55f, 0.2f, 1f);
            }

            switch (weapon.statusEffectType)
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
