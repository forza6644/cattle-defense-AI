using UnityEngine;

namespace Stonehold
{
    public enum RelicRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public enum RelicEffectType
    {
        None = 0,
        CooldownReductionGlobal = 1,  // e.g. 0.20 = 20% faster ultimate cooldowns
        ElementalReactionBoost = 2,   // e.g. 0.35 = +35% elemental reaction damage & AoE
        CritCastleVampirism = 3,      // e.g. 0.05 = 5% of crit damage heals castle
        CastleShieldRecharge = 4,     // e.g. 150 = grants 150 HP recharging shield to castle
        EliteBossGoldBonus = 5,       // e.g. 0.30 = +30% gold from Elites and Bosses
        ShockChainBonus = 6,          // e.g. 2 = +2 extra shock chained targets
        SlowDamageAmp = 7,            // e.g. 0.20 = +20% damage to slowed/frozen foes
        SpectralMinionMastery = 8,    // e.g. 0.50 = +50% duration and +25% attack speed for minions
        ExecuteThreshold = 9          // e.g. 0.12 = automatically execute foes below 12% max HP
    }

    /// <summary>
    /// ScriptableObject defining a passive run-defining Relic/Artifact.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRelic", menuName = "Stonehold/Relic Definition")]
    public class RelicDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "relic_id";
        public string displayName = "Relic Name";
        [TextArea(2, 4)]
        public string description = "Relic passive effect description.";
        public RelicRarity rarity = RelicRarity.Rare;
        public Sprite icon;
        public Color themeColor = new Color(0.85f, 0.75f, 0.25f);

        [Header("Effect & Value")]
        public RelicEffectType effectType = RelicEffectType.None;
        public float effectValue = 0.2f;

        public Color GetRarityColor()
        {
            switch (rarity)
            {
                case RelicRarity.Common: return new Color(0.7f, 0.75f, 0.8f);
                case RelicRarity.Rare: return new Color(0.2f, 0.6f, 1f);
                case RelicRarity.Epic: return new Color(0.75f, 0.25f, 0.95f);
                case RelicRarity.Legendary: return new Color(1f, 0.7f, 0.1f);
                default: return Color.white;
            }
        }
    }
}
