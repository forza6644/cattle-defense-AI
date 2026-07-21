using System.IO;
using UnityEditor;
using UnityEngine;

namespace Stonehold.Editor
{
    public static class ParityRecorder
    {
        private const string OutputDir = @"C:\Users\forza\OneDrive\Desktop\Stonehold-GameplayParity-Recordings";

        [MenuItem("Stonehold/Capture Parity Gameplay Screenshots")]
        public static void CaptureParityScreenshots()
        {
            if (!Directory.Exists(OutputDir))
            {
                Directory.CreateDirectory(OutputDir);
            }

            string screenshotPath = Path.Combine(OutputDir, "Stonehold_Parity_Battlefield.png");
            ScreenCapture.CaptureScreenshot(screenshotPath);
            Debug.Log($"[ParityRecorder] Saved Game View screenshot to: {screenshotPath}");
        }
    }
}
