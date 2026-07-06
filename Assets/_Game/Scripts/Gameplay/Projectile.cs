using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Prototype projectile: flies in a straight line toward its target enemy and,
    /// on contact, kills the enemy instantly (awarding gold) and destroys itself.
    /// If the target is already gone, the projectile removes itself.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float hitDistance = 0.3f;

        private Enemy target;

        /// <summary>Called by the tower right after this projectile is spawned.</summary>
        public void SetTarget(Enemy enemy)
        {
            target = enemy;
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
                target.Kill();
                Destroy(gameObject);
            }
        }
    }
}
