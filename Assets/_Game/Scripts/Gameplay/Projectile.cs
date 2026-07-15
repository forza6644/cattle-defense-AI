using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// A shot fired by a tower. Flies straight at its target and, on contact, deals
    /// its damage (splash and slow included) and asks the VfxManager/AudioManager for
    /// the right impact effect. Pooled per prefab (Spawn/Return) so firing never
    /// allocates or destroys at runtime.
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float hitDistance = 0.3f;

        private Enemy target;
        private float damage;
        private float splashRadius;
        private float slowMultiplier = 1f;
        private float slowDuration;
        private string sourceHeroId;
        private TrailRenderer trail;
        private GameObject sourcePrefab;
        private Color impactColor = Color.white;
        private bool isCrit;

        private StatusEffectType statusEffectType = StatusEffectType.None;
        private float statusEffectValue;
        private float statusEffectDuration;

        private Vector3 baseScale;
        private Vector3 targetLastPosition;
        private Vector3 startPosition;
        private float travelTime;
        private float elapsedTravelTime;
        private bool useArc;

        private static readonly Dictionary<GameObject, Queue<Projectile>> pools = new Dictionary<GameObject, Queue<Projectile>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            pools.Clear();
        }

        private void Awake()
        {
            baseScale = transform.localScale;
            trail = GetComponent<TrailRenderer>();
        }

        /// <summary>Gets a pooled projectile (or instantiates one) at the position.</summary>
        public static Projectile Spawn(GameObject prefab, Vector3 position)
        {
            Projectile projectile = null;
            if (pools.TryGetValue(prefab, out Queue<Projectile> pool) && pool.Count > 0)
            {
                projectile = pool.Dequeue();
            }

            if (projectile == null)
            {
                GameObject go = Instantiate(prefab, position, Quaternion.identity);
                projectile = go.GetComponent<Projectile>();
                projectile.sourcePrefab = prefab;
                projectile.baseScale = go.transform.localScale;
            }
            else
            {
                projectile.transform.position = position;
                projectile.transform.localScale = projectile.baseScale;
                projectile.gameObject.SetActive(true);
            }

            return projectile;
        }

        /// <summary>Called by the tower right after this projectile is spawned.</summary>
        public void Init(Enemy targetEnemy, float damageAmount, float splash, float slowMult, float slowDur, Color trailColor)
        {
            Init(targetEnemy, damageAmount, splash, slowMult, slowDur, trailColor, null, false);
        }

        public void Init(Enemy targetEnemy, float damageAmount, float splash, float slowMult, float slowDur, Color trailColor, string damageSourceHeroId, bool isCritical = false)
        {
            speed = (damageSourceHeroId == "sniper") ? 60f : 12f;
            target = targetEnemy;
            damage = damageAmount;
            splashRadius = splash;
            slowMultiplier = slowMult;
            slowDuration = slowDur;
            sourceHeroId = damageSourceHeroId;
            impactColor = trailColor;
            isCrit = isCritical;

            targetLastPosition = target != null ? GetTargetPosition(target) : transform.position;
            startPosition = transform.position;
            useArc = (damageSourceHeroId == "bombardier");
            if (useArc)
            {
                float distance = Vector3.Distance(startPosition, targetLastPosition);
                travelTime = Mathf.Max(0.1f, distance / speed);
                elapsedTravelTime = 0f;
            }

            if (slowMult < 1f)
            {
                statusEffectType = StatusEffectType.Slow;
                statusEffectValue = slowMult;
                statusEffectDuration = slowDur;
            }
            else
            {
                statusEffectType = StatusEffectType.None;
                statusEffectValue = 0f;
                statusEffectDuration = 0f;
            }

            if (trail == null)
            {
                trail = GetComponent<TrailRenderer>();
            }

            if (trail != null)
            {
                trail.Clear();
                trail.emitting = true;
                trail.startColor = trailColor;
                Color end = trailColor;
                end.a = 0f;
                trail.endColor = end;
                trail.startWidth = GetTrailWidth();
                trail.endWidth = 0.05f;
                trail.time = 0.4f;
            }
        }

        public void InitWithStatusEffect(
            Enemy targetEnemy,
            float damageAmount,
            float splash,
            Color trailColor,
            string damageSourceHeroId,
            StatusEffectType effectType,
            float effectValue,
            float effectDuration,
            bool isCritical = false)
        {
            speed = (damageSourceHeroId == "sniper") ? 60f : 12f;
            target = targetEnemy;
            damage = damageAmount;
            splashRadius = splash;
            sourceHeroId = damageSourceHeroId;
            statusEffectType = effectType;
            statusEffectValue = effectValue;
            statusEffectDuration = effectDuration;
            impactColor = trailColor;
            isCrit = isCritical;

            targetLastPosition = target != null ? target.transform.position : transform.position;
            startPosition = transform.position;
            useArc = (damageSourceHeroId == "bombardier");
            if (useArc)
            {
                float distance = Vector3.Distance(startPosition, targetLastPosition);
                travelTime = Mathf.Max(0.1f, distance / speed);
                elapsedTravelTime = 0f;
            }

            if (effectType == StatusEffectType.Slow)
            {
                slowMultiplier = effectValue;
                slowDuration = effectDuration;
            }
            else
            {
                slowMultiplier = 1f;
                slowDuration = 0f;
            }

            if (trail == null)
            {
                trail = GetComponent<TrailRenderer>();
            }

            if (trail != null)
            {
                trail.Clear();
                trail.emitting = true;
                trail.startColor = trailColor;
                Color end = trailColor;
                end.a = 0f;
                trail.endColor = end;
                trail.startWidth = GetTrailWidth();
                trail.endWidth = 0.05f;
                trail.time = 0.4f;
            }
        }

        private void Update()
        {
            if (target != null && target.gameObject.activeInHierarchy && !target.IsDead)
            {
                targetLastPosition = GetTargetPosition(target);
            }

            Vector3 dest = targetLastPosition;

            if (useArc)
            {
                elapsedTravelTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTravelTime / travelTime);

                Vector3 currentPos = Vector3.Lerp(startPosition, dest, t);
                float arcHeight = 2.4f;
                float height = Mathf.Sin(t * Mathf.PI) * arcHeight;
                currentPos.y += height;

                transform.position = currentPos;

                if (t >= 1.0f)
                {
                    Impact(dest);
                    Return();
                }
            }
            else
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    dest,
                    speed * Time.deltaTime);

                if (Vector3.Distance(transform.position, dest) <= hitDistance)
                {
                    Impact(dest);
                    Return();
                }
            }
        }

        private void Impact(Vector3 impactPoint)
        {
            if (VfxManager.Instance != null)
            {
                if (splashRadius > 0f)
                {
                    VfxManager.Instance.PlayExplosion(impactPoint);
                }
                else if (statusEffectType == StatusEffectType.Slow)
                {
                    VfxManager.Instance.PlayFrost(impactPoint);
                }
                else if (statusEffectType == StatusEffectType.Burn)
                {
                    VfxManager.Instance.PlayFireImpact(impactPoint);
                }
                else if (statusEffectType == StatusEffectType.Shock)
                {
                    VfxManager.Instance.PlayShockImpact(impactPoint);
                }
                else if (sourceHeroId == "sniper")
                {
                    VfxManager.Instance.PlaySniperImpact(impactPoint);
                }
                else
                {
                    VfxManager.Instance.PlayHit(impactPoint, impactColor);
                }
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayHeroImpact(sourceHeroId, false);
            }

            if (splashRadius > 0f)
            {
                var all = EnemyManager.All;
                for (int i = all.Count - 1; i >= 0; i--)
                {
                    if (i >= all.Count)
                    {
                        continue;
                    }

                    Enemy enemy = all[i];
                    if (enemy != null && Vector3.Distance(impactPoint, enemy.transform.position) <= splashRadius)
                    {
                        HitEnemy(enemy);
                    }
                }
            }
            else
            {
                if (target != null && target.gameObject.activeInHierarchy && !target.IsDead)
                {
                    HitEnemy(target);
                }
            }
        }

        private float GetTrailWidth()
        {
            if (splashRadius > 0f) return 0.55f;
            if (statusEffectType == StatusEffectType.Burn) return 0.48f;
            if (statusEffectType == StatusEffectType.Slow) return 0.42f;
            if (statusEffectType == StatusEffectType.Shock) return 0.4f;
            if (sourceHeroId == "sniper") return 0.24f;
            return 0.34f;
        }

        private void HitEnemy(Enemy enemy)
        {
            if (enemy == null || enemy.IsDead) return;
            float appliedDamage = enemy.TakeDamage(damage, false, isCrit);
            DamageTracker.RecordDamage(sourceHeroId, appliedDamage);

            if (statusEffectType != StatusEffectType.None && statusEffectDuration > 0f)
            {
                enemy.ApplyStatusEffect(new StatusEffect(statusEffectType, statusEffectValue, statusEffectDuration, sourceHeroId));
            }
        }

        private void Return()
        {
            target = null;
            sourceHeroId = null;

            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }

            gameObject.SetActive(false);

            if (sourcePrefab == null)
            {
                Destroy(gameObject);
                return;
            }

            if (!pools.TryGetValue(sourcePrefab, out Queue<Projectile> pool))
            {
                pool = new Queue<Projectile>();
                pools[sourcePrefab] = pool;
            }

            pool.Enqueue(this);
        }

        private Vector3 GetTargetPosition(Enemy targetEnemy)
        {
            if (targetEnemy == null) return targetLastPosition;
            ArtAdapter adapter = targetEnemy.GetComponent<ArtAdapter>();
            if (adapter != null && adapter.impactPoint != null)
            {
                return adapter.impactPoint.position;
            }
            return targetEnemy.transform.position;
        }
    }
}
