using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    public enum MetaUpgradeEffectType
    {
        CastleHp,
        CastleRegen,
        GlobalDamage,
        GlobalFireRate,
        GlobalRange,
        GoldBonus
    }

    [System.Serializable]
    public class MetaUpgrade
    {
        public string id;
        public string displayName;
        public string description;
        public int currentLevel;
        public int maxLevel;
        public int baseCost;
        public float costGrowth;
        public MetaUpgradeEffectType effectType;
        public float effectValuePerLevel;

        public MetaUpgrade(string id, string name, string desc, int maxLvl, int baseCst, float growth, MetaUpgradeEffectType type, float val)
        {
            this.id = id;
            this.displayName = name;
            this.description = desc;
            this.maxLevel = maxLvl;
            this.baseCost = baseCst;
            this.costGrowth = growth;
            this.effectType = type;
            this.effectValuePerLevel = val;
            this.currentLevel = 0;
        }

        public int GetCost()
        {
            if (currentLevel >= maxLevel) return 0;
            return Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, currentLevel));
        }
    }

    /// <summary>
    /// Singleton manager that governs permanent meta progression upgrades.
    /// Loaded on Awake/Start, and governs active runs.
    /// </summary>
    public class MetaUpgradeManager : MonoBehaviour
    {
        public static MetaUpgradeManager Instance { get; private set; }

        private readonly List<MetaUpgrade> upgrades = new List<MetaUpgrade>();

        public IReadOnlyList<MetaUpgrade> Upgrades => upgrades;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeUpgrades();
            LoadUpgrades();
        }

        private void InitializeUpgrades()
        {
            upgrades.Clear();
            upgrades.Add(new MetaUpgrade("castle_hp", "Castle Fortification", "+10 Castle HP per level", 10, 100, 1.5f, MetaUpgradeEffectType.CastleHp, 10f));
            upgrades.Add(new MetaUpgrade("damage", "Crystal Attack", "+15% Damage per level", 10, 150, 1.5f, MetaUpgradeEffectType.GlobalDamage, 0.15f));
            upgrades.Add(new MetaUpgrade("gold_bonus", "MetaGold Bonus", "+10% MetaGold per level", 10, 120, 1.5f, MetaUpgradeEffectType.GoldBonus, 0.10f));
            upgrades.Add(new MetaUpgrade("castle_regen", "Castle Regeneration", "+1 HP every 5 seconds", 10, 125, 1.5f, MetaUpgradeEffectType.CastleRegen, 1f));
            upgrades.Add(new MetaUpgrade("fire_rate", "Faster Defenders", "+3% Fire Rate per level", 10, 150, 1.5f, MetaUpgradeEffectType.GlobalFireRate, 0.03f));
            upgrades.Add(new MetaUpgrade("range", "Longer Watch", "+3% Range per level", 10, 120, 1.5f, MetaUpgradeEffectType.GlobalRange, 0.03f));
        }

        public void LoadUpgrades()
        {
            foreach (var u in upgrades)
            {
                u.currentLevel = SaveManager.GetUpgradeLevel(u.id);
            }
            Debug.Log("[MetaUpgradeManager] Meta upgrades loaded from PlayerPrefs.");
        }

        public bool PurchaseUpgrade(string id)
        {
            var u = upgrades.Find(x => x.id == id);
            if (u == null) return false;

            int cost = u.GetCost();
            if (u.currentLevel < u.maxLevel && SaveManager.MetaGold >= cost)
            {
                if (SaveManager.TryPurchaseUpgrade(u.id, cost))
                {
                    u.currentLevel = SaveManager.GetUpgradeLevel(u.id);
                    Debug.Log($"[MetaUpgradeManager] Successfully upgraded {u.displayName} to level {u.currentLevel}.");
                    return true;
                }
            }
            return false;
        }

        public int GetCastleHpBonus()
        {
            var u = upgrades.Find(x => x.id == "castle_hp");
            if (u != null)
            {
                return Mathf.RoundToInt(u.currentLevel * u.effectValuePerLevel);
            }
            return 0;
        }

        public int GetCastleRegenPerTick()
        {
            var u = upgrades.Find(x => x.id == "castle_regen");
            int upgradeBonus = u != null
                ? Mathf.RoundToInt(u.currentLevel * u.effectValuePerLevel)
                : 0;
            return 1 + upgradeBonus;
        }

        public float GetGlobalDamageMultiplier()
        {
            var u = upgrades.Find(x => x.id == "damage");
            if (u != null)
            {
                return 1.0f + (u.currentLevel * u.effectValuePerLevel);
            }
            return 1.0f;
        }

        public float GetGlobalFireRateMultiplier()
        {
            var u = upgrades.Find(x => x.id == "fire_rate");
            if (u != null)
            {
                return 1.0f + (u.currentLevel * u.effectValuePerLevel);
            }
            return 1.0f;
        }

        public float GetGlobalRangeMultiplier()
        {
            var u = upgrades.Find(x => x.id == "range");
            if (u != null)
            {
                return 1.0f + (u.currentLevel * u.effectValuePerLevel);
            }
            return 1.0f;
        }

        public float GetGoldBonusMultiplier()
        {
            var u = upgrades.Find(x => x.id == "gold_bonus");
            if (u != null)
            {
                return 1.0f + (u.currentLevel * u.effectValuePerLevel);
            }
            return 1.0f;
        }
    }
}
