using UnityEngine;

namespace Stonehold
{
    [CreateAssetMenu(fileName = "BattlefieldDefenseDefinition", menuName = "Stonehold/Expansion/Battlefield Defense Definition")]
    public class BattlefieldDefenseDefinition : BattlefieldContentDefinition
    {
        [Min(0f)] public float health;
        [Min(0f)] public float armor;
        [Min(1)] public int maxActive = 1;
    }
}
