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
        public int CachedEmptySlotCount => CountCachedEmptySlots();
        public IReadOnlyList<HeroSlot> Slots => slots;

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

        public void CopyOwnedAttackTypesTo(ICollection<AttackType> destination)
        {
            if (destination == null) return;
            foreach (string heroId in ownedHeroIds)
            {
                if (heroDefinitions.TryGetValue(heroId, out HeroDefinition hero)
                    && hero != null && hero.weapon != null && !destination.Contains(hero.weapon.attackType))
                {
                    destination.Add(hero.weapon.attackType);
                }
            }
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

            string startingHeroId = SaveManager.SelectedStartingDefenderId;
            if (string.IsNullOrEmpty(startingHeroId))
            {
                startingHeroId = DefaultHeroId;
            }

            if (!heroDefinitions.TryGetValue(startingHeroId, out HeroDefinition startingHero))
            {
                // Fallback to archer
                startingHeroId = DefaultHeroId;
                if (!heroDefinitions.TryGetValue(startingHeroId, out startingHero))
                {
                    Debug.LogWarning("[HeroRosterManager] Starting hero definition not found.");
                    initialized = true;
                    return;
                }
            }

            HeroSlot firstSlot = FindNextEmptySlot();
            if (firstSlot != null && firstSlot.SpawnHero(startingHero))
            {
                ownedHeroIds.Add(startingHeroId);
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
            ApplyCenterOutOrder();
        }

        /// <summary>
        /// Reorders the (left-to-right) slot list so recruitment fills from the centre of
        /// the wall outward: nearest-to-centre first, left before right on ties. For the
        /// six slots at x = -4.5,-2.7,-0.9,0.9,2.7,4.5 the visit order becomes
        /// -0.9, 0.9, -2.7, 2.7, -4.5, 4.5, so the starting Archer lands near the centre
        /// and later recruits fill outward to the edges.
        /// Assumes slot name order (HeroSlot_01..NN) matches left-to-right placement,
        /// which the scene setup guarantees.
        /// </summary>
        private void ApplyCenterOutOrder()
        {
            int count = slots.Count;
            if (count <= 1)
            {
                return;
            }

            HeroSlot[] leftToRight = slots.ToArray();
            float center = (count - 1) / 2f;

            List<int> order = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                order.Add(i);
            }

            order.Sort((a, b) =>
            {
                float distanceCompare = Mathf.Abs(a - center).CompareTo(Mathf.Abs(b - center));
                if (distanceCompare != 0f)
                {
                    return distanceCompare < 0f ? -1 : 1;
                }

                return a.CompareTo(b);
            });

            slots.Clear();
            for (int i = 0; i < order.Count; i++)
            {
                slots.Add(leftToRight[order[i]]);
            }
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
            return CountCachedEmptySlots();
        }

        private int CountCachedEmptySlots()
        {
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
