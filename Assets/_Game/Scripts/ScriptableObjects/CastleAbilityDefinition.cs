using UnityEngine;

namespace Stonehold
{
    public enum CastleAbilityType
    {
        CallMilitia,
        ArcaneMortar,
        FortressAegis
    }

    [CreateAssetMenu(fileName = "NewCastleAbility", menuName = "Stonehold/Castle Ability Definition")]
    public class CastleAbilityDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public CastleAbilityType abilityType = CastleAbilityType.ArcaneMortar;
        public float cooldown = 25f;
        public float energyCost = 30f;
        public float damage = 350f;
        public float radius = 5.0f;
        public float shieldAmount = 300f;
        public string iconBadge = "☄️";
        public Color themeColor = new Color(1f, 0.6f, 0.2f);
    }
}
