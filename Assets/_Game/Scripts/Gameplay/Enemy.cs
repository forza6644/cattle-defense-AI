using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Runtime component placed on every enemy prefab.
    /// Moves in a straight line toward the Castle. If a tower kills it, it awards
    /// gold (Kill). If it reaches the Castle instead, it damages the Castle and is
    /// removed with no reward.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float arriveDistance = 0.1f;
        [SerializeField] private int castleDamage = 1;
        [SerializeField] private int goldReward = 5;

        private Transform target;
        private Castle targetCastle;

        /// <summary>Called by the spawner right after this enemy is created.</summary>
        public void SetTarget(Transform castle)
        {
            target = castle;
            targetCastle = castle != null ? castle.GetComponent<Castle>() : null;
        }

        /// <summary>Called by a tower's projectile. Awards gold, then destroys the enemy.</summary>
        public void Kill()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddGold(goldReward);
            }

            Destroy(gameObject);
        }

        private void Update()
        {
            if (target == null)
            {
                return;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) <= arriveDistance)
            {
                if (targetCastle != null)
                {
                    targetCastle.TakeDamage(castleDamage);
                }

                Destroy(gameObject); // Reached the castle: no gold reward.
            }
        }
    }
}
