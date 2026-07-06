using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Central registry of alive enemies. Enemies register on spawn and unregister
    /// on death/despawn, so lookups (nearest target, alive count, iteration for
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
    }
}
