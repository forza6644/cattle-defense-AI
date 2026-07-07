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
        private TrailRenderer trail;
        private GameObject sourcePrefab;

        private static readonly Dictionary<GameObject, Queue<Projectile>> pools = new Dictionary<GameObject, Queue<Projectile>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            pools.Clear();
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
            }
            else
            {
                projectile.transform.position = position;
                projectile.gameObject.SetActive(true);
            }

            return projectile;
        }

        /// <summary>Called by the tower right after this projectile is spawned.</summary>
        public void Init(Enemy targetEnemy, float damageAmount, float splash, float slowMult, float slowDur, Color trailColor)
        {
            target = targetEnemy;
            damage = damageAmount;
            splashRadius = splash;
            slowMultiplier = slowMult;
            slowDuration = slowDur;

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
            }
        }

        private void Update()
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                Return();
                return;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                target.transform.position,
                speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.transform.position) <= hitDistance)
            {
                Impact();
                Return();
            }
        }

        private void Impact()
        {
            Vector3 impactPoint = target.transform.position;

            if (VfxManager.Instance != null)
            {
                if (splashRadius > 0f)
                {
                    VfxManager.Instance.PlayExplosion(impactPoint);
                }
                else if (slowMultiplier < 1f)
                {
                    VfxManager.Instance.PlayFrost(impactPoint);
                }
                else
                {
                    VfxManager.Instance.PlayHit(impactPoint);
                }
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayImpact(splashRadius > 0f, slowMultiplier < 1f);
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
                HitEnemy(target);
            }
        }

        private void HitEnemy(Enemy enemy)
        {
            if (slowMultiplier < 1f)
            {
                enemy.ApplySlow(slowMultiplier, slowDuration);
            }

            enemy.TakeDamage(damage);
        }

        private void Return()
        {
            target = null;

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
    }
}
