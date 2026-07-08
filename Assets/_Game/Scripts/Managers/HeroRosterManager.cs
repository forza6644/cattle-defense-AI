using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Runtime roster for the current run. Owns which heroes are recruited and which
    /// HeroSlot receives the next recruit.
    /// </summary>
    public class HeroRosterManager : MonoBehaviour
    {
        public static HeroRosterManager Instance { get; private set; }

        private const string DefaultHeroId = "archer";

        private readonly HashSet<string> ownedHeroIds = new HashSet<string>();
        private readonly Dictionary<string, HeroDefinition> heroDefinitions = new Dictionary<string, HeroDefinition>();
        private readonly List<HeroSlot> slots = new List<HeroSlot>();

        private bool initialized;

        public IReadOnlyCollection<string> OwnedHeroIds => ownedHeroIds;
        public int EmptySlotCount => CountEmptySlots();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            InitializeRunRoster();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RegisterSlot(HeroSlot slot)
        {
            if (slot == null || slots.Contains(slot))
            {
                return;
            }

            slots.Add(slot);
            CaptureDefinition(slot.startingHero);
        }

        public bool IsHeroOwned(string heroId)
        {
            return !string.IsNullOrEmpty(heroId) && ownedHeroIds.Contains(heroId);
        }

        public bool CanRecruit(string heroId)
        {
            return !string.IsNullOrEmpty(heroId)
                && !ownedHeroIds.Contains(heroId)
                && heroDefinitions.ContainsKey(heroId)
                && CountEmptySlots() > 0;
        }

        public bool CanUpgrade(string heroId)
        {
            return IsHeroOwned(heroId);
        }

        public bool HasOwnedHeroWithAttackType(AttackType attackType)
        {
            foreach (string heroId in ownedHeroIds)
            {
                if (heroDefinitions.TryGetValue(heroId, out HeroDefinition hero)
                    && hero != null
                    && hero.weapon != null
                    && hero.weapon.attackType == attackType)
                {
                    return true;
                }
            }

            return false;
        }

        public bool RecruitHero(string heroId)
        {
            if (!CanRecruit(heroId))
            {
                return false;
            }

            HeroSlot slot = FindNextEmptySlot();
            if (slot == null)
            {
                return false;
            }

            HeroDefinition hero = heroDefinitions[heroId];
            if (!slot.SpawnHero(hero))
            {
                return false;
            }

            ownedHeroIds.Add(heroId);
            Debug.Log($"[HeroRosterManager] Recruited hero: {heroId}");
            return true;
        }

        public void InitializeRunRoster()
        {
            if (initialized)
            {
                return;
            }

            RefreshSlots();

            for (int i = 0; i < slots.Count; i++)
            {
                CaptureDefinition(slots[i].startingHero);
                slots[i].ClearHero();
            }

            ownedHeroIds.Clear();

            if (!heroDefinitions.TryGetValue(DefaultHeroId, out HeroDefinition archer))
            {
                Debug.LogWarning("[HeroRosterManager] Default archer hero definition was not found on any HeroSlot.");
                initialized = true;
                return;
            }

            HeroSlot firstSlot = FindNextEmptySlot();
            if (firstSlot != null && firstSlot.SpawnHero(archer))
            {
                ownedHeroIds.Add(DefaultHeroId);
            }

            initialized = true;
        }

        private void RefreshSlots()
        {
            HeroSlot[] sceneSlots = FindObjectsByType<HeroSlot>(FindObjectsSortMode.None);
            for (int i = 0; i < sceneSlots.Length; i++)
            {
                RegisterSlot(sceneSlots[i]);
            }

            slots.Sort(CompareSlots);
            KeepOneSlotPerName();
        }

        private static int CompareSlots(HeroSlot a, HeroSlot b)
        {
            int nameCompare = string.Compare(a.name, b.name, System.StringComparison.Ordinal);
            if (nameCompare != 0)
            {
                return nameCompare;
            }

            return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
        }

        private void KeepOneSlotPerName()
        {
            HashSet<string> seenNames = new HashSet<string>();
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                HeroSlot slot = slots[i];
                if (slot == null)
                {
                    slots.RemoveAt(i);
                    continue;
                }

                string slotName = slot.name;
                if (seenNames.Contains(slotName))
                {
                    slots.RemoveAt(i);
                    continue;
                }

                seenNames.Add(slotName);
            }

            slots.Sort(CompareSlots);
        }

        private void CaptureDefinition(HeroDefinition hero)
        {
            if (hero == null || string.IsNullOrEmpty(hero.id))
            {
                return;
            }

            heroDefinitions[hero.id] = hero;
        }

        private HeroSlot FindNextEmptySlot()
        {
            RefreshSlots();
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null && !slots[i].IsOccupied)
                {
                    return slots[i];
                }
            }

            return null;
        }

        private int CountEmptySlots()
        {
            RefreshSlots();
            int count = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null && !slots[i].IsOccupied)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
