using UnityEngine;

namespace Stonehold
{
    public enum HapticImpactType
    {
        Light,
        Medium,
        Heavy
    }

    /// <summary>
    /// Cross-platform mobile haptic vibration manager for Android & iOS.
    /// Provides tactile feedback for critical hits, elemental synergies, castle ultimates, and boss events.
    /// Explicitly excludes screen shake.
    /// </summary>
    public static class HapticFeedbackManager
    {
        private const string PrefsKeyHapticsEnabled = "settings_haptics_enabled";

        private static bool? isHapticsEnabledCache;

        public static bool IsHapticsEnabled
        {
            get
            {
                if (!isHapticsEnabledCache.HasValue)
                {
                    isHapticsEnabledCache = PlayerPrefs.GetInt(PrefsKeyHapticsEnabled, 1) == 1;
                }
                return isHapticsEnabledCache.Value;
            }
            set
            {
                isHapticsEnabledCache = value;
                PlayerPrefs.SetInt(PrefsKeyHapticsEnabled, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void ToggleHaptics()
        {
            IsHapticsEnabled = !IsHapticsEnabled;
        }

        public static void TriggerLight()
        {
            if (!IsHapticsEnabled) return;
            TriggerImpact(HapticImpactType.Light);
        }

        public static void TriggerMedium()
        {
            if (!IsHapticsEnabled) return;
            TriggerImpact(HapticImpactType.Medium);
        }

        public static void TriggerHeavy()
        {
            if (!IsHapticsEnabled) return;
            TriggerImpact(HapticImpactType.Heavy);
        }

        public static void TriggerImpact(HapticImpactType impactType)
        {
            if (!IsHapticsEnabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator != null && vibrator.Call<bool>("hasVibrator"))
                    {
                        long durationMs;
                        switch (impactType)
                        {
                            case HapticImpactType.Light:
                                durationMs = 18;
                                break;
                            case HapticImpactType.Heavy:
                                durationMs = 70;
                                break;
                            case HapticImpactType.Medium:
                            default:
                                durationMs = 38;
                                break;
                        }

                        using (var versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
                        {
                            int sdkInt = versionClass.GetStatic<int>("SDK_INT");
                            if (sdkInt >= 26) // Android 8.0 Oreo+
                            {
                                int amplitude = impactType == HapticImpactType.Light ? 60 : (impactType == HapticImpactType.Heavy ? 255 : 160);
                                using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                                using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", durationMs, amplitude))
                                {
                                    vibrator.Call("vibrate", effect);
                                }
                            }
                            else
                            {
                                vibrator.Call("vibrate", durationMs);
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[HapticFeedbackManager] Android haptic error: {ex.Message}");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        public static void ResetForTesting()
        {
            isHapticsEnabledCache = null;
        }
    }
}
