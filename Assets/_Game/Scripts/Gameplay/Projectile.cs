using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// A shot fired by a tower. Flies straight at its target and, on contact,
    /// deals the damage it was given by the tower. Splash radius > 0 damages every
    /// registered enemy near the impact; a slow multiplier &lt; 1 also slows them.
    /// Splash iterates the EnemyManager registry — no scene scans, no allocations.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float hitDistance = 0.3f;

        private Enemy target;
        private float damage;
        private float splashRadius;
        private float slowMultiplier = 1f;
        private float slowDuration;

        /// <summary>Called by the tower right after this projectile is spawned.</summary>
        public void Init(Enemy targetEnemy, float damageAmount, float splash, float slowMult, float slowDur)
        {
            target = targetEnemy;
            damage = damageAmount;
            splashRadius = splash;
            slowMultiplier = slowMult;
            slowDuration = slowDur;
        }

        private void Update()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                target.transform.position,
                speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.transform.position) <= hitDistance)
            {
                Impact();
                Destroy(gameObject);
            }
        }

        private void Impact()
        {
            Vector3 impactPoint = target.transform.position;

            if (splashRadius > 0f)
            {
                // Backwards: HitEnemy can kill, which unregisters mid-iteration.
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
    }
}
