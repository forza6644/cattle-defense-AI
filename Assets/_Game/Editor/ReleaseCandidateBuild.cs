using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Stonehold.Editor
{
    public static class ReleaseCandidateBuild
    {
        private const string DevOutputPath = "Builds/Android/Stonehold-Development.apk";
        private const string ReleaseOutputPath = "Builds/Android/Stonehold-ReleaseCandidate.apk";

        [MenuItem("Stonehold/Android/Build Development APK")]
        public static void BuildDevelopment()
        {
            Build(true, DevOutputPath);
        }

        [MenuItem("Stonehold/Android/Build Release Candidate APK")]
        public static void BuildReleaseCandidate()
        {
            Build(false, ReleaseOutputPath);
        }

        // Keep legacy method for test compatibility
        public static void BuildAndroidDevelopment()
        {
            Build(true, DevOutputPath);
        }

        private static void Build(bool isDevelopment, string relativeOutputPath)
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes are configured in Build Settings.");

            string outputPath = Path.GetFullPath(relativeOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            // Save original settings
            BuildTargetGroup originalGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            BuildTarget originalTarget = EditorUserBuildSettings.activeBuildTarget;
            bool originalDev = EditorUserBuildSettings.development;
            ScriptingImplementation originalBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
            AndroidArchitecture originalArchitecture = PlayerSettings.Android.targetArchitectures;
            UIOrientation originalOrientation = PlayerSettings.defaultInterfaceOrientation;

            try
            {
                // Switch target temporarily
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                EditorUserBuildSettings.development = isDevelopment;

                // Force portrait mode
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

                // Configure Scripting Backend and Architecture
                bool il2cppSupported = true;
                try
                {
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ReleaseCandidateBuild] IL2CPP or ARM64 not fully supported in this environment: {ex.Message}. Falling back to default backend.");
                    il2cppSupported = false;
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, originalBackend);
                    PlayerSettings.Android.targetArchitectures = originalArchitecture;
                }

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    options = isDevelopment ? BuildOptions.Development : BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;

                Debug.Log($"Android {(isDevelopment ? "Development" : "Release")} build result: {summary.result}; size: {summary.totalSize}; duration: {summary.totalTime}");
                if (summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException($"Android Build failed: {summary.result}");
            }
            finally
            {
                // Restore settings
                EditorUserBuildSettings.SwitchActiveBuildTarget(originalGroup, originalTarget);
                EditorUserBuildSettings.development = originalDev;
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, originalBackend);
                PlayerSettings.Android.targetArchitectures = originalArchitecture;
                PlayerSettings.defaultInterfaceOrientation = originalOrientation;
            }
        }

        [MenuItem("Stonehold/Win64/Build Standalone Win64")]
        public static void BuildWin64()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                scenes = new string[] { "Assets/_Game/Scenes/V2/GameplayIntegration_V2.unity" };
            }

            string relativeOutputPath = "Builds/Win64/StoneholdV2.exe";
            string outputPath = Path.GetFullPath(relativeOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log($"Standalone Win64 build result: {summary.result}; size: {summary.totalSize}; duration: {summary.totalTime}");
            if (summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Win64 Build failed: {summary.result}");
        }

        private const string AndroidV2OutputPath = "Builds/Android/StoneholdV2.apk";

        [MenuItem("Stonehold/Android/Build Android APK")]
        public static void BuildAndroid()
        {
            string[] preferredScenes = new[]
            {
                "Assets/_Game/Scenes/MainMenu.unity",
                "Assets/_Game/Scenes/GameScene.unity",
                "Assets/_Game/Scenes/V2/GameplayIntegration_V2.unity"
            };

            string[] scenes = preferredScenes.Where(File.Exists).ToArray();
            if (scenes.Length == 0)
            {
                scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray();
            }

            if (scenes.Length == 0)
            {
                scenes = new string[] { "Assets/_Game/Scenes/V2/GameplayIntegration_V2.unity" };
            }

            string outputPath = Path.GetFullPath(AndroidV2OutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            BuildTargetGroup originalGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            BuildTarget originalTarget = EditorUserBuildSettings.activeBuildTarget;
            bool originalDev = EditorUserBuildSettings.development;
            ScriptingImplementation originalBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
            AndroidArchitecture originalArchitecture = PlayerSettings.Android.targetArchitectures;

            try
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                EditorUserBuildSettings.development = false;

                try
                {
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ReleaseCandidateBuild] IL2CPP or ARM64 setting warning: {ex.Message}");
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                }

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;

                Debug.Log($"Android Build result: {summary.result}; size: {summary.totalSize}; duration: {summary.totalTime}");
                if (summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException($"Android Build failed: {summary.result}");
            }
            finally
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(originalGroup, originalTarget);
                EditorUserBuildSettings.development = originalDev;
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, originalBackend);
                PlayerSettings.Android.targetArchitectures = originalArchitecture;
            }
        }
    }
}
