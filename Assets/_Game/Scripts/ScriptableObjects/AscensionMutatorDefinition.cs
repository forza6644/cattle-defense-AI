using System;
using UnityEngine;

namespace Stonehold
{
    public enum AscensionMutatorType
    {
        EnemySpeedMultiplier = 0,
        EnemyArmorBonus = 1,
        EnemyRegenPercent = 2,
        GoldRewardMultiplier = 3,
        EliteExtraAffixAndHealth = 4,
        CastleMaxHealthPenalty = 5,
        WaveCountdownReduction = 6,
        NullifierShieldChance = 7
    }

    [CreateAssetMenu(fileName = "NewAscensionMutator", menuName = "Stonehold/Ascension Mutator")]
    public class AscensionMutatorDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public int heatPoints = 1;
        public float scoreMultiplierBonus = 0.10f;
        public Color themeColor = new Color(1f, 0.45f, 0.15f, 1f);
        public AscensionMutatorType mutatorType;
        public float effectValue;
    }
}
