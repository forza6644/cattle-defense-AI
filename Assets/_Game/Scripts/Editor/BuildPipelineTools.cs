#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Stonehold.Editor
{
    public static class BuildPipelineTools
    {
        private const string BuildOutputDirectory = "Builds";

        [MenuItem("Stonehold/Build/Build Standalone Windows Release Candidate")]
        public static void BuildStandaloneReleaseCandidate()
        {
            Debug.Log("[BuildPipeline] Starting Standalone Windows 64 Release Candidate Build...");

            string outputFolder = Path.Combine(BuildOutputDirectory, "StandaloneWindows64");
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            string targetPath = Path.Combine(outputFolder, "Stonehold_RC.exe");
            string[] scenes = GetEnabledBuildScenes();

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = targetPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(options);
            PrintBuildSummary(report, "Standalone Windows 64");
        }

        [MenuItem("Stonehold/Build/Build Android Release Candidate APK")]
        public static void BuildAndroidReleaseCandidate()
        {
            Debug.Log("[BuildPipeline] Starting Android Release Candidate APK Build...");

            string outputFolder = Path.Combine(BuildOutputDirectory, "Android");
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            string targetPath = Path.Combine(outputFolder, "Stonehold_RC.apk");
            string[] scenes = GetEnabledBuildScenes();

            // Mobile Configuration
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            EditorUserBuildSettings.buildAppBundle = false;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = targetPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(options);
            PrintBuildSummary(report, "Android APK");
        }

        [MenuItem("Stonehold/Build/Build Android App Bundle (AAB)")]
        public static void BuildAndroidAppBundle()
        {
            Debug.Log("[BuildPipeline] Starting Google Play App Bundle (AAB) Build...");

            string outputFolder = Path.Combine(BuildOutputDirectory, "Android");
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            string targetPath = Path.Combine(outputFolder, "Stonehold_Release.aab");
            string[] scenes = GetEnabledBuildScenes();

            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            EditorUserBuildSettings.buildAppBundle = true;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = targetPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(options);
            PrintBuildSummary(report, "Google Play AAB");
        }

        private static string[] GetEnabledBuildScenes()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes == null || scenes.Length == 0)
            {
                scenes = new[]
                {
                    "Assets/_Game/Scenes/MainMenu.unity",
                    "Assets/_Game/Scenes/GameScene.unity"
                };
            }

            return scenes;
        }

        private static void PrintBuildSummary(BuildReport report, string platformName)
        {
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=#00FF00>[BuildPipeline] ✅ SUCCESS:</color> {platformName} build succeeded in {summary.totalTime.TotalSeconds:F1}s! Output Size: {summary.totalSize / (1024 * 1024):F2} MB. Path: {summary.outputPath}");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"<color=#FF0000>[BuildPipeline] ❌ FAILED:</color> {platformName} build failed with {summary.totalErrors} errors. Check Editor log for details.");
            }
            else if (summary.result == BuildResult.Cancelled)
            {
                Debug.LogWarning($"[BuildPipeline] ⚠️ CANCELLED: {platformName} build was cancelled.");
            }
        }
    }
}
#endif
