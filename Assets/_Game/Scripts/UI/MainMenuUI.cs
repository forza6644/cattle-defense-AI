using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Stonehold
{
    /// <summary>
    /// Main menu, built in code (responsive CanvasScaler, legacy uGUI Text):
    /// Play loads the game scene, Settings offers real quality levels, Quit exits
    /// the app on desktop and is hidden on mobile platforms.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "GameScene";

        private Font font;
        private RectTransform canvasRect;
        private CanvasGroup settingsGroup;
        private Text qualityLabel;
        private RectTransform titleRect;
        private float introTime;
        private Text statsText;
        private Text stageNameText;
        private Text stageDescText;
        private Button prevStageBtn;
        private Button nextStageBtn;
        private Button startButton;
        private Text startButtonLabel;

        private Text defenderNameText;
        private Button prevDefenderBtn;
        private Button nextDefenderBtn;
        private int currentDefenderIndex = 0;

        private struct DefenderInfo
        {
            public string id;
            public string displayName;
            public DefenderInfo(string id, string displayName)
            {
                this.id = id;
                this.displayName = displayName;
            }
        }

        private readonly DefenderInfo[] lobbyDefenders = new DefenderInfo[]
        {
            new DefenderInfo("archer_defender", "Archer Defender"),
            new DefenderInfo("machine_gun_soldier", "Machine Gun Soldier"),
            new DefenderInfo("catapult_defender", "Catapult Defender"),
            new DefenderInfo("ice_mage", "Ice Mage"),
            new DefenderInfo("sniper", "Sniper")
        };

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildMenu();
        }

        private void Update()
        {
            if (titleRect == null)
            {
                return;
            }

            introTime += Time.unscaledDeltaTime;
            float pop = introTime < 0.45f ? Mathf.SmoothStep(0.55f, 1f, introTime / 0.45f) : 1f;
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 1.6f) * 0.02f;
            titleRect.localScale = Vector3.one * (pop * pulse);
        }

        private void Play()
        {
            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeToScene(gameSceneName);
            }
            else
            {
                SceneManager.LoadScene(gameSceneName);
            }
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetQuality(int level)
        {
            QualitySettings.SetQualityLevel(level, true);
            RefreshQualityLabel();
        }

        private void RefreshQualityLabel()
        {
            qualityLabel.text = "Quality: " + QualitySettings.names[QualitySettings.GetQualityLevel()];
        }

        private void ShowSettings(bool visible)
        {
            settingsGroup.alpha = visible ? 1f : 0f;
            settingsGroup.interactable = visible;
            settingsGroup.blocksRaycasts = visible;
        }

        // ------------------------------------------------------------ UI build

        private void BuildMenu()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            GameObject canvasObject = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasRect = canvas.GetComponent<RectTransform>();

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Background
            Image background = CreateImage(canvasRect, "Background", new Color(0.07f, 0.09f, 0.14f));
            Stretch(background.rectTransform);

            // Title
            Text title = CreateText(canvasRect, "Title", "STONEHOLD", 110, new Color(1f, 0.85f, 0.35f));
            title.fontStyle = FontStyle.Bold;
            Place(title.rectTransform, new Vector2(0.5f, 0.74f), Vector2.zero, new Vector2(1200f, 140f));
            titleRect = title.rectTransform;

            Text subtitle = CreateText(canvasRect, "Subtitle", "Defend the keep. Hold the line.", 30, new Color(0.8f, 0.8f, 0.85f));
            Place(subtitle.rectTransform, new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(900f, 50f));

            // Main buttons
            startButton = CreateButton(canvasRect, "StartButton", "Start Stage", new Vector2(340f, 84f), new Vector2(0.5f, 0.30f), Vector2.zero, Play);
            startButtonLabel = startButton.GetComponentInChildren<Text>();

            CreateButton(canvasRect, "SettingsButton", "Settings", new Vector2(340f, 70f), new Vector2(0.5f, 0.20f), Vector2.zero, () => ShowSettings(true));

            if (!Application.isMobilePlatform)
            {
                CreateButton(canvasRect, "QuitButton", "Quit", new Vector2(340f, 70f), new Vector2(0.5f, 0.11f), Vector2.zero, QuitGame);
            }

            // Stats (top-left)
            statsText = CreateText(canvasRect, "StatsText", "", 24, new Color(0.85f, 0.85f, 0.9f));
            statsText.alignment = TextAnchor.UpperLeft;
            Place(statsText.rectTransform, new Vector2(0f, 1f), new Vector2(220f, -120f), new Vector2(400f, 200f));
            RefreshStats();

            // Reset Button (top-right)
            CreateButton(canvasRect, "ResetStatsButton", "Reset Stats", new Vector2(200f, 54f),
                new Vector2(1f, 1f), new Vector2(-120f, -50f), ResetStats);

            // Stage Selection Panel (center-top)
            Image stageBg = CreateImage(canvasRect, "StagePanel", new Color(0.12f, 0.16f, 0.24f, 0.6f));
            Place(stageBg.rectTransform, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(700f, 130f));

            stageNameText = CreateText(stageBg.rectTransform, "StageName", "", 24, Color.white);
            Place(stageNameText.rectTransform, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(600f, 32f));

            stageDescText = CreateText(stageBg.rectTransform, "StageDesc", "", 18, new Color(0.8f, 0.8f, 0.85f));
            Place(stageDescText.rectTransform, new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(600f, 60f));

            prevStageBtn = CreateButton(stageBg.rectTransform, "PrevStage", "<", new Vector2(46f, 46f),
                new Vector2(0f, 0.5f), new Vector2(30f, 0f), () => CycleStage(-1));

            nextStageBtn = CreateButton(stageBg.rectTransform, "NextStage", ">", new Vector2(46f, 46f),
                new Vector2(1f, 0.5f), new Vector2(-30f, 0f), () => CycleStage(1));

            // Starting Defender Selection Panel (center-lower)
            Image defenderBg = CreateImage(canvasRect, "DefenderPanel", new Color(0.12f, 0.16f, 0.24f, 0.6f));
            Place(defenderBg.rectTransform, new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(700f, 100f));

            Text defenderTitleText = CreateText(defenderBg.rectTransform, "DefenderTitle", "STARTING DEFENDER", 16, new Color(1f, 0.85f, 0.35f));
            Place(defenderTitleText.rectTransform, new Vector2(0.5f, 0.80f), Vector2.zero, new Vector2(600f, 24f));

            defenderNameText = CreateText(defenderBg.rectTransform, "DefenderName", "", 24, Color.white);
            Place(defenderNameText.rectTransform, new Vector2(0.5f, 0.35f), Vector2.zero, new Vector2(600f, 36f));

            prevDefenderBtn = CreateButton(defenderBg.rectTransform, "PrevDefender", "<", new Vector2(46f, 46f),
                new Vector2(0f, 0.5f), new Vector2(30f, 0f), () => CycleDefender(-1));

            nextDefenderBtn = CreateButton(defenderBg.rectTransform, "NextDefender", ">", new Vector2(46f, 46f),
                new Vector2(1f, 0.5f), new Vector2(-30f, 0f), () => CycleDefender(1));

            // Initialize starting defender index from saved settings
            string savedId = SaveManager.SelectedStartingDefenderId;
            currentDefenderIndex = 0;
            for (int i = 0; i < lobbyDefenders.Length; i++)
            {
                if (lobbyDefenders[i].id == savedId)
                {
                    currentDefenderIndex = i;
                    break;
                }
            }
            RefreshDefenderSelection();

            // Offers / Events Placeholder (right-side)
            Image offersBg = CreateImage(canvasRect, "OffersPanel", new Color(0.12f, 0.16f, 0.24f, 0.6f));
            Place(offersBg.rectTransform, new Vector2(1f, 0.5f), new Vector2(-220f, -50f), new Vector2(320f, 400f));

            Text offersTitle = CreateText(offersBg.rectTransform, "Title", "OFFERS & EVENTS", 24, new Color(1f, 0.85f, 0.35f));
            Place(offersTitle.rectTransform, new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(280f, 40f));

            Text offersBody = CreateText(offersBg.rectTransform, "Body", "Offers / Events\ncoming soon", 20, new Color(0.7f, 0.7f, 0.75f));
            Place(offersBody.rectTransform, new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(280f, 200f));

            RefreshStageSelection();
            BuildSettingsPanel();
        }

        private void BuildSettingsPanel()
        {
            Image dim = CreateImage(canvasRect, "SettingsPanel", new Color(0f, 0f, 0f, 0.8f));
            Stretch(dim.rectTransform);

            Text header = CreateText(dim.rectTransform, "Header", "Settings", 64, Color.white);
            header.fontStyle = FontStyle.Bold;
            Place(header.rectTransform, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(700f, 90f));

            qualityLabel = CreateText(dim.rectTransform, "QualityLabel", "Quality:", 32, new Color(0.85f, 0.85f, 0.9f));
            Place(qualityLabel.rectTransform, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(700f, 46f));

            string[] names = QualitySettings.names;
            float spacing = 240f;
            float startX = -(Mathf.Min(names.Length, 3) - 1) * spacing / 2f;
            int shown = Mathf.Min(names.Length, 3);
            for (int i = 0; i < shown; i++)
            {
                // Map the shown buttons across the available quality levels.
                int level = names.Length <= 3 ? i : Mathf.RoundToInt(i * (names.Length - 1) / (float)(shown - 1));
                string label = names[level];
                CreateButton(dim.rectTransform, "Quality_" + label, label, new Vector2(220f, 64f),
                    new Vector2(0.5f, 0.46f), new Vector2(startX + i * spacing, 0f), () => SetQuality(level));
            }

            CreateVolumeRow(dim.rectTransform, "Master", 0.38f,
                () => AudioManager.Instance != null ? AudioManager.Instance.MasterVolume : 1f,
                v => { if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(v); });
            CreateVolumeRow(dim.rectTransform, "Music", 0.30f,
                () => AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.6f,
                v => { if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v); });
            CreateVolumeRow(dim.rectTransform, "SFX", 0.22f,
                () => AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 0.9f,
                v => { if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(v); });

            CreateButton(dim.rectTransform, "BackButton", "Back", new Vector2(280f, 60f),
                new Vector2(0.5f, 0.1f), Vector2.zero, () => ShowSettings(false));

            settingsGroup = dim.gameObject.AddComponent<CanvasGroup>();
            ShowSettings(false);
            RefreshQualityLabel();
        }

        /// <summary>A label + [-]/[+] control that steps a 0..1 volume in 0.1 increments.</summary>
        private void CreateVolumeRow(RectTransform parent, string label, float y,
            System.Func<float> getter, System.Action<float> setter)
        {
            Text nameLabel = CreateText(parent, label + "Label", label, 28, new Color(0.85f, 0.85f, 0.9f));
            Place(nameLabel.rectTransform, new Vector2(0.5f, y), new Vector2(-260f, 0f), new Vector2(220f, 46f));

            Text value = CreateText(parent, label + "Value", "", 28, Color.white);
            Place(value.rectTransform, new Vector2(0.5f, y), new Vector2(60f, 0f), new Vector2(120f, 46f));
            System.Action refresh = () => value.text = Mathf.RoundToInt(getter() * 100f) + "%";
            refresh();

            CreateButton(parent, label + "Minus", "-", new Vector2(60f, 56f), new Vector2(0.5f, y), new Vector2(-60f, 0f),
                () => { setter(Mathf.Clamp01(getter() - 0.1f)); refresh(); });
            CreateButton(parent, label + "Plus", "+", new Vector2(60f, 56f), new Vector2(0.5f, y), new Vector2(180f, 0f),
                () => { setter(Mathf.Clamp01(getter() + 0.1f)); refresh(); });
        }

        // ------------------------------------------------------------- Helpers

        private Text CreateText(RectTransform parent, string name, string content, int size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(RectTransform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Button CreateButton(RectTransform parent, string name, string label, Vector2 size,
            Vector2 anchor, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            Image bg = CreateImage(parent, name, Color.white);
            bg.raycastTarget = true;
            Place(bg.rectTransform, anchor, position, size);

            Button button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock cb = button.colors;
            cb.normalColor = new Color(0.20f, 0.24f, 0.32f, 0.95f);
            cb.highlightedColor = new Color(0.28f, 0.34f, 0.45f, 1f);
            cb.pressedColor = new Color(0.14f, 0.18f, 0.24f, 1f);
            cb.selectedColor = new Color(0.24f, 0.28f, 0.38f, 1f);
            cb.disabledColor = new Color(0.12f, 0.14f, 0.18f, 0.6f);
            button.colors = cb;

            button.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayButton();
                }

                onClick();
            });

            Text text = CreateText(bg.rectTransform, "Label", label, 30, Color.white);
            Stretch(text.rectTransform);
            return button;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void RefreshStats()
        {
            if (statsText != null)
            {
                statsText.text = "<color=#ffd759><b>KEEP STATISTICS</b></color>\n\n" +
                                 "Best Wave Reached:  " + SaveManager.BestWave + "\n" +
                                 "Total Victories:            " + SaveManager.TotalWins + "\n" +
                                 "Total Defeats:              " + SaveManager.TotalLosses + "\n" +
                                 "Total Runs Played:     " + SaveManager.TotalRuns;
            }
        }

        private void ResetStats()
        {
            SaveManager.ResetProgress();
            RefreshStats();
            RefreshStageSelection();

            // Reset starting defender index in UI
            currentDefenderIndex = 0;
            RefreshDefenderSelection();
        }

        private void RefreshStageSelection()
        {
            int selected = SaveManager.SelectedStageIndex;
            if (stageNameText != null && stageDescText != null)
            {
                if (selected == 0)
                {
                    stageNameText.text = "<b>Stage 1: Castle Road</b>";
                    stageDescText.text = "Defend the road to the keep against grunts, armored troops, runners, and the final boss.\n" +
                                         "<color=#ffd759>Progress: " + (SaveManager.Stage1Completed ? "Completed" : "Not Cleared") + "</color>";
                    if (startButtonLabel != null) startButtonLabel.text = "Start Stage";
                    if (startButton != null) startButton.interactable = true;
                }
                else
                {
                    stageNameText.text = "<b>Stage 2: Highlands</b>";
                    bool isUnlocked = SaveManager.HighestStageUnlocked >= 2;
                    stageDescText.text = isUnlocked
                        ? "Defend the highland pass. Expected enemy types: Giants, Wyverns.\n<color=#ffd759>Progress: Not Cleared</color>"
                        : "Defend the highland pass. Expected enemy types: Giants, Wyverns.\n<color=#ff5959>LOCKED: Complete Stage 1 to unlock</color>";

                    if (startButtonLabel != null) startButtonLabel.text = isUnlocked ? "Start Stage" : "LOCKED";
                    if (startButton != null) startButton.interactable = isUnlocked;
                }
            }
        }

        private void CycleStage(int delta)
        {
            int newIndex = SaveManager.SelectedStageIndex + delta;
            if (newIndex >= 0 && newIndex < 2)
            {
                SaveManager.SetSelectedStage(newIndex);
                RefreshStageSelection();
            }
        }

        private void CycleDefender(int delta)
        {
            currentDefenderIndex += delta;
            if (currentDefenderIndex < 0) currentDefenderIndex = lobbyDefenders.Length - 1;
            if (currentDefenderIndex >= lobbyDefenders.Length) currentDefenderIndex = 0;

            SaveManager.SetSelectedStartingDefender(lobbyDefenders[currentDefenderIndex].id);
            RefreshDefenderSelection();
        }

        private void RefreshDefenderSelection()
        {
            if (defenderNameText != null)
            {
                defenderNameText.text = $"<b>{lobbyDefenders[currentDefenderIndex].displayName}</b>";
            }
        }
    }
}
