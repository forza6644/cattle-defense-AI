using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    [System.Serializable]
    public class AbyssalOvercharge
    {
        public string id;
        public string name;
        public string description;
        public string rarityBadge = "ABYSSAL";
        public Color themeColor = new Color(0.75f, 0.35f, 1f, 1f);
    }

    /// <summary>
    /// Manages Endless Abyssal Survival Mode: procedural infinite scaling waves,
    /// dynamic boss spawns every 5 waves, and Overcharge blessing drafts.
    /// </summary>
    public class EndlessSurvivalManager : MonoBehaviour
    {
        public static EndlessSurvivalManager Instance { get; private set; }

        public static event Action<int> OnAbyssalWaveStarted;
        public static event Action<int> OnAbyssalWaveCleared;
        public static event Action<AbyssalOvercharge> OnOverchargeAcquired;

        private const string PrefsKeyHighestAbyssalWave = "highest_abyssal_wave";
        private const string PrefsKeyAbyssalTrophies = "abyssal_trophies";

        public bool IsEndlessActive { get; private set; }
        public int AbyssalWaveNumber { get; private set; }
        public int HighestAbyssalWave { get; private set; }
        public int AbyssalTrophies { get; private set; }

        private readonly List<AbyssalOvercharge> activeOvercharges = new List<AbyssalOvercharge>();
        public IReadOnlyList<AbyssalOvercharge> ActiveOvercharges => activeOvercharges;

        private readonly List<AbyssalOvercharge> overchargePool = new List<AbyssalOvercharge>
        {
            new AbyssalOvercharge
            {
                id = "abyssal_hypercharge",
                name = "Void Surge",
                description = "+25% Hero Attack Speed and Ability Recovery across all defenders.",
                rarityBadge = "ABYSSAL"
            },
            new AbyssalOvercharge
            {
                id = "abyssal_resonance",
                name = "Eternal Resonance",
                description = "+50% Elemental Reaction and Status Effect Burst Damage.",
                rarityBadge = "ABYSSAL"
            },
            new AbyssalOvercharge
            {
                id = "abyssal_singularity",
                name = "Singularity Barrier",
                description = "+200 Castle Kinetic Shield Capacity and absorbs 25% extra damage.",
                rarityBadge = "ABYSSAL"
            },
            new AbyssalOvercharge
            {
                id = "abyssal_harvest",
                name = "Midas Singularity",
                description = "+50% Gold and Materials reward from all defeated enemies.",
                rarityBadge = "ABYSSAL"
            },
            new AbyssalOvercharge
            {
                id = "abyssal_fortress",
                name = "Abyssal Aegis",
                description = "Instantly repairs Castle to full HP and permanently adds +50 Max HP.",
                rarityBadge = "ABYSSAL"
            }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            HighestAbyssalWave = PlayerPrefs.GetInt(PrefsKeyHighestAbyssalWave, 0);
            AbyssalTrophies = PlayerPrefs.GetInt(PrefsKeyAbyssalTrophies, 0);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void StartEndlessMode()
        {
            IsEndlessActive = true;
            AbyssalWaveNumber = 1;
            activeOvercharges.Clear();
            OnAbyssalWaveStarted?.Invoke(AbyssalWaveNumber);
        }

        public void StopEndlessMode()
        {
            IsEndlessActive = false;
        }

        public void AdvanceAbyssalWave()
        {
            if (!IsEndlessActive) return;

            OnAbyssalWaveCleared?.Invoke(AbyssalWaveNumber);

            if (AbyssalWaveNumber > HighestAbyssalWave)
            {
                HighestAbyssalWave = AbyssalWaveNumber;
                PlayerPrefs.SetInt(PrefsKeyHighestAbyssalWave, HighestAbyssalWave);
                PlayerPrefs.Save();
            }

            // Award Abyssal Trophies
            AbyssalTrophies += 5 + (AbyssalWaveNumber * 2);
            PlayerPrefs.SetInt(PrefsKeyAbyssalTrophies, AbyssalTrophies);
            PlayerPrefs.Save();

            AbyssalWaveNumber++;
            OnAbyssalWaveStarted?.Invoke(AbyssalWaveNumber);
        }

        public float GetAbyssalHealthMultiplier(int wave)
        {
            int safeWave = Mathf.Max(1, wave);
            return Mathf.Pow(1f + 0.15f * safeWave, 1.12f);
        }

        public float GetAbyssalDamageMultiplier(int wave)
        {
            int safeWave = Mathf.Max(1, wave);
            return 1f + 0.10f * safeWave;
        }

        public int GetAbyssalEnemyCount(int wave, int baseCount = 15)
        {
            int safeWave = Mathf.Max(1, wave);
            return Mathf.Min(120, Mathf.RoundToInt(baseCount * (1f + 0.10f * safeWave)));
        }

        public List<AbyssalOvercharge> RollOverchargeDraft(int count = 3)
        {
            var available = new List<AbyssalOvercharge>(overchargePool);
            var result = new List<AbyssalOvercharge>();
            int rollCount = Mathf.Min(count, available.Count);

            for (int i = 0; i < rollCount; i++)
            {
                int idx = UnityEngine.Random.Range(0, available.Count);
                result.Add(available[idx]);
                available.RemoveAt(idx);
            }
            return result;
        }

        public void ClaimOvercharge(AbyssalOvercharge overcharge)
        {
            if (overcharge == null) return;
            activeOvercharges.Add(overcharge);

            // Apply blessing mechanics directly
            if (overcharge.id == "abyssal_singularity")
            {
                Castle.Instance?.AddKineticShield(200f);
            }
            else if (overcharge.id == "abyssal_fortress")
            {
                if (Castle.Instance != null)
                {
                    Castle.Instance.Repair(Castle.Instance.MaxHealth);
                }
            }

            OnOverchargeAcquired?.Invoke(overcharge);
        }
    }
}
