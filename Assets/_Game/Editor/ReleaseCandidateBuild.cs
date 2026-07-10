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
        private const string AndroidOutput = "Builds/Android/Stonehold-Development.apk";

        public static void BuildAndroidDevelopment()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes are configured in Build Settings.");

            string outputPath = Path.GetFullPath(AndroidOutput);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.development = true;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log($"Android build result: {summary.result}; size: {summary.totalSize}; duration: {summary.totalTime}");
            if (summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Android build failed: {summary.result}");
        }
    }
}
