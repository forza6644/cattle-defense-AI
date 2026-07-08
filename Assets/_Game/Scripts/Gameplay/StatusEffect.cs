using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Runtime model representing a status effect currently applied to an enemy.
    /// Supports Slow, Burn, and Shock.
    /// </summary>
    public class StatusEffect
    {
        public StatusEffectType EffectType { get; private set; }
        public float Value { get; set; }
        public float Duration { get; set; }
        public float RemainingTime { get; set; }
        public string SourceHeroId { get; set; }
        public float TickTimer { get; set; }

        public StatusEffect(StatusEffectType type, float value, float duration, string sourceHeroId = null)
        {
            EffectType = type;
            Value = value;
            Duration = duration;
            RemainingTime = duration;
            SourceHeroId = sourceHeroId;
            TickTimer = 1.0f; // Default burn/DoT tick interval: 1 second
        }
    }
}
