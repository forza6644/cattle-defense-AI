using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Central manager for the in-game Bestiary & Elemental Codex.
    /// Tracks enemy discovery, kill counts, elemental strengths/weaknesses, and tactical tips.
    /// </summary>
    public class BestiaryManager : MonoBehaviour
    {
        public static BestiaryManager Instance { get; private set; }

        private readonly List<BestiaryEntryDefinition> allEntries = new List<BestiaryEntryDefinition>();
        private readonly Dictionary<string, BestiaryEntryDefinition> entryMap = new Dictionary<string, BestiaryEntryDefinition>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<BestiaryEntryDefinition> AllEntries
        {
            get
            {
                EnsureLoaded();
                return allEntries;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        public static void ResetForTesting()
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
            LoadAllEntries();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void EnsureLoaded()
        {
            if (allEntries == null || allEntries.Count == 0)
            {
                LoadAllEntries();
            }
        }

        public void LoadAllEntries()
        {
            allEntries.Clear();
            entryMap.Clear();

            var loaded = Resources.LoadAll<BestiaryEntryDefinition>("Bestiary");
            if (loaded != null && loaded.Length > 0)
            {
                for (int i = 0; i < loaded.Length; i++)
                {
                    AddEntry(loaded[i]);
                }
            }

            if (allEntries.Count == 0)
            {
                CreateDefaultEntries();
            }
        }

        public void AddEntry(BestiaryEntryDefinition entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.enemyId)) return;
            if (!entryMap.ContainsKey(entry.enemyId))
            {
                allEntries.Add(entry);
                entryMap[entry.enemyId] = entry;
            }
        }

        public BestiaryEntryDefinition GetEntry(string enemyId)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(enemyId)) return null;
            return entryMap.TryGetValue(enemyId, out var entry) ? entry : null;
        }

        public List<BestiaryEntryDefinition> GetEntriesByCategory(EnemyCategory category)
        {
            EnsureLoaded();
            var list = new List<BestiaryEntryDefinition>();
            for (int i = 0; i < allEntries.Count; i++)
            {
                if (allEntries[i].category == category)
                {
                    list.Add(allEntries[i]);
                }
            }
            return list;
        }

        public void RegisterEncounter(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId)) return;
            string key = "bestiary_seen_" + enemyId;
            if (PlayerPrefs.GetInt(key, 0) == 0)
            {
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
                Debug.Log($"[Bestiary] New Codex Discovery: {enemyId}!");
            }
        }

        public void RegisterKill(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId)) return;
            string key = "bestiary_kills_" + enemyId;
            int count = PlayerPrefs.GetInt(key, 0) + 1;
            PlayerPrefs.SetInt(key, count);
            PlayerPrefs.Save();
        }

        public int GetKillCount(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId)) return 0;
            return PlayerPrefs.GetInt("bestiary_kills_" + enemyId, 0);
        }

        public bool IsEncountered(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId)) return false;
            return PlayerPrefs.GetInt("bestiary_seen_" + enemyId, 0) > 0 || GetKillCount(enemyId) > 0;
        }

        public void ResetEncounters()
        {
            EnsureLoaded();
            for (int i = 0; i < allEntries.Count; i++)
            {
                string id = allEntries[i].enemyId;
                PlayerPrefs.DeleteKey("bestiary_seen_" + id);
                PlayerPrefs.DeleteKey("bestiary_kills_" + id);
            }
            PlayerPrefs.Save();
        }

        private void CreateDefaultEntries()
        {
            var goblin = ScriptableObject.CreateInstance<BestiaryEntryDefinition>();
            goblin.enemyId = "goblin_grunt";
            goblin.displayName = "Goblin Grunt";
            goblin.category = EnemyCategory.Swarm;
            goblin.threatLevel = 1;
            goblin.baseHealth = 45;
            goblin.baseSpeed = 2.4f;
            goblin.baseArmor = 0;
            goblin.loreDescription = "Fierce and rapid scavengers that attack in ferocious swarms. Weak individually, but overwhelm undefended choke points.";
            goblin.tacticalCounterTips = "Use splash damage or rapid-fire archers to quickly thin out large crowds.";
            goblin.weaknesses.Add(StatusEffectType.Burn);
            goblin.weaknesses.Add(StatusEffectType.Poison);
            goblin.recommendedHeroCounter = "Fire Mage / Bombardier";
            goblin.themeColor = new Color(0.4f, 0.85f, 0.4f);
            AddEntry(goblin);

            var orc = ScriptableObject.CreateInstance<BestiaryEntryDefinition>();
            orc.enemyId = "orc_warrior";
            orc.displayName = "Orc Warrior";
            orc.category = EnemyCategory.Bruiser;
            orc.threatLevel = 2;
            orc.baseHealth = 180;
            orc.baseSpeed = 1.6f;
            orc.baseArmor = 15;
            orc.loreDescription = "Armored frontline brawlers with forged steel shields capable of soaking heavy physical damage.";
            orc.tacticalCounterTips = "Melt through their armor using Corrosive Blast (Poison + Burn) or freeze them in place.";
            orc.weaknesses.Add(StatusEffectType.Slow);
            orc.weaknesses.Add(StatusEffectType.Burn);
            orc.recommendedHeroCounter = "Frost Mage / Plague Doctor";
            orc.themeColor = new Color(0.85f, 0.55f, 0.25f);
            AddEntry(orc);

            var skeleton = ScriptableObject.CreateInstance<BestiaryEntryDefinition>();
            skeleton.enemyId = "skeleton_archer";
            skeleton.displayName = "Skeleton Archer";
            skeleton.category = EnemyCategory.Ranged;
            skeleton.threatLevel = 2;
            skeleton.baseHealth = 75;
            skeleton.baseSpeed = 1.9f;
            skeleton.baseArmor = 5;
            skeleton.loreDescription = "Reanimated marksmen that unleash volleys of necrotic arrows from outside defensive ranges.";
            skeleton.tacticalCounterTips = "Deploy Snipers to eliminate them before they can bombard your barricades.";
            skeleton.weaknesses.Add(StatusEffectType.Shock);
            skeleton.recommendedHeroCounter = "Sniper";
            skeleton.themeColor = new Color(0.7f, 0.8f, 0.95f);
            AddEntry(skeleton);

            var siegeRam = ScriptableObject.CreateInstance<BestiaryEntryDefinition>();
            siegeRam.enemyId = "armored_siege_ram";
            siegeRam.displayName = "Siege Golem";
            siegeRam.category = EnemyCategory.Siege;
            siegeRam.threatLevel = 3;
            siegeRam.baseHealth = 450;
            siegeRam.baseSpeed = 1.0f;
            siegeRam.baseArmor = 35;
            siegeRam.loreDescription = "Heavy mechanized stone constructs built specifically to breach fortress gates.";
            siegeRam.tacticalCounterTips = "Chain shock pulses and shatter their armor with concentrated frost bursts.";
            siegeRam.weaknesses.Add(StatusEffectType.Shock);
            siegeRam.weaknesses.Add(StatusEffectType.Slow);
            siegeRam.recommendedHeroCounter = "Electric Engineer / Frost Mage";
            siegeRam.themeColor = new Color(0.6f, 0.65f, 0.75f);
            AddEntry(siegeRam);

            var warlord = ScriptableObject.CreateInstance<BestiaryEntryDefinition>();
            warlord.enemyId = "warlord_boss";
            warlord.displayName = "Warlord Boss";
            warlord.category = EnemyCategory.Boss;
            warlord.isBoss = true;
            warlord.threatLevel = 5;
            warlord.baseHealth = 1200;
            warlord.baseSpeed = 1.2f;
            warlord.baseArmor = 40;
            warlord.loreDescription = "Supreme commander of the invasion horde, wielding twin dread blades and commanding elite vanguard waves.";
            warlord.tacticalCounterTips = "Trigger Thermal Shock (Burn + Frost) and Overload (Burn + Shock) to chunk massive percentages of his health.";
            warlord.weaknesses.Add(StatusEffectType.Burn);
            warlord.weaknesses.Add(StatusEffectType.Shock);
            warlord.recommendedHeroCounter = "Fire Mage + Frost Mage Synergy";
            warlord.themeColor = new Color(0.95f, 0.2f, 0.2f);
            AddEntry(warlord);

            var abyssal = ScriptableObject.CreateInstance<BestiaryEntryDefinition>();
            abyssal.enemyId = "abyssal_dreadnought";
            abyssal.displayName = "Abyssal Dreadnought";
            abyssal.category = EnemyCategory.Boss;
            abyssal.isBoss = true;
            abyssal.threatLevel = 5;
            abyssal.baseHealth = 3000;
            abyssal.baseSpeed = 1.1f;
            abyssal.baseArmor = 60;
            abyssal.loreDescription = "An entity born from the deepest abyssal rift. Shrouded in void armor that deflects ordinary kinetic rounds.";
            abyssal.tacticalCounterTips = "Requires high-stack elemental reactions and Abyssal Overcharge blessings to penetrate void shields.";
            abyssal.weaknesses.Add(StatusEffectType.Poison);
            abyssal.weaknesses.Add(StatusEffectType.Shock);
            abyssal.recommendedHeroCounter = "Plague Doctor + Electric Engineer Synergy";
            abyssal.themeColor = new Color(0.8f, 0.3f, 1f);
            AddEntry(abyssal);
        }
    }
}
