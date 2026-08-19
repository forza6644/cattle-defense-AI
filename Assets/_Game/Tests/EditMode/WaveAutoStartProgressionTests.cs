using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Stonehold.Tests
{
    public class WaveAutoStartProgressionTests
    {
        [Test]
        public void UIManager_BuildUI_DoesNotCreateStartButtonOrCountdownBox()
        {
            GameObject uiGo = new GameObject("UIManager_Test", typeof(UIManager));
            UIManager ui = uiGo.GetComponent<UIManager>();

            try
            {
                // Force UI Build
                MethodInfo buildUiMethod = typeof(UIManager).GetMethod("EnsureUIBuilt", BindingFlags.NonPublic | BindingFlags.Instance);
                buildUiMethod?.Invoke(ui, null);

                // Verify that no START button or WaveControl panel exists in the hierarchy
                Button[] buttons = uiGo.GetComponentsInChildren<Button>(true);
                foreach (Button btn in buttons)
                {
                    Assert.AreNotEqual("StartNextWaveButton", btn.gameObject.name, "StartNextWaveButton must not exist.");
                    Text txt = btn.GetComponentInChildren<Text>();
                    if (txt != null)
                    {
                        Assert.AreNotEqual("START", txt.text.Trim(), "Manual START button text must not exist.");
                    }
                }

                Transform waveControl = uiGo.transform.Find("WaveControl");
                Assert.IsNull(waveControl, "WaveControl countdown panel should not exist in the new passive auto-start design.");
            }
            finally
            {
                Object.DestroyImmediate(uiGo);
            }
        }

        [Test]
        public void UIManager_OnWaveCountdownStarted_FormatsCenteredTransitionBanner()
        {
            GameObject uiGo = new GameObject("UIManager_Test", typeof(UIManager));
            UIManager ui = uiGo.GetComponent<UIManager>();

            try
            {
                MethodInfo buildUiMethod = typeof(UIManager).GetMethod("EnsureUIBuilt", BindingFlags.NonPublic | BindingFlags.Instance);
                buildUiMethod?.Invoke(ui, null);

                FieldInfo bannerTextField = typeof(UIManager).GetField("bannerText", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(bannerTextField, "bannerText field must exist.");

                WaveData dummyWave = ScriptableObject.CreateInstance<WaveData>();
                dummyWave.waveLabel = "Heavy Flank Pressure";

                MethodInfo onCountdownStarted = typeof(UIManager).GetMethod("OnWaveCountdownStarted", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(onCountdownStarted, "OnWaveCountdownStarted method must exist.");

                onCountdownStarted.Invoke(ui, new object[] { 4, dummyWave, 2.5f });

                Text bannerText = (Text)bannerTextField.GetValue(ui);
                Assert.IsNotNull(bannerText, "bannerText instance must not be null.");
                Assert.IsTrue(bannerText.text.Contains("NEXT WAVE — 4"), $"Banner text was '{bannerText.text}', expected to contain 'NEXT WAVE — 4'.");
                Assert.IsTrue(bannerText.text.Contains("HEAVY FLANK PRESSURE"), $"Banner text was '{bannerText.text}', expected to contain 'HEAVY FLANK PRESSURE'.");

                Object.DestroyImmediate(dummyWave);
            }
            finally
            {
                Object.DestroyImmediate(uiGo);
            }
        }

        [Test]
        public void WaveManager_WaitForWaveStart_CompletesAutomatically()
        {
            GameObject waveGo = new GameObject("WaveManager_Test", typeof(WaveManager));
            WaveManager waves = waveGo.GetComponent<WaveManager>();

            GameConfig config = ScriptableObject.CreateInstance<GameConfig>();
            config.timeBetweenWaves = 0.1f; // Short duration for test

            FieldInfo configField = typeof(WaveManager).GetField("config", BindingFlags.NonPublic | BindingFlags.Instance);
            configField?.SetValue(waves, config);

            try
            {
                MethodInfo waitMethod = typeof(WaveManager).GetMethod("WaitForWaveStart", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(waitMethod, "WaitForWaveStart must exist.");

                IEnumerator routine = (IEnumerator)waitMethod.Invoke(waves, new object[] { 1, null });
                Assert.IsNotNull(routine);

                // First MoveNext enters the coroutine and initializes countdown
                Assert.IsTrue(routine.MoveNext());
                Assert.IsTrue(waves.IsWaitingForWave);
                Assert.Greater(waves.NextWaveCountdown, 0f);

                // Step the coroutine forward (simulating frames) until countdown finishes
                int steps = 0;
                while (routine.MoveNext() && steps < 200)
                {
                    steps++;
                }

                // After wait time has elapsed, IsWaitingForWave becomes false automatically
                Assert.IsFalse(waves.IsWaitingForWave, "WaveManager must automatically exit waiting state without player input.");
                Assert.AreEqual(0f, waves.NextWaveCountdown, "NextWaveCountdown must reach 0 upon auto-start.");

                Object.DestroyImmediate(config);
            }
            finally
            {
                Object.DestroyImmediate(waveGo);
            }
        }
        [Test]
        public void UIManager_CastleHealthBar_ExistsAndUpdatesCorrectly()
        {
            GameObject uiGo = new GameObject("UIManager_Test", typeof(UIManager));
            UIManager ui = uiGo.GetComponent<UIManager>();

            GameObject castleGo = new GameObject("Castle_Test", typeof(Castle));
            Castle castle = castleGo.GetComponent<Castle>();

            FieldInfo castleField = typeof(UIManager).GetField("castle", BindingFlags.NonPublic | BindingFlags.Instance);
            castleField?.SetValue(ui, castle);

            try
            {
                MethodInfo buildUiMethod = typeof(UIManager).GetMethod("EnsureUIBuilt", BindingFlags.NonPublic | BindingFlags.Instance);
                buildUiMethod?.Invoke(ui, null);

                FieldInfo safeAreaField = typeof(UIManager).GetField("safeAreaRect", BindingFlags.NonPublic | BindingFlags.Instance);
                RectTransform safeArea = (RectTransform)safeAreaField?.GetValue(ui);
                Assert.IsNotNull(safeArea, "SafeArea container must exist.");

                Transform hpBar = safeArea.Find("CastleHpBar");
                Assert.IsNotNull(hpBar, "CastleHpBar must exist within SafeArea.");

                Text hpText = hpBar.GetComponentInChildren<Text>();
                Assert.IsNotNull(hpText, "CastleHpBar must contain a Text component.");

                // Test RefreshCastleHealth
                MethodInfo refreshMethod = typeof(UIManager).GetMethod("RefreshCastleHealth", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(refreshMethod);
                refreshMethod.Invoke(ui, null);

                Assert.IsTrue(hpText.text.StartsWith("CASTLE"), $"Castle HP text was '{hpText.text}', expected to start with 'CASTLE'.");
            }
            finally
            {
                Object.DestroyImmediate(uiGo);
                Object.DestroyImmediate(castleGo);
            }
        }

        [Test]
        public void UIManager_WaveBannerPanel_PositionedAtTopCenter()
        {
            GameObject uiGo = new GameObject("UIManager_Test", typeof(UIManager));
            UIManager ui = uiGo.GetComponent<UIManager>();

            try
            {
                MethodInfo buildUiMethod = typeof(UIManager).GetMethod("EnsureUIBuilt", BindingFlags.NonPublic | BindingFlags.Instance);
                buildUiMethod?.Invoke(ui, null);

                FieldInfo safeAreaField = typeof(UIManager).GetField("safeAreaRect", BindingFlags.NonPublic | BindingFlags.Instance);
                RectTransform safeArea = (RectTransform)safeAreaField?.GetValue(ui);
                Assert.IsNotNull(safeArea, "SafeArea container must exist.");

                Transform banner = safeArea.Find("WaveBannerPanel");
                Assert.IsNotNull(banner, "WaveBannerPanel must exist within SafeArea.");

                RectTransform bannerRt = banner.GetComponent<RectTransform>();
                Assert.IsNotNull(bannerRt);

                // Verify Top-Center anchor configuration
                Assert.AreEqual(0.5f, bannerRt.anchorMin.x, 0.001f, "Banner anchorMin.x should be 0.5 (centered).");
                Assert.AreEqual(1.0f, bannerRt.anchorMin.y, 0.001f, "Banner anchorMin.y should be 1.0 (top).");
                Assert.AreEqual(0.5f, bannerRt.anchorMax.x, 0.001f, "Banner anchorMax.x should be 0.5 (centered).");
                Assert.AreEqual(1.0f, bannerRt.anchorMax.y, 0.001f, "Banner anchorMax.y should be 1.0 (top).");
                Assert.AreEqual(0.5f, bannerRt.pivot.x, 0.001f, "Banner pivot.x should be 0.5.");
                Assert.AreEqual(1.0f, bannerRt.pivot.y, 0.001f, "Banner pivot.y should be 1.0.");

                // Verify Y position is below top HUD
                Assert.LessOrEqual(bannerRt.anchoredPosition.y, -120f, "Banner should be positioned below the top HUD elements.");
            }
            finally
            {
                Object.DestroyImmediate(uiGo);
            }
        }
    }
}
