using UnityEngine;

namespace Stonehold
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Stonehold/Heroes/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        public AttackType attackType = AttackType.SingleTarget;
        public GameObject projectilePrefab;
        public float splashRadius;
        public StatusEffectType statusEffectType = StatusEffectType.None;
        public float statusEffectValue;
        public float statusEffectDuration;
    }
}
