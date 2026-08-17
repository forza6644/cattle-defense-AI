using System;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Supported mobile graphics quality tiers tailored for battery economy,
    /// smooth 60 FPS performance, and crisp visual clarity.
    /// </summary>
    public enum MobileQualityTier
    {
        Low = 0,    // Battery Saver (0.85x scale, 1x MSAA, 512 hard shadows)
        Medium = 1, // Balanced Default (1.0x scale, 2x MSAA, 1024 soft shadows)
        High = 2    // Crisp High-End (1.0x native scale, 4x MSAA, 2048 soft shadows, 4-bone skinning)
    }

    /// <summary>
    /// Centralized manager for mobile graphics tiers, QualitySettings level switching,
    /// and PlayerPrefs persistence across sessions.
    /// </summary>
    public static class MobileQualityManager
    {
        private const string PrefsKeyQualityTier = "Stonehold_MobileQualityTier";
        private const MobileQualityTier DefaultQualityTier = MobileQualityTier.Medium;

        public static event Action<MobileQualityTier> OnQualityTierChanged;

        private static bool isInitialized;
        private static MobileQualityTier currentTier = DefaultQualityTier;

        public static MobileQualityTier CurrentTier
        {
            get
            {
                EnsureInitialized();
                return currentTier;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeOnBoot()
        {
            EnsureInitialized();
        }

        public static void EnsureInitialized()
        {
            if (isInitialized) return;

            int saved = PlayerPrefs.GetInt(PrefsKeyQualityTier, (int)DefaultQualityTier);
            saved = Mathf.Clamp(saved, 0, 2);
            currentTier = (MobileQualityTier)saved;

            ApplyQualityTierInternal(currentTier, saveToPrefs: false);
            isInitialized = true;
        }

        /// <summary>
        /// Sets the active graphics quality tier (0 = Low, 1 = Medium, 2 = High),
        /// updates Unity QualitySettings, and persists to PlayerPrefs.
        /// </summary>
        public static void SetQualityTier(MobileQualityTier tier)
        {
            tier = (MobileQualityTier)Mathf.Clamp((int)tier, 0, 2);
            currentTier = tier;
            ApplyQualityTierInternal(tier, saveToPrefs: true);
            OnQualityTierChanged?.Invoke(tier);
        }

        /// <summary>
        /// Convenience overload for integer level index (0, 1, 2).
        /// </summary>
        public static void SetQualityTier(int tierIndex)
        {
            SetQualityTier((MobileQualityTier)tierIndex);
        }

        private static void ApplyQualityTierInternal(MobileQualityTier tier, bool saveToPrefs)
        {
            int levelIndex = (int)tier;
            int maxLevel = QualitySettings.names.Length - 1;
            int clampedLevel = Mathf.Clamp(levelIndex, 0, Mathf.Max(0, maxLevel));

            QualitySettings.SetQualityLevel(clampedLevel, true);

            // Configure target FPS & VSync according to tier
            if (PerformanceOptimizer.Instance != null)
            {
                PerformanceOptimizer.Instance.ApplySettings();
            }

            if (saveToPrefs)
            {
                PlayerPrefs.SetInt(PrefsKeyQualityTier, (int)tier);
                PlayerPrefs.Save();
            }

            Debug.Log($"[MobileQualityManager] Active Graphics Tier: {tier} ({GetTierDisplayName(tier)})");
        }

        public static string GetTierDisplayName(MobileQualityTier tier)
        {
            switch (tier)
            {
                case MobileQualityTier.Low:
                    return "Low (Battery Saver)";
                case MobileQualityTier.Medium:
                    return "Medium (Balanced)";
                case MobileQualityTier.High:
                    return "High (Crisp 4x MSAA)";
                default:
                    return tier.ToString();
            }
        }

        public static string GetTierDescription(MobileQualityTier tier)
        {
            switch (tier)
            {
                case MobileQualityTier.Low:
                    return "0.85x scale, 1x MSAA, low shadows. Max battery life and low thermals.";
                case MobileQualityTier.Medium:
                    return "1.0x native scale, 2x MSAA, 1024 soft shadows. Balanced for smooth 60 FPS.";
                case MobileQualityTier.High:
                    return "1.0x native scale, 4x MSAA, 2048 soft shadows. Maximum visual sharpness.";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Resets quality preferences (for testing).
        /// </summary>
        public static void ResetForTesting()
        {
            PlayerPrefs.DeleteKey(PrefsKeyQualityTier);
            isInitialized = false;
            EnsureInitialized();
        }
    }
}
