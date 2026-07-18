using UnityEngine;

namespace Stonehold
{
    [CreateAssetMenu(fileName = "TrapDefinition", menuName = "Stonehold/Expansion/Trap Definition")]
    public class TrapDefinition : BattlefieldContentDefinition
    {
        public TrapRuntimeType runtimeType;
        [Min(1)] public int maxActive = 1;
        [Min(1)] public int maxTicksPerEnemy = 20;
        [Min(0f)] public float ignitionDelay;
        [Min(0f)] public float burningDuration;
        [Min(0f)] public float retriggerCooldown;
    }
}
