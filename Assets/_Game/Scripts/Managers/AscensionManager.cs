using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Central manager for Ascension Heat Mutators (pre-run difficulty & score modifiers).
    /// Tracks active mutators, computes total Heat Level, and provides combat hooks.
    /// </summary>
    public class AscensionManager : MonoBehaviour
    {
        public static AscensionManager Instance { get; private set; }

        public static event Action OnAscensionChanged;

        private const string PrefsKeyActiveMutators = "ascension_active_mutators";

        private readonly HashSet<string> activeMutatorIds = new HashSet<string>();
        public IReadOnlyCollection<string> ActiveMutatorIds => activeMutatorIds;

        private readonly List<AscensionMutatorDefinition> allMutators = new List<AscensionMutatorDefinition>();
        public IReadOnlyList<AscensionMutatorDefinition> AllMutators => allMutators;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            OnAscensionChanged = null;
        }

        public static void ResetForTesting()
        {
            Instance = null;
            OnAscensionChanged = null;
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
            LoadAllMutators();
            LoadActiveMutatorsFromPrefs();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void LoadAllMutators()
        {
            allMutators.Clear();
            var loaded = Resources.LoadAll<AscensionMutatorDefinition>("Mutators");
            if (loaded != null && loaded.Length > 0)
            {
                allMutators.AddRange(loaded);
            }
        }

        public void RegisterMutators(IEnumerable<AscensionMutatorDefinition> mutators)
        {
            allMutators.Clear();
            if (mutators != null)
            {
                allMutators.AddRange(mutators);
            }
        }

        public bool IsMutatorActive(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return activeMutatorIds.Contains(id);
        }

        public void SetMutatorActive(string id, bool active)
        {
            if (string.IsNullOrEmpty(id)) return;

            bool changed = false;
            if (active && !activeMutatorIds.Contains(id))
            {
                activeMutatorIds.Add(id);
                changed = true;
            }
            else if (!active && activeMutatorIds.Contains(id))
            {
                activeMutatorIds.Remove(id);
                changed = true;
            }

            if (changed)
            {
                SaveActiveMutatorsToPrefs();
                OnAscensionChanged?.Invoke();
            }
        }

        public void ToggleMutator(string id)
        {
            SetMutatorActive(id, !IsMutatorActive(id));
        }

        public void ClearAllMutators()
        {
            if (activeMutatorIds.Count > 0)
            {
                activeMutatorIds.Clear();
                SaveActiveMutatorsToPrefs();
                OnAscensionChanged?.Invoke();
            }
        }

        public int GetCurrentHeatLevel()
        {
            int heat = 0;
            for (int i = 0; i < allMutators.Count; i++)
            {
                var m = allMutators[i];
                if (m != null && activeMutatorIds.Contains(m.id))
                {
                    heat += m.heatPoints;
                }
            }
            return heat;
        }

        public float GetScoreMultiplier()
        {
            float mult = 1.0f;
            for (int i = 0; i < allMutators.Count; i++)
            {
                var m = allMutators[i];
                if (m != null && activeMutatorIds.Contains(m.id))
                {
                    mult += m.scoreMultiplierBonus;
                }
            }
            return mult;
        }

        // ------------------------------------------------------------- Combat & Economy Hooks

        public float GetEnemySpeedMultiplier()
        {
            float mult = 1.0f;
            for (int i = 0; i < allMutators.Count; i++)
            {
                var m = allMutators[i];
                if (m != null && activeMutatorIds.Contains(m.id) && m.mutatorType == AscensionMutatorType.EnemySpeedMultiplier)
                {
                    mult += m.effectValue;
                }
            }
            return mult;
        }

        public float GetEnemyArmorBonus()
        {
            float bonus = 0f;
            for (int i = 0; i < allMutators.Count; i++)
            {
                var m = allMutators[i];
                if (m != null && activeMutatorIds.Contains(m.id) && m.mutatorType == AscensionMutatorType.EnemyArmorBonus)
                {
                    bonus += m.effectValue;
                }
            }
            return bonus;
        }

        public float GetEnemyRegenPercent()
        {
            float regen = 0f;
            for (int i = 0; i < allMutators.Count; i++)
            {
                var m = allMutators[i];
                if (m != null && activeMutatorIds.Contains(m.id) && m.mutatorType == AscensionMutatorType.EnemyRegenPercent)
                {
                    regen += m.effectValue;
                }
            }
            return regen;
        }

        public float GetGoldMultiplier()
        {
            float mult = 1.0f;
            for (int i = 0; i < allMutators.Count; i++)
            {
                var m = allMutators[i];
                if (m != null && activeMutatorIds.Contains(m.id) && m.mutatorType == AscensionMutatorType.GoldRewardMultiplier)
                {
                    mult *= Mathf.Max(0.1f, 1.0f - m.effectValue);
                }
            }
            return mult;
        }

        public float GetEliteHealthMultiplier()
        {
            float mult = 1.0f;
            for (int i = 0; i < allMutators.Count; i++)
            {
                var m = allMutators[i];
                if (m != null && activeMutatorIds.Contains(m.id) && m.mutatorType == AscensionMutatorType.EliteExtraAffixAndHealth)
                {
                    mult += m.effectValue;
                }
            }
            return mult;
        }

        public float GetCastleHealthMultiplier()
        {
            float mult = 1.0f;
            for (int i = 0; i < allMutators.Count; i++)
            {
                var m = allMutators[i];
                if (m != null && activeMutatorIds.Contains(m.id) && m.mutatorType == AscensionMutatorType.CastleMaxHealthPenalty)
                {
                    mult -= m.effectValue;
                }
            }
            return Mathf.Clamp(mult, 0.2f, 1.0f);
        }

        public float GetWaveCountdownMultiplier()
        {
            float mult = 1.0f;
            for (int i = 0; i < allMutators.Count; i++)
            {
                var m = allMutators[i];
                if (m != null && activeMutatorIds.Contains(m.id) && m.mutatorType == AscensionMutatorType.WaveCountdownReduction)
                {
                    mult -= m.effectValue;
                }
            }
            return Mathf.Clamp(mult, 0.2f, 1.0f);
        }

        public float GetNullifierShieldChance()
        {
            float chance = 0f;
            for (int i = 0; i < allMutators.Count; i++)
            {
                var m = allMutators[i];
                if (m != null && activeMutatorIds.Contains(m.id) && m.mutatorType == AscensionMutatorType.NullifierShieldChance)
                {
                    chance += m.effectValue;
                }
            }
            return Mathf.Clamp01(chance);
        }

        // ------------------------------------------------------------- Persistence

        private void SaveActiveMutatorsToPrefs()
        {
            string joined = string.Join(",", activeMutatorIds);
            PlayerPrefs.SetString(PrefsKeyActiveMutators, joined);
            PlayerPrefs.Save();
        }

        private void LoadActiveMutatorsFromPrefs()
        {
            activeMutatorIds.Clear();
            if (PlayerPrefs.HasKey(PrefsKeyActiveMutators))
            {
                string stored = PlayerPrefs.GetString(PrefsKeyActiveMutators, "");
                if (!string.IsNullOrEmpty(stored))
                {
                    string[] split = stored.Split(',');
                    for (int i = 0; i < split.Length; i++)
                    {
                        string id = split[i].Trim();
                        if (!string.IsNullOrEmpty(id))
                        {
                            activeMutatorIds.Add(id);
                        }
                    }
                }
            }
        }
    }
}
