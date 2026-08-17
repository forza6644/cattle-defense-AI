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
        [SerializeField] private string gameSceneName = "GameplayIntegration_V2";

        private Font font;
        private RectTransform canvasRect;
        private CanvasGroup settingsGroup;
        private Text qualityLabel;
        private Text qualityDescriptionText;
        private readonly System.Collections.Generic.List<Button> qualityTierButtons = new System.Collections.Generic.List<Button>();
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

        private Button[] crystalButtons;
        private Text[] crystalButtonLabels;

        private Text[] metaUpgradeNameTexts = new Text[6];
        private Button[] metaUpgradeButtons = new Button[6];
        private Text[] metaUpgradeButtonLabels = new Text[6];

        private CanvasGroup ascensionDrawerGroup;
        private Text ascensionHeatButtonLabel;
        private Text ascensionDrawerHeatTitle;
        private readonly System.Collections.Generic.List<Text> mutatorStatusTexts = new System.Collections.Generic.List<Text>();
        private readonly System.Collections.Generic.List<Image> mutatorCheckBgs = new System.Collections.Generic.List<Image>();

        private CanvasGroup bestiaryDrawerGroup;
        private Text bestiarySelectedNameText;
        private Text bestiarySelectedCategoryText;
        private Text bestiarySelectedLoreText;
        private Text bestiarySelectedStatsText;
        private Text bestiarySelectedWeaknessText;
        private Text bestiarySelectedTipsText;
        private BestiaryEntryDefinition selectedBestiaryEntry;

        private CanvasGroup achievementsDrawerGroup;
        private Text achievementsHeaderStatsText;
        private readonly System.Collections.Generic.List<GameObject> achievementCardObjects = new System.Collections.Generic.List<GameObject>();
        private Image achievementsListContainer;

        private CanvasGroup worldMapDrawerGroup;
        private Text worldMapStarsTotalText;
        private Text worldMapSelectedStageTitle;
        private Text worldMapSelectedStageLore;
        private Text worldMapSelectedStageStarsText;
        private Text worldMapSelectedStageScoreText;
        private Button worldMapLaunchButton;
        private int worldMapSelectedStageIndex = 0;
        private readonly System.Collections.Generic.List<GameObject> worldMapNodeObjects = new System.Collections.Generic.List<GameObject>();
        private Image worldMapCanvasPanel;

        private CanvasGroup treasuryDrawerGroup;
        private Text treasuryDetailsText;
        private Image treasuryFillBar;
        private Button claimTreasuryBtn;
        private Text claimTreasuryBtnLabel;

        private CanvasGroup dailyRewardsDrawerGroup;
        private readonly System.Collections.Generic.List<Text> dailyRewardStatusTexts = new System.Collections.Generic.List<Text>();
        private Button claimDailyRewardBtn;
        private Text claimDailyRewardBtnLabel;

        private CanvasGroup questsDrawerGroup;
        private Text questsTabDailyLabel;
        private Text questsTabWeeklyLabel;
        private Image questsListContainer;
        private QuestType selectedQuestTab = QuestType.Daily;
        private readonly System.Collections.Generic.List<GameObject> questCardObjects = new System.Collections.Generic.List<GameObject>();

        private CanvasGroup relicsDrawerGroup;
        private Text relicsSummaryText;
        private readonly System.Collections.Generic.List<Text> relicSlotNameTexts = new System.Collections.Generic.List<Text>();
        private readonly System.Collections.Generic.List<GameObject> relicCatalogCardObjects = new System.Collections.Generic.List<GameObject>();
        private Image relicsListContainer;

        private void Awake()
        {
            CleanupGeneratedMenuObjects();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (FindAnyObjectByType<MetaUpgradeManager>() == null)
            {
                GameObject managerGo = new GameObject("MetaUpgradeManager", typeof(MetaUpgradeManager));
                DontDestroyOnLoad(managerGo);
            }
            if (FindAnyObjectByType<AscensionManager>() == null)
            {
                GameObject ascensionGo = new GameObject("AscensionManager", typeof(AscensionManager));
                DontDestroyOnLoad(ascensionGo);
            }
            if (FindAnyObjectByType<BestiaryManager>() == null)
            {
                GameObject bestiaryGo = new GameObject("BestiaryManager", typeof(BestiaryManager));
                DontDestroyOnLoad(bestiaryGo);
            }
            if (FindAnyObjectByType<AchievementManager>() == null)
            {
                GameObject achvGo = new GameObject("AchievementManager", typeof(AchievementManager));
                DontDestroyOnLoad(achvGo);
            }
            if (FindAnyObjectByType<CampaignProgressionManager>() == null)
            {
                GameObject campaignGo = new GameObject("CampaignProgressionManager", typeof(CampaignProgressionManager));
                DontDestroyOnLoad(campaignGo);
            }
            if (FindAnyObjectByType<IdleTreasuryManager>() == null)
            {
                GameObject treasuryGo = new GameObject("IdleTreasuryManager", typeof(IdleTreasuryManager));
                DontDestroyOnLoad(treasuryGo);
            }
            if (FindAnyObjectByType<QuestManager>() == null)
            {
                GameObject questGo = new GameObject("QuestManager", typeof(QuestManager));
                DontDestroyOnLoad(questGo);
            }
            if (FindAnyObjectByType<HeroArtifactManager>() == null)
            {
                GameObject relicGo = new GameObject("HeroArtifactManager", typeof(HeroArtifactManager));
                DontDestroyOnLoad(relicGo);
            }

            if (heroDefinitions == null || heroDefinitions.Length == 0)
            {
                heroDefinitions = Resources.LoadAll<HeroDefinition>("Heroes");
                if (heroDefinitions == null || heroDefinitions.Length == 0)
                {
                    heroDefinitions = Resources.LoadAll<HeroDefinition>("");
                }
            }

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.04f, 0.03f, 0.06f);
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
            string targetScene = string.IsNullOrEmpty(gameSceneName) || gameSceneName == "GameScene" ? "GameplayIntegration_V2" : gameSceneName;
            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeToScene(targetScene);
            }
            else
            {
                SceneManager.LoadScene(targetScene);
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
            MobileQualityManager.SetQualityTier(level);
            RefreshQualityLabel();
        }

        private void RefreshQualityLabel()
        {
            if (qualityLabel != null)
            {
                var tier = MobileQualityManager.CurrentTier;
                qualityLabel.text = "Graphics: " + MobileQualityManager.GetTierDisplayName(tier);
            }
            if (qualityDescriptionText != null)
            {
                var tier = MobileQualityManager.CurrentTier;
                qualityDescriptionText.text = MobileQualityManager.GetTierDescription(tier);
            }
            RefreshQualityButtonHighlights();
        }

        private void RefreshQualityButtonHighlights()
        {
            int current = (int)MobileQualityManager.CurrentTier;
            for (int i = 0; i < qualityTierButtons.Count; i++)
            {
                if (qualityTierButtons[i] == null) continue;
                Image img = qualityTierButtons[i].GetComponent<Image>();
                Text txt = qualityTierButtons[i].GetComponentInChildren<Text>();
                bool isActive = (i == current);
                if (img != null)
                {
                    img.color = isActive ? new Color(0.85f, 0.65f, 0.20f, 1f) : new Color(0.18f, 0.22f, 0.28f, 0.9f);
                }
                if (txt != null)
                {
                    txt.color = isActive ? new Color(0.08f, 0.08f, 0.1f, 1f) : new Color(0.9f, 0.9f, 0.95f, 1f);
                    txt.fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal;
                }
            }
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

            // Background (High-Fidelity Castle Interior)
            Sprite bgSprite = LoadSprite("UI/menu_bg_castle");
            Image background = CreateImage(canvasRect, "Background", bgSprite != null ? Color.white : new Color(0.06f, 0.05f, 0.09f, 1f));
            if (bgSprite != null)
            {
                background.sprite = bgSprite;
                background.type = Image.Type.Simple;
                background.preserveAspect = false;
            }
            Stretch(background.rectTransform);

            // Ambient floating embers & dust motes
            CreateAmbientMenuParticles(canvasObject.transform);

            // Safe Area
            RectTransform safeAreaRect = CreateSafeArea(canvasRect);

            // 1. Top Header Bar
            Image headerBar = CreateImage(safeAreaRect, "HeaderBar", new Color(0.04f, 0.05f, 0.08f, 0.94f));
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
            Image profileAvatar = CreateImage(headerBar.rectTransform, "ProfileAvatar", new Color(0.24f, 0.32f, 0.44f, 1.0f));
            Place(profileAvatar.rectTransform, new Vector2(0f, 0.5f), new Vector2(80f, 0f), new Vector2(64f, 64f));

            Text profileName = CreateText(headerBar.rectTransform, "ProfileName", "Commander_01", 24, new Color(1f, 0.85f, 0.35f));
            profileName.alignment = TextAnchor.MiddleLeft;
            Place(profileName.rectTransform, new Vector2(0f, 0.5f), new Vector2(250f, 12f), new Vector2(240f, 32f));

            Text profileLevel = CreateText(headerBar.rectTransform, "ProfileLevel", "Lv.15", 18, new Color(0.9f, 0.9f, 0.95f));
            profileLevel.alignment = TextAnchor.MiddleLeft;
            Place(profileLevel.rectTransform, new Vector2(0f, 0.5f), new Vector2(250f, -14f), new Vector2(240f, 24f));

            // Currencies (Top-Right Area)
            currencyText = CreateText(headerBar.rectTransform, "Currencies", "", isPortrait ? 15 : 22, new Color(1f, 0.95f, 0.8f));
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

            // Top Bar Buttons (Relics, Quests, Daily, Treasury)
            CreateButton(headerBar.rectTransform, "RelicsButton", "🛡️ Relics", new Vector2(130f, 54f),
                new Vector2(1f, 0.5f), new Vector2(-480f, 0f), () => ShowRelicsDrawer(true));

            CreateButton(headerBar.rectTransform, "QuestsButton", "📜 Quests", new Vector2(130f, 54f),
                new Vector2(1f, 0.5f), new Vector2(-340f, 0f), () => ShowQuestsDrawer(true));

            CreateButton(headerBar.rectTransform, "DailyButton", "🎁 Daily", new Vector2(120f, 54f),
                new Vector2(1f, 0.5f), new Vector2(-210f, 0f), () => ShowDailyRewardsDrawer(true));

            CreateButton(headerBar.rectTransform, "TreasuryButton", "📦 Vault", new Vector2(120f, 54f),
                new Vector2(1f, 0.5f), new Vector2(-80f, 0f), () => ShowTreasuryModal(true));

            // 2. Title (Runic Stone Block with Emerald Rune Inlay)
            Sprite titleSprite = LoadSprite("UI/title_stonehold");
            if (titleSprite != null)
            {
                Image titleImage = CreateImage(safeAreaRect, "TitleBlock", Color.white);
                titleImage.sprite = titleSprite;
                titleImage.preserveAspect = true;
                if (isPortrait)
                {
                    Place(titleImage.rectTransform, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(920f, 210f));
                }
                else
                {
                    Place(titleImage.rectTransform, new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(760f, 170f));
                }
                titleRect = titleImage.rectTransform;
            }
            else
            {
                Text title = CreateText(safeAreaRect, "Title", "STONEHOLD", 110, new Color(0.35f, 0.95f, 0.65f));
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
            }

            // 3. Central Stage Select Panel
            Image stageBg = CreateImage(safeAreaRect, "StagePanel", new Color(0.06f, 0.08f, 0.12f, 0.88f));
            if (isPortrait)
            {
                Place(stageBg.rectTransform, new Vector2(0.5f, 0.74f), Vector2.zero, new Vector2(860f, 200f));
            }
            else
            {
                Place(stageBg.rectTransform, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(860f, 200f));
            }

            stageNumText = CreateText(stageBg.rectTransform, "StageNumText", "STAGE 1", 20, new Color(1f, 0.85f, 0.35f));
            Place(stageNumText.rectTransform, new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(700f, 30f));

            stageNameText = CreateText(stageBg.rectTransform, "StageName", "", 32, Color.white);
            stageNameText.fontStyle = FontStyle.Bold;
            Place(stageNameText.rectTransform, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(700f, 44f));

            stageDescText = CreateText(stageBg.rectTransform, "StageDesc", "", 18, new Color(0.85f, 0.85f, 0.9f));
            Place(stageDescText.rectTransform, new Vector2(0.5f, 0.34f), Vector2.zero, new Vector2(700f, 60f));

            stageRewardText = CreateText(stageBg.rectTransform, "StageRewardText", "Rewards: 🪙 Gold  💎 Gems  📦 Loot Box", 16, new Color(0.75f, 0.75f, 0.8f));
            Place(stageRewardText.rectTransform, new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(700f, 24f));

            prevStageBtn = CreateButton(stageBg.rectTransform, "PrevStage", "<", new Vector2(46f, 46f),
                new Vector2(0f, 0.5f), new Vector2(30f, 0f), () => CycleStage(-1));

            nextStageBtn = CreateButton(stageBg.rectTransform, "NextStage", ">", new Vector2(46f, 46f),
                new Vector2(1f, 0.5f), new Vector2(-30f, 0f), () => CycleStage(1));

            Button heatBtn = CreateButton(stageBg.rectTransform, "AscensionHeatBtn", "🔥 HEAT: 0", new Vector2(150f, 36f),
                new Vector2(0.85f, 0.82f), Vector2.zero, () => ShowAscensionDrawer(true));
            ascensionHeatButtonLabel = heatBtn.GetComponentInChildren<Text>();
            if (ascensionHeatButtonLabel != null)
            {
                ascensionHeatButtonLabel.fontSize = 14;
                ascensionHeatButtonLabel.fontStyle = FontStyle.Bold;
                ascensionHeatButtonLabel.color = new Color(1f, 0.6f, 0.2f);
            }

            Button bestiaryBtn = CreateButton(stageBg.rectTransform, "BestiaryBtn", "📚 CODEX", new Vector2(120f, 36f),
                new Vector2(0.85f, 0.52f), Vector2.zero, () => ShowBestiaryDrawer(true));
            Text bLabel = bestiaryBtn.GetComponentInChildren<Text>();
            if (bLabel != null)
            {
                bLabel.fontSize = 14;
                bLabel.fontStyle = FontStyle.Bold;
                bLabel.color = new Color(0.4f, 0.85f, 1f);
            }

            Button achvBtn = CreateButton(stageBg.rectTransform, "AchievementsBtn", "🏆 TROPHIES", new Vector2(120f, 36f),
                new Vector2(0.85f, 0.22f), Vector2.zero, () => ShowAchievementsDrawer(true));
            Text achvLabel = achvBtn.GetComponentInChildren<Text>();
            if (achvLabel != null)
            {
                achvLabel.fontSize = 13;
                achvLabel.fontStyle = FontStyle.Bold;
                achvLabel.color = new Color(1f, 0.85f, 0.25f);
            }

            Button mapBtn = CreateButton(stageBg.rectTransform, "WorldMapBtn", "🗺️ MAP", new Vector2(120f, 36f),
                new Vector2(0.85f, -0.08f), Vector2.zero, () => ShowWorldMapDrawer(true));
            Text mapLabel = mapBtn.GetComponentInChildren<Text>();
            if (mapLabel != null)
            {
                mapLabel.fontSize = 14;
                mapLabel.fontStyle = FontStyle.Bold;
                mapLabel.color = new Color(0.5f, 1f, 0.5f);
            }

            // 4. Starter Crystal Selection Panel
            Image defenderBg = CreateImage(safeAreaRect, "DefenderPanel", new Color(0.06f, 0.08f, 0.12f, 0.88f));
            if (isPortrait)
            {
                Place(defenderBg.rectTransform, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(860f, 250f));
            }
            else
            {
                Place(defenderBg.rectTransform, new Vector2(0.5f, 0.40f), new Vector2(0f, -20f), new Vector2(860f, 240f));
            }

            Text defenderTitleText = CreateText(defenderBg.rectTransform, "DefenderTitle", "FORTRESS STARTER CRYSTAL", 16, new Color(1f, 0.85f, 0.35f));
            Place(defenderTitleText.rectTransform, new Vector2(0.5f, 0.90f), Vector2.zero, new Vector2(700f, 24f));

            defenderNameText = CreateText(defenderBg.rectTransform, "DefenderName", "", 24, Color.white);
            Place(defenderNameText.rectTransform, new Vector2(0.5f, 0.54f), Vector2.zero, new Vector2(700f, 36f));

            defenderStatsText = CreateText(defenderBg.rectTransform, "DefenderStatsText", "", 17, Color.white);
            defenderStatsText.alignment = TextAnchor.MiddleCenter;
            Place(defenderStatsText.rectTransform, new Vector2(0.5f, 0.26f), Vector2.zero, new Vector2(740f, 90f));

            string[] cIds = { "crystal_fire", "crystal_ice", "crystal_lightning", "crystal_stone", "crystal_shadow" };
            string[] cLabels = { "FIRE", "ICE", "LIGHTNING", "STONE", "SHADOW" };
            crystalButtons = new Button[5];
            crystalButtonLabels = new Text[5];

            float spacingX = 145f;
            float startX = -(cIds.Length - 1) * spacingX / 2f;

            for (int i = 0; i < cIds.Length; i++)
            {
                string id = cIds[i];
                string label = cLabels[i];
                int idx = i;
                Button btn = CreateButton(defenderBg.rectTransform, "CrystalBtn_" + id, label, new Vector2(135f, 44f),
                    new Vector2(0.5f, 0.74f), new Vector2(startX + i * spacingX, 0f), () => OnCrystalSelected(id));
                crystalButtons[idx] = btn;
                crystalButtonLabels[idx] = btn.GetComponentInChildren<Text>();
                crystalButtonLabels[idx].fontSize = 15;
                crystalButtonLabels[idx].fontStyle = FontStyle.Bold;
            }

            RefreshCrystalSelection();

            // 5. Primary Action Buttons (Vertical Stack: CAMPAIGN + UPGRADES)
            Sprite campaignSprite = LoadSprite("UI/btn_campaign");
            Sprite upgradesSprite = LoadSprite("UI/btn_upgrades");
            Sprite secondarySprite = LoadSprite("UI/btn_secondary");

            Vector2 primBtnSize = isPortrait ? new Vector2(680f, 130f) : new Vector2(460f, 90f);

            // CAMPAIGN Button
            if (campaignSprite != null)
            {
                startButton = CreateSpriteButton(safeAreaRect, "CampaignButton", "", campaignSprite, primBtnSize,
                    isPortrait ? new Vector2(0.5f, 0.44f) : new Vector2(0.5f, 0.22f), Vector2.zero, Play);
            }
            else
            {
                startButton = CreateButton(safeAreaRect, "StartButton", "CAMPAIGN", primBtnSize,
                    isPortrait ? new Vector2(0.5f, 0.44f) : new Vector2(0.5f, 0.22f), Vector2.zero, Play);
                ColorBlock scb = startButton.colors;
                scb.normalColor = new Color(0.8f, 0.2f, 0.15f, 1.0f);
                scb.highlightedColor = new Color(0.95f, 0.3f, 0.25f, 1.0f);
                startButton.colors = scb;
            }
            startButtonLabel = startButton.GetComponentInChildren<Text>();
            if (startButtonLabel != null)
            {
                startButtonLabel.fontStyle = FontStyle.Bold;
                startButtonLabel.fontSize = 34;
                startButtonLabel.color = new Color(1f, 0.9f, 0.35f);
            }

            // UPGRADES Button
            Button upgradesBtn;
            if (upgradesSprite != null)
            {
                upgradesBtn = CreateSpriteButton(safeAreaRect, "UpgradesMainButton", "", upgradesSprite, primBtnSize,
                    isPortrait ? new Vector2(0.5f, 0.35f) : new Vector2(0.5f, 0.12f), Vector2.zero, () => ShowRelicsDrawer(true));
            }
            else
            {
                upgradesBtn = CreateButton(safeAreaRect, "UpgradesMainButton", "UPGRADES", primBtnSize,
                    isPortrait ? new Vector2(0.5f, 0.35f) : new Vector2(0.5f, 0.12f), Vector2.zero, () => ShowRelicsDrawer(true));
                ColorBlock ucb = upgradesBtn.colors;
                ucb.normalColor = new Color(0.85f, 0.65f, 0.2f, 1.0f);
                ucb.highlightedColor = new Color(0.95f, 0.78f, 0.3f, 1.0f);
                upgradesBtn.colors = ucb;
            }

            // 6. Secondary Buttons Row (Settings, About, Quit)
            Vector2 secBtnSize = isPortrait ? new Vector2(230f, 54f) : new Vector2(180f, 48f);
            Vector2 secRowAnchor = isPortrait ? new Vector2(0.5f, 0.27f) : new Vector2(0.18f, 0.12f);

            float secSpacing = isPortrait ? 250f : 190f;
            float secStartX = Application.isMobilePlatform ? -secSpacing / 2f : -secSpacing;

            // Settings Button
            if (secondarySprite != null)
            {
                CreateSpriteButton(safeAreaRect, "SettingsBtnSec", "⚙️ SETTINGS", secondarySprite, secBtnSize,
                    secRowAnchor, new Vector2(secStartX, 0f), () => ShowSettings(true), new Color(0.9f, 0.92f, 0.98f), 17);
            }
            else
            {
                CreateButton(safeAreaRect, "SettingsBtnSec", "⚙️ Settings", secBtnSize,
                    secRowAnchor, new Vector2(secStartX, 0f), () => ShowSettings(true));
            }

            // About Button
            if (secondarySprite != null)
            {
                CreateSpriteButton(safeAreaRect, "AboutBtnSec", "ℹ️ ABOUT", secondarySprite, secBtnSize,
                    secRowAnchor, new Vector2(secStartX + secSpacing, 0f), () =>
                    {
                        Debug.Log("[Stonehold] V2.0 - Defend the Keep against relentless enemy sieges!");
                    }, new Color(0.9f, 0.92f, 0.98f), 17);
            }
            else
            {
                CreateButton(safeAreaRect, "AboutBtnSec", "ℹ️ About", secBtnSize,
                    secRowAnchor, new Vector2(secStartX + secSpacing, 0f), () =>
                    {
                        Debug.Log("[Stonehold] V2.0 - Defend the Keep against relentless enemy sieges!");
                    });
            }

            // Quit Button (Desktop only)
            if (!Application.isMobilePlatform)
            {
                if (secondarySprite != null)
                {
                    CreateSpriteButton(safeAreaRect, "QuitBtnSec", "✖ QUIT", secondarySprite, secBtnSize,
                        secRowAnchor, new Vector2(secStartX + secSpacing * 2f, 0f), QuitGame, new Color(1f, 0.6f, 0.6f), 17);
                }
                else
                {
                    CreateButton(safeAreaRect, "QuitBtnSec", "✖ Quit", secBtnSize,
                        secRowAnchor, new Vector2(secStartX + secSpacing * 2f, 0f), QuitGame);
                }
            }

            // 7. Stats & Meta Upgrades Panels (Preserved for compatibility and deep progression)
            Image statsBg = CreateImage(safeAreaRect, "StatsPanel", new Color(0.06f, 0.08f, 0.12f, 0.7f));
            if (isPortrait)
            {
                Place(statsBg.rectTransform, new Vector2(0.5f, 0.17f), new Vector2(-220f, 0f), new Vector2(400f, 210f));
            }
            else
            {
                Place(statsBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(260f, 50f), new Vector2(400f, 400f));
            }

            statsText = CreateText(statsBg.rectTransform, "StatsText", "", 18, new Color(0.85f, 0.85f, 0.9f));
            statsText.alignment = TextAnchor.UpperLeft;
            Place(statsText.rectTransform, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(340f, 130f));
            RefreshStats();

            CreateButton(statsBg.rectTransform, "ResetStatsButton", "Reset Stats", new Vector2(160f, 38f),
                new Vector2(0.5f, 0.18f), Vector2.zero, ResetStats);

            Image upgradesBg = CreateImage(safeAreaRect, "UpgradesPanel", new Color(0.06f, 0.08f, 0.12f, 0.7f));
            if (isPortrait)
            {
                Place(upgradesBg.rectTransform, new Vector2(0.5f, 0.17f), new Vector2(220f, 0f), new Vector2(400f, 210f));
            }
            else
            {
                Place(upgradesBg.rectTransform, new Vector2(1f, 0.5f), new Vector2(-260f, 30f), new Vector2(400f, 460f));
            }

            Text upgradesTitle = CreateText(upgradesBg.rectTransform, "Title", "META UPGRADES", 18, new Color(1f, 0.85f, 0.35f));
            Place(upgradesTitle.rectTransform, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(340f, 26f));

            if (MetaUpgradeManager.Instance != null)
            {
                var list = MetaUpgradeManager.Instance.Upgrades;
                int rowCount = Mathf.Min(list.Count, metaUpgradeNameTexts.Length);
                float rowStartY = 0.65f;
                float rowSpacingY = 0.30f;
                for (int i = 0; i < rowCount; i++)
                {
                    int index = i;
                    var upgrade = list[i];

                    metaUpgradeNameTexts[index] = CreateText(upgradesBg.rectTransform, $"UpgradeName_{index}", "", 14, Color.white);
                    metaUpgradeNameTexts[index].alignment = TextAnchor.MiddleLeft;
                    Place(metaUpgradeNameTexts[index].rectTransform, new Vector2(0.5f, rowStartY - index * rowSpacingY), new Vector2(-50f, 12f), new Vector2(240f, 20f));

                    metaUpgradeButtons[index] = CreateButton(upgradesBg.rectTransform, $"BuyBtn_{index}", "", new Vector2(90f, 32f),
                        new Vector2(0.5f, rowStartY - index * rowSpacingY), new Vector2(130f, 4f), () => OnMetaUpgradeClicked(upgrade.id));
                    metaUpgradeButtonLabels[index] = metaUpgradeButtons[index].GetComponentInChildren<Text>();
                    metaUpgradeButtonLabels[index].fontSize = 13;
                    metaUpgradeButtonLabels[index].fontStyle = FontStyle.Bold;
                }
            }
            RefreshMetaUpgradesPanel();

            // 8. Bottom Navigation Bar
            Image bottomBar = CreateImage(safeAreaRect, "BottomBar", new Color(0.04f, 0.05f, 0.08f, 0.98f));
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
                UnityEngine.Events.UnityAction onTabClick = null;
                switch (labelText)
                {
                    case "CHARACTERS": onTabClick = () => ShowRelicsDrawer(true); break;
                    case "LIBRARY": onTabClick = () => ShowBestiaryDrawer(true); break;
                    case "MAP": onTabClick = () => ShowWorldMapDrawer(true); break;
                    default: onTabClick = () => Debug.Log($"Tab clicked: {labelText}."); break;
                }

                Button tabBtn = CreateButton(bottomBar.rectTransform, "Tab_" + labelText, labelText, new Vector2(tabWidth, 60f),
                    new Vector2(0.5f, 0.5f), new Vector2(tabStartX + i * tabSpacing, 0f), onTabClick);

                Text tabBtnLabel = tabBtn.GetComponentInChildren<Text>();
                tabBtnLabel.fontSize = isPortrait ? 15 : 22;
                tabBtnLabel.fontStyle = FontStyle.Bold;

                ColorBlock tcb = tabBtn.colors;
                if (labelText == "BATTLES")
                {
                    tcb.normalColor = new Color(0.85f, 0.25f, 0.18f, 1.0f);
                    tcb.highlightedColor = new Color(0.98f, 0.35f, 0.28f, 1.0f);
                    tabBtnLabel.color = new Color(1f, 0.92f, 0.4f);
                }
                else
                {
                    tcb.normalColor = new Color(0.10f, 0.14f, 0.20f, 0.95f);
                    tcb.highlightedColor = new Color(0.18f, 0.24f, 0.35f, 1.0f);
                }
                tabBtn.colors = tcb;
            }

            RefreshStageSelection();
            BuildSettingsPanel();
            BuildAscensionDrawer();
            BuildBestiaryDrawer();
            BuildAchievementsDrawer();
            BuildWorldMapDrawer();
            BuildTreasuryModal();
            BuildDailyRewardsDrawer();
            BuildQuestsDrawer();
            BuildRelicsDrawer();
            RefreshAscensionHeatButton();
        }

        private Text fpsBtnLabel;
        private Text hapticsBtnLabel;

        public static int TargetFps
        {
            get => PlayerPrefs.GetInt("settings_target_fps", 60);
            set
            {
                int clamped = (value <= 30) ? 30 : 60;
                PlayerPrefs.SetInt("settings_target_fps", clamped);
                PlayerPrefs.Save();
                Application.targetFrameRate = clamped;
            }
        }

        private static string GetFpsButtonText()
        {
            return TargetFps == 30 ? "FPS: 30 (Battery)" : "FPS: 60 (Smooth)";
        }

        private static string GetHapticsButtonText()
        {
            return HapticFeedbackManager.IsHapticsEnabled ? "Haptics: ON" : "Haptics: OFF";
        }

        private void RefreshFpsAndHapticsLabels()
        {
            if (fpsBtnLabel != null) fpsBtnLabel.text = GetFpsButtonText();
            if (hapticsBtnLabel != null) hapticsBtnLabel.text = GetHapticsButtonText();
        }

        private void BuildSettingsPanel()
        {
            Image dim = CreateImage(canvasRect, "SettingsPanel", new Color(0f, 0f, 0f, 0.8f));
            Stretch(dim.rectTransform);

            Text header = CreateText(dim.rectTransform, "Header", "Settings", 56, Color.white);
            header.fontStyle = FontStyle.Bold;
            Place(header.rectTransform, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(700f, 80f));

            qualityLabel = CreateText(dim.rectTransform, "QualityLabel", "Graphics:", 28, new Color(0.85f, 0.85f, 0.9f));
            Place(qualityLabel.rectTransform, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(700f, 36f));

            qualityTierButtons.Clear();
            string[] tierLabels = { "Low\n(Saver)", "Medium\n(Balanced)", "High\n(Crisp)" };
            float spacing = 220f;
            float startX = -(3 - 1) * spacing / 2f;
            for (int i = 0; i < 3; i++)
            {
                int level = i;
                Button btn = CreateButton(dim.rectTransform, "QualityTier_" + i, tierLabels[i], new Vector2(200f, 54f),
                    new Vector2(0.5f, 0.63f), new Vector2(startX + i * spacing, 0f), () => SetQuality(level));
                Text btnText = btn.GetComponentInChildren<Text>();
                if (btnText != null)
                {
                    btnText.fontSize = 18;
                    btnText.alignment = TextAnchor.MiddleCenter;
                }
                qualityTierButtons.Add(btn);
            }

            qualityDescriptionText = CreateText(dim.rectTransform, "QualityDesc", "", 18, new Color(0.72f, 0.78f, 0.88f, 0.9f));
            Place(qualityDescriptionText.rectTransform, new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(720f, 32f));

            Button fpsBtn = CreateButton(dim.rectTransform, "FpsToggleBtn", GetFpsButtonText(), new Vector2(250f, 50f),
                new Vector2(0.5f, 0.50f), new Vector2(-140f, 0f), () =>
                {
                    TargetFps = TargetFps == 60 ? 30 : 60;
                    RefreshFpsAndHapticsLabels();
                });
            fpsBtnLabel = fpsBtn.GetComponentInChildren<Text>();
            if (fpsBtnLabel != null) fpsBtnLabel.fontSize = 20;

            Button hapticBtn = CreateButton(dim.rectTransform, "HapticsToggleBtn", GetHapticsButtonText(), new Vector2(250f, 50f),
                new Vector2(0.5f, 0.50f), new Vector2(140f, 0f), () =>
                {
                    HapticFeedbackManager.ToggleHaptics();
                    RefreshFpsAndHapticsLabels();
                });
            hapticsBtnLabel = hapticBtn.GetComponentInChildren<Text>();
            if (hapticsBtnLabel != null) hapticsBtnLabel.fontSize = 20;

            CreateVolumeRow(dim.rectTransform, "Master", 0.40f,
                () => AudioManager.Instance != null ? AudioManager.Instance.MasterVolume : 1f,
                v => { if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(v); });
            CreateVolumeRow(dim.rectTransform, "Music", 0.31f,
                () => AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.6f,
                v => { if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v); });
            CreateVolumeRow(dim.rectTransform, "SFX", 0.22f,
                () => AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 0.9f,
                v => { if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(v); });

            CreateButton(dim.rectTransform, "BackButton", "Back", new Vector2(280f, 56f),
                new Vector2(0.5f, 0.10f), Vector2.zero, () => ShowSettings(false));

            settingsGroup = dim.gameObject.AddComponent<CanvasGroup>();
            ShowSettings(false);
            RefreshQualityLabel();
            RefreshFpsAndHapticsLabels();
        }

        private void BuildAscensionDrawer()
        {
            Image dim = CreateImage(canvasRect, "AscensionDrawerPanel", new Color(0.04f, 0.05f, 0.08f, 0.94f));
            Stretch(dim.rectTransform);

            Image panelBox = CreateImage(dim.rectTransform, "AscensionBox", new Color(0.10f, 0.13f, 0.20f, 0.98f));
            Place(panelBox.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 680f));

            Text header = CreateText(panelBox.rectTransform, "AscensionHeader", "🔥 ASCENSION HEAT MUTATORS", 34, new Color(1f, 0.82f, 0.3f));
            header.fontStyle = FontStyle.Bold;
            Place(header.rectTransform, new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(860f, 44f));

            ascensionDrawerHeatTitle = CreateText(panelBox.rectTransform, "AscensionHeatTitle", "", 20, new Color(0.9f, 0.9f, 0.95f));
            Place(ascensionDrawerHeatTitle.rectTransform, new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(860f, 30f));

            mutatorStatusTexts.Clear();
            mutatorCheckBgs.Clear();

            var mutators = AscensionManager.Instance != null ? AscensionManager.Instance.AllMutators : null;
            if (mutators == null || mutators.Count == 0)
            {
                if (AscensionManager.Instance != null)
                {
                    AscensionManager.Instance.LoadAllMutators();
                    mutators = AscensionManager.Instance.AllMutators;
                }
            }

            int count = mutators != null ? mutators.Count : 0;
            float colWidth = 430f;
            float rowHeight = 100f;
            float startY = 0.70f;
            float stepY = 0.16f;

            for (int i = 0; i < count; i++)
            {
                var mutator = mutators[i];
                if (mutator == null) continue;
                int col = i % 2;
                int row = i / 2;
                float posX = col == 0 ? -225f : 225f;
                float posY = startY - (row * stepY);

                Image cardBg = CreateImage(panelBox.rectTransform, $"MutatorCard_{mutator.id}", new Color(0.15f, 0.19f, 0.28f, 0.9f));
                Place(cardBg.rectTransform, new Vector2(0.5f, posY), new Vector2(posX, 0f), new Vector2(colWidth, rowHeight));

                Button cardBtn = cardBg.gameObject.AddComponent<Button>();
                cardBtn.targetGraphic = cardBg;
                string mId = mutator.id;
                cardBtn.onClick.AddListener(() =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayButton();
                    if (AscensionManager.Instance != null)
                    {
                        AscensionManager.Instance.ToggleMutator(mId);
                    }
                    RefreshAscensionDrawer();
                });

                // Checkbox Image
                Image checkImg = CreateImage(cardBg.rectTransform, "CheckBg", new Color(0.25f, 0.3f, 0.4f, 1f));
                Place(checkImg.rectTransform, new Vector2(0.08f, 0.5f), Vector2.zero, new Vector2(32f, 32f));
                mutatorCheckBgs.Add(checkImg);

                Text checkMark = CreateText(checkImg.rectTransform, "Mark", "", 22, Color.white);
                checkMark.fontStyle = FontStyle.Bold;
                mutatorStatusTexts.Add(checkMark);

                // Title + Heat Points
                Text cardTitle = CreateText(cardBg.rectTransform, "Title", $"{mutator.displayName}  [+{mutator.heatPoints} HEAT]", 17, mutator.themeColor);
                cardTitle.alignment = TextAnchor.MiddleLeft;
                cardTitle.fontStyle = FontStyle.Bold;
                Place(cardTitle.rectTransform, new Vector2(0.58f, 0.72f), Vector2.zero, new Vector2(320f, 26f));

                // Description
                Text cardDesc = CreateText(cardBg.rectTransform, "Desc", mutator.description, 13, new Color(0.8f, 0.82f, 0.88f));
                cardDesc.alignment = TextAnchor.MiddleLeft;
                cardDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
                Place(cardDesc.rectTransform, new Vector2(0.58f, 0.35f), Vector2.zero, new Vector2(320f, 44f));
            }

            CreateButton(panelBox.rectTransform, "ClearAscensionBtn", "CLEAR ALL", new Vector2(180f, 50f),
                new Vector2(0.35f, 0.08f), Vector2.zero, () =>
                {
                    if (AscensionManager.Instance != null) AscensionManager.Instance.ClearAllMutators();
                    RefreshAscensionDrawer();
                });

            CreateButton(panelBox.rectTransform, "CloseAscensionBtn", "APPLY & CLOSE", new Vector2(240f, 50f),
                new Vector2(0.65f, 0.08f), Vector2.zero, () => ShowAscensionDrawer(false));

            ascensionDrawerGroup = dim.gameObject.AddComponent<CanvasGroup>();
            ShowAscensionDrawer(false);
        }

        private void ShowAscensionDrawer(bool show)
        {
            if (ascensionDrawerGroup != null)
            {
                ascensionDrawerGroup.alpha = show ? 1f : 0f;
                ascensionDrawerGroup.interactable = show;
                ascensionDrawerGroup.blocksRaycasts = show;
            }
            if (show)
            {
                RefreshAscensionDrawer();
            }
            RefreshAscensionHeatButton();
        }

        private void RefreshAscensionDrawer()
        {
            int heat = AscensionManager.Instance != null ? AscensionManager.Instance.GetCurrentHeatLevel() : 0;
            float scoreBonus = AscensionManager.Instance != null ? (AscensionManager.Instance.GetScoreMultiplier() - 1f) * 100f : 0f;

            if (ascensionDrawerHeatTitle != null)
            {
                ascensionDrawerHeatTitle.text = $"🔥 TOTAL HEAT: {heat}  |  SCORE & REWARDS BONUS: +{Mathf.RoundToInt(scoreBonus)}%";
            }

            var mutators = AscensionManager.Instance != null ? AscensionManager.Instance.AllMutators : null;
            if (mutators != null)
            {
                for (int i = 0; i < mutators.Count; i++)
                {
                    if (i >= mutatorStatusTexts.Count || i >= mutatorCheckBgs.Count) break;
                    var m = mutators[i];
                    bool isActive = AscensionManager.Instance != null && AscensionManager.Instance.IsMutatorActive(m.id);
                    mutatorStatusTexts[i].text = isActive ? "✓" : "";
                    mutatorCheckBgs[i].color = isActive ? new Color(0.95f, 0.45f, 0.1f, 1f) : new Color(0.2f, 0.25f, 0.35f, 1f);
                }
            }

            RefreshAscensionHeatButton();
        }

        private void RefreshAscensionHeatButton()
        {
            if (ascensionHeatButtonLabel != null)
            {
                int heat = AscensionManager.Instance != null ? AscensionManager.Instance.GetCurrentHeatLevel() : 0;
                ascensionHeatButtonLabel.text = heat > 0 ? $"🔥 HEAT: +{heat}" : "🔥 HEAT: 0";
                ascensionHeatButtonLabel.color = heat > 0 ? new Color(1f, 0.5f, 0.15f) : new Color(0.7f, 0.7f, 0.75f);
            }
        }

        private void BuildBestiaryDrawer()
        {
            Image dim = CreateImage(canvasRect, "BestiaryDrawerPanel", new Color(0.04f, 0.05f, 0.08f, 0.94f));
            Stretch(dim.rectTransform);

            Image panelBox = CreateImage(dim.rectTransform, "BestiaryBox", new Color(0.10f, 0.13f, 0.20f, 0.98f));
            Place(panelBox.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060f, 700f));

            Text header = CreateText(panelBox.rectTransform, "BestiaryHeader", "📚 BESTIARY & ELEMENTAL CODEX", 32, new Color(0.4f, 0.85f, 1f));
            header.fontStyle = FontStyle.Bold;
            Place(header.rectTransform, new Vector2(0.5f, 0.93f), Vector2.zero, new Vector2(960f, 44f));

            Text subtitle = CreateText(panelBox.rectTransform, "BestiarySubtitle", "Tactical analysis of enemy weaknesses, armor classes, and counter strategies.", 16, new Color(0.8f, 0.85f, 0.95f));
            Place(subtitle.rectTransform, new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(960f, 26f));

            // Left Side: Enemy List Container
            Image listBg = CreateImage(panelBox.rectTransform, "EnemyListBg", new Color(0.06f, 0.08f, 0.12f, 0.9f));
            Place(listBg.rectTransform, new Vector2(0.24f, 0.44f), Vector2.zero, new Vector2(440f, 520f));

            // Right Side: Details Inspector Panel
            Image detailsBg = CreateImage(panelBox.rectTransform, "DetailsBg", new Color(0.08f, 0.10f, 0.16f, 0.95f));
            Place(detailsBg.rectTransform, new Vector2(0.72f, 0.44f), Vector2.zero, new Vector2(500f, 520f));

            bestiarySelectedNameText = CreateText(detailsBg.rectTransform, "SelectedName", "ENEMY NAME", 26, Color.white);
            bestiarySelectedNameText.fontStyle = FontStyle.Bold;
            Place(bestiarySelectedNameText.rectTransform, new Vector2(0.5f, 0.90f), Vector2.zero, new Vector2(460f, 36f));

            bestiarySelectedCategoryText = CreateText(detailsBg.rectTransform, "SelectedCategory", "CATEGORY • THREAT LEVEL: ★★★☆☆", 15, new Color(1f, 0.8f, 0.3f));
            Place(bestiarySelectedCategoryText.rectTransform, new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(460f, 24f));

            bestiarySelectedStatsText = CreateText(detailsBg.rectTransform, "SelectedStats", "❤️ HP: 100   🛡️ ARMOR: 10   ⚡ SPEED: 2.0", 16, new Color(0.85f, 0.9f, 1f));
            Place(bestiarySelectedStatsText.rectTransform, new Vector2(0.5f, 0.74f), Vector2.zero, new Vector2(460f, 26f));

            bestiarySelectedLoreText = CreateText(detailsBg.rectTransform, "SelectedLore", "Lore description...", 14, new Color(0.8f, 0.85f, 0.9f));
            bestiarySelectedLoreText.alignment = TextAnchor.UpperLeft;
            Place(bestiarySelectedLoreText.rectTransform, new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(460f, 90f));

            bestiarySelectedWeaknessText = CreateText(detailsBg.rectTransform, "SelectedWeakness", "💥 ELEMENTAL WEAKNESSES:\n• 🔥 BURN: +50% Area Damage\n• ⚡ SHOCK: Overload Vulnerable", 14, new Color(1f, 0.6f, 0.4f));
            bestiarySelectedWeaknessText.alignment = TextAnchor.UpperLeft;
            Place(bestiarySelectedWeaknessText.rectTransform, new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(460f, 100f));

            bestiarySelectedTipsText = CreateText(detailsBg.rectTransform, "SelectedTips", "🛡️ RECOMMENDED COUNTER:\nFrost Mage slow + Fire Mage area burn to melt armored frontline.", 14, new Color(0.4f, 0.95f, 0.6f));
            bestiarySelectedTipsText.alignment = TextAnchor.UpperLeft;
            Place(bestiarySelectedTipsText.rectTransform, new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(460f, 75f));

            // Populate Left List
            var entries = BestiaryManager.Instance != null ? BestiaryManager.Instance.AllEntries : null;
            if (entries == null || entries.Count == 0)
            {
                if (BestiaryManager.Instance != null)
                {
                    BestiaryManager.Instance.LoadAllEntries();
                    entries = BestiaryManager.Instance.AllEntries;
                }
            }

            int count = entries != null ? entries.Count : 0;
            float itemHeight = 72f;
            float startY = 210f;

            for (int i = 0; i < count; i++)
            {
                var entry = entries[i];
                if (entry == null) continue;

                Image cardBg = CreateImage(listBg.rectTransform, "Card_" + entry.enemyId, new Color(0.12f, 0.16f, 0.24f, 0.95f));
                Place(cardBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, startY - i * itemHeight), new Vector2(410f, 64f));

                int kills = BestiaryManager.Instance != null ? BestiaryManager.Instance.GetKillCount(entry.enemyId) : 0;
                bool encountered = BestiaryManager.Instance != null && BestiaryManager.Instance.IsEncountered(entry.enemyId);

                string badge = entry.isBoss ? "👑 BOSS" : entry.category.ToString().ToUpper();
                Text nameTxt = CreateText(cardBg.rectTransform, "Name", $"{(encountered ? entry.displayName : "??? Unidentified")}\n<size=12><color=#F5C842>{badge}</color> | Kills: {kills}</size>", 15, encountered ? Color.white : new Color(0.6f, 0.6f, 0.6f));
                nameTxt.alignment = TextAnchor.MiddleLeft;
                Place(nameTxt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(20f, 0f), new Vector2(340f, 50f));

                Button cardBtn = cardBg.gameObject.AddComponent<Button>();
                cardBtn.onClick.AddListener(() => SelectBestiaryEntry(entry));
            }

            CreateButton(panelBox.rectTransform, "CloseBestiaryBtn", "CLOSE CODEX", new Vector2(240f, 48f),
                new Vector2(0.5f, 0.05f), Vector2.zero, () => ShowBestiaryDrawer(false));

            bestiaryDrawerGroup = dim.gameObject.AddComponent<CanvasGroup>();
            ShowBestiaryDrawer(false);

            if (entries != null && entries.Count > 0)
            {
                SelectBestiaryEntry(entries[0]);
            }
        }

        public void ShowBestiaryDrawer(bool show)
        {
            if (bestiaryDrawerGroup != null)
            {
                bestiaryDrawerGroup.alpha = show ? 1f : 0f;
                bestiaryDrawerGroup.interactable = show;
                bestiaryDrawerGroup.blocksRaycasts = show;
            }
        }

        private void SelectBestiaryEntry(BestiaryEntryDefinition entry)
        {
            if (entry == null) return;
            selectedBestiaryEntry = entry;

            bool encountered = BestiaryManager.Instance != null && BestiaryManager.Instance.IsEncountered(entry.enemyId);
            int kills = BestiaryManager.Instance != null ? BestiaryManager.Instance.GetKillCount(entry.enemyId) : 0;

            if (bestiarySelectedNameText != null)
            {
                bestiarySelectedNameText.text = encountered ? entry.displayName.ToUpperInvariant() : "??? UNIDENTIFIED FOE";
                bestiarySelectedNameText.color = entry.themeColor;
            }

            if (bestiarySelectedCategoryText != null)
            {
                string stars = new string('★', Mathf.Clamp(entry.threatLevel, 1, 5)) + new string('☆', 5 - Mathf.Clamp(entry.threatLevel, 1, 5));
                bestiarySelectedCategoryText.text = $"{entry.category.ToString().ToUpper()} • THREAT: {stars} • KILLS: {kills}";
            }

            if (bestiarySelectedStatsText != null)
            {
                bestiarySelectedStatsText.text = encountered
                    ? $"❤️ HP: {entry.baseHealth}   🛡️ ARMOR: {entry.baseArmor}   ⚡ SPEED: {entry.baseSpeed:F1}"
                    : "❤️ HP: ???   🛡️ ARMOR: ???   ⚡ SPEED: ???";
            }

            if (bestiarySelectedLoreText != null)
            {
                bestiarySelectedLoreText.text = encountered
                    ? entry.loreDescription
                    : "Defeat this creature in battle to unlock comprehensive reconnaissance, elemental vulnerability analyses, and tactical counters.";
            }

            if (bestiarySelectedWeaknessText != null)
            {
                if (!encountered)
                {
                    bestiarySelectedWeaknessText.text = "💥 ELEMENTAL WEAKNESSES:\n• Data encrypted. Encounter enemy to decode.";
                }
                else
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.AppendLine("💥 ELEMENTAL WEAKNESSES & SYNERGIES:");
                    if (entry.weaknesses.Count > 0)
                    {
                        foreach (var w in entry.weaknesses)
                        {
                            sb.AppendLine($"• {w.ToString().ToUpper()}: +50% damage & increased vulnerability");
                        }
                    }
                    else
                    {
                        sb.AppendLine("• Neutral to standard elemental effects.");
                    }
                    bestiarySelectedWeaknessText.text = sb.ToString().TrimEnd();
                }
            }

            if (bestiarySelectedTipsText != null)
            {
                bestiarySelectedTipsText.text = encountered
                    ? $"🛡️ RECOMMENDED COUNTER:\n{entry.recommendedHeroCounter}\n\n💡 TACTICAL TIP:\n{entry.tacticalCounterTips}"
                    : "🛡️ RECOMMENDED COUNTER: Unknown";
            }
        }

        private void BuildAchievementsDrawer()
        {
            Image dim = CreateImage(canvasRect, "AchievementsDrawerPanel", new Color(0.04f, 0.05f, 0.08f, 0.94f));
            Stretch(dim.rectTransform);

            Image panelBox = CreateImage(dim.rectTransform, "AchievementsBox", new Color(0.10f, 0.13f, 0.20f, 0.98f));
            Place(panelBox.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060f, 700f));

            Text header = CreateText(panelBox.rectTransform, "AchievementsHeader", "🏆 ACHIEVEMENTS & MILESTONE QUESTS", 32, new Color(1f, 0.85f, 0.25f));
            header.fontStyle = FontStyle.Bold;
            Place(header.rectTransform, new Vector2(0.5f, 0.93f), Vector2.zero, new Vector2(960f, 44f));

            achievementsHeaderStatsText = CreateText(panelBox.rectTransform, "AchievementsStats", "", 16, new Color(0.85f, 0.90f, 0.98f));
            Place(achievementsHeaderStatsText.rectTransform, new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(960f, 26f));

            // List Container
            achievementsListContainer = CreateImage(panelBox.rectTransform, "AchievementsList", new Color(0.06f, 0.08f, 0.12f, 0.92f));
            Place(achievementsListContainer.rectTransform, new Vector2(0.5f, 0.46f), Vector2.zero, new Vector2(980f, 480f));

            CreateButton(panelBox.rectTransform, "CloseAchievementsBtn", "CLOSE TROPHY ROOM", new Vector2(260f, 48f),
                new Vector2(0.5f, 0.05f), Vector2.zero, () => ShowAchievementsDrawer(false));

            achievementsDrawerGroup = dim.gameObject.AddComponent<CanvasGroup>();
            ShowAchievementsDrawer(false);
        }

        public void ShowAchievementsDrawer(bool show)
        {
            if (achievementsDrawerGroup != null)
            {
                achievementsDrawerGroup.alpha = show ? 1f : 0f;
                achievementsDrawerGroup.interactable = show;
                achievementsDrawerGroup.blocksRaycasts = show;
            }
            if (show)
            {
                RefreshAchievementsDrawer();
            }
        }

        public void RefreshAchievementsDrawer()
        {
            if (achievementsListContainer == null) return;

            var manager = AchievementManager.Instance;
            if (manager == null) return;

            if (achievementsHeaderStatsText != null)
            {
                int unlocked = manager.GetUnlockedCount();
                int total = manager.GetTotalCount();
                float pct = manager.GetCompletionPercentage();
                achievementsHeaderStatsText.text = $"🏆 TOTAL UNLOCKED: {unlocked} / {total} ({pct:F0}%)  •  Claim rewards to power up Meta Upgrades!";
            }

            for (int i = 0; i < achievementCardObjects.Count; i++)
            {
                if (achievementCardObjects[i] != null) Destroy(achievementCardObjects[i]);
            }
            achievementCardObjects.Clear();

            var list = manager.AllAchievements;
            if (list == null || list.Count == 0) return;

            float colWidth = 460f;
            float rowHeight = 90f;
            float startX = -colWidth * 0.5f - 10f;
            float startY = 180f;

            for (int i = 0; i < list.Count && i < 10; i++)
            {
                var achv = list[i];
                if (achv == null) continue;

                int col = i % 2;
                int row = i / 2;
                float posX = col == 0 ? startX : -startX;
                float posY = startY - row * (rowHeight + 10f);

                GameObject cardObj = new GameObject("AchvCard_" + achv.id, typeof(RectTransform));
                RectTransform cardRt = cardObj.GetComponent<RectTransform>();
                cardRt.SetParent(achievementsListContainer.rectTransform, false);
                Place(cardRt, new Vector2(0.5f, 0.5f), new Vector2(posX, posY), new Vector2(colWidth, rowHeight));

                Image cardBg = CreateImage(cardRt, "CardBg", new Color(0.12f, 0.16f, 0.24f, 0.95f));
                Stretch(cardBg.rectTransform);

                bool unlocked = manager.IsUnlocked(achv.id);
                bool claimed = manager.IsClaimed(achv.id);
                float progress = manager.GetProgress(achv.id);
                float target = achv.targetValue;
                float fillPct = target > 0f ? Mathf.Clamp01(progress / target) : (unlocked ? 1f : 0f);

                Text icon = CreateText(cardRt, "Icon", achv.iconBadge, 26, Color.white);
                Place(icon.rectTransform, new Vector2(0.08f, 0.5f), Vector2.zero, new Vector2(50f, 50f));

                Text titleTxt = CreateText(cardRt, "Title", achv.title, 16, unlocked ? new Color(1f, 0.85f, 0.25f) : Color.white);
                titleTxt.fontStyle = FontStyle.Bold;
                titleTxt.alignment = TextAnchor.MiddleLeft;
                Place(titleTxt.rectTransform, new Vector2(0.42f, 0.72f), Vector2.zero, new Vector2(240f, 24f));

                Text descTxt = CreateText(cardRt, "Desc", achv.description, 12, new Color(0.8f, 0.85f, 0.9f));
                descTxt.alignment = TextAnchor.MiddleLeft;
                Place(descTxt.rectTransform, new Vector2(0.42f, 0.38f), Vector2.zero, new Vector2(240f, 28f));

                // Progress Bar Underline
                Image pBg = CreateImage(cardRt, "ProgBg", new Color(0.06f, 0.08f, 0.12f, 1f));
                Place(pBg.rectTransform, new Vector2(0.42f, 0.14f), Vector2.zero, new Vector2(240f, 8f));

                Image pFill = CreateImage(pBg.rectTransform, "ProgFill", unlocked ? new Color(0.3f, 0.9f, 0.35f) : new Color(0.35f, 0.65f, 1f));
                Place(pFill.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(240f * fillPct, 8f));

                // Action Button
                if (unlocked && !claimed)
                {
                    Button claimBtn = CreateButton(cardRt, "ClaimBtn", $"CLAIM\n+{achv.rewardGold}G +{achv.rewardMaterials}M", new Vector2(120f, 48f),
                        new Vector2(0.84f, 0.5f), Vector2.zero, () =>
                        {
                            manager.ClaimReward(achv.id, out _, out _);
                            if (AudioManager.Instance != null) AudioManager.Instance.PlayUpgrade();
                            RefreshAchievementsDrawer();
                        });
                    var bImg = claimBtn.GetComponent<Image>();
                    if (bImg != null) bImg.color = new Color(0.85f, 0.65f, 0.15f, 1f);
                    var bTxt = claimBtn.GetComponentInChildren<Text>();
                    if (bTxt != null) { bTxt.fontSize = 11; bTxt.fontStyle = FontStyle.Bold; }
                }
                else if (claimed)
                {
                    Button doneBtn = CreateButton(cardRt, "DoneBtn", "✓ CLAIMED", new Vector2(100f, 38f),
                        new Vector2(0.84f, 0.5f), Vector2.zero, () => { });
                    doneBtn.interactable = false;
                    var bTxt = doneBtn.GetComponentInChildren<Text>();
                    if (bTxt != null) { bTxt.fontSize = 12; bTxt.color = new Color(0.5f, 0.8f, 0.5f); }
                }
                else
                {
                    Button lockBtn = CreateButton(cardRt, "LockBtn", $"{progress:F0}/{target:F0}", new Vector2(100f, 38f),
                        new Vector2(0.84f, 0.5f), Vector2.zero, () => { });
                    lockBtn.interactable = false;
                    var bTxt = lockBtn.GetComponentInChildren<Text>();
                    if (bTxt != null) { bTxt.fontSize = 12; bTxt.color = new Color(0.6f, 0.65f, 0.7f); }
                }

                achievementCardObjects.Add(cardObj);
            }
        }

        private void BuildWorldMapDrawer()
        {
            Image dim = CreateImage(canvasRect, "WorldMapDrawerPanel", new Color(0.03f, 0.04f, 0.07f, 0.96f));
            Stretch(dim.rectTransform);

            Image panelBox = CreateImage(dim.rectTransform, "WorldMapBox", new Color(0.08f, 0.11f, 0.17f, 0.98f));
            Place(panelBox.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 720f));

            Text header = CreateText(panelBox.rectTransform, "WorldMapHeader", "🗺️ REALM OF STONEHOLD • CAMPAIGN WORLD MAP", 28, new Color(0.4f, 0.85f, 1f));
            header.fontStyle = FontStyle.Bold;
            Place(header.rectTransform, new Vector2(0.5f, 0.94f), Vector2.zero, new Vector2(980f, 38f));

            worldMapStarsTotalText = CreateText(panelBox.rectTransform, "WorldMapStats", "", 16, new Color(1f, 0.85f, 0.25f));
            Place(worldMapStarsTotalText.rectTransform, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(980f, 26f));

            // Map Area (Left)
            worldMapCanvasPanel = CreateImage(panelBox.rectTransform, "MapCanvas", new Color(0.05f, 0.07f, 0.11f, 0.95f));
            Place(worldMapCanvasPanel.rectTransform, new Vector2(0.36f, 0.46f), Vector2.zero, new Vector2(740f, 480f));

            // Stage Intel Panel (Right)
            Image intelBox = CreateImage(panelBox.rectTransform, "StageIntelBox", new Color(0.12f, 0.15f, 0.22f, 0.98f));
            Place(intelBox.rectTransform, new Vector2(0.85f, 0.46f), Vector2.zero, new Vector2(300f, 480f));

            worldMapSelectedStageTitle = CreateText(intelBox.rectTransform, "StageTitle", "STAGE INTEL", 20, Color.white);
            worldMapSelectedStageTitle.fontStyle = FontStyle.Bold;
            Place(worldMapSelectedStageTitle.rectTransform, new Vector2(0.5f, 0.90f), Vector2.zero, new Vector2(280f, 32f));

            worldMapSelectedStageLore = CreateText(intelBox.rectTransform, "StageLore", "", 13, new Color(0.85f, 0.9f, 0.95f));
            worldMapSelectedStageLore.alignment = TextAnchor.UpperLeft;
            Place(worldMapSelectedStageLore.rectTransform, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(270f, 120f));

            worldMapSelectedStageStarsText = CreateText(intelBox.rectTransform, "StageStars", "", 13, new Color(1f, 0.85f, 0.3f));
            worldMapSelectedStageStarsText.alignment = TextAnchor.UpperLeft;
            Place(worldMapSelectedStageStarsText.rectTransform, new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(270f, 100f));

            worldMapSelectedStageScoreText = CreateText(intelBox.rectTransform, "StageScore", "", 13, new Color(0.6f, 0.85f, 1f));
            Place(worldMapSelectedStageScoreText.rectTransform, new Vector2(0.5f, 0.20f), Vector2.zero, new Vector2(270f, 30f));

            worldMapLaunchButton = CreateButton(intelBox.rectTransform, "DeployStageBtn", "🚀 DEPLOY TO STAGE", new Vector2(260f, 44f),
                new Vector2(0.5f, 0.08f), Vector2.zero, () =>
                {
                    SaveManager.SetSelectedStage(worldMapSelectedStageIndex);
                    RefreshStageSelection();
                    ShowWorldMapDrawer(false);
                });
            var deployImg = worldMapLaunchButton.GetComponent<Image>();
            if (deployImg != null) deployImg.color = new Color(0.15f, 0.6f, 0.3f, 1f);

            CreateButton(panelBox.rectTransform, "CloseMapBtn", "CLOSE MAP", new Vector2(200f, 42f),
                new Vector2(0.5f, 0.04f), Vector2.zero, () => ShowWorldMapDrawer(false));

            worldMapDrawerGroup = dim.gameObject.AddComponent<CanvasGroup>();
            ShowWorldMapDrawer(false);
        }

        public void ShowWorldMapDrawer(bool show)
        {
            if (worldMapDrawerGroup != null)
            {
                worldMapDrawerGroup.alpha = show ? 1f : 0f;
                worldMapDrawerGroup.interactable = show;
                worldMapDrawerGroup.blocksRaycasts = show;
            }
            if (show)
            {
                RefreshWorldMapDrawer();
            }
        }

        public void RefreshWorldMapDrawer()
        {
            if (worldMapCanvasPanel == null) return;

            var manager = CampaignProgressionManager.Instance;
            if (manager == null) return;

            if (worldMapStarsTotalText != null)
            {
                int earned = manager.GetTotalStarsEarned();
                int maxStars = (manager.AllNodes != null && manager.AllNodes.Count > 0) ? manager.AllNodes.Count * 3 : 30;
                worldMapStarsTotalText.text = $"⭐ CAMPAIGN STARS: {earned} / {maxStars}   •   Select a node to inspect sector recon & deploy!";
            }

            for (int i = 0; i < worldMapNodeObjects.Count; i++)
            {
                if (worldMapNodeObjects[i] != null) Destroy(worldMapNodeObjects[i]);
            }
            worldMapNodeObjects.Clear();

            var nodes = manager.AllNodes;
            if (nodes == null || nodes.Count == 0) return;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node == null) continue;

                int stageIdx = node.stageIndex;
                bool unlocked = manager.IsStageUnlocked(stageIdx);
                int stars = manager.GetStarsForStage(stageIdx);
                int heat = manager.GetHighestHeatCleared(stageIdx);

                GameObject pinObj = new GameObject("MapPin_" + stageIdx, typeof(RectTransform));
                RectTransform pinRt = pinObj.GetComponent<RectTransform>();
                pinRt.SetParent(worldMapCanvasPanel.rectTransform, false);
                Place(pinRt, new Vector2(0.5f, 0.5f), node.mapCoordinates, new Vector2(110f, 85f));

                Image pinBg = CreateImage(pinRt, "PinBg", unlocked ? new Color(0.15f, 0.20f, 0.30f, 0.95f) : new Color(0.10f, 0.12f, 0.16f, 0.8f));
                Stretch(pinBg.rectTransform);

                Button pinBtn = pinObj.AddComponent<Button>();
                pinBtn.onClick.AddListener(() =>
                {
                    SelectWorldMapNode(stageIdx);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayUpgrade();
                });

                Text iconTxt = CreateText(pinRt, "Icon", $"{node.biomeIcon} STAGE {stageIdx + 1}", 12, unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f));
                iconTxt.fontStyle = FontStyle.Bold;
                Place(iconTxt.rectTransform, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(100f, 20f));

                string starStr = "";
                for (int s = 1; s <= 3; s++)
                {
                    starStr += (s <= stars) ? "★" : "☆";
                }
                Text starsTxt = CreateText(pinRt, "Stars", starStr, 16, unlocked ? new Color(1f, 0.85f, 0.2f) : new Color(0.4f, 0.4f, 0.4f));
                Place(starsTxt.rectTransform, new Vector2(0.5f, 0.40f), Vector2.zero, new Vector2(100f, 22f));

                string subLabel = unlocked ? (heat > 0 ? $"🔥 H:{heat}" : "CLEAR") : $"🔒 {node.requiredTotalStarsToUnlock}★";
                Text subTxt = CreateText(pinRt, "Sub", subLabel, 10, unlocked ? (heat > 0 ? new Color(1f, 0.6f, 0.2f) : new Color(0.4f, 0.9f, 0.4f)) : new Color(0.7f, 0.3f, 0.3f));
                Place(subTxt.rectTransform, new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(100f, 16f));

                worldMapNodeObjects.Add(pinObj);
            }

            SelectWorldMapNode(SaveManager.SelectedStageIndex);
        }

        private void SelectWorldMapNode(int stageIndex)
        {
            worldMapSelectedStageIndex = stageIndex;
            var manager = CampaignProgressionManager.Instance;
            if (manager == null) return;

            var node = manager.GetNode(stageIndex);
            if (node == null) return;

            bool unlocked = manager.IsStageUnlocked(stageIndex);
            int stars = manager.GetStarsForStage(stageIndex);
            int bestScore = manager.GetBestScore(stageIndex);
            int highestHeat = manager.GetHighestHeatCleared(stageIndex);

            if (worldMapSelectedStageTitle != null)
            {
                worldMapSelectedStageTitle.text = $"{node.biomeIcon} {node.stageName.ToUpper()}\n<size=12><color=#88bbff>{node.biomeTheme}</color></size>";
                worldMapSelectedStageTitle.color = node.themeColor;
            }

            if (worldMapSelectedStageLore != null)
            {
                worldMapSelectedStageLore.text = unlocked ? node.loreBriefing : $"🔒 LOCKED SECTOR\nEarn {node.requiredTotalStarsToUnlock} total stars across previous stages to unlock.";
            }

            if (worldMapSelectedStageStarsText != null)
            {
                string s1 = stars >= 1 ? "✅" : "⬜";
                string s2 = stars >= 2 ? "✅" : "⬜";
                string s3 = stars >= 3 ? "✅" : "⬜";
                worldMapSelectedStageStarsText.text = $"★ OBJECTIVES:\n{s1} {node.star1Condition}\n{s2} {node.star2Condition}\n{s3} {node.star3Condition}";
            }

            if (worldMapSelectedStageScoreText != null)
            {
                worldMapSelectedStageScoreText.text = $"👑 BEST SCORE: {bestScore:N0}   •   🔥 MAX HEAT: H:{highestHeat}";
            }

            if (worldMapLaunchButton != null)
            {
                worldMapLaunchButton.interactable = unlocked;
                var bTxt = worldMapLaunchButton.GetComponentInChildren<Text>();
                if (bTxt != null)
                {
                    bTxt.text = unlocked ? $"🚀 DEPLOY TO STAGE {stageIndex + 1}" : "🔒 STAGE LOCKED";
                }
            }
        }

        private void BuildTreasuryModal()
        {
            Image dim = CreateImage(canvasRect, "TreasuryModalPanel", new Color(0.04f, 0.05f, 0.08f, 0.94f));
            Stretch(dim.rectTransform);

            Image panelBox = CreateImage(dim.rectTransform, "TreasuryBox", new Color(0.10f, 0.13f, 0.20f, 0.98f));
            Place(panelBox.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 520f));

            Text header = CreateText(panelBox.rectTransform, "Header", "📦 FORTRESS IDLE TREASURY", 32, new Color(1f, 0.82f, 0.3f));
            header.fontStyle = FontStyle.Bold;
            Place(header.rectTransform, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(700f, 44f));

            Text subtitle = CreateText(panelBox.rectTransform, "Subtitle", "Generates passive gold & materials while you are away (8 Hours Max Storage).", 16, new Color(0.85f, 0.88f, 0.95f));
            Place(subtitle.rectTransform, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(700f, 30f));

            treasuryDetailsText = CreateText(panelBox.rectTransform, "Details", "", 24, Color.white);
            treasuryDetailsText.fontStyle = FontStyle.Bold;
            Place(treasuryDetailsText.rectTransform, new Vector2(0.5f, 0.54f), Vector2.zero, new Vector2(650f, 100f));

            // Fill Bar Background
            Image barBg = CreateImage(panelBox.rectTransform, "BarBg", new Color(0.15f, 0.18f, 0.25f, 1f));
            Place(barBg.rectTransform, new Vector2(0.5f, 0.35f), Vector2.zero, new Vector2(500f, 28f));

            treasuryFillBar = CreateImage(barBg.rectTransform, "BarFill", new Color(0.95f, 0.75f, 0.2f, 1f));
            Place(treasuryFillBar.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(500f, 28f));
            treasuryFillBar.rectTransform.pivot = new Vector2(0f, 0.5f);

            claimTreasuryBtn = CreateButton(panelBox.rectTransform, "ClaimTreasuryBtn", "CLAIM REWARDS", new Vector2(260f, 54f),
                new Vector2(0.35f, 0.14f), Vector2.zero, () =>
                {
                    if (IdleTreasuryManager.Instance != null && IdleTreasuryManager.Instance.ClaimTreasury(out int gold, out int mats))
                    {
                        RefreshCurrencies();
                        RefreshTreasuryUI();
                    }
                });
            claimTreasuryBtnLabel = claimTreasuryBtn.GetComponentInChildren<Text>();

            CreateButton(panelBox.rectTransform, "CloseTreasuryBtn", "CLOSE", new Vector2(200f, 54f),
                new Vector2(0.68f, 0.14f), Vector2.zero, () => ShowTreasuryModal(false));

            treasuryDrawerGroup = dim.gameObject.AddComponent<CanvasGroup>();
            ShowTreasuryModal(false);
        }

        public void ShowTreasuryModal(bool show)
        {
            if (treasuryDrawerGroup != null)
            {
                treasuryDrawerGroup.alpha = show ? 1f : 0f;
                treasuryDrawerGroup.interactable = show;
                treasuryDrawerGroup.blocksRaycasts = show;
            }
            if (show) RefreshTreasuryUI();
        }

        private void RefreshTreasuryUI()
        {
            if (IdleTreasuryManager.Instance == null) return;

            int gold = IdleTreasuryManager.Instance.AccumulatedGold;
            int mats = IdleTreasuryManager.Instance.AccumulatedMaterials;
            float fill = IdleTreasuryManager.Instance.FillPercentage01;

            if (treasuryDetailsText != null)
            {
                treasuryDetailsText.text = $"🪙 <b>{gold}</b> / 400 Meta Gold\n📦 <b>{mats}</b> / 40 Core Materials\n<size=14><color=#88ccff>Vault Capacity: {Mathf.RoundToInt(fill * 100f)}%</color></size>";
            }

            if (treasuryFillBar != null)
            {
                treasuryFillBar.rectTransform.sizeDelta = new Vector2(500f * fill, 28f);
            }

            if (claimTreasuryBtn != null)
            {
                bool canClaim = gold > 0 || mats > 0;
                claimTreasuryBtn.interactable = canClaim;
                if (claimTreasuryBtnLabel != null)
                {
                    claimTreasuryBtnLabel.text = canClaim ? $"CLAIM (+{gold}G)" : "VAULT EMPTY";
                }
            }
        }

        private void BuildDailyRewardsDrawer()
        {
            Image dim = CreateImage(canvasRect, "DailyRewardsPanel", new Color(0.04f, 0.05f, 0.08f, 0.94f));
            Stretch(dim.rectTransform);

            Image panelBox = CreateImage(dim.rectTransform, "DailyBox", new Color(0.10f, 0.13f, 0.20f, 0.98f));
            Place(panelBox.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 540f));

            Text header = CreateText(panelBox.rectTransform, "Header", "🎁 7-DAY LOGIN STREAK REWARDS", 32, new Color(1f, 0.82f, 0.3f));
            header.fontStyle = FontStyle.Bold;
            Place(header.rectTransform, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(900f, 44f));

            dailyRewardStatusTexts.Clear();
            float cardWidth = 115f;
            float spacing = 18f;
            float startX = -((7 * cardWidth + 6 * spacing) * 0.5f) + cardWidth * 0.5f;

            for (int i = 0; i < 7; i++)
            {
                int day = i;
                DailyRewardInfo reward = IdleTreasuryManager.Instance != null
                    ? IdleTreasuryManager.Instance.GetRewardForDay(day)
                    : new DailyRewardInfo { dayNumber = day + 1, gold = 100 * (day + 1), materials = 10 * day, iconBadge = "🪙" };

                Image cardBg = CreateImage(panelBox.rectTransform, $"DayCard_{day + 1}", new Color(0.15f, 0.19f, 0.28f, 0.95f));
                Place(cardBg.rectTransform, new Vector2(0.5f, 0.52f), new Vector2(startX + day * (cardWidth + spacing), 0f), new Vector2(cardWidth, 180f));

                Text dayNumTxt = CreateText(cardBg.rectTransform, "DayNum", $"DAY {day + 1}", 15, new Color(1f, 0.85f, 0.3f));
                dayNumTxt.fontStyle = FontStyle.Bold;
                Place(dayNumTxt.rectTransform, new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(100f, 22f));

                Text iconTxt = CreateText(cardBg.rectTransform, "Icon", reward.iconBadge, 36, Color.white);
                Place(iconTxt.rectTransform, new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(80f, 45f));

                string loot = reward.materials > 0 ? $"🪙{reward.gold}\n📦{reward.materials}" : $"🪙{reward.gold}";
                Text lootTxt = CreateText(cardBg.rectTransform, "Loot", loot, 13, Color.white);
                lootTxt.fontStyle = FontStyle.Bold;
                Place(lootTxt.rectTransform, new Vector2(0.5f, 0.26f), Vector2.zero, new Vector2(100f, 36f));

                Text statusTxt = CreateText(cardBg.rectTransform, "Status", "", 12, new Color(0.4f, 1f, 0.4f));
                statusTxt.fontStyle = FontStyle.Bold;
                Place(statusTxt.rectTransform, new Vector2(0.5f, 0.08f), Vector2.zero, new Vector2(100f, 20f));
                dailyRewardStatusTexts.Add(statusTxt);
            }

            claimDailyRewardBtn = CreateButton(panelBox.rectTransform, "ClaimDailyBtn", "CLAIM TODAY'S REWARD", new Vector2(280f, 54f),
                new Vector2(0.35f, 0.12f), Vector2.zero, () =>
                {
                    if (IdleTreasuryManager.Instance != null && IdleTreasuryManager.Instance.ClaimDailyReward(out DailyRewardInfo claimed))
                    {
                        RefreshCurrencies();
                        RefreshDailyRewardsUI();
                    }
                });
            claimDailyRewardBtnLabel = claimDailyRewardBtn.GetComponentInChildren<Text>();

            CreateButton(panelBox.rectTransform, "CloseDailyBtn", "CLOSE", new Vector2(200f, 54f),
                new Vector2(0.68f, 0.12f), Vector2.zero, () => ShowDailyRewardsDrawer(false));

            dailyRewardsDrawerGroup = dim.gameObject.AddComponent<CanvasGroup>();
            ShowDailyRewardsDrawer(false);
        }

        public void ShowDailyRewardsDrawer(bool show)
        {
            if (dailyRewardsDrawerGroup != null)
            {
                dailyRewardsDrawerGroup.alpha = show ? 1f : 0f;
                dailyRewardsDrawerGroup.interactable = show;
                dailyRewardsDrawerGroup.blocksRaycasts = show;
            }
            if (show) RefreshDailyRewardsUI();
        }

        private void RefreshDailyRewardsUI()
        {
            if (IdleTreasuryManager.Instance == null) return;

            int currentStreak = IdleTreasuryManager.Instance.CurrentStreakIndex;
            bool available = IdleTreasuryManager.Instance.IsDailyRewardAvailable();

            for (int i = 0; i < dailyRewardStatusTexts.Count; i++)
            {
                if (dailyRewardStatusTexts[i] == null) continue;
                if (i < currentStreak)
                {
                    dailyRewardStatusTexts[i].text = "✅ CLAIMED";
                    dailyRewardStatusTexts[i].color = new Color(0.4f, 1f, 0.4f);
                }
                else if (i == currentStreak)
                {
                    dailyRewardStatusTexts[i].text = available ? "READY" : "CLAIMED";
                    dailyRewardStatusTexts[i].color = available ? new Color(1f, 0.85f, 0.2f) : new Color(0.6f, 0.6f, 0.6f);
                }
                else
                {
                    dailyRewardStatusTexts[i].text = "LOCKED";
                    dailyRewardStatusTexts[i].color = new Color(0.5f, 0.5f, 0.55f);
                }
            }

            if (claimDailyRewardBtn != null)
            {
                claimDailyRewardBtn.interactable = available;
                if (claimDailyRewardBtnLabel != null)
                {
                    claimDailyRewardBtnLabel.text = available ? "CLAIM TODAY'S REWARD" : "COME BACK TOMORROW";
                }
            }
        }

        private void BuildQuestsDrawer()
        {
            Image dim = CreateImage(canvasRect, "QuestsDrawerPanel", new Color(0.04f, 0.05f, 0.08f, 0.94f));
            Stretch(dim.rectTransform);

            Image panelBox = CreateImage(dim.rectTransform, "QuestsBox", new Color(0.10f, 0.13f, 0.20f, 0.98f));
            Place(panelBox.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 600f));

            Text header = CreateText(panelBox.rectTransform, "Header", "📜 COMMANDER MISSIONS & QUESTS", 30, new Color(1f, 0.85f, 0.3f));
            header.fontStyle = FontStyle.Bold;
            Place(header.rectTransform, new Vector2(0.5f, 0.91f), Vector2.zero, new Vector2(800f, 40f));

            // Tabs: Daily vs Weekly
            Button dailyTabBtn = CreateButton(panelBox.rectTransform, "TabDaily", "DAILY MISSIONS (24H)", new Vector2(260f, 44f),
                new Vector2(0.35f, 0.81f), Vector2.zero, () =>
                {
                    selectedQuestTab = QuestType.Daily;
                    RefreshQuestsUI();
                });
            questsTabDailyLabel = dailyTabBtn.GetComponentInChildren<Text>();

            Button weeklyTabBtn = CreateButton(panelBox.rectTransform, "TabWeekly", "WEEKLY CAMPAIGNS (7D)", new Vector2(260f, 44f),
                new Vector2(0.65f, 0.81f), Vector2.zero, () =>
                {
                    selectedQuestTab = QuestType.Weekly;
                    RefreshQuestsUI();
                });
            questsTabWeeklyLabel = weeklyTabBtn.GetComponentInChildren<Text>();

            questsListContainer = CreateImage(panelBox.rectTransform, "QuestsList", new Color(0.06f, 0.08f, 0.12f, 0.8f));
            Place(questsListContainer.rectTransform, new Vector2(0.5f, 0.44f), Vector2.zero, new Vector2(800f, 320f));

            CreateButton(panelBox.rectTransform, "CloseQuestsBtn", "CLOSE", new Vector2(220f, 48f),
                new Vector2(0.5f, 0.08f), Vector2.zero, () => ShowQuestsDrawer(false));

            questsDrawerGroup = dim.gameObject.AddComponent<CanvasGroup>();
            ShowQuestsDrawer(false);
        }

        public void ShowQuestsDrawer(bool show)
        {
            if (questsDrawerGroup != null)
            {
                questsDrawerGroup.alpha = show ? 1f : 0f;
                questsDrawerGroup.interactable = show;
                questsDrawerGroup.blocksRaycasts = show;
            }
            if (show) RefreshQuestsUI();
        }

        private void RefreshQuestsUI()
        {
            if (QuestManager.Instance == null) return;
            QuestManager.Instance.CheckAndRefreshQuests();

            foreach (var go in questCardObjects)
            {
                if (go != null) Destroy(go);
            }
            questCardObjects.Clear();

            if (questsTabDailyLabel != null)
            {
                questsTabDailyLabel.color = (selectedQuestTab == QuestType.Daily) ? new Color(1f, 0.85f, 0.2f) : Color.white;
            }
            if (questsTabWeeklyLabel != null)
            {
                questsTabWeeklyLabel.color = (selectedQuestTab == QuestType.Weekly) ? new Color(1f, 0.85f, 0.2f) : Color.white;
            }

            var quests = QuestManager.Instance.ActiveQuests;
            var filtered = new System.Collections.Generic.List<QuestData>();
            foreach (var q in quests)
            {
                if (q.questType == selectedQuestTab) filtered.Add(q);
            }

            float cardH = 90f;
            float startY = 105f;

            for (int i = 0; i < filtered.Count; i++)
            {
                var q = filtered[i];
                Image card = CreateImage(questsListContainer.rectTransform, "QuestCard_" + i, new Color(0.12f, 0.16f, 0.24f, 0.95f));
                Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, startY - i * (cardH + 10f)), new Vector2(760f, cardH));
                questCardObjects.Add(card.gameObject);

                Text title = CreateText(card.rectTransform, "Title", q.title, 16, new Color(1f, 0.85f, 0.35f));
                title.alignment = TextAnchor.MiddleLeft;
                title.fontStyle = FontStyle.Bold;
                Place(title.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(-120f, 0f), new Vector2(460f, 24f));

                Text desc = CreateText(card.rectTransform, "Desc", q.description, 13, new Color(0.85f, 0.88f, 0.92f));
                desc.alignment = TextAnchor.MiddleLeft;
                Place(desc.rectTransform, new Vector2(0.5f, 0.38f), new Vector2(-120f, 0f), new Vector2(460f, 22f));

                string progressStr = $"[{q.currentAmount}/{q.targetAmount}]";
                Text prog = CreateText(card.rectTransform, "Prog", progressStr, 12, q.IsCompleted ? new Color(0.4f, 1f, 0.4f) : new Color(0.7f, 0.75f, 0.85f));
                prog.alignment = TextAnchor.MiddleLeft;
                Place(prog.rectTransform, new Vector2(0.5f, 0.12f), new Vector2(-120f, 0f), new Vector2(460f, 18f));

                // Loot Badge
                Text lootTxt = CreateText(card.rectTransform, "Loot", $"🪙{q.goldReward}\n📦{q.materialsReward}", 13, Color.white);
                lootTxt.fontStyle = FontStyle.Bold;
                Place(lootTxt.rectTransform, new Vector2(0.72f, 0.5f), Vector2.zero, new Vector2(100f, 40f));

                // Claim Button
                Button claimBtn = CreateButton(card.rectTransform, "ClaimBtn", q.isClaimed ? "CLAIMED" : (q.IsCompleted ? "CLAIM" : "IN PROGRESS"), new Vector2(130f, 44f),
                    new Vector2(0.89f, 0.5f), Vector2.zero, () =>
                    {
                        if (QuestManager.Instance.ClaimQuestReward(q.questId, out _, out _))
                        {
                            RefreshCurrencies();
                            RefreshQuestsUI();
                        }
                    });

                claimBtn.interactable = q.IsCompleted && !q.isClaimed;
                var bLabel = claimBtn.GetComponentInChildren<Text>();
                if (bLabel != null)
                {
                    bLabel.fontSize = 13;
                    bLabel.fontStyle = FontStyle.Bold;
                    bLabel.color = q.isClaimed ? new Color(0.5f, 0.5f, 0.5f) : (q.IsCompleted ? new Color(1f, 0.9f, 0.2f) : new Color(0.7f, 0.7f, 0.7f));
                }
            }
        }

        private void BuildRelicsDrawer()
        {
            Image dim = CreateImage(canvasRect, "RelicsDrawerPanel", new Color(0.04f, 0.05f, 0.08f, 0.94f));
            Stretch(dim.rectTransform);

            Image panelBox = CreateImage(dim.rectTransform, "RelicsBox", new Color(0.10f, 0.13f, 0.20f, 0.98f));
            Place(panelBox.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940f, 620f));

            Text header = CreateText(panelBox.rectTransform, "Header", "🛡️ ANCIENT HERO ARTIFACTS & RELICS", 28, new Color(1f, 0.85f, 0.3f));
            header.fontStyle = FontStyle.Bold;
            Place(header.rectTransform, new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(900f, 38f));

            // 3 Equipped Slots Container
            relicSlotNameTexts.Clear();
            float slotW = 260f;
            float slotGap = 20f;
            float startX = -((3 * slotW + 2 * slotGap) * 0.5f) + slotW * 0.5f;

            for (int i = 0; i < HeroArtifactManager.MaxEquipSlots; i++)
            {
                int slotIdx = i;
                Image slotBg = CreateImage(panelBox.rectTransform, $"Slot_{i}", new Color(0.14f, 0.18f, 0.26f, 0.95f));
                Place(slotBg.rectTransform, new Vector2(0.5f, 0.77f), new Vector2(startX + i * (slotW + slotGap), 0f), new Vector2(slotW, 90f));

                Text slotHeader = CreateText(slotBg.rectTransform, "Header", $"SLOT {i + 1}", 12, new Color(0.8f, 0.8f, 0.85f));
                Place(slotHeader.rectTransform, new Vector2(0.5f, 0.80f), Vector2.zero, new Vector2(240f, 18f));

                Text slotName = CreateText(slotBg.rectTransform, "Name", "Empty Slot", 14, Color.white);
                slotName.fontStyle = FontStyle.Bold;
                Place(slotName.rectTransform, new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(240f, 22f));
                relicSlotNameTexts.Add(slotName);

                CreateButton(slotBg.rectTransform, "UnequipBtn", "UNEQUIP", new Vector2(90f, 24f),
                    new Vector2(0.5f, 0.16f), Vector2.zero, () =>
                    {
                        if (HeroArtifactManager.Instance != null)
                        {
                            HeroArtifactManager.Instance.UnequipRelic(slotIdx);
                            RefreshRelicsUI();
                        }
                    });
            }

            // Summary Text
            relicsSummaryText = CreateText(panelBox.rectTransform, "Summary", "", 13, new Color(0.4f, 0.85f, 1f));
            relicsSummaryText.fontStyle = FontStyle.Bold;
            Place(relicsSummaryText.rectTransform, new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(880f, 26f));

            // Catalog List
            relicsListContainer = CreateImage(panelBox.rectTransform, "RelicsList", new Color(0.06f, 0.08f, 0.12f, 0.8f));
            Place(relicsListContainer.rectTransform, new Vector2(0.5f, 0.36f), Vector2.zero, new Vector2(880f, 260f));

            CreateButton(panelBox.rectTransform, "CloseRelicsBtn", "CLOSE", new Vector2(220f, 48f),
                new Vector2(0.5f, 0.06f), Vector2.zero, () => ShowRelicsDrawer(false));

            relicsDrawerGroup = dim.gameObject.AddComponent<CanvasGroup>();
            ShowRelicsDrawer(false);
        }

        public void ShowRelicsDrawer(bool show)
        {
            if (relicsDrawerGroup != null)
            {
                relicsDrawerGroup.alpha = show ? 1f : 0f;
                relicsDrawerGroup.interactable = show;
                relicsDrawerGroup.blocksRaycasts = show;
            }
            if (show) RefreshRelicsUI();
        }

        private void RefreshRelicsUI()
        {
            if (HeroArtifactManager.Instance == null) return;

            // Update equipped slots
            for (int i = 0; i < relicSlotNameTexts.Count; i++)
            {
                var relic = HeroArtifactManager.Instance.GetEquipped(i);
                if (relic != null)
                {
                    relicSlotNameTexts[i].text = $"{relic.iconBadge} {relic.displayName}";
                    relicSlotNameTexts[i].color = relic.RarityColor;
                }
                else
                {
                    relicSlotNameTexts[i].text = "— Empty Slot —";
                    relicSlotNameTexts[i].color = new Color(0.6f, 0.6f, 0.65f);
                }
            }

            if (relicsSummaryText != null)
            {
                float rxn = HeroArtifactManager.Instance.TotalReactionDamageBonus * 100f;
                float crit = HeroArtifactManager.Instance.TotalCritChanceBonus * 100f;
                float castle = HeroArtifactManager.Instance.TotalCastleDamageReduction * 100f;
                float gold = HeroArtifactManager.Instance.TotalGoldGainBonus * 100f;
                float spd = HeroArtifactManager.Instance.TotalAttackSpeedBonus * 100f;

                relicsSummaryText.text = $"⚡ Reaction Dmg: +{rxn:0}%   •   🎯 Crit Rate: +{crit:0}%   •   🛡️ Castle Armor: +{castle:0}%   •   🪙 Gold Gain: +{gold:0}%   •   ⚔️ Speed: +{spd:0}%";
            }

            // Catalog list
            foreach (var go in relicCatalogCardObjects)
            {
                if (go != null) Destroy(go);
            }
            relicCatalogCardObjects.Clear();

            var catalog = HeroArtifactManager.Instance.AllCatalog;
            float rowH = 68f;
            float startY = 85f;

            for (int i = 0; i < catalog.Count; i++)
            {
                var r = catalog[i];
                bool unlocked = HeroArtifactManager.Instance.IsUnlocked(r.id);
                bool isEquipped = false;
                for (int s = 0; s < HeroArtifactManager.MaxEquipSlots; s++)
                {
                    var eq = HeroArtifactManager.Instance.GetEquipped(s);
                    if (eq != null && eq.id == r.id) isEquipped = true;
                }

                Image card = CreateImage(relicsListContainer.rectTransform, "RelicCard_" + i, new Color(0.12f, 0.16f, 0.24f, 0.95f));
                Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, startY - i * (rowH + 8f)), new Vector2(840f, rowH));
                relicCatalogCardObjects.Add(card.gameObject);

                Text title = CreateText(card.rectTransform, "Title", $"{r.iconBadge} {r.displayName.ToUpper()}  <size=12><color=#{ColorUtility.ToHtmlStringRGB(r.RarityColor)}>[{r.rarity.ToString().ToUpper()}]</color></size>", 15, r.RarityColor);
                title.alignment = TextAnchor.MiddleLeft;
                title.fontStyle = FontStyle.Bold;
                Place(title.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(-100f, 0f), new Vector2(580f, 22f));

                Text desc = CreateText(card.rectTransform, "Desc", r.description, 13, new Color(0.85f, 0.88f, 0.92f));
                desc.alignment = TextAnchor.MiddleLeft;
                Place(desc.rectTransform, new Vector2(0.5f, 0.28f), new Vector2(-100f, 0f), new Vector2(580f, 20f));

                Button actionBtn = CreateButton(card.rectTransform, "ActionBtn", isEquipped ? "EQUIPPED" : (unlocked ? "EQUIP" : "LOCKED"), new Vector2(120f, 40f),
                    new Vector2(0.88f, 0.5f), Vector2.zero, () =>
                    {
                        if (unlocked && !isEquipped)
                        {
                            // Find first empty slot or overwrite slot 0
                            int targetSlot = 0;
                            for (int s = 0; s < HeroArtifactManager.MaxEquipSlots; s++)
                            {
                                if (HeroArtifactManager.Instance.GetEquipped(s) == null)
                                {
                                    targetSlot = s;
                                    break;
                                }
                            }
                            HeroArtifactManager.Instance.EquipRelic(r.id, targetSlot);
                            RefreshRelicsUI();
                        }
                    });

                actionBtn.interactable = unlocked && !isEquipped;
                var bLabel = actionBtn.GetComponentInChildren<Text>();
                if (bLabel != null)
                {
                    bLabel.fontSize = 12;
                    bLabel.fontStyle = FontStyle.Bold;
                    bLabel.color = isEquipped ? new Color(0.4f, 1f, 0.4f) : (unlocked ? new Color(1f, 0.85f, 0.2f) : new Color(0.5f, 0.5f, 0.5f));
                }
            }
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

        private static Sprite LoadSprite(string resourcePath)
        {
            Sprite sp = Resources.Load<Sprite>(resourcePath);
            if (sp != null) return sp;

            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }

            // Direct file system fallback (Editor / Dev)
            try
            {
                string[] extensions = { ".jpg", ".png", ".jpeg" };
                foreach (string ext in extensions)
                {
                    string diskPath = System.IO.Path.Combine(Application.dataPath, "_Game/Resources", resourcePath + ext);
                    if (System.IO.File.Exists(diskPath))
                    {
                        byte[] bytes = System.IO.File.ReadAllBytes(diskPath);
                        Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (diskTex.LoadImage(bytes))
                        {
                            return Sprite.Create(diskTex, new Rect(0, 0, diskTex.width, diskTex.height), new Vector2(0.5f, 0.5f));
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MainMenuUI] Could not load sprite from disk: {resourcePath} - {ex.Message}");
            }

            return null;
        }

        private Button CreateSpriteButton(RectTransform parent, string name, string label, Sprite sprite, Vector2 size,
            Vector2 anchor, Vector2 position, UnityEngine.Events.UnityAction onClick, Color? labelColor = null, int fontSize = 28)
        {
            Image bg = CreateImage(parent, name, Color.white);
            bg.raycastTarget = true;
            if (sprite != null)
            {
                bg.sprite = sprite;
                bg.type = Image.Type.Simple;
                bg.preserveAspect = false;
            }
            Place(bg.rectTransform, anchor, position, size);

            Button button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock cb = button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            button.colors = cb;

            button.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayButton();
                }

                onClick();
            });

            if (!string.IsNullOrEmpty(label))
            {
                Text text = CreateText(bg.rectTransform, "Label", label, fontSize, labelColor ?? new Color(1f, 0.92f, 0.7f));
                text.fontStyle = FontStyle.Bold;
                Stretch(text.rectTransform);
            }
            return button;
        }

        private void CreateAmbientMenuParticles(Transform parent)
        {
            if (parent.Find("MenuAmbientParticles") != null) return;

            GameObject psGo = new GameObject("MenuAmbientParticles");
            psGo.transform.SetParent(parent, false);

            ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 8.0f;
            main.startSpeed = 20.0f;
            main.startSize = 4.0f;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.85f, 0.4f, 0.6f),
                new Color(1f, 0.5f, 0.15f, 0.8f)
            );
            main.maxParticles = 25;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var emission = ps.emission;
            emission.rateOverTime = 3.0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(Screen.width > 0 ? Screen.width : 1080f, 60f, 1f);

            psGo.transform.localPosition = new Vector3(0f, -(Screen.height > 0 ? Screen.height / 2f : 960f), 0f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.y = new ParticleSystem.MinMaxCurve(25f, 50f);
            vel.x = new ParticleSystem.MinMaxCurve(-10f, 10f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.45f, 0.1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;
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
                string[] names =
                {
                    "Castle Road",
                    "Highlands",
                    "Frozen Frontier",
                    "Titan Citadel",
                    "Volcanic Caldera",
                    "Toxic Mire",
                    "Thunder Peaks",
                    "Sunken Necropolis",
                    "The Void Rift",
                    "Celestial Sanctum"
                };
                string[] descriptions =
                {
                    "Defend the road to the keep against grunts, armored troops, runners, and the final boss.",
                    "Defend the highland pass against heavier enemy formations and shield phalanxes.",
                    "Hold the frozen frontier against sub-zero blizzards and rapid frostbite waves.",
                    "Endure the ancient titan ruins, massive colossi, and speed vanguards.",
                    "Conquer the scorched infernal magma flows, living molten beasts, and the Magma Core Lord.",
                    "Survive noxious rot wetlands swarming with caustic abominations and toxic chimeras.",
                    "Withstand high-voltage storm cliffs whipped by galvanic harpies and the Tempest Archon.",
                    "Purge ancient cursed crypts swarming with undead skeleton legions and the Dread Necromancer.",
                    "Survive the cosmic horrors of the Abyssal Void Rift, phase-shifting stalkers, and the Void Lord.",
                    "Face the ultimate celestial trial atop the Throne of Eternity against the Ancient King."
                };
                string[] rewards =
                {
                    "Rewards: 🪙 Gold  💎 Gems  📦 Common Chest",
                    "Rewards: 🪙 Gold  💎 Gems  📦 Rare Chest",
                    "Rewards: 🪙 Gold  💎 Gems  📦 Epic Chest",
                    "Rewards: 🪙 Gold  💎 Gems  📦 Mythic Chest",
                    "Rewards: 🪙 Gold  💎 Gems  📦 Radiant Chest",
                    "Rewards: 🪙 Gold  💎 Gems  📦 Toxic Abyssal Chest",
                    "Rewards: 🪙 Gold  💎 Gems  📦 Tempest Storm Chest",
                    "Rewards: 🪙 Gold  💎 Gems  📦 Necrotic Bone Chest",
                    "Rewards: 🪙 Gold  💎 Gems  📦 Cosmic Singularity Chest",
                    "Rewards: 🪙 Gold  💎 Gems  📦 Crown of Eternity Divine Chest"
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
            if (newIndex >= 0 && newIndex < 10)
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
                case "bombardier": return "Rare";
                case "frost_mage": return "Rare";
                case "fire_mage": return "Rare";
                case "electric_engineer": return "Rare";
                case "sniper": return "Epic";
                case "plague_doctor": return "Epic";
                case "radiant_paladin": return "Legendary";
                case "shadow_assassin": return "Epic";
                case "storm_druid": return "Legendary";
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
                case HeroAbilityType.PlagueFlask: return "Plague Flask";
                case HeroAbilityType.Consecration: return "Consecration";
                case HeroAbilityType.ShadowStep: return "Shadow Step";
                case HeroAbilityType.TempestCyclone: return "Tempest Cyclone";
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
                case HeroAbilityType.PlagueFlask: return "Throws a caustic flask releasing toxic poison and reanimating spectral minions.";
                case HeroAbilityType.Consecration: return "Smites the ground with radiant holy energy, shattering kinetic shields.";
                case HeroAbilityType.ShadowStep: return "Instantly strikes high-priority targets with lethal critical damage and resets on kill.";
                case HeroAbilityType.TempestCyclone: return "Conjures a whirling vortex drawing enemies inward while striking with continuous lightning.";
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
                case TargetingMode.Nearest: return "Nearest";
                case TargetingMode.Clustered: return "Clustered";
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

        private void OnCrystalSelected(string crystalId)
        {
            SaveManager.SetSelectedStarterCrystal(crystalId);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButton();
            }
            RefreshCrystalSelection();
        }

        private void RefreshCrystalSelection()
        {
            string selectedId = SaveManager.SelectedStarterCrystalId;
            if (string.IsNullOrEmpty(selectedId))
            {
                selectedId = "crystal_lightning";
            }

            string[] cIds = { "crystal_fire", "crystal_ice", "crystal_lightning", "crystal_stone", "crystal_shadow" };
            Color[] cColors = {
                new Color(1.0f, 0.4f, 0.1f),   // Fire
                new Color(0.2f, 0.8f, 1.0f),   // Ice
                new Color(1.0f, 0.85f, 0.2f),  // Lightning
                new Color(0.7f, 0.6f, 0.5f),   // Stone
                new Color(0.8f, 0.4f, 1.0f)    // Shadow
            };

            for (int i = 0; i < cIds.Length; i++)
            {
                if (crystalButtons != null && i < crystalButtons.Length && crystalButtons[i] != null)
                {
                    bool isSelected = cIds[i] == selectedId;
                    ColorBlock cb = crystalButtons[i].colors;
                    if (isSelected)
                    {
                        cb.normalColor = cColors[i];
                        cb.highlightedColor = cColors[i] * 1.15f;
                        cb.pressedColor = cColors[i] * 0.85f;
                        cb.selectedColor = cColors[i];
                        if (crystalButtonLabels != null && i < crystalButtonLabels.Length && crystalButtonLabels[i] != null)
                        {
                            crystalButtonLabels[i].color = Color.white;
                        }
                    }
                    else
                    {
                        cb.normalColor = new Color(0.18f, 0.22f, 0.30f, 0.95f);
                        cb.highlightedColor = new Color(0.26f, 0.32f, 0.42f, 1f);
                        cb.pressedColor = new Color(0.14f, 0.18f, 0.24f, 1f);
                        cb.selectedColor = new Color(0.18f, 0.22f, 0.30f, 0.95f);
                        if (crystalButtonLabels != null && i < crystalButtonLabels.Length && crystalButtonLabels[i] != null)
                        {
                            crystalButtonLabels[i].color = new Color(0.8f, 0.8f, 0.85f);
                        }
                    }
                    crystalButtons[i].colors = cb;
                }
            }

            if (defenderNameText != null && defenderStatsText != null)
            {
                switch (selectedId)
                {
                    case "crystal_fire":
                        defenderNameText.text = "<color=#ff6600><b>FIRE CRYSTAL</b></color> <color=#45ff70><size=16>[SELECTED]</size></color>  <color=#ffb380>(Splash + Burn)</color>";
                        defenderStatsText.text = "Element: <b>Fire</b> | Identity: <color=#ff6600><b>Splash + Burn</b></color> | Priority: <b>Closest to Keep</b>\n" +
                            "Base Dmg: <b>14</b> | Fire Rate: <b>1.0/s</b> | Range: <b>Full Battlefield</b>\n" +
                            "Special: <color=#ffd759><b>Infernal Splash & Burn</b></color> - Area splash & persistent Burn DoT.";
                        break;
                    case "crystal_ice":
                        defenderNameText.text = "<color=#33ccff><b>ICE CRYSTAL</b></color> <color=#45ff70><size=16>[SELECTED]</size></color>  <color=#99ebff>(Damage + Slow)</color>";
                        defenderStatsText.text = "Element: <b>Ice</b> | Identity: <color=#33ccff><b>Damage + Slow</b></color> | Priority: <b>Closest to Keep</b>\n" +
                            "Base Dmg: <b>12</b> | Fire Rate: <b>1.1/s</b> | Range: <b>Full Battlefield</b>\n" +
                            "Special: <color=#ffd759><b>Glacial Slow</b></color> - 40% movement Slow status effect.";
                        break;
                    case "crystal_lightning":
                        defenderNameText.text = "<color=#ffd726><b>LIGHTNING CRYSTAL</b></color> <color=#45ff70><size=16>[SELECTED]</size></color>  <color=#fffa99>(Fast + Chain)</color>";
                        defenderStatsText.text = "Element: <b>Lightning</b> | Identity: <color=#ffd726><b>Fast + Chain</b></color> | Priority: <b>Closest to Keep</b>\n" +
                            "Base Dmg: <b>15</b> | Fire Rate: <b>1.4/s</b> | Range: <b>Full Battlefield</b>\n" +
                            "Special: <color=#ffd759><b>Chain Storm</b></color> - Fast arcs bounce across up to 3 targets.";
                        break;
                    case "crystal_stone":
                        defenderNameText.text = "<color=#bfa68f><b>STONE CRYSTAL</b></color> <color=#45ff70><size=16>[SELECTED]</size></color>  <color=#dfd0c0>(Heavy + Impact)</color>";
                        defenderStatsText.text = "Element: <b>Stone</b> | Identity: <color=#bfa68f><b>Heavy + Impact</b></color> | Priority: <b>Closest to Keep</b>\n" +
                            "Base Dmg: <b>28</b> | Fire Rate: <b>0.6/s</b> | Range: <b>Full Battlefield</b>\n" +
                            "Special: <color=#ffd759><b>Heavy Cataclysm</b></color> - High single-target & ground impact ring.";
                        break;
                    case "crystal_shadow":
                        defenderNameText.text = "<color=#cc66ff><b>SHADOW CRYSTAL</b></color> <color=#45ff70><size=16>[SELECTED]</size></color>  <color=#e6b3ff>(Curse + DoT)</color>";
                        defenderStatsText.text = "Element: <b>Shadow</b> | Identity: <color=#cc66ff><b>Curse + DoT</b></color> | Priority: <b>Closest to Keep</b>\n" +
                            "Base Dmg: <b>13</b> | Fire Rate: <b>1.0/s</b> | Range: <b>Full Battlefield</b>\n" +
                            "Special: <color=#ffd759><b>Dark Curse</b></color> - Piercing shadow curse DoT & purple mark.";
                        break;
                }
            }
        }

        private void RefreshDefenderSelection()
        {
            RefreshCrystalSelection();
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
                case "plague_doctor": return new Color(0.35f, 0.95f, 0.25f);
                case "radiant_paladin": return new Color(1f, 0.88f, 0.25f);
                case "shadow_assassin": return new Color(0.70f, 0.25f, 0.95f);
                case "storm_druid": return new Color(0.25f, 0.85f, 0.95f);
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
                case "plague_doctor":
                    shape = PrimitiveType.Sphere;
                    localPos = new Vector3(0.24f, 0.45f, 0.16f);
                    localScale = new Vector3(0.18f, 0.18f, 0.18f);
                    break;
                case "radiant_paladin":
                    shape = PrimitiveType.Cube;
                    localPos = new Vector3(-0.30f, 0.48f, 0.15f);
                    localScale = new Vector3(0.10f, 0.48f, 0.30f);
                    break;
                case "shadow_assassin":
                    shape = PrimitiveType.Cube;
                    localPos = new Vector3(0.26f, 0.40f, 0.18f);
                    localScale = new Vector3(0.05f, 0.30f, 0.06f);
                    localRot = Quaternion.Euler(0f, 0f, -30f);
                    break;
                case "storm_druid":
                    shape = PrimitiveType.Cylinder;
                    localPos = new Vector3(0.26f, 0.50f, 0.18f);
                    localScale = new Vector3(0.055f, 0.50f, 0.055f);
                    localRot = Quaternion.Euler(0f, 0f, -8f);
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
