using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    [Serializable]
    public class HeroRelicData
    {
        public string id;
        public string displayName;
        public RelicRarity rarity;
        public string description;
        public string iconBadge;
        public float reactionDamageBonus;
        public float critChanceBonus;
        public float castleDamageReduction;
        public float goldGainBonus;
        public float attackSpeedBonus;

        public Color RarityColor
        {
            get
            {
                switch (rarity)
                {
                    case RelicRarity.Common: return new Color(0.75f, 0.75f, 0.75f);
                    case RelicRarity.Rare: return new Color(0.25f, 0.65f, 1f);
                    case RelicRarity.Epic: return new Color(0.75f, 0.35f, 1f);
                    case RelicRarity.Legendary: return new Color(1f, 0.75f, 0.2f);
                    default: return Color.white;
                }
            }
        }
    }

    /// <summary>
    /// Manages collectible artifacts/relics, inventory unlocking, equipped slots,
    /// and aggregate stat multipliers for combat and economy.
    /// </summary>
    public class HeroArtifactManager : MonoBehaviour
    {
        public static HeroArtifactManager Instance { get; private set; }

        private const string KeyUnlockedRelics = "relics_unlocked_ids_csv";
        private const string KeyEquippedSlotPrefix = "relics_equipped_slot_";
        public const int MaxEquipSlots = 3;

        private static readonly HeroRelicData[] Catalog =
        {
            new HeroRelicData
            {
                id = "relic_pyromancer_crown",
                displayName = "Crown of the Pyromancer",
                rarity = RelicRarity.Epic,
                description = "+25% Elemental Reaction Damage",
                iconBadge = "👑",
                reactionDamageBonus = 0.25f
            },
            new HeroRelicData
            {
                id = "relic_vanguard_aegis",
                displayName = "Aegis of the Vanguard",
                rarity = RelicRarity.Rare,
                description = "+15% Castle Damage Reduction",
                iconBadge = "🛡️",
                castleDamageReduction = 0.15f
            },
            new HeroRelicData
            {
                id = "relic_sniper_monocle",
                displayName = "Hawk-Eye Monocle",
                rarity = RelicRarity.Epic,
                description = "+20% Critical Strike Chance",
                iconBadge = "🎯",
                critChanceBonus = 0.20f
            },
            new HeroRelicData
            {
                id = "relic_midas_ring",
                displayName = "Alchemist's Midas Ring",
                rarity = RelicRarity.Rare,
                description = "+30% Gold Rewards Earned",
                iconBadge = "💍",
                goldGainBonus = 0.30f
            },
            new HeroRelicData
            {
                id = "relic_chrono_dial",
                displayName = "Chrono Hourglass",
                rarity = RelicRarity.Legendary,
                description = "+18% Hero Attack Speed",
                iconBadge = "⏳",
                attackSpeedBonus = 0.18f
            },
            new HeroRelicData
            {
                id = "relic_storm_shard",
                displayName = "Tempest Core Shard",
                rarity = RelicRarity.Legendary,
                description = "+15% Crit Chance & +20% Reaction Dmg",
                iconBadge = "⚡",
                critChanceBonus = 0.15f,
                reactionDamageBonus = 0.20f
            }
        };

        private readonly HashSet<string> unlockedIds = new HashSet<string>();
        private readonly string[] equippedSlots = new string[MaxEquipSlots];

        public event Action OnRelicsChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
                return;
            }
            Instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            LoadState();
        }

        public IReadOnlyList<HeroRelicData> AllCatalog => Catalog;

        private void EnsureStateLoaded()
        {
            if (unlockedIds.Count == 0)
            {
                LoadState();
            }
        }

        public bool IsUnlocked(string relicId)
        {
            EnsureStateLoaded();
            return unlockedIds.Contains(relicId);
        }

        public HeroRelicData GetDefinition(string relicId)
        {
            foreach (var r in Catalog)
            {
                if (r.id == relicId) return r;
            }
            return null;
        }

        public HeroRelicData GetEquipped(int slotIndex)
        {
            EnsureStateLoaded();
            if (slotIndex < 0 || slotIndex >= MaxEquipSlots) return null;
            string id = equippedSlots[slotIndex];
            return string.IsNullOrEmpty(id) ? null : GetDefinition(id);
        }

        public bool UnlockRelic(string relicId)
        {
            EnsureStateLoaded();
            if (string.IsNullOrEmpty(relicId) || unlockedIds.Contains(relicId)) return false;
            unlockedIds.Add(relicId);
            SaveState();
            OnRelicsChanged?.Invoke();
            return true;
        }

        public bool EquipRelic(string relicId, int slotIndex)
        {
            EnsureStateLoaded();
            if (slotIndex < 0 || slotIndex >= MaxEquipSlots) return false;
            if (!string.IsNullOrEmpty(relicId) && !unlockedIds.Contains(relicId)) return false;

            // Unequip from any existing slot
            for (int i = 0; i < MaxEquipSlots; i++)
            {
                if (equippedSlots[i] == relicId) equippedSlots[i] = "";
            }

            equippedSlots[slotIndex] = relicId ?? "";
            SaveState();
            HapticFeedbackManager.TriggerLight();
            OnRelicsChanged?.Invoke();
            return true;
        }

        public void UnequipRelic(int slotIndex)
        {
            EnsureStateLoaded();
            if (slotIndex >= 0 && slotIndex < MaxEquipSlots)
            {
                equippedSlots[slotIndex] = "";
                SaveState();
                OnRelicsChanged?.Invoke();
            }
        }

        public float TotalReactionDamageBonus
        {
            get
            {
                EnsureStateLoaded();
                float total = 0f;
                for (int i = 0; i < MaxEquipSlots; i++)
                {
                    var r = GetEquipped(i);
                    if (r != null) total += r.reactionDamageBonus;
                }
                return total;
            }
        }

        public float TotalCritChanceBonus
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < MaxEquipSlots; i++)
                {
                    var r = GetEquipped(i);
                    if (r != null) total += r.critChanceBonus;
                }
                return total;
            }
        }

        public float TotalCastleDamageReduction
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < MaxEquipSlots; i++)
                {
                    var r = GetEquipped(i);
                    if (r != null) total += r.castleDamageReduction;
                }
                return total;
            }
        }

        public float TotalGoldGainBonus
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < MaxEquipSlots; i++)
                {
                    var r = GetEquipped(i);
                    if (r != null) total += r.goldGainBonus;
                }
                return total;
            }
        }

        public float TotalAttackSpeedBonus
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < MaxEquipSlots; i++)
                {
                    var r = GetEquipped(i);
                    if (r != null) total += r.attackSpeedBonus;
                }
                return total;
            }
        }

        private void SaveState()
        {
            PlayerPrefs.SetString(KeyUnlockedRelics, string.Join(",", unlockedIds));
            for (int i = 0; i < MaxEquipSlots; i++)
            {
                PlayerPrefs.SetString(KeyEquippedSlotPrefix + i, equippedSlots[i] ?? "");
            }
            PlayerPrefs.Save();
        }

        private void LoadState()
        {
            unlockedIds.Clear();
            string csv = PlayerPrefs.GetString(KeyUnlockedRelics, "");
            if (string.IsNullOrEmpty(csv))
            {
                // Unlock starter relic by default
                unlockedIds.Add("relic_pyromancer_crown");
                unlockedIds.Add("relic_vanguard_aegis");
            }
            else
            {
                string[] split = csv.Split(',');
                foreach (var s in split)
                {
                    if (!string.IsNullOrEmpty(s)) unlockedIds.Add(s.Trim());
                }
            }

            for (int i = 0; i < MaxEquipSlots; i++)
            {
                equippedSlots[i] = PlayerPrefs.GetString(KeyEquippedSlotPrefix + i, "");
            }

            // Auto-equip defaults if empty
            if (string.IsNullOrEmpty(equippedSlots[0]) && unlockedIds.Contains("relic_pyromancer_crown"))
            {
                equippedSlots[0] = "relic_pyromancer_crown";
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static void ResetForTesting()
        {
            Instance = null;
            PlayerPrefs.DeleteKey(KeyUnlockedRelics);
            for (int i = 0; i < MaxEquipSlots; i++)
            {
                PlayerPrefs.DeleteKey(KeyEquippedSlotPrefix + i);
            }
        }
    }
}
