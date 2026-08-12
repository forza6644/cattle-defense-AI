using UnityEngine;
using UnityEngine.Scripting;

namespace Stonehold
{
    /// <summary>
    /// Centralized performance manager that configures frame rate caps, VSync,
    /// and GC collection policies for smooth 60 FPS mobile execution.
    /// </summary>
    public sealed class PerformanceOptimizer : MonoBehaviour
    {
        public static PerformanceOptimizer Instance { get; private set; }

        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool disableVSyncForMobile = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("PerformanceOptimizer");
                go.AddComponent<PerformanceOptimizer>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ApplySettings();
        }

        public void ApplySettings()
        {
            // Set Target Frame Rate to 60 FPS
            Application.targetFrameRate = targetFrameRate;

            // Turn off VSync on mobile/desktop platforms to enforce fixed 60 FPS target
            if (disableVSyncForMobile)
            {
                QualitySettings.vSyncCount = 0;
            }

            // Ensure Incremental GC is active
            GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
        }
    }
}
