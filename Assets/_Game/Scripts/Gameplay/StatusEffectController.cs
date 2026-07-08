using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Component placed on or attached to an Enemy to track, update, and apply status effects (Slow, Burn, Shock).
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class StatusEffectController : MonoBehaviour
    {
        private Enemy enemy;
        private readonly List<StatusEffect> activeEffects = new List<StatusEffect>();

        public IReadOnlyList<StatusEffect> ActiveEffects => activeEffects;

        private void Awake()
        {
            enemy = GetComponent<Enemy>();
        }

        /// <summary>
        /// Applies a status effect to the enemy.
        /// </summary>
        public void ApplyEffect(StatusEffect effect)
        {
            if (effect == null || effect.EffectType == StatusEffectType.None)
            {
                return;
            }

            switch (effect.EffectType)
            {
                case StatusEffectType.Slow:
                    HandleApplySlow(effect);
                    break;
                case StatusEffectType.Burn:
                    HandleApplyBurn(effect);
                    break;
                case StatusEffectType.Shock:
                    HandleApplyShock(effect);
                    break;
            }
        }

        private void HandleApplySlow(StatusEffect newEffect)
        {
            StatusEffect existingSlow = activeEffects.Find(e => e.EffectType == StatusEffectType.Slow);
            if (existingSlow != null)
            {
                // Rule: refresh duration if same or stronger slow (new slow multiplier <= existing slow multiplier)
                if (newEffect.Value <= existingSlow.Value)
                {
                    existingSlow.Value = newEffect.Value;
                    existingSlow.Duration = newEffect.Duration;
                    existingSlow.RemainingTime = newEffect.Duration;
                    existingSlow.SourceHeroId = newEffect.SourceHeroId;
                }
            }
            else
            {
                activeEffects.Add(newEffect);
            }
            UpdateEnemySlowMultiplier();
        }

        private void HandleApplyBurn(StatusEffect newEffect)
        {
            // Rule: refresh duration/value if same source hero
            StatusEffect existingBurn = activeEffects.Find(e => e.EffectType == StatusEffectType.Burn && e.SourceHeroId == newEffect.SourceHeroId);
            if (existingBurn != null)
            {
                existingBurn.Value = newEffect.Value; // Update Burn damage per tick
                existingBurn.Duration = newEffect.Duration;
                existingBurn.RemainingTime = newEffect.Duration;
            }
            else
            {
                activeEffects.Add(newEffect);
            }
        }

        private void HandleApplyShock(StatusEffect newEffect)
        {
            // Rule: refresh duration of shock
            StatusEffect existingShock = activeEffects.Find(e => e.EffectType == StatusEffectType.Shock);
            if (existingShock != null)
            {
                existingShock.Duration = newEffect.Duration;
                existingShock.RemainingTime = newEffect.Duration;
            }
            else
            {
                activeEffects.Add(newEffect);
            }
        }

        private void Update()
        {
            if (enemy == null || enemy.IsDead)
            {
                return;
            }

            bool slowChanged = false;

            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                StatusEffect effect = activeEffects[i];
                effect.RemainingTime -= Time.deltaTime;

                if (effect.RemainingTime <= 0f)
                {
                    if (effect.EffectType == StatusEffectType.Slow)
                    {
                        slowChanged = true;
                    }
                    activeEffects.RemoveAt(i);
                    continue;
                }

                // Handle Burn DoT tick (deals damage once per second)
                if (effect.EffectType == StatusEffectType.Burn)
                {
                    effect.TickTimer -= Time.deltaTime;
                    if (effect.TickTimer <= 0f)
                    {
                        effect.TickTimer = 1.0f; // Reset tick timer

                        float damageAmount = effect.Value;
                        float appliedDamage = enemy.TakeDamage(damageAmount);
                        DamageTracker.RecordDamage(effect.SourceHeroId, appliedDamage);
                    }
                }
            }

            if (slowChanged)
            {
                UpdateEnemySlowMultiplier();
            }
        }

        private void UpdateEnemySlowMultiplier()
        {
            if (enemy == null) return;

            StatusEffect activeSlow = activeEffects.Find(e => e.EffectType == StatusEffectType.Slow);
            if (activeSlow != null)
            {
                enemy.SlowMultiplier = activeSlow.Value;
                enemy.SlowTimer = activeSlow.RemainingTime;
            }
            else
            {
                enemy.SlowMultiplier = 1f;
                enemy.SlowTimer = 0f;
            }
        }

        /// <summary>
        /// Query if the enemy is currently shocked.
        /// </summary>
        public bool IsShocked()
        {
            return activeEffects.Exists(e => e.EffectType == StatusEffectType.Shock);
        }
    }
}
