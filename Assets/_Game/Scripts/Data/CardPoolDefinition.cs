using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    public enum RecruitOptionPolicy
    {
        Weighted = 0,
        GuaranteeWhileAvailable = 1
    }

    [Serializable]
    public sealed class CardPoolEntry
    {
        public CardDefinition card;
        public CardRarity rarity = CardRarity.Common;
        [Min(0.01f)] public float weight = 1f;
    }

    [CreateAssetMenu(fileName = "CardPoolDefinition", menuName = "Stonehold/Cards/Card Pool Definition")]
    public sealed class CardPoolDefinition : ScriptableObject
    {
        public string stableId;
        public string displayName;
        [TextArea(2, 5)] public string description;
        public string startingHeroId;
        public string[] supportedHeroIds = { "archer", "bombardier", "frost_mage", "electric_engineer" };
        [Min(1)] public int expectedCardCount = 18;
        public RecruitOptionPolicy recruitOptionPolicy = RecruitOptionPolicy.GuaranteeWhileAvailable;
        public CardCategory[] allowedCategories = { CardCategory.Modifier, CardCategory.RecruitHero, CardCategory.HeroUpgrade };
        public CardRarity[] allowedRarities = { CardRarity.Common, CardRarity.Rare, CardRarity.Epic };
        public List<CardPoolEntry> cards = new List<CardPoolEntry>();
    }
}
