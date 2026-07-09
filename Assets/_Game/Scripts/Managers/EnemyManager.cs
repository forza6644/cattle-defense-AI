using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Central registry of alive enemies. Enemies register on spawn and unregister
    /// on death/despawn, so lookups (priority target, alive count, iteration for
    /// splash and health bars) are allocation-free — no scene scans, no per-frame
    /// garbage. The static API is backed by one list; the component on _Systems
    /// clears it on scene (re)start so no stale entries survive reloads.
    /// </summary>
    public class EnemyManager : MonoBehaviour
    {
        private static readonly List<Enemy> enemies = new List<Enemy>(64);

        /// <summary>All currently registered enemies. Do not cache across frames.</summary>
        public static IReadOnlyList<Enemy> All => enemies;

        public static int AliveCount => enemies.Count;

        private void Awake()
        {
            // Fresh run (scene load or reload, with or without domain reload).
            enemies.Clear();
        }

        public static void Register(Enemy enemy)
        {
            if (enemy != null && !enemies.Contains(enemy))
            {
                enemies.Add(enemy);
            }
        }

        public static void Unregister(Enemy enemy)
        {
            enemies.Remove(enemy);
        }

        public static int PruneInvalidEntries()
        {
            int removed = 0;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i] == null)
                {
                    enemies.RemoveAt(i);
                    removed++;
                }
            }

            return removed;
        }

        /// <summary>Nearest registered enemy within maxRange of position, or null.</summary>
        public static Enemy FindNearest(Vector3 position, float maxRange)
        {
            Enemy nearest = null;
            float bestDistance = maxRange;

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        /// <summary>Most advanced registered enemy within maxRange of position, or null.</summary>
        public static Enemy FindClosestToGoal(Vector3 position, float maxRange)
        {
            Enemy closestToGoal = null;
            float bestRemainingDistance = float.PositiveInfinity;
            float maxRangeSqr = maxRange * maxRange;

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy == null)
                {
                    continue;
                }

                float rangeSqr = (enemy.transform.position - position).sqrMagnitude;
                if (rangeSqr > maxRangeSqr)
                {
                    continue;
                }

                float remainingDistance = enemy.RemainingDistanceToTarget;
                if (remainingDistance < bestRemainingDistance)
                {
                    bestRemainingDistance = remainingDistance;
                    closestToGoal = enemy;
                }
            }

            return closestToGoal;
        }

        /// <summary>First registered enemy (oldest) within range.</summary>
        public static Enemy FindFirstInRange(Vector3 position, float maxRange)
        {
            float maxRangeSqr = maxRange * maxRange;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy == null) continue;

                float rangeSqr = (enemy.transform.position - position).sqrMagnitude;
                if (rangeSqr <= maxRangeSqr)
                {
                    return enemy;
                }
            }
            return null;
        }

        /// <summary>Last registered enemy (newest) within range.</summary>
        public static Enemy FindLastInRange(Vector3 position, float maxRange)
        {
            float maxRangeSqr = maxRange * maxRange;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                if (enemy == null) continue;

                float rangeSqr = (enemy.transform.position - position).sqrMagnitude;
                if (rangeSqr <= maxRangeSqr)
                {
                    return enemy;
                }
            }
            return null;
        }

        /// <summary>Enemy in range with highest current HP.</summary>
        public static Enemy FindStrongest(Vector3 position, float maxRange)
        {
            Enemy strongest = null;
            float bestHealth = -1f;
            float maxRangeSqr = maxRange * maxRange;

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy == null) continue;

                float rangeSqr = (enemy.transform.position - position).sqrMagnitude;
                if (rangeSqr > maxRangeSqr) continue;

                if (enemy.CurrentHealth > bestHealth)
                {
                    bestHealth = enemy.CurrentHealth;
                    strongest = enemy;
                }
            }
            return strongest;
        }

        /// <summary>Enemy in range with lowest current HP.</summary>
        public static Enemy FindWeakest(Vector3 position, float maxRange)
        {
            Enemy weakest = null;
            float bestHealth = float.PositiveInfinity;
            float maxRangeSqr = maxRange * maxRange;

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy == null) continue;

                float rangeSqr = (enemy.transform.position - position).sqrMagnitude;
                if (rangeSqr > maxRangeSqr) continue;

                if (enemy.CurrentHealth < bestHealth)
                {
                    bestHealth = enemy.CurrentHealth;
                    weakest = enemy;
                }
            }
            return weakest;
        }

        /// <summary>General query supporting all targeting modes.</summary>
        public static Enemy FindTarget(Vector3 position, float maxRange, TargetingMode mode)
        {
            switch (mode)
            {
                case TargetingMode.ClosestToGoal:
                    return FindClosestToGoal(position, maxRange);
                case TargetingMode.FirstInRange:
                    return FindFirstInRange(position, maxRange);
                case TargetingMode.LastInRange:
                    return FindLastInRange(position, maxRange);
                case TargetingMode.Strongest:
                    return FindStrongest(position, maxRange);
                case TargetingMode.Weakest:
                    return FindWeakest(position, maxRange);
                default:
                    return FindClosestToGoal(position, maxRange);
            }
        }
    }
}
