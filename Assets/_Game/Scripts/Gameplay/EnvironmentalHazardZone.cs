using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    public enum HazardType
    {
        LavaFissure,
        ToxicMire,
        BlizzardStorm,
        VoidRift
    }

    /// <summary>
    /// Interactive environmental hazard zone deployed in lanes or boss arenas.
    /// Deals periodic damage and applies elemental status effects to moving units.
    /// </summary>
    public class EnvironmentalHazardZone : MonoBehaviour
    {
        [Header("Hazard Configuration")]
        public HazardType hazardType = HazardType.LavaFissure;
        public float radius = 3.0f;
        public float duration = 12.0f;
        public float tickInterval = 0.5f;
        public float damagePerTick = 15f;
        public StatusEffectType appliedEffect = StatusEffectType.Burn;
        public float effectDuration = 3.0f;

        private SphereCollider triggerCollider;
        private readonly HashSet<Enemy> activeEnemies = new HashSet<Enemy>();
        private Coroutine tickRoutine;

        private void Awake()
        {
            triggerCollider = GetComponent<SphereCollider>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<SphereCollider>();
            }
            triggerCollider.isTrigger = true;
            triggerCollider.radius = radius;
        }

        private void Start()
        {
            tickRoutine = StartCoroutine(HazardTickLoop());
            if (duration > 0f)
            {
                Destroy(gameObject, duration);
            }
        }

        private void OnDestroy()
        {
            if (tickRoutine != null)
            {
                StopCoroutine(tickRoutine);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null && !enemy.IsDead)
            {
                activeEnemies.Add(enemy);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                activeEnemies.Remove(enemy);
            }
        }

        private IEnumerator HazardTickLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(tickInterval);
                activeEnemies.RemoveWhere(e => e == null || e.IsDead);

                foreach (var enemy in activeEnemies)
                {
                    if (enemy != null && !enemy.IsDead)
                    {
                        enemy.TakeDamage(damagePerTick);
                        enemy.ApplyStatusEffect(new StatusEffect(appliedEffect, 1f, effectDuration, "Hazard"));
                    }
                }
            }
        }

        public void ApplyDirectTickToTarget(Enemy enemy)
        {
            if (enemy == null || enemy.IsDead) return;
            enemy.TakeDamage(damagePerTick);
            enemy.ApplyStatusEffect(new StatusEffect(appliedEffect, 1f, effectDuration, "Hazard"));
        }
    }
}
