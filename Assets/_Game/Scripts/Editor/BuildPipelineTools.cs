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
        private static bool isRunningPlayModeTests;

        [MenuItem("Stonehold/Tests/Run All PlayMode Tests")]
        public static void RunAllPlayModeTests()
        {
            Debug.Log("[TestRunner] Starting Automated PlayMode Test Execution...");
            isRunningPlayModeTests = true;
            var api = ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();
            var callbacks = new PlayModeTestCallbacks();
            api.RegisterCallbacks(callbacks);
            api.Execute(new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings(
                new UnityEditor.TestTools.TestRunner.Api.Filter()
                {
                    testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode
                }
            ));

            EditorApplication.update += TestUpdateWait;
        }

        private static void TestUpdateWait()
        {
            if (!isRunningPlayModeTests)
            {
                EditorApplication.update -= TestUpdateWait;
            }
        }

        private class PlayModeTestCallbacks : UnityEditor.TestTools.TestRunner.Api.ICallbacks
        {
            public void RunStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor testsToRun)
            {
                Debug.Log($"[TestRunner] PlayMode test run started. Total test cases: {testsToRun.TestCaseCount}");
            }

            public void RunFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result)
            {
                isRunningPlayModeTests = false;
                int passed = result.PassCount;
                int failed = result.FailCount;
                int skipped = result.SkipCount;
                int total = passed + failed + skipped;

                Debug.Log($"[TestRunner] PlayMode tests finished! Total: {total}, Passed: {passed}, Failed: {failed}, Inconclusive/Skipped: {skipped}");
                if (failed > 0)
                {
                    Debug.LogError($"[TestRunner] ❌ {failed} PlayMode tests failed!");
                }
                else
                {
                    Debug.Log($"[TestRunner] ✅ ALL {passed} PlayMode tests passed (100%)!");
                }
                EditorApplication.Exit(failed > 0 ? 1 : 0);
            }

            public void TestStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor test) { }

            public void TestFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result)
            {
                if (result.TestStatus == UnityEditor.TestTools.TestRunner.Api.TestStatus.Failed)
                {
                    Debug.LogError($"[TestRunner] FAILED: {result.Test.FullName} - {result.Message}");
                }
            }
        }
    }
}
#endif
