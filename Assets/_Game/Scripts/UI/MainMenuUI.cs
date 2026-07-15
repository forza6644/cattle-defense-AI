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
        private Text stageNumText;
        private Text stageRewardText;
        private Button prevStageBtn;
        private Button nextStageBtn;
        private Button startButton;
        private Text startButtonLabel;
        private Text currencyText;

        [SerializeField] private HeroDefinition[] heroDefinitions;
        public void SetHeroDefinitions(HeroDefinition[] definitions)
        {
            heroDefinitions = definitions;
        }

        private Text defenderNameText;
        private Text defenderStatsText;
        private Button prevDefenderBtn;
        private Button nextDefenderBtn;
        private Text metaLevelText;
        private Text upgradeCostText;
        private Button upgradeDefenderBtn;
        private Text upgradeDefenderBtnLabel;
        private int currentDefenderIndex = 0;
        private GameObject activePreviewInstance;

        private Text[] metaUpgradeNameTexts = new Text[5];
        private Button[] metaUpgradeButtons = new Button[5];
        private Text[] metaUpgradeButtonLabels = new Text[5];

        private void Awake()
        {
            CleanupGeneratedMenuObjects();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (FindAnyObjectByType<MetaUpgradeManager>() == null)
            {
                GameObject managerGo = new GameObject("MetaUpgradeManager", typeof(MetaUpgradeManager));
                DontDestroyOnLoad(managerGo);
            }

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.07f, 0.09f, 0.14f);
            }
        }

        private void Start()
        {
            StartCoroutine(BuildMenuDelayed());
        }

        private System.Collections.IEnumerator BuildMenuDelayed()
        {
            yield return null;
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
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasRect = canvas.GetComponent<RectTransform>();

            bool isPortrait = Screen.width < Screen.height;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            if (isPortrait)
            {
                scaler.referenceResolution = new Vector2(1080f, 1920f);
            }
            else
            {
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }
            scaler.matchWidthOrHeight = 0.5f;

            // Background
            Image background = CreateImage(canvasRect, "Background", new Color(0.07f, 0.09f, 0.14f, 0f));
            Stretch(background.rectTransform);

            // Safe Area
            RectTransform safeAreaRect = CreateSafeArea(canvasRect);

            // 1. Top Header Bar
            Image headerBar = CreateImage(safeAreaRect, "HeaderBar", new Color(0.05f, 0.06f, 0.08f, 0.95f));
            if (isPortrait)
            {
                headerBar.rectTransform.anchorMin = new Vector2(0f, 1f);
                headerBar.rectTransform.anchorMax = new Vector2(1f, 1f);
                headerBar.rectTransform.pivot = new Vector2(0.5f, 1f);
                headerBar.rectTransform.offsetMin = new Vector2(0f, -100f);
                headerBar.rectTransform.offsetMax = Vector2.zero;
            }
            else
            {
                Place(headerBar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1920f, 100f));
            }

            // Player Profile (Top-Left)
            Image profileAvatar = CreateImage(headerBar.rectTransform, "ProfileAvatar", new Color(0.2f, 0.28f, 0.4f, 1.0f));
            Place(profileAvatar.rectTransform, new Vector2(0f, 0.5f), new Vector2(80f, 0f), new Vector2(64f, 64f));

            Text profileName = CreateText(headerBar.rectTransform, "ProfileName", "Commander_01", 24, new Color(1f, 0.85f, 0.35f));
            profileName.alignment = TextAnchor.MiddleLeft;
            Place(profileName.rectTransform, new Vector2(0f, 0.5f), new Vector2(250f, 12f), new Vector2(240f, 32f));

            Text profileLevel = CreateText(headerBar.rectTransform, "ProfileLevel", "Lv.15", 18, Color.white);
            profileLevel.alignment = TextAnchor.MiddleLeft;
            Place(profileLevel.rectTransform, new Vector2(0f, 0.5f), new Vector2(250f, -14f), new Vector2(240f, 24f));

            // Currencies (Top-Right Area)
            currencyText = CreateText(headerBar.rectTransform, "Currencies", "", isPortrait ? 15 : 22, new Color(0.9f, 0.9f, 0.95f));
            if (isPortrait)
            {
                Place(currencyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(320f, 40f));
                currencyText.alignment = TextAnchor.MiddleCenter;
            }
            else
            {
                Place(currencyText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-480f, 0f), new Vector2(500f, 40f));
                currencyText.alignment = TextAnchor.MiddleRight;
            }
            RefreshCurrencies();

            // Settings & Quit Buttons in Top Bar
            CreateButton(headerBar.rectTransform, "SettingsButton", "Settings", new Vector2(160f, 54f),
                new Vector2(1f, 0.5f), new Vector2(-110f, 0f), () => ShowSettings(true));

            if (!Application.isMobilePlatform)
            {
                CreateButton(headerBar.rectTransform, "QuitButton", "Quit", new Vector2(120f, 54f),
                    new Vector2(1f, 0.5f), new Vector2(-260f, 0f), QuitGame);
            }

            // 2. Title
            Text title = CreateText(safeAreaRect, "Title", "STONEHOLD", 110, new Color(1f, 0.85f, 0.35f));
            title.fontStyle = FontStyle.Bold;
            if (isPortrait)
            {
                Place(title.rectTransform, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(1000f, 130f));
            }
            else
            {
                Place(title.rectTransform, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(1200f, 130f));
            }
            titleRect = title.rectTransform;

            Text subtitle = CreateText(safeAreaRect, "Subtitle", "Defend the keep. Hold the line.", 24, new Color(0.8f, 0.8f, 0.85f));
            if (isPortrait)
            {
                Place(subtitle.rectTransform, new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(900f, 40f));
            }
            else
            {
                Place(subtitle.rectTransform, new Vector2(0.5f, 0.70f), Vector2.zero, new Vector2(900f, 40f));
            }

            // 3. Stats & Progress Panel (Left Side Container)
            Image statsBg = CreateImage(safeAreaRect, "StatsPanel", new Color(0.12f, 0.16f, 0.24f, 0.5f));
            if (isPortrait)
            {
                Place(statsBg.rectTransform, new Vector2(0.5f, 0.24f), new Vector2(-220f, 0f), new Vector2(400f, 440f));
            }
            else
            {
                Place(statsBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(260f, 50f), new Vector2(400f, 400f));
            }

            statsText = CreateText(statsBg.rectTransform, "StatsText", "", 22, new Color(0.85f, 0.85f, 0.9f));
            statsText.alignment = TextAnchor.UpperLeft;
            Place(statsText.rectTransform, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(340f, 260f));
            RefreshStats();

            CreateButton(statsBg.rectTransform, "ResetStatsButton", "Reset Stats", new Vector2(180f, 48f),
                new Vector2(0.5f, 0.12f), Vector2.zero, ResetStats);

            // 4. Central Stage Select Panel
            Image stageBg = CreateImage(safeAreaRect, "StagePanel", new Color(0.12f, 0.16f, 0.24f, 0.8f));
            if (isPortrait)
            {
                Place(stageBg.rectTransform, new Vector2(0.5f, 0.76f), Vector2.zero, new Vector2(800f, 220f));
            }
            else
            {
                Place(stageBg.rectTransform, new Vector2(0.5f, 0.53f), Vector2.zero, new Vector2(800f, 220f));
            }

            stageNumText = CreateText(stageBg.rectTransform, "StageNumText", "STAGE 1", 20, new Color(1f, 0.85f, 0.35f));
            Place(stageNumText.rectTransform, new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(700f, 30f));

            stageNameText = CreateText(stageBg.rectTransform, "StageName", "", 32, Color.white);
            stageNameText.fontStyle = FontStyle.Bold;
            Place(stageNameText.rectTransform, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(700f, 44f));

            stageDescText = CreateText(stageBg.rectTransform, "StageDesc", "", 18, new Color(0.85f, 0.85f, 0.9f));
            Place(stageDescText.rectTransform, new Vector2(0.5f, 0.34f), Vector2.zero, new Vector2(700f, 60f));

            stageRewardText = CreateText(stageBg.rectTransform, "StageRewardText", "Rewards: 🪙 Gold  💎 Gems  📦 Loot Box", 16, new Color(0.7f, 0.7f, 0.75f));
            Place(stageRewardText.rectTransform, new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(700f, 24f));

            prevStageBtn = CreateButton(stageBg.rectTransform, "PrevStage", "<", new Vector2(46f, 46f),
                new Vector2(0f, 0.5f), new Vector2(30f, 0f), () => CycleStage(-1));

            nextStageBtn = CreateButton(stageBg.rectTransform, "NextStage", ">", new Vector2(46f, 46f),
                new Vector2(1f, 0.5f), new Vector2(-30f, 0f), () => CycleStage(1));

            // 5. Starting Defender Panel
            Image defenderBg = CreateImage(safeAreaRect, "DefenderPanel", new Color(0.12f, 0.16f, 0.24f, 0.7f));
            if (isPortrait)
            {
                Place(defenderBg.rectTransform, new Vector2(0.5f, 0.60f), Vector2.zero, new Vector2(800f, 340f));
            }
            else
            {
                Place(defenderBg.rectTransform, new Vector2(0.5f, 0.40f), new Vector2(0f, -20f), new Vector2(800f, 280f));
            }

            Text defenderTitleText = CreateText(defenderBg.rectTransform, "DefenderTitle", "STARTING DEFENDER", 16, new Color(1f, 0.85f, 0.35f));
            Place(defenderTitleText.rectTransform, new Vector2(0.5f, 0.90f), Vector2.zero, new Vector2(700f, 24f));

            defenderNameText = CreateText(defenderBg.rectTransform, "DefenderName", "", 26, Color.white);
            Place(defenderNameText.rectTransform, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(700f, 36f));

            prevDefenderBtn = CreateButton(defenderBg.rectTransform, "PrevDefender", "<", new Vector2(46f, 46f),
                new Vector2(0f, 0.78f), new Vector2(30f, 0f), () => CycleDefender(-1));

            nextDefenderBtn = CreateButton(defenderBg.rectTransform, "NextDefender", ">", new Vector2(46f, 46f),
                new Vector2(1f, 0.78f), new Vector2(-30f, 0f), () => CycleDefender(1));

            defenderStatsText = CreateText(defenderBg.rectTransform, "DefenderStatsText", "", 18, Color.white);
            defenderStatsText.alignment = TextAnchor.MiddleCenter;
            Place(defenderStatsText.rectTransform, new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(740f, 90f));

            metaLevelText = CreateText(defenderBg.rectTransform, "MetaLevelText", "", 20, new Color(0.85f, 0.85f, 0.9f));
            Place(metaLevelText.rectTransform, new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(700f, 30f));

            upgradeCostText = CreateText(defenderBg.rectTransform, "UpgradeCostText", "", 20, new Color(0.7f, 0.7f, 0.75f));
            upgradeCostText.alignment = TextAnchor.MiddleLeft;
            Place(upgradeCostText.rectTransform, new Vector2(0.35f, 0.11f), Vector2.zero, new Vector2(400f, 30f));

            upgradeDefenderBtn = CreateButton(defenderBg.rectTransform, "UpgradeDefenderBtn", "UPGRADE", new Vector2(200f, 50f),
                new Vector2(0.78f, 0.11f), Vector2.zero, OnUpgradeDefenderClicked);
            upgradeDefenderBtnLabel = upgradeDefenderBtn.GetComponentInChildren<Text>();
            upgradeDefenderBtnLabel.fontSize = 20;
            upgradeDefenderBtnLabel.fontStyle = FontStyle.Bold;

            // Initialize starting defender index from saved settings
            string savedId = SaveManager.SelectedStartingDefenderId;
            currentDefenderIndex = 0;
            if (heroDefinitions != null && heroDefinitions.Length > 0)
            {
                for (int i = 0; i < heroDefinitions.Length; i++)
                {
                    if (heroDefinitions[i] != null && heroDefinitions[i].id == savedId)
                    {
                        currentDefenderIndex = i;
                        break;
                    }
                }
            }
            RefreshDefenderSelection();
            RefreshMetaUpgradeUI();

            // 6. Large Premium Battle Button
            if (isPortrait)
            {
                startButton = CreateButton(safeAreaRect, "StartButton", "BATTLE", new Vector2(400f, 96f), new Vector2(0.5f, 0.44f), Vector2.zero, Play);
            }
            else
            {
                startButton = CreateButton(safeAreaRect, "StartButton", "BATTLE", new Vector2(400f, 96f), new Vector2(0.5f, 0.23f), Vector2.zero, Play);
            }
            startButtonLabel = startButton.GetComponentInChildren<Text>();
            startButtonLabel.fontStyle = FontStyle.Bold;
            startButtonLabel.fontSize = 36;
            startButtonLabel.color = new Color(1f, 0.9f, 0.3f);

            ColorBlock cb = startButton.colors;
            cb.normalColor = new Color(0.8f, 0.2f, 0.15f, 1.0f);
            cb.highlightedColor = new Color(0.95f, 0.3f, 0.25f, 1.0f);
            cb.pressedColor = new Color(0.6f, 0.15f, 0.1f, 1.0f);
            cb.selectedColor = new Color(0.8f, 0.2f, 0.15f, 1.0f);
            startButton.colors = cb;

            // 7. Right Meta Upgrades Container
            Image upgradesBg = CreateImage(safeAreaRect, "UpgradesPanel", new Color(0.12f, 0.16f, 0.24f, 0.8f));
            if (isPortrait)
            {
                Place(upgradesBg.rectTransform, new Vector2(0.5f, 0.24f), new Vector2(220f, 0f), new Vector2(400f, 440f));
            }
            else
            {
                Place(upgradesBg.rectTransform, new Vector2(1f, 0.5f), new Vector2(-260f, 30f), new Vector2(400f, 460f));
            }

            Text upgradesTitle = CreateText(upgradesBg.rectTransform, "Title", "META UPGRADES", 22, new Color(1f, 0.85f, 0.35f));
            Place(upgradesTitle.rectTransform, new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(340f, 32f));

            // Populate all permanent upgrades.
            float rowStartY = 0.79f;
            float rowSpacingY = 0.16f;

            if (MetaUpgradeManager.Instance != null)
            {
                var list = MetaUpgradeManager.Instance.Upgrades;
                int rowCount = Mathf.Min(list.Count, metaUpgradeNameTexts.Length);
                for (int i = 0; i < rowCount; i++)
                {
                    int index = i;
                    var upgrade = list[i];

                    // Name & level text
                    metaUpgradeNameTexts[index] = CreateText(upgradesBg.rectTransform, $"UpgradeName_{index}", "", 18, Color.white);
                    metaUpgradeNameTexts[index].alignment = TextAnchor.MiddleLeft;
                    Place(metaUpgradeNameTexts[index].rectTransform, new Vector2(0.5f, rowStartY - index * rowSpacingY), new Vector2(-50f, 20f), new Vector2(260f, 24f));

                    // Description text (subtext)
                    Text descText = CreateText(upgradesBg.rectTransform, $"UpgradeDesc_{index}", upgrade.description, 13, new Color(0.7f, 0.7f, 0.75f));
                    descText.alignment = TextAnchor.MiddleLeft;
                    Place(descText.rectTransform, new Vector2(0.5f, rowStartY - index * rowSpacingY), new Vector2(-50f, -8f), new Vector2(260f, 20f));

                    // Buy button
                    metaUpgradeButtons[index] = CreateButton(upgradesBg.rectTransform, $"BuyBtn_{index}", "", new Vector2(110f, 44f),
                        new Vector2(0.5f, rowStartY - index * rowSpacingY), new Vector2(130f, 6f), () => OnMetaUpgradeClicked(upgrade.id));
                    metaUpgradeButtonLabels[index] = metaUpgradeButtons[index].GetComponentInChildren<Text>();
                    metaUpgradeButtonLabels[index].fontSize = 16;
                    metaUpgradeButtonLabels[index].fontStyle = FontStyle.Bold;
                }
            }

            RefreshMetaUpgradesPanel();

            // 8. Bottom Navigation Bar Placeholder
            Image bottomBar = CreateImage(safeAreaRect, "BottomBar", new Color(0.05f, 0.06f, 0.08f, 1.0f));
            if (isPortrait)
            {
                bottomBar.rectTransform.anchorMin = new Vector2(0f, 0f);
                bottomBar.rectTransform.anchorMax = new Vector2(1f, 0f);
                bottomBar.rectTransform.pivot = new Vector2(0.5f, 0f);
                bottomBar.rectTransform.offsetMin = Vector2.zero;
                bottomBar.rectTransform.offsetMax = new Vector2(0f, 100f);
            }
            else
            {
                Place(bottomBar.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(1920f, 100f));
            }

            string[] tabLabels = { "SHOP", "CHARACTERS", "BATTLES", "LIBRARY", "MAP" };
            float tabWidth = isPortrait ? 150f : 240f;
            float tabSpacing = isPortrait ? 180f : 280f;
            float tabStartX = -(tabLabels.Length - 1) * tabSpacing / 2f;

            for (int i = 0; i < tabLabels.Length; i++)
            {
                string labelText = tabLabels[i];
                Button tabBtn = CreateButton(bottomBar.rectTransform, "Tab_" + labelText, labelText, new Vector2(tabWidth, 60f),
                    new Vector2(0.5f, 0.5f), new Vector2(tabStartX + i * tabSpacing, 0f), () => {
                        Debug.Log($"Tab clicked: {labelText}. Section coming soon!");
                    });

                Text tabBtnLabel = tabBtn.GetComponentInChildren<Text>();
                tabBtnLabel.fontSize = isPortrait ? 15 : 22;
                tabBtnLabel.fontStyle = FontStyle.Bold;

                ColorBlock tcb = tabBtn.colors;
                if (labelText == "BATTLES")
                {
                    tcb.normalColor = new Color(0.8f, 0.2f, 0.15f, 1.0f);
                    tcb.highlightedColor = new Color(0.95f, 0.3f, 0.25f, 1.0f);
                    tabBtnLabel.color = new Color(1f, 0.9f, 0.3f);
                }
                else
                {
                    tcb.normalColor = new Color(0.12f, 0.16f, 0.24f, 0.95f);
                    tcb.highlightedColor = new Color(0.20f, 0.26f, 0.38f, 1.0f);
                }
                tabBtn.colors = tcb;
            }

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

        private static RectTransform CreateSafeArea(RectTransform parent)
        {
            GameObject go = new GameObject("SafeAreaContainer", typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Rect safeArea = Screen.safeArea;
            float sw = Screen.width;
            float sh = Screen.height;
            if (sw > 0 && sh > 0)
            {
                rt.anchorMin = new Vector2(safeArea.x / sw, safeArea.y / sh);
                rt.anchorMax = new Vector2((safeArea.x + safeArea.width) / sw, (safeArea.y + safeArea.height) / sh);
            }
            return rt;
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
            if (MetaUpgradeManager.Instance != null)
            {
                MetaUpgradeManager.Instance.LoadUpgrades();
            }
            RefreshStats();
            RefreshStageSelection();
            RefreshCurrencies();
            RefreshMetaUpgradeUI();
            RefreshMetaUpgradesPanel();
        }

        private void RefreshStageSelection()
        {
            int selected = SaveManager.SelectedStageIndex;
            if (stageNameText != null && stageDescText != null)
            {
                string[] names = { "Castle Road", "Highlands", "Frozen Frontier" };
                string[] descriptions =
                {
                    "Defend the road to the keep against grunts, armored troops, runners, and the final boss.",
                    "Defend the highland pass against heavier enemy formations.",
                    "Hold the frozen frontier against faster and tougher waves."
                };
                string[] rewards =
                {
                    "Rewards: 🪙 Gold  💎 Gems  📦 Common Chest",
                    "Rewards: 🪙 Gold  💎 Gems  📦 Rare Chest",
                    "Rewards: 🪙 Gold  💎 Gems  📦 Epic Chest"
                };

                int stageIndex = Mathf.Clamp(selected, 0, names.Length - 1);
                int stageNumber = stageIndex + 1;
                bool isUnlocked = SaveManager.HighestStageUnlocked >= stageNumber;
                bool isCompleted = stageIndex == 0
                    ? SaveManager.Stage1Completed
                    : SaveManager.HighestStageUnlocked > stageNumber;

                if (stageNumText != null) stageNumText.text = "STAGE " + stageNumber;
                stageNameText.text = "<b>" + names[stageIndex] + "</b>";
                stageDescText.text = descriptions[stageIndex] + "\n" +
                    (isUnlocked
                        ? "<color=#ffd759>Progress: " + (isCompleted ? "Completed" : "Not Cleared") + "</color>"
                        : "<color=#ff5959>LOCKED: Complete Stage " + (stageNumber - 1) + " to unlock</color>");
                if (stageRewardText != null) stageRewardText.text = isUnlocked ? rewards[stageIndex] : "Rewards: LOCKED";
                if (startButtonLabel != null) startButtonLabel.text = isUnlocked ? "BATTLE" : "LOCKED";
                if (startButton != null) startButton.interactable = isUnlocked;
            }
        }

        private void CycleStage(int delta)
        {
            int newIndex = SaveManager.SelectedStageIndex + delta;
            if (newIndex >= 0 && newIndex < 3)
            {
                SaveManager.SetSelectedStage(newIndex);
                RefreshStageSelection();
            }
        }

        private static string GetHeroRarity(string heroId)
        {
            switch (heroId)
            {
                case "archer": return "Common";
                case "sniper": return "Epic";
                default: return "Rare";
            }
        }

        private static string GetAttackIdentityDescription(HeroDefinition hd)
        {
            if (hd == null || hd.weapon == null) return "Unknown";
            string typeStr = hd.weapon.attackType.ToString();
            if (hd.id == "electric_engineer")
            {
                return "Chain Shock Lightning";
            }
            if (hd.weapon.statusEffectType != StatusEffectType.None)
            {
                return $"{typeStr} ({hd.weapon.statusEffectType})";
            }
            return typeStr;
        }

        private static string GetAbilityDisplayName(HeroAbilityType type)
        {
            switch (type)
            {
                case HeroAbilityType.MultiShot: return "Multi Shot";
                case HeroAbilityType.ArtilleryBarrage: return "Artillery Barrage";
                case HeroAbilityType.FrostNova: return "Frost Nova";
                case HeroAbilityType.FlameWave: return "Flame Wave";
                case HeroAbilityType.ChainStorm: return "Chain Storm";
                case HeroAbilityType.PowerShot: return "Power Shot";
                default: return "None";
            }
        }

        private static string GetAbilityDescription(HeroAbilityType type)
        {
            switch (type)
            {
                case HeroAbilityType.MultiShot: return "Fires actual projectile volleys at multiple random targets.";
                case HeroAbilityType.ArtilleryBarrage: return "Fires a large arcing bomb dealing massive splash damage.";
                case HeroAbilityType.FrostNova: return "Releases a cold blast slowing and damaging all nearby enemies.";
                case HeroAbilityType.FlameWave: return "Unleashes a fiery wave burning enemies in a large area.";
                case HeroAbilityType.ChainStorm: return "Releases chain lightning bouncing across multiple targets.";
                case HeroAbilityType.PowerShot: return "Fires a piercing line trace that damages all enemies in its path.";
                default: return "No active ability.";
            }
        }

        private static string GetTargetingModeDisplayName(TargetingMode mode)
        {
            switch (mode)
            {
                case TargetingMode.ClosestToGoal: return "First";
                case TargetingMode.FirstInRange: return "First In Range";
                case TargetingMode.LastInRange: return "Last";
                case TargetingMode.Strongest: return "Strongest";
                case TargetingMode.Weakest: return "Weakest";
                default: return mode.ToString();
            }
        }

        private void CycleDefender(int delta)
        {
            if (heroDefinitions == null || heroDefinitions.Length == 0) return;

            currentDefenderIndex += delta;
            if (currentDefenderIndex < 0) currentDefenderIndex = heroDefinitions.Length - 1;
            if (currentDefenderIndex >= heroDefinitions.Length) currentDefenderIndex = 0;

            if (heroDefinitions[currentDefenderIndex] != null)
            {
                SaveManager.SetSelectedStartingDefender(heroDefinitions[currentDefenderIndex].id);
            }
            RefreshDefenderSelection();
            RefreshMetaUpgradeUI();
        }

        private void RefreshDefenderSelection()
        {
            if (heroDefinitions == null || heroDefinitions.Length == 0 || currentDefenderIndex >= heroDefinitions.Length) return;

            HeroDefinition hd = heroDefinitions[currentDefenderIndex];
            if (hd == null) return;

            if (defenderNameText != null)
            {
                string colorHex = "#d9d9f2"; // Common
                string rarity = GetHeroRarity(hd.id);
                if (rarity == "Rare")
                {
                    colorHex = "#66ccff";
                }
                else if (rarity == "Epic")
                {
                    colorHex = "#d980ff";
                }

                defenderNameText.text = $"<color={colorHex}><b>{hd.displayName}</b></color> <size=16>({rarity})</size> <color=#45ff70><size=16>[SELECTED]</size></color>";
            }

            if (defenderStatsText != null)
            {
                string attackType = GetAttackIdentityDescription(hd);
                string targetSpecialty = GetTargetingModeDisplayName(hd.defaultTargetingMode);
                string abilityName = GetAbilityDisplayName(hd.abilityType);
                string abilityDesc = GetAbilityDescription(hd.abilityType);

                defenderStatsText.text =
                    $"Type: <b>{attackType}</b> | Target Priority: <b>{targetSpecialty}</b>\n" +
                    $"Dmg: <b>{hd.baseDamage}</b> | Speed: <b>{hd.baseFireRate}/s</b> | Range: <b>{hd.baseRange}</b>\n" +
                    $"Ability: <color=#ffd759><b>{abilityName}</b></color> - {abilityDesc}";
            }

            UpdateHeroPreview();
        }

        private void RefreshCurrencies()
        {
            if (currencyText != null)
            {
                currencyText.text = $"🪙 {SaveManager.MetaGold}    ⚡ XP: {SaveManager.AccountXp}    📦 Mat: {SaveManager.CoreMaterials}";
            }
        }

        private void OnMetaUpgradeClicked(string id)
        {
            if (MetaUpgradeManager.Instance != null)
            {
                if (MetaUpgradeManager.Instance.PurchaseUpgrade(id))
                {
                    RefreshCurrencies();
                    RefreshMetaUpgradesPanel();
                }
            }
        }

        private void RefreshMetaUpgradesPanel()
        {
            if (MetaUpgradeManager.Instance == null) return;

            var list = MetaUpgradeManager.Instance.Upgrades;
            int rowCount = Mathf.Min(list.Count, metaUpgradeNameTexts.Length);
            for (int i = 0; i < rowCount; i++)
            {
                var upgrade = list[i];
                int level = upgrade.currentLevel;
                int cost = upgrade.GetCost();

                if (metaUpgradeNameTexts[i] != null)
                {
                    metaUpgradeNameTexts[i].text = $"<b>{upgrade.displayName}</b> <size=14>(Lv. {level}/{upgrade.maxLevel})</size>";
                }

                if (metaUpgradeButtons[i] != null && metaUpgradeButtonLabels[i] != null)
                {
                    if (level >= upgrade.maxLevel)
                    {
                        metaUpgradeButtonLabels[i].text = "MAX";
                        metaUpgradeButtons[i].interactable = false;
                        ColorBlock cb = metaUpgradeButtons[i].colors;
                        cb.normalColor = new Color(0.24f, 0.24f, 0.24f, 0.8f);
                        metaUpgradeButtons[i].colors = cb;
                    }
                    else
                    {
                        metaUpgradeButtonLabels[i].text = $"🪙 {cost}";
                        bool affordable = SaveManager.MetaGold >= cost;
                        metaUpgradeButtons[i].interactable = affordable;

                        ColorBlock cb = metaUpgradeButtons[i].colors;
                        if (affordable)
                        {
                            cb.normalColor = new Color(0.20f, 0.45f, 0.24f, 0.95f); // Greenish
                            cb.highlightedColor = new Color(0.28f, 0.58f, 0.34f, 1f);
                            cb.pressedColor = new Color(0.14f, 0.35f, 0.18f, 1f);
                            cb.selectedColor = new Color(0.20f, 0.45f, 0.24f, 0.95f);
                        }
                        else
                        {
                            cb.normalColor = new Color(0.24f, 0.24f, 0.24f, 0.8f); // Grey
                            cb.highlightedColor = new Color(0.24f, 0.24f, 0.24f, 0.8f);
                            cb.pressedColor = new Color(0.24f, 0.24f, 0.24f, 0.8f);
                            cb.selectedColor = new Color(0.24f, 0.24f, 0.24f, 0.8f);
                        }
                        metaUpgradeButtons[i].colors = cb;
                    }
                }
            }
        }

        private int GetMetaUpgradeCost(int level)
        {
            switch (level)
            {
                case 1: return 100;
                case 2: return 150;
                case 3: return 225;
                case 4: return 300;
                case 5: return 400;
                case 6: return 550;
                case 7: return 750;
                case 8: return 1000;
                case 9: return 1500;
                default: return 0; // Max level
            }
        }

        private void RefreshMetaUpgradeUI()
        {
            if (heroDefinitions == null || heroDefinitions.Length == 0 || currentDefenderIndex >= heroDefinitions.Length) return;

            HeroDefinition hd = heroDefinitions[currentDefenderIndex];
            if (hd == null) return;

            string id = hd.id;
            int level = SaveManager.GetMetaLevel(id);
            int cost = GetMetaUpgradeCost(level);

            if (metaLevelText != null)
            {
                int dmgBonus = (level - 1) * 8;
                metaLevelText.text = $"<b>Meta Progression</b>: Level {level}/10  <color=#ffd759>(+{dmgBonus}% Damage)</color>";
            }

            if (level >= 10)
            {
                if (upgradeCostText != null) upgradeCostText.text = "MAX LEVEL REACHED";
                if (upgradeDefenderBtnLabel != null) upgradeDefenderBtnLabel.text = "MAX";
                if (upgradeDefenderBtn != null) upgradeDefenderBtn.interactable = false;
            }
            else
            {
                if (upgradeCostText != null) upgradeCostText.text = $"Cost: 🪙 {cost} Meta Gold";
                if (upgradeDefenderBtnLabel != null) upgradeDefenderBtnLabel.text = "UPGRADE";

                bool affordable = SaveManager.MetaGold >= cost;
                if (upgradeDefenderBtn != null)
                {
                    upgradeDefenderBtn.interactable = affordable;

                    ColorBlock cb = upgradeDefenderBtn.colors;
                    if (affordable)
                    {
                        cb.normalColor = new Color(0.20f, 0.45f, 0.24f, 0.95f); // Greenish
                        cb.highlightedColor = new Color(0.28f, 0.58f, 0.34f, 1f);
                        cb.pressedColor = new Color(0.14f, 0.35f, 0.18f, 1f);
                        cb.selectedColor = new Color(0.20f, 0.45f, 0.24f, 0.95f);
                    }
                    else
                    {
                        cb.normalColor = new Color(0.24f, 0.24f, 0.24f, 0.8f); // Disabled grey
                        cb.highlightedColor = new Color(0.24f, 0.24f, 0.24f, 0.8f);
                        cb.pressedColor = new Color(0.24f, 0.24f, 0.24f, 0.8f);
                        cb.selectedColor = new Color(0.24f, 0.24f, 0.24f, 0.8f);
                    }
                    upgradeDefenderBtn.colors = cb;
                }
            }
        }

        private void OnUpgradeDefenderClicked()
        {
            if (heroDefinitions == null || heroDefinitions.Length == 0 || currentDefenderIndex >= heroDefinitions.Length) return;

            HeroDefinition hd = heroDefinitions[currentDefenderIndex];
            if (hd == null) return;

            string id = hd.id;
            int level = SaveManager.GetMetaLevel(id);
            int cost = GetMetaUpgradeCost(level);

            if (level < 10 && SaveManager.MetaGold >= cost)
            {
                SaveManager.AddMetaGold(-cost);
                SaveManager.UpgradeMetaLevel(id);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayButton();
                }

                RefreshCurrencies();
                RefreshMetaUpgradeUI();
            }
        }

        private void OnDestroy()
        {
            if (activePreviewInstance != null)
            {
                Destroy(activePreviewInstance);
            }
        }

        private void UpdateHeroPreview()
        {
            if (activePreviewInstance != null)
            {
                Destroy(activePreviewInstance);
                activePreviewInstance = null;
            }

            if (heroDefinitions == null || currentDefenderIndex < 0 || currentDefenderIndex >= heroDefinitions.Length)
            {
                return;
            }

            HeroDefinition hd = heroDefinitions[currentDefenderIndex];
            if (hd == null || hd.heroPrefab == null)
            {
                return;
            }

            activePreviewInstance = Instantiate(hd.heroPrefab, transform);
            activePreviewInstance.name = "HeroPreview_" + hd.id;
            activePreviewInstance.transform.position = new Vector3(0f, -0.6f, -6.5f);
            activePreviewInstance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            activePreviewInstance.transform.localScale = Vector3.one * 1.8f;

            Collider[] colliders = activePreviewInstance.GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                Destroy(col);
            }

            if (hd.id == "bombardier")
            {
                Transform sword = FindTransformRecursiveInPreview(activePreviewInstance.transform, "Warrior_Sword");
                if (sword != null) sword.gameObject.SetActive(false);
            }
            else if (hd.id == "sniper")
            {
                Transform dagger = FindTransformRecursiveInPreview(activePreviewInstance.transform, "Rogue_Dagger");
                if (dagger != null) dagger.gameObject.SetActive(false);
            }

            Color accentColor = GetHeroAccentColor(hd.id);
            CreatePreviewWeaponProp(hd.id, activePreviewInstance.transform, accentColor);
        }

        private void CleanupGeneratedMenuObjects()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name == "MenuCanvas" || child.name.StartsWith("HeroPreview_"))
                {
                    Destroy(child.gameObject);
                }
            }

            GameObject staleCanvas = GameObject.Find("MenuCanvas");
            if (staleCanvas != null && staleCanvas.transform.parent != transform)
            {
                Destroy(staleCanvas);
            }

            Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (Transform sceneTransform in sceneTransforms)
            {
                if (sceneTransform.parent == null && sceneTransform.name.StartsWith("HeroPreview_"))
                {
                    Destroy(sceneTransform.gameObject);
                }
            }
        }

        private static Transform FindTransformRecursiveInPreview(Transform parent, string name)
        {
            if (parent.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return parent;
            }
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindTransformRecursiveInPreview(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static Color GetHeroAccentColor(string heroId)
        {
            switch (heroId)
            {
                case "archer": return new Color(0.5f, 0.35f, 0.2f);
                case "bombardier": return new Color(0.9f, 0.5f, 0.15f);
                case "frost_mage": return new Color(0.85f, 0.92f, 1f);
                case "fire_mage": return new Color(1f, 0.5f, 0.1f);
                case "electric_engineer": return new Color(0.3f, 0.3f, 0.35f);
                case "sniper": return new Color(0.7f, 0.5f, 0.85f);
                default: return Color.white;
            }
        }

        private void CreatePreviewWeaponProp(string heroId, Transform parent, Color color)
        {
            PrimitiveType shape;
            Vector3 localPos;
            Vector3 localScale;
            Quaternion localRot = Quaternion.identity;

            switch (heroId)
            {
                case "archer":
                    shape = PrimitiveType.Cylinder;
                    localPos = new Vector3(-0.25f, 0.35f, -0.15f);
                    localScale = new Vector3(0.08f, 0.25f, 0.08f);
                    localRot = Quaternion.Euler(0f, 0f, 15f);
                    break;
                case "bombardier":
                    shape = PrimitiveType.Sphere;
                    localPos = new Vector3(0.3f, 0.1f, 0f);
                    localScale = new Vector3(0.22f, 0.22f, 0.22f);
                    break;
                case "frost_mage":
                    shape = PrimitiveType.Cube;
                    localPos = new Vector3(0.25f, 0.4f, 0f);
                    localScale = new Vector3(0.1f, 0.15f, 0.1f);
                    localRot = Quaternion.Euler(0f, 45f, 45f);
                    break;
                case "fire_mage":
                    shape = PrimitiveType.Sphere;
                    localPos = new Vector3(0.25f, 0.45f, 0f);
                    localScale = new Vector3(0.14f, 0.14f, 0.14f);
                    break;
                case "electric_engineer":
                    shape = PrimitiveType.Cylinder;
                    localPos = new Vector3(0f, 0.65f, 0f);
                    localScale = new Vector3(0.06f, 0.18f, 0.06f);
                    break;
                case "sniper":
                    shape = PrimitiveType.Cylinder;
                    localPos = new Vector3(0.35f, 0.3f, 0f);
                    localScale = new Vector3(0.04f, 0.3f, 0.04f);
                    localRot = Quaternion.Euler(0f, 0f, 90f);
                    break;
                default:
                    return;
            }

            Transform propParent = parent;
            if (heroId == "bombardier" || heroId == "sniper")
            {
                Transform weaponMount = FindTransformRecursiveInPreview(parent, "Weapon.R");
                if (weaponMount != null)
                {
                    propParent = weaponMount;
                    if (heroId == "bombardier")
                    {
                        localPos = new Vector3(0f, 0f, 0f);
                    }
                    else if (heroId == "sniper")
                    {
                        localPos = new Vector3(0f, 0.15f, 0f);
                        localRot = Quaternion.Euler(0f, 0f, 90f);
                        localScale = new Vector3(0.04f, 0.35f, 0.04f);
                    }
                }
            }

            GameObject prop = GameObject.CreatePrimitive(shape);
            prop.name = "WeaponProp";
            prop.transform.SetParent(propParent);
            prop.transform.localPosition = localPos;
            prop.transform.localRotation = localRot;
            prop.transform.localScale = CompensatePreviewMountedScale(localScale, parent, propParent);

            Collider propCollider = prop.GetComponent<Collider>();
            if (propCollider != null) Destroy(propCollider);

            Renderer propRenderer = prop.GetComponent<Renderer>();
            if (propRenderer != null)
            {
                Color propColor = heroId == "fire_mage" ? new Color(1f, 0.45f, 0.08f) : color;
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                mpb.SetColor(Shader.PropertyToID("_BaseColor"), propColor);
                propRenderer.SetPropertyBlock(mpb);
            }
        }

        private static Vector3 CompensatePreviewMountedScale(Vector3 desiredRootScale, Transform visualRoot, Transform mount)
        {
            Vector3 rootScale = visualRoot.lossyScale;
            Vector3 mountScale = mount.lossyScale;
            return new Vector3(
                desiredRootScale.x * SafePreviewScaleRatio(rootScale.x, mountScale.x),
                desiredRootScale.y * SafePreviewScaleRatio(rootScale.y, mountScale.y),
                desiredRootScale.z * SafePreviewScaleRatio(rootScale.z, mountScale.z));
        }

        private static float SafePreviewScaleRatio(float rootScale, float mountScale)
        {
            return Mathf.Abs(mountScale) > 0.0001f ? Mathf.Abs(rootScale / mountScale) : 1f;
        }
    }
}
