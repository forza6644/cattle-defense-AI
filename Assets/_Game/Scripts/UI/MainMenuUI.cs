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

        private Text defenderNameText;
        private Button prevDefenderBtn;
        private Button nextDefenderBtn;
        private Text metaLevelText;
        private Text upgradeCostText;
        private Button upgradeDefenderBtn;
        private Text upgradeDefenderBtnLabel;
        private int currentDefenderIndex = 0;

        private Text[] metaUpgradeNameTexts = new Text[4];
        private Button[] metaUpgradeButtons = new Button[4];
        private Text[] metaUpgradeButtonLabels = new Text[4];

        private struct DefenderInfo
        {
            public string id;
            public string displayName;
            public string rarity;
            public DefenderInfo(string id, string displayName, string rarity)
            {
                this.id = id;
                this.displayName = displayName;
                this.rarity = rarity;
            }
        }

        private readonly DefenderInfo[] lobbyDefenders = new DefenderInfo[]
        {
            new DefenderInfo("archer_defender", "Archer Defender", "Common"),
            new DefenderInfo("machine_gun_soldier", "Machine Gun Soldier", "Rare"),
            new DefenderInfo("catapult_defender", "Catapult Defender", "Rare"),
            new DefenderInfo("ice_mage", "Ice Mage", "Rare"),
            new DefenderInfo("sniper", "Sniper", "Epic")
        };

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (FindAnyObjectByType<MetaUpgradeManager>() == null)
            {
                GameObject managerGo = new GameObject("MetaUpgradeManager", typeof(MetaUpgradeManager));
                DontDestroyOnLoad(managerGo);
            }
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

            // 1. Top Header Bar
            Image headerBar = CreateImage(canvasRect, "HeaderBar", new Color(0.05f, 0.06f, 0.08f, 0.95f));
            Place(headerBar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(1920f, 100f));

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
            currencyText = CreateText(headerBar.rectTransform, "Currencies", "", 22, new Color(0.9f, 0.9f, 0.95f));
            currencyText.alignment = TextAnchor.MiddleRight;
            Place(currencyText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-480f, 0f), new Vector2(500f, 40f));
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
            Text title = CreateText(canvasRect, "Title", "STONEHOLD", 110, new Color(1f, 0.85f, 0.35f));
            title.fontStyle = FontStyle.Bold;
            Place(title.rectTransform, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(1200f, 130f));
            titleRect = title.rectTransform;

            Text subtitle = CreateText(canvasRect, "Subtitle", "Defend the keep. Hold the line.", 24, new Color(0.8f, 0.8f, 0.85f));
            Place(subtitle.rectTransform, new Vector2(0.5f, 0.70f), Vector2.zero, new Vector2(900f, 40f));

            // 3. Stats & Progress Panel (Left Side Container)
            Image statsBg = CreateImage(canvasRect, "StatsPanel", new Color(0.12f, 0.16f, 0.24f, 0.5f));
            Place(statsBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(260f, 50f), new Vector2(400f, 400f));

            statsText = CreateText(statsBg.rectTransform, "StatsText", "", 22, new Color(0.85f, 0.85f, 0.9f));
            statsText.alignment = TextAnchor.UpperLeft;
            Place(statsText.rectTransform, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(340f, 260f));
            RefreshStats();

            CreateButton(statsBg.rectTransform, "ResetStatsButton", "Reset Stats", new Vector2(180f, 48f),
                new Vector2(0.5f, 0.12f), Vector2.zero, ResetStats);

            // 4. Central Stage Select Panel
            Image stageBg = CreateImage(canvasRect, "StagePanel", new Color(0.12f, 0.16f, 0.24f, 0.8f));
            Place(stageBg.rectTransform, new Vector2(0.5f, 0.53f), Vector2.zero, new Vector2(800f, 220f));

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
            Image defenderBg = CreateImage(canvasRect, "DefenderPanel", new Color(0.12f, 0.16f, 0.24f, 0.7f));
            Place(defenderBg.rectTransform, new Vector2(0.5f, 0.40f), new Vector2(0f, -20f), new Vector2(800f, 210f));

            Text defenderTitleText = CreateText(defenderBg.rectTransform, "DefenderTitle", "STARTING DEFENDER", 16, new Color(1f, 0.85f, 0.35f));
            Place(defenderTitleText.rectTransform, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(700f, 24f));

            defenderNameText = CreateText(defenderBg.rectTransform, "DefenderName", "", 26, Color.white);
            Place(defenderNameText.rectTransform, new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(700f, 36f));

            prevDefenderBtn = CreateButton(defenderBg.rectTransform, "PrevDefender", "<", new Vector2(46f, 46f),
                new Vector2(0f, 0.7f), new Vector2(30f, 0f), () => CycleDefender(-1));

            nextDefenderBtn = CreateButton(defenderBg.rectTransform, "NextDefender", ">", new Vector2(46f, 46f),
                new Vector2(1f, 0.7f), new Vector2(-30f, 0f), () => CycleDefender(1));

            metaLevelText = CreateText(defenderBg.rectTransform, "MetaLevelText", "", 20, new Color(0.85f, 0.85f, 0.9f));
            Place(metaLevelText.rectTransform, new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(700f, 30f));

            upgradeCostText = CreateText(defenderBg.rectTransform, "UpgradeCostText", "", 20, new Color(0.7f, 0.7f, 0.75f));
            upgradeCostText.alignment = TextAnchor.MiddleLeft;
            Place(upgradeCostText.rectTransform, new Vector2(0.35f, 0.2f), Vector2.zero, new Vector2(400f, 30f));

            upgradeDefenderBtn = CreateButton(defenderBg.rectTransform, "UpgradeDefenderBtn", "UPGRADE", new Vector2(200f, 50f),
                new Vector2(0.78f, 0.2f), Vector2.zero, OnUpgradeDefenderClicked);
            upgradeDefenderBtnLabel = upgradeDefenderBtn.GetComponentInChildren<Text>();
            upgradeDefenderBtnLabel.fontSize = 20;
            upgradeDefenderBtnLabel.fontStyle = FontStyle.Bold;

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
            RefreshMetaUpgradeUI();

            // 6. Large Premium Battle Button
            startButton = CreateButton(canvasRect, "StartButton", "BATTLE", new Vector2(400f, 96f), new Vector2(0.5f, 0.23f), Vector2.zero, Play);
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
            Image upgradesBg = CreateImage(canvasRect, "UpgradesPanel", new Color(0.12f, 0.16f, 0.24f, 0.8f));
            Place(upgradesBg.rectTransform, new Vector2(1f, 0.5f), new Vector2(-260f, 30f), new Vector2(400f, 460f));

            Text upgradesTitle = CreateText(upgradesBg.rectTransform, "Title", "META UPGRADES", 22, new Color(1f, 0.85f, 0.35f));
            Place(upgradesTitle.rectTransform, new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(340f, 32f));

            // Populate the 4 rows
            float rowStartY = 0.76f;
            float rowSpacingY = 0.21f;

            if (MetaUpgradeManager.Instance != null)
            {
                var list = MetaUpgradeManager.Instance.Upgrades;
                for (int i = 0; i < 4; i++)
                {
                    int index = i;
                    var upgrade = list[i];

                    // Name & level text
                    metaUpgradeNameTexts[i] = CreateText(upgradesBg.rectTransform, $"UpgradeName_{i}", "", 18, Color.white);
                    metaUpgradeNameTexts[i].alignment = TextAnchor.MiddleLeft;
                    Place(metaUpgradeNameTexts[i].rectTransform, new Vector2(0.5f, rowStartY - i * rowSpacingY), new Vector2(-50f, 20f), new Vector2(260f, 24f));

                    // Description text (subtext)
                    Text descText = CreateText(upgradesBg.rectTransform, $"UpgradeDesc_{i}", upgrade.description, 13, new Color(0.7f, 0.7f, 0.75f));
                    descText.alignment = TextAnchor.MiddleLeft;
                    Place(descText.rectTransform, new Vector2(0.5f, rowStartY - i * rowSpacingY), new Vector2(-50f, -8f), new Vector2(260f, 20f));

                    // Buy button
                    metaUpgradeButtons[i] = CreateButton(upgradesBg.rectTransform, $"BuyBtn_{i}", "", new Vector2(110f, 44f),
                        new Vector2(0.5f, rowStartY - i * rowSpacingY), new Vector2(130f, 6f), () => OnMetaUpgradeClicked(upgrade.id));
                    metaUpgradeButtonLabels[i] = metaUpgradeButtons[i].GetComponentInChildren<Text>();
                    metaUpgradeButtonLabels[i].fontSize = 16;
                    metaUpgradeButtonLabels[i].fontStyle = FontStyle.Bold;
                }
            }

            RefreshMetaUpgradesPanel();

            // 8. Bottom Navigation Bar Placeholder
            Image bottomBar = CreateImage(canvasRect, "BottomBar", new Color(0.05f, 0.06f, 0.08f, 1.0f));
            Place(bottomBar.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(1920f, 100f));

            string[] tabLabels = { "SHOP", "CHARACTERS", "BATTLES", "LIBRARY", "MAP" };
            float tabWidth = 240f;
            float tabSpacing = 280f;
            float tabStartX = -(tabLabels.Length - 1) * tabSpacing / 2f;

            for (int i = 0; i < tabLabels.Length; i++)
            {
                string labelText = tabLabels[i];
                Button tabBtn = CreateButton(bottomBar.rectTransform, "Tab_" + labelText, labelText, new Vector2(tabWidth, 60f),
                    new Vector2(0.5f, 0.5f), new Vector2(tabStartX + i * tabSpacing, 0f), () => {
                        Debug.Log($"Tab clicked: {labelText}. Section coming soon!");
                    });

                Text tabBtnLabel = tabBtn.GetComponentInChildren<Text>();
                tabBtnLabel.fontSize = 22;
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
                if (selected == 0)
                {
                    if (stageNumText != null) stageNumText.text = "STAGE 1";
                    stageNameText.text = "<b>Castle Road</b>";
                    stageDescText.text = "Defend the road to the keep against grunts, armored troops, runners, and the final boss.\n" +
                                         "<color=#ffd759>Progress: " + (SaveManager.Stage1Completed ? "Completed (3/3 Stars)" : "Not Cleared (0/3 Stars)") + "</color>";
                    if (stageRewardText != null) stageRewardText.text = "Rewards: 🪙 Gold  💎 Gems  📦 Common Chest";
                    if (startButtonLabel != null) startButtonLabel.text = "BATTLE";
                    if (startButton != null) startButton.interactable = true;
                }
                else
                {
                    if (stageNumText != null) stageNumText.text = "STAGE 2";
                    stageNameText.text = "<b>Highlands</b>";
                    bool isUnlocked = SaveManager.HighestStageUnlocked >= 2;
                    stageDescText.text = isUnlocked
                        ? "Defend the highland pass. Expected enemy types: Giants, Wyverns.\n<color=#ffd759>Progress: Not Cleared</color>"
                        : "Defend the highland pass. Expected enemy types: Giants, Wyverns.\n<color=#ff5959>LOCKED: Complete Stage 1 to unlock</color>";
                    if (stageRewardText != null) stageRewardText.text = isUnlocked ? "Rewards: 🪙 Gold  💎 Gems  📦 Rare Chest" : "Rewards: LOCKED";

                    if (startButtonLabel != null) startButtonLabel.text = isUnlocked ? "BATTLE" : "LOCKED";
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
            RefreshMetaUpgradeUI();
        }

        private void RefreshDefenderSelection()
        {
            if (defenderNameText != null)
            {
                string colorHex = "#d9d9f2"; // Common
                string rarity = lobbyDefenders[currentDefenderIndex].rarity;
                if (rarity == "Rare")
                {
                    colorHex = "#66ccff";
                }
                else if (rarity == "Epic")
                {
                    colorHex = "#d980ff";
                }

                defenderNameText.text = $"<color={colorHex}><b>{lobbyDefenders[currentDefenderIndex].displayName}</b></color> <size=16>({rarity})</size>";
            }
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
            for (int i = 0; i < 4; i++)
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
            if (lobbyDefenders == null || currentDefenderIndex >= lobbyDefenders.Length) return;

            string id = lobbyDefenders[currentDefenderIndex].id;
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
            if (lobbyDefenders == null || currentDefenderIndex >= lobbyDefenders.Length) return;

            string id = lobbyDefenders[currentDefenderIndex].id;
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
    }
}
