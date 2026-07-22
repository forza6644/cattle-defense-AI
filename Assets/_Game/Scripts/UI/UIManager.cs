using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Stonehold
{
    /// <summary>
    /// Builds and runs the whole in-game UI from code (legacy uGUI Text, built-in
    /// font, responsive CanvasScaler): HUD (gold, wave, castle HP), combat feedback
    /// bars, damage numbers and gold popups, animated wave banner, build menu,
    /// tower upgrade/sell panel, pause menu and victory/defeat screens.
    /// Everything it displays is driven by gameplay events — no game logic here.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private const float PanelFadeSeconds = 0.15f;

        private Font font;
        private RectTransform canvasRect;

        // HUD
        private Text goldText;
        private Text waveText;
        private Text timerText;
        private RectTransform waveProgressBarFill;
        private Image waveProgressBarFillImage;
        private Text castleHpText;
        private RectTransform castleHpFill;
        private Image castleHpFillImage;
        private CanvasGroup waveControlGroup;
        private Text waveStatusText;
        private Text waveCountdownText;
        private Button startWaveButton;
        private Button rerollButton;
        private Text rerollButtonText;

        // Banner
        private CanvasGroup bannerGroup;
        private Text bannerText;

        // Panels
        private CanvasGroup buildMenuGroup;
        private CanvasGroup towerPanelGroup;
        private CanvasGroup pauseGroup;
        private CanvasGroup victoryGroup;
        private CanvasGroup defeatGroup;
        private Text speedButtonLabel;
        private Text towerPanelTitle;
        private Text upgradeButtonLabel;
        private Text sellButtonLabel;
        private Text targetButtonLabel;
        private Button upgradeButton;
        private Button targetButton;
        private Button sellButton;
        private Image xpBgImage;
        private readonly List<Text> buildButtonLabels = new List<Text>();
        private readonly List<Button> buildButtons = new List<Button>();

        // Level Up & XP
        private CanvasGroup levelUpPanelGroup;
        private Text xpText;
        private RectTransform xpFill;
        private Image xpFillImage;
        private readonly Text[] cardTitleTexts = new Text[3];
        private readonly Text[] cardDescriptionTexts = new Text[3];
        private readonly Button[] cardButtons = new Button[3];
        private readonly Image[] cardBorders = new Image[3];
        private readonly Image[] cardInnerBgs = new Image[3];
        private readonly Image[] cardTypeBadges = new Image[3];
        private readonly Text[] cardTypeLabels = new Text[3];
        private RunProgressionManager progression;

        // Battle Result Panel
        private CanvasGroup resultPanelGroup;
        private Text resultTitleText;
        private Text resultSubtitleText;
        private Text resultRewardsText;
        private Text resultDamageReportText;
        private Button resultOkButton;
        private Button resultDoubleButton;
        private bool rewardsClaimed;

        private Text hintText;
        private Image hintBg;
        private float hintTimer;
        private bool hasShownTargetingHint;

        // Cooldown HUD
        private class AbilityCooldownUIItem
        {
            public GameObject Root;
            public Image Background;
            public Image Icon;
            public Image Fill;
            public Text LevelText;
            public Text TimerText;
            public string HeroId;
        }
        private RectTransform abilityHudContainer;
        private readonly List<AbilityCooldownUIItem> abilityUiItems = new List<AbilityCooldownUIItem>();

        // Floating combat text
        private RectTransform floatingRoot;
        private RectTransform safeAreaRect;
        private readonly Queue<Text> floatingPool = new Queue<Text>();

        // Gameplay references
        private EconomyManager economy;
        private WaveManager waves;
        private Castle castle;
        private GameManager game;
        private TowerManager towers;
        private UnlockManager unlocks;
        private Camera cam;
        private Coroutine bannerRoutine;
        private RangeIndicator rangeIndicator;

        // Selection
        private TowerSlot selectedSlot;
        private Tower selectedTower;
        private HeroAttack selectedHero;

        private bool isUiBuilt = false;

        private void Awake()
        {
            Instance = this;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureUIBuilt();
        }

        private void EnsureUIBuilt()
        {
            if (isUiBuilt) return;
            isUiBuilt = true;
            economy = EconomyManager.Instance;
            waves = FindFirstObjectByType<WaveManager>();
            castle = FindFirstObjectByType<Castle>();
            game = GameManager.Instance;
            towers = FindFirstObjectByType<TowerManager>();
            unlocks = UnlockManager.Instance != null ? UnlockManager.Instance : FindFirstObjectByType<UnlockManager>();
            cam = Camera.main;

            BuildUI();
        }

        private void Start()
        {
            EnsureUIBuilt();
            rangeIndicator = RangeIndicator.Create();

            if (towers != null && towers.Config != null && towers.Config.draftRunMode)
            {
                if (goldText != null)
                {
                    goldText.gameObject.SetActive(false);
                }
                if (xpBgImage != null)
                {
                    xpBgImage.rectTransform.anchoredPosition = new Vector2(175f, -72f);
                }
            }

            // Enhance gate, lane, and wall visibility programmatically
            GameObject gate = GameObject.Find("CastleGate_Wood");
            if (gate != null)
            {
                gate.transform.localScale = new Vector3(3.5f, 2.5f, 0.4f);
                Renderer r = gate.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material.color = new Color(0.45f, 0.25f, 0.15f); // Rich oak wood
                }
            }

            GameObject road = GameObject.Find("Road_Dirt");
            if (road != null)
            {
                Renderer r = road.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material.color = new Color(0.35f, 0.28f, 0.22f); // Dark dirt road path
                }
            }

            GameObject wall = GameObject.Find("CastleWall_Stone");
            if (wall != null)
            {
                Renderer r = wall.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material.color = new Color(0.3f, 0.32f, 0.35f); // Stone grey
                }
            }

            progression = RunProgressionManager.Instance != null ? RunProgressionManager.Instance : FindFirstObjectByType<RunProgressionManager>();
            if (progression != null)
            {
                progression.XpChanged += OnXpChanged;
                progression.ShowLevelUpDraft += OnShowLevelUpDraftFromProgression;
            }

            if (economy != null)
            {
                economy.GoldChanged += RefreshGold;
                economy.WaveClearBonusAwarded += OnWaveClearBonusAwarded;
            }
            if (waves != null)
            {
                waves.WaveCountdownStarted += OnWaveCountdownStarted;
                waves.WaveCountdownChanged += OnWaveCountdownChanged;
                waves.WaveCountdownFinished += OnWaveCountdownFinished;
                waves.WaveStarted += OnWaveStarted;
                waves.WaveCleared += OnWaveCleared;
            }
            if (castle != null) castle.HealthChanged += RefreshCastleHealth;
            if (game != null)
            {
                game.StateChanged += OnStateChanged;
                game.GameSpeedChanged += OnGameSpeedChanged;
            }
            if (unlocks != null)
            {
                unlocks.UnlocksChanged += RefreshBuildMenu;
                unlocks.TowerUnlocked += OnTowerUnlocked;
            }

            Enemy.AnyDamagedDetailed += OnEnemyDamagedDetailed;
            Enemy.AnyKilled += OnEnemyKilled;

            RefreshGold();
            RefreshCastleHealth();
            if (progression != null)
            {
                OnXpChanged(progression.CurrentXp, progression.GetXpNeededForNextLevel(), progression.CurrentLevel);
            }
            waveText.text = "Wave -/" + (waves != null ? waves.TotalWaves.ToString() : "-");

            ShowHint("Build Arrow Defenders to stop the first Grunts.");
        }

        private void OnDestroy()
        {
            if (progression != null)
            {
                progression.XpChanged -= OnXpChanged;
                progression.ShowLevelUpDraft -= OnShowLevelUpDraftFromProgression;
            }

            if (economy != null)
            {
                economy.GoldChanged -= RefreshGold;
                economy.WaveClearBonusAwarded -= OnWaveClearBonusAwarded;
            }
            if (waves != null)
            {
                waves.WaveCountdownStarted -= OnWaveCountdownStarted;
                waves.WaveCountdownChanged -= OnWaveCountdownChanged;
                waves.WaveCountdownFinished -= OnWaveCountdownFinished;
                waves.WaveStarted -= OnWaveStarted;
                waves.WaveCleared -= OnWaveCleared;
            }
            if (castle != null) castle.HealthChanged -= RefreshCastleHealth;
            if (game != null)
            {
                game.StateChanged -= OnStateChanged;
                game.GameSpeedChanged -= OnGameSpeedChanged;
            }
            if (unlocks != null)
            {
                unlocks.UnlocksChanged -= RefreshBuildMenu;
                unlocks.TowerUnlocked -= OnTowerUnlocked;
            }

            Enemy.AnyDamagedDetailed -= OnEnemyDamagedDetailed;
            Enemy.AnyKilled -= OnEnemyKilled;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ------------------------------------------------------------------ HUD

        private void RefreshGold()
        {
            EnsureUIBuilt();
            if (goldText != null)
            {
                goldText.text = "Gold: " + (economy != null ? economy.Gold : 0);
            }
            RefreshBuildMenu();
            RefreshTowerPanel();
        }

        private int lastCastleHealth = -1;
        private Coroutine castleHpFlashRoutine;

        private void RefreshCastleHealth()
        {
            EnsureUIBuilt();
            if (castle == null || castleHpFill == null || castleHpText == null)
            {
                return;
            }

            int currentHp = castle.CurrentHealth;
            int maxHp = castle.MaxHealth;
            float pct = maxHp > 0 ? (float)currentHp / maxHp : 0f;
            castleHpFill.localScale = new Vector3(pct, 1f, 1f);
            castleHpText.text = "CASTLE  " + currentHp + " / " + maxHp;

            if (lastCastleHealth != -1 && currentHp != lastCastleHealth)
            {
                if (currentHp < lastCastleHealth)
                {
                    if (castleHpFlashRoutine != null) StopCoroutine(castleHpFlashRoutine);
                    castleHpFlashRoutine = StartCoroutine(FlashCastleHpBar(new Color(1f, 0.2f, 0.2f), pct));
                }
                else
                {
                    if (castleHpFlashRoutine != null) StopCoroutine(castleHpFlashRoutine);
                    castleHpFlashRoutine = StartCoroutine(FlashCastleHpBar(new Color(0.35f, 1f, 0.35f), pct));

                    if (VfxManager.Instance != null)
                    {
                        VfxManager.Instance.PlayCastleRegenFeedback(castle.transform.position + Vector3.up * 1f);
                    }
                }
            }
            else
            {
                castleHpFillImage.color = Color.Lerp(new Color(0.85f, 0.2f, 0.2f), new Color(0.25f, 0.8f, 0.3f), pct);
            }

            lastCastleHealth = currentHp;
        }

        private IEnumerator FlashCastleHpBar(Color flashColor, float finalPct)
        {
            float elapsed = 0f;
            float duration = 0.3f;
            Color baseColor = Color.Lerp(new Color(0.85f, 0.2f, 0.2f), new Color(0.25f, 0.8f, 0.3f), finalPct);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.PingPong(elapsed * 2.5f, 0.5f) / 0.5f;
                castleHpFillImage.color = Color.Lerp(baseColor, flashColor, t);
                yield return null;
            }
            castleHpFillImage.color = baseColor;
        }

        private void OnWaveStarted(int number, WaveData wave)
        {
            EnsureUIBuilt();
            ShowPanel(waveControlGroup, false);

            int total = waves != null ? waves.TotalWaves : 0;
            string totalStr = total > 0 ? total.ToString() : "-";
            if (waveText != null)
            {
                waveText.text = "Wave " + number + "/" + totalStr;
            }

            if (bannerText != null)
            {
                if (total > 0 && number == total)
                {
                    bannerText.color = new Color(1f, 0.25f, 0.2f); // High-contrast Red for Boss
                    ShowBanner("!!! FINAL BOSS WAVE !!!");
                }
                else
                {
                    bannerText.color = Color.white;
                    string label = wave != null && !string.IsNullOrEmpty(wave.waveLabel) ? wave.waveLabel : "Stage Battle";
                    ShowBanner("Wave " + number + " - " + label);
                }
            }

            if (number == 2)
            {
                ShowHint("Armored enemies reduce damage. Cannon hits them harder.");
            }
            else if (number == 3)
            {
                ShowHint("Runners are fast. Frost and Cannon help stop them.");
            }
        }

        private void OnWaveCountdownStarted(int number, WaveData wave, float secondsRemaining)
        {
            EnsureUIBuilt();
            if (waves == null)
            {
                waves = FindFirstObjectByType<WaveManager>();
            }

            int totalWaves = waves != null ? waves.TotalWaves : 0;
            if (waveText != null)
            {
                waveText.text = "Next: Wave " + number + "/" + (totalWaves > 0 ? totalWaves.ToString() : "-");
            }

            if (waveStatusText != null)
            {
                string label = wave != null && !string.IsNullOrEmpty(wave.waveLabel) ? wave.waveLabel : "Wave " + number;
                waveStatusText.text = "Next: " + label;
            }

            OnWaveCountdownChanged(secondsRemaining);
            if (startWaveButton != null)
            {
                startWaveButton.interactable = true;
            }

            ShowPanel(waveControlGroup, true);

            if (bannerText != null)
            {
                if (totalWaves > 0 && number == totalWaves)
                {
                    bannerText.color = new Color(1f, 0.5f, 0.2f); // Orange warning for Boss Countdown
                    ShowBanner("Boss Preparing...");
                    ShowHint("Final Boss incoming! Upgrade defenders and use targeting modes.");
                }
                else
                {
                    bannerText.color = Color.white;
                    ShowBanner("Prepare: Wave " + number);
                }
            }
        }

        private void OnWaveCountdownChanged(float secondsRemaining)
        {
            EnsureUIBuilt();
            if (waveCountdownText != null)
            {
                waveCountdownText.text = "Auto starts in " + Mathf.CeilToInt(secondsRemaining) + "s";
            }
        }

        private void OnWaveCountdownFinished()
        {
            EnsureUIBuilt();
            if (startWaveButton != null)
            {
                startWaveButton.interactable = false;
            }

            ShowPanel(waveControlGroup, false);
        }

        private void OnWaveCleared(int number, WaveData wave)
        {
            EnsureUIBuilt();
            int total = waves != null ? waves.TotalWaves : 0;
            if (waveText != null)
            {
                waveText.text = "Wave " + number + "/" + (total > 0 ? total.ToString() : "-") + " cleared";
            }
        }

        private void OnWaveClearBonusAwarded(int waveNumber, int amount)
        {
            EnsureUIBuilt();
            int total = waves != null ? waves.TotalWaves : 0;
            string totalStr = total > 0 ? total.ToString() : "-";

            if (towers != null && towers.Config != null && towers.Config.draftRunMode)
            {
                if (waveText != null) waveText.text = "Wave " + waveNumber + "/" + totalStr + " cleared";
                ShowBanner("Wave " + waveNumber + " Cleared!");
                return;
            }

            if (waveText != null) waveText.text = "Wave " + waveNumber + "/" + totalStr + " cleared  +" + amount + "g";
            ShowBanner("Wave Clear Bonus: +" + amount + " gold");
        }

        private void OnStartNextWaveClicked()
        {
            if (waves != null)
            {
                waves.StartNextWaveNow();
            }
        }

        private void OnTowerUnlocked(string message)
        {
            RefreshBuildMenu();
            ShowBanner(message);

            if (message.ToLowerInvariant().Contains("cannon"))
            {
                ShowHint("Cannon unlocked! Use splash damage against groups and Armored enemies.");
            }
            else if (message.ToLowerInvariant().Contains("frost"))
            {
                ShowHint("Frost unlocked! Slow fast Runners and tough Brutes.");
            }
        }

        private void ShowBanner(string message)
        {
            if (bannerRoutine != null)
            {
                StopCoroutine(bannerRoutine);
            }

            bannerRoutine = StartCoroutine(PlayBanner(message));
        }

        private IEnumerator PlayBanner(string message)
        {
            bannerText.text = message;
            RectTransform rect = bannerText.rectTransform;

            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                float k = t / 0.25f;
                bannerGroup.alpha = k;
                rect.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, k);
                yield return null;
            }

            bannerGroup.alpha = 1f;
            rect.localScale = Vector3.one;
            yield return new WaitForSeconds(1.2f);

            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                bannerGroup.alpha = 1f - t / 0.5f;
                yield return null;
            }

            bannerGroup.alpha = 0f;
        }

        // ------------------------------------------------- Damage numbers / gold

        private void OnEnemyDamagedDetailed(Enemy enemy, float amount, bool isCrit)
        {
            if (isCrit)
            {
                SpawnFloatingText("CRIT\n-" + Mathf.RoundToInt(amount), enemy.transform.position + Vector3.up * 1.0f,
                    new Color(1f, 0.15f, 0.15f), 38);
            }
            else
            {
                SpawnFloatingText("-" + Mathf.RoundToInt(amount), enemy.transform.position + Vector3.up * 0.8f,
                    new Color(1f, 0.55f, 0.2f), 30);
            }
        }

        private void OnEnemyKilled(Enemy enemy, int gold)
        {
            if (towers != null && towers.Config != null && towers.Config.draftRunMode)
            {
                int xpAmount = enemy.Data.xpValue > 0 ? enemy.Data.xpValue : enemy.Data.goldReward;
                SpawnFloatingText("+" + xpAmount + " XP", enemy.transform.position + Vector3.up * 1.2f,
                    new Color(0.7f, 0.3f, 0.9f), 34);
                return;
            }

            SpawnFloatingText("+" + gold, enemy.transform.position + Vector3.up * 1.2f,
                new Color(1f, 0.9f, 0.2f), 34);
        }

        private void SpawnFloatingText(string message, Vector3 worldPos, Color color, int size)
        {
            if (!TryWorldToCanvas(worldPos, out Vector2 local))
            {
                return;
            }

            Text text;
            if (floatingPool.Count > 0)
            {
                text = floatingPool.Dequeue();
                text.gameObject.SetActive(true);
            }
            else
            {
                text = CreateText(floatingRoot, "Floating", "", 30, color, TextAnchor.MiddleCenter);
                text.fontStyle = FontStyle.Bold;
                text.raycastTarget = false;
            }

            text.text = message;
            text.fontSize = size;
            text.color = color;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(200f, 50f);
            rect.anchoredPosition = local;
            rect.localScale = Vector3.one;
            StartCoroutine(AnimateFloatingText(text));
        }

        private IEnumerator AnimateFloatingText(Text text)
        {
            RectTransform rect = text.rectTransform;
            Vector2 start = rect.anchoredPosition;
            float driftX = Random.Range(-28f, 28f);
            Color color = text.color;
            const float duration = 0.85f;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (text == null)
                {
                    yield break;
                }

                float k = t / duration;
                float pop = k < 0.15f ? Mathf.Lerp(0.3f, 1.25f, k / 0.15f) : Mathf.Lerp(1.25f, 1f, (k - 0.15f) / 0.85f);
                rect.localScale = Vector3.one * pop;
                float ease = 1f - (1f - k) * (1f - k); // ease-out
                rect.anchoredPosition = start + new Vector2(driftX * ease, 85f * ease);
                color.a = 1f - k * k;
                text.color = color;
                yield return null;
            }

            text.gameObject.SetActive(false);
            floatingPool.Enqueue(text);
        }

        private bool TryWorldToCanvas(Vector3 worldPos, out Vector2 local)
        {
            local = Vector2.zero;
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null)
                {
                    return false;
                }
            }

            Vector3 screen = cam.WorldToScreenPoint(worldPos);
            if (screen.z < 0f)
            {
                return false;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local);
            return true;
        }

        // ------------------------------------------------------ Selection panels

        public void ShowBuildMenu(TowerSlot slot)
        {
            if (towers != null && towers.Config != null && towers.Config.draftRunMode)
            {
                return;
            }
            selectedSlot = slot;
            selectedTower = null;
            RefreshBuildMenu();
            ShowPanel(buildMenuGroup, true);
            ShowPanel(towerPanelGroup, false);
            ShowBuildRangeForFirstAvailableTower();
        }

        public void ShowTowerPanel(Tower tower)
        {
            selectedTower = tower;
            selectedHero = null;
            selectedSlot = null;
            RefreshTowerPanel();
            ShowPanel(towerPanelGroup, true);

            if (towers != null && towers.Config != null && towers.Config.draftRunMode)
            {
                if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
                if (sellButton != null) sellButton.gameObject.SetActive(false);
                if (targetButton != null) targetButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 55f);
            }
            else
            {
                if (upgradeButton != null) upgradeButton.gameObject.SetActive(true);
                if (sellButton != null) sellButton.gameObject.SetActive(true);
                if (targetButton != null) targetButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 55f);
            }

            ShowPanel(buildMenuGroup, false);
            ShowTowerRange(tower);

            if (!hasShownTargetingHint)
            {
                hasShownTargetingHint = true;
                ShowHint("Use Target mode to choose how this defender picks enemies.");
            }
        }

        public void ShowHeroPanel(HeroAttack hero)
        {
            if (hero == null)
            {
                return;
            }

            selectedHero = hero;
            selectedTower = null;
            selectedSlot = null;
            RefreshTowerPanel();
            ShowPanel(buildMenuGroup, false);
            ShowPanel(towerPanelGroup, true);

            if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
            if (sellButton != null) sellButton.gameObject.SetActive(false);
            if (targetButton != null) targetButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 55f);

            Color color = hero.Definition != null && hero.Definition.weapon != null
                ? GetHeroRangeColor(hero.Definition.weapon)
                : Color.white;
            rangeIndicator?.Show(hero.transform.position, hero.GetModifiedRange(), color);

            if (!hasShownTargetingHint)
            {
                hasShownTargetingHint = true;
                ShowHint("Tap a hero, then use Target to change enemy priority.");
            }
        }

        public void HideSelectionPanels()
        {
            selectedSlot = null;
            selectedTower = null;
            selectedHero = null;
            ShowPanel(buildMenuGroup, false);
            ShowPanel(towerPanelGroup, false);
            HideRangeIndicator();
        }

        private void RefreshBuildMenu()
        {
            if (towers == null || buildButtonLabels.Count == 0)
            {
                return;
            }

            for (int i = 0; i < towers.AvailableTowers.Length && i < buildButtonLabels.Count; i++)
            {
                TowerData data = towers.AvailableTowers[i];
                string displayName = GetTowerDisplayName(data);
                bool locked = unlocks != null && !unlocks.IsTowerUnlocked(data);
                bool affordable = economy != null && economy.Gold >= data.cost;
                string lockMessage = locked ? unlocks.GetLockMessage(data).Replace("Unlocks after ", "Locked: ") : string.Empty;
                buildButtonLabels[i].text = locked
                    ? displayName + "\n" + lockMessage
                    : affordable
                        ? displayName + "\n" + data.cost + " g"
                        : displayName + "\nNeed " + GetGoldShortfall(data.cost) + " g";
                buildButtonLabels[i].fontSize = locked || !affordable || displayName.Length > 14 ? 18 : 20;
                buildButtonLabels[i].color = locked
                    ? new Color(0.65f, 0.65f, 0.7f)
                    : affordable ? Color.white : new Color(1f, 0.45f, 0.45f);

                if (i < buildButtons.Count)
                {
                    var btn = buildButtons[i];
                    ColorBlock cb = btn.colors;
                    if (locked)
                    {
                        cb.normalColor = new Color(0.10f, 0.11f, 0.14f, 0.95f);
                        cb.highlightedColor = new Color(0.14f, 0.15f, 0.19f, 1f);
                        cb.pressedColor = new Color(0.08f, 0.09f, 0.11f, 1f);
                    }
                    else if (!affordable)
                    {
                        cb.normalColor = new Color(0.22f, 0.14f, 0.16f, 0.95f);
                        cb.highlightedColor = new Color(0.28f, 0.18f, 0.20f, 1f);
                        cb.pressedColor = new Color(0.16f, 0.10f, 0.11f, 1f);
                    }
                    else
                    {
                        cb.normalColor = new Color(0.20f, 0.24f, 0.32f, 0.95f);
                        cb.highlightedColor = new Color(0.28f, 0.34f, 0.45f, 1f);
                        cb.pressedColor = new Color(0.14f, 0.18f, 0.24f, 1f);
                    }
                    cb.selectedColor = cb.normalColor;
                    btn.colors = cb;

                    if (btn.targetGraphic != null)
                    {
                        btn.targetGraphic.color = Color.white;
                    }
                }
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

        private void RefreshTowerPanel()
        {
            if (selectedHero != null)
            {
                HeroDefinition hero = selectedHero.Definition;
                towerPanelTitle.text = (hero != null ? hero.displayName : "Hero")
                    + "  -  Range " + selectedHero.GetModifiedRange().ToString("0.#");
                targetButtonLabel.text = "Target:\n" + GetTargetingModeDisplayName(selectedHero.CurrentTargetingMode) + " >";
                targetButtonLabel.fontSize = 20;
                return;
            }

            if (selectedTower == null)
            {
                return;
            }

            towerPanelTitle.text = GetTowerDisplayName(selectedTower.Data) + "  -  Level " + selectedTower.Level
                + "  (damage " + selectedTower.Damage.ToString("0.#") + ")";

            if (selectedTower.IsMaxLevel)
            {
                upgradeButtonLabel.text = "MAX LEVEL";
                upgradeButtonLabel.fontSize = 24;
                upgradeButtonLabel.color = new Color(0.7f, 0.7f, 0.7f);
                SetButtonVisual(upgradeButton, new Color(0.12f, 0.12f, 0.16f, 0.95f));
            }
            else
            {
                bool affordable = economy != null && economy.Gold >= selectedTower.UpgradeCost;
                upgradeButtonLabel.text = affordable
                    ? "Upgrade\n" + selectedTower.UpgradeCost + " g"
                    : "Upgrade\nNeed " + GetGoldShortfall(selectedTower.UpgradeCost) + " g";
                upgradeButtonLabel.fontSize = affordable ? 24 : 20;
                upgradeButtonLabel.color = affordable ? Color.white : new Color(1f, 0.45f, 0.45f);
                SetButtonVisual(upgradeButton, affordable
                    ? new Color(0.22f, 0.25f, 0.34f, 0.95f)
                    : new Color(0.17f, 0.11f, 0.12f, 0.95f));
            }

            sellButtonLabel.text = "Sell\n+" + towers.GetSellValue(selectedTower) + " g";

            if (targetButtonLabel != null)
            {
                targetButtonLabel.text = "Target:\n" + GetTargetingModeDisplayName(selectedTower.CurrentTargetingMode) + " >";
                targetButtonLabel.fontSize = 20;
            }
        }

        private void OnUpgradeClicked()
        {
            if (towers != null && towers.Config != null && towers.Config.draftRunMode)
            {
                return;
            }

            if (selectedTower == null)
            {
                return;
            }

            if (selectedTower.IsMaxLevel)
            {
                ShowBanner(GetTowerDisplayName(selectedTower.Data) + " is already max level");
                return;
            }

            if (economy == null || economy.Gold < selectedTower.UpgradeCost)
            {
                ShowBanner("Need " + GetGoldShortfall(selectedTower.UpgradeCost) + " more gold to upgrade");
                RefreshTowerPanel();
                return;
            }

            if (selectedTower.TryUpgrade())
            {
                if (VfxManager.Instance != null)
                {
                    VfxManager.Instance.PlayUpgrade(selectedTower.transform.position + Vector3.up * 0.5f);
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayUpgrade();
                }

                RefreshTowerPanel();
            }
        }

        private void OnTargetClicked()
        {
            if (selectedHero != null)
            {
                TargetingMode heroCurrent = selectedHero.CurrentTargetingMode;
                int heroNext = ((int)heroCurrent + 1) % System.Enum.GetValues(typeof(TargetingMode)).Length;
                selectedHero.CurrentTargetingMode = (TargetingMode)heroNext;
                AudioManager.Instance?.PlayButton();
                RefreshTowerPanel();
                return;
            }

            if (selectedTower == null)
            {
                return;
            }

            TargetingMode current = selectedTower.CurrentTargetingMode;
            int nextIndex = ((int)current + 1) % System.Enum.GetValues(typeof(TargetingMode)).Length;
            selectedTower.CurrentTargetingMode = (TargetingMode)nextIndex;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButton();
            }

            RefreshTowerPanel();
        }

        private void OnSellClicked()
        {
            if (towers != null && towers.Config != null && towers.Config.draftRunMode)
            {
                return;
            }

            if (selectedTower != null && towers != null)
            {
                towers.SellTower(selectedTower);
                HideSelectionPanels();
            }
        }

        private void OnBuildClicked(int index)
        {
            if (selectedSlot == null || towers == null || index >= towers.AvailableTowers.Length)
            {
                return;
            }

            TowerData tower = towers.AvailableTowers[index];
            string displayName = GetTowerDisplayName(tower);
            if (unlocks != null && !unlocks.IsTowerUnlocked(tower))
            {
                ShowBanner(displayName + " locked - " + unlocks.GetLockMessage(tower));
                return;
            }

            if (economy == null || economy.Gold < tower.cost)
            {
                PreviewBuildRange(index);
                ShowBanner("Need " + GetGoldShortfall(tower.cost) + " more gold for " + displayName);
                RefreshBuildMenu();
                return;
            }

            if (towers.PlaceTower(selectedSlot, tower))
            {
                HideSelectionPanels();
            }
        }

        // -------------------------------------------------------- State screens

        private void OnGameSpeedChanged(float speed)
        {
            if (speedButtonLabel != null)
            {
                speedButtonLabel.text = FormatSpeed(speed);
            }
        }

        private static string FormatSpeed(float speed)
        {
            return speed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "x";
        }

        private void OnStateChanged(GameState state)
        {
            HideSelectionPanels();
            ShowPanel(pauseGroup, state == GameState.Paused);
            if (abilityHudContainer != null)
            {
                abilityHudContainer.gameObject.SetActive(state == GameState.Playing);
            }

            if (state == GameState.Victory || state == GameState.Defeat)
            {
                ShowPanel(victoryGroup, false);
                ShowPanel(defeatGroup, false);
                ShowBattleResult(state == GameState.Victory);
            }
            else
            {
                ShowPanel(resultPanelGroup, false);
                ShowPanel(victoryGroup, false);
                ShowPanel(defeatGroup, false);
            }

            if (state != GameState.LevelUp)
            {
                ShowPanel(levelUpPanelGroup, false);
            }
        }

        private void OnXpChanged(int currentXp, int xpNeeded, int level)
        {
            if (xpText != null)
            {
                xpText.text = "Lv." + level + "  XP: " + currentXp + " / " + xpNeeded;
            }

            if (xpFill != null)
            {
                float pct = xpNeeded > 0 ? (float)currentXp / xpNeeded : 0f;
                xpFill.localScale = new Vector3(pct, 1f, 1f);
            }
        }

        private void OnShowLevelUpDraftFromProgression(RunProgressionManager.CardChoice[] choices)
        {
            SetLevelUpPanelTitle("LEVEL UP!", "Choose a blessing for your defenders");
            OnShowLevelUpDraft(choices);
        }

        public void SetLevelUpPanelTitle(string title, string subtitle)
        {
            if (levelUpPanelGroup != null)
            {
                Text[] texts = levelUpPanelGroup.GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                {
                    if (t.name == "Title")
                    {
                        t.text = title;
                    }
                    else if (t.name == "Subtitle")
                    {
                        t.text = subtitle;
                    }
                }
            }
        }

        public void OnShowLevelUpDraft(RunProgressionManager.CardChoice[] choices)
        {
            if (choices == null || choices.Length < 3)
            {
                return;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLevelUp();
            }

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                if (cardTitleTexts[i] != null)
                {
                    cardTitleTexts[i].text = choices[i].title;
                }
                if (cardDescriptionTexts[i] != null)
                {
                    cardDescriptionTexts[i].text = choices[i].description;
                }

                string cType = choices[i].cardType ?? "Boost";
                string cRarity = choices[i].rarity ?? "Common";

                // Setup Type Badge colors & texts
                Color badgeColor = new Color(0.45f, 0.20f, 0.75f, 0.9f); // Default Boost Purple
                string badgeLabel = "BOOST";

                if (cType == "Add")
                {
                    badgeColor = new Color(0.08f, 0.50f, 0.24f, 0.9f); // Emerald Green
                    badgeLabel = "ADD DEFENDER";
                }
                else if (cType == "Upgrade")
                {
                    badgeColor = new Color(0.80f, 0.45f, 0.00f, 0.9f); // Fiery Orange
                    badgeLabel = "UPGRADE";
                }

                if (cardTypeBadges[i] != null)
                {
                    cardTypeBadges[i].color = badgeColor;
                }
                if (cardTypeLabels[i] != null)
                {
                    cardTypeLabels[i].text = $"{badgeLabel} ({cRarity.ToUpper()})";
                }

                // Setup Rarity colors
                Color borderColor = new Color(0.35f, 0.40f, 0.50f, 0.8f); // Common Slate
                Color innerBgColor = new Color(0.08f, 0.10f, 0.15f, 0.95f);
                Color btnColor = new Color(0.20f, 0.25f, 0.35f, 1.0f);
                Color titleColor = new Color(0.90f, 0.90f, 0.95f, 1.0f);

                if (cRarity == "Rare")
                {
                    borderColor = new Color(0.12f, 0.55f, 0.95f, 1.0f); // Rare Blue
                    innerBgColor = new Color(0.05f, 0.08f, 0.18f, 0.95f);
                    btnColor = new Color(0.10f, 0.40f, 0.80f, 1.0f);
                    titleColor = new Color(0.40f, 0.80f, 1.0f, 1.0f);
                }
                else if (cRarity == "Epic")
                {
                    borderColor = new Color(0.65f, 0.15f, 0.85f, 1.0f); // Epic Purple
                    innerBgColor = new Color(0.08f, 0.05f, 0.18f, 0.95f);
                    btnColor = new Color(0.50f, 0.10f, 0.70f, 1.0f);
                    titleColor = new Color(0.85f, 0.50f, 1.0f, 1.0f);
                }
                else if (cRarity == "Legendary")
                {
                    borderColor = new Color(1.00f, 0.70f, 0.00f, 1.0f); // Legendary Gold
                    innerBgColor = new Color(0.12f, 0.08f, 0.02f, 0.95f);
                    btnColor = new Color(0.80f, 0.50f, 0.00f, 1.0f);
                    titleColor = new Color(1.00f, 0.85f, 0.20f, 1.0f);
                }

                if (cardBorders[i] != null)
                {
                    cardBorders[i].color = borderColor;
                }
                if (cardInnerBgs[i] != null)
                {
                    cardInnerBgs[i].color = innerBgColor;
                }
                if (cardTitleTexts[i] != null)
                {
                    cardTitleTexts[i].color = titleColor;
                }

                if (cardButtons[i] != null)
                {
                    cardButtons[i].onClick.RemoveAllListeners();
                    cardButtons[i].onClick.AddListener(() =>
                    {
                        if (AudioManager.Instance != null)
                        {
                            AudioManager.Instance.PlayUpgrade();
                        }
                        if (progression != null)
                        {
                            progression.ApplyChoice(choices[index]);
                        }
                    });

                    ColorBlock cb = cardButtons[i].colors;
                    cb.normalColor = btnColor;
                    cb.highlightedColor = btnColor * 1.2f;
                    cb.pressedColor = btnColor * 0.7f;
                    cb.selectedColor = btnColor;
                    cardButtons[i].colors = cb;
                }
            }

            if (rerollButton != null)
            {
                bool canAfford = economy != null ? economy.Gold >= CardDraftManager.RerollCost : true;
                rerollButton.interactable = canAfford;
                if (rerollButtonText != null)
                {
                    rerollButtonText.text = $"Reroll ({CardDraftManager.RerollCost} Gold)";
                }
            }

            ShowPanel(levelUpPanelGroup, true);
        }

        public void ShowHint(string message)
        {
            if (hintBg != null && hintText != null)
            {
                hintText.text = message;
                hintBg.gameObject.SetActive(true);
                hintTimer = 7.5f;
            }
        }

        private void Update()
        {
            if (hintBg != null && hintBg.gameObject.activeSelf)
            {
                hintTimer -= Time.deltaTime;
                if (hintTimer <= 0f)
                {
                    hintBg.gameObject.SetActive(false);
                }
            }

            bool showAbilityHud = GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;
            if (abilityHudContainer != null && abilityHudContainer.gameObject.activeSelf != showAbilityHud)
            {
                abilityHudContainer.gameObject.SetActive(showAbilityHud);
            }

            if (showAbilityHud)
            {
                UpdateAbilityCooldownHUD();
            }

            if (timerText != null)
            {
                int totalSec = Mathf.FloorToInt(Time.timeSinceLevelLoad);
                int minutes = totalSec / 60;
                int seconds = totalSec % 60;
                timerText.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);
            }

            if (waveProgressBarFill != null && waves != null && waves.TotalWaves > 0)
            {
                float progress = Mathf.Clamp01((float)waves.CurrentWave / waves.TotalWaves);
                waveProgressBarFill.anchorMax = new Vector2(progress, 1f);
            }
        }

        private void ShowPanel(CanvasGroup group, bool visible)
        {
            if (group == null)
            {
                return;
            }

            group.interactable = visible;
            group.blocksRaycasts = visible;
            StartCoroutine(FadePanel(group, visible ? 1f : 0f));
        }


        private IEnumerator FadePanel(CanvasGroup group, float targetAlpha)
        {
            if (group == null)
            {
                yield break;
            }

            RectTransform rt = group.transform as RectTransform;
            bool overlay = rt != null && rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one;
            if (overlay)
            {
                group.alpha = targetAlpha;
                group.transform.localScale = Vector3.one;
                yield break;
            }

            float startAlpha = group.alpha;
            Vector3 startScale = group.transform.localScale;
            if (targetAlpha > 0.5f && startAlpha < 0.5f)
            {
                startScale = Vector3.one * 0.9f;
            }

            for (float t = 0f; t < PanelFadeSeconds; t += Time.unscaledDeltaTime)
            {
                if (group == null)
                {
                    yield break;
                }

                float k = t / PanelFadeSeconds;
                group.alpha = Mathf.Lerp(startAlpha, targetAlpha, k);
                group.transform.localScale = Vector3.Lerp(startScale, Vector3.one, k);
                yield return null;
            }

            if (group == null)
            {
                yield break;
            }

            group.alpha = targetAlpha;
            group.transform.localScale = Vector3.one;
        }

        // ------------------------------------------------------------ UI build

        private void BuildUI()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            GameObject canvasObject = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

            // World-space enemy bars are owned by EnemyHealthBar. Keep only damage
            // numbers on this canvas so each enemy has exactly one health bar.
            floatingRoot = CreateRoot(canvasRect, "FloatingText");

            // Safe Area Container
            safeAreaRect = CreateSafeArea(canvasRect);

            // Gold (top-left)
            goldText = CreateText(safeAreaRect, "GoldText", "Gold: 0", 40, new Color(1f, 0.85f, 0.2f), TextAnchor.UpperLeft);
            SetAnchored(goldText.rectTransform, new Vector2(0f, 1f), new Vector2(25f, -20f), new Vector2(400f, 60f));

            // XP and combat controls share one clean row below the primary counters.
            xpBgImage = CreateImage(safeAreaRect, "XpBarBg", new Color(0f, 0f, 0f, 0.6f));
            SetAnchored(xpBgImage.rectTransform, new Vector2(0f, 1f), new Vector2(175f, -72f), new Vector2(300f, 28f));
            xpFillImage = CreateImage(xpBgImage.rectTransform, "XpFill", new Color(0.6f, 0.25f, 0.85f));
            xpFill = xpFillImage.rectTransform;
            xpFill.anchorMin = Vector2.zero;
            xpFill.anchorMax = Vector2.one;
            xpFill.pivot = new Vector2(0f, 0.5f);
            xpFill.offsetMin = new Vector2(2f, 2f);
            xpFill.offsetMax = new Vector2(-2f, -2f);
            xpText = CreateText(xpBgImage.rectTransform, "XpText", "Lv.1  XP: 0 / 100", 18, Color.white, TextAnchor.MiddleCenter);
            xpText.rectTransform.anchorMin = Vector2.zero;
            xpText.rectTransform.anchorMax = Vector2.one;
            xpText.rectTransform.offsetMin = Vector2.zero;
            xpText.rectTransform.offsetMax = Vector2.zero;

            // Wave counter & Timer (top-center)
            waveText = CreateText(safeAreaRect, "WaveText", "Wave -", 36, Color.white, TextAnchor.UpperCenter);
            SetAnchored(waveText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(480f, 40f));

            // Wave progress bar (top-center, beneath wave details)
            Image progressBg = CreateImage(safeAreaRect, "WaveProgressBarBg", new Color(0f, 0f, 0f, 0.6f));
            SetAnchored(progressBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(340f, 12f));
            waveProgressBarFillImage = CreateImage(progressBg.rectTransform, "Fill", new Color(0.2f, 0.75f, 1f));
            waveProgressBarFill = waveProgressBarFillImage.rectTransform;
            waveProgressBarFill.anchorMin = Vector2.zero;
            waveProgressBarFill.anchorMax = Vector2.one;
            waveProgressBarFill.pivot = new Vector2(0f, 0.5f);
            waveProgressBarFill.offsetMin = new Vector2(1f, 1f);
            waveProgressBarFill.offsetMax = new Vector2(-1f, -1f);

            // Elapsed Timer text (top-center below progress bar)
            timerText = CreateText(safeAreaRect, "TimerText", "00:00", 20, new Color(0.85f, 0.85f, 0.9f), TextAnchor.UpperCenter);
            SetAnchored(timerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(200f, 26f));

            // Hint panel background (top-center, below timer)
            hintBg = CreateImage(safeAreaRect, "HintPanel", new Color(0.08f, 0.1f, 0.15f, 0.85f));
            SetAnchored(hintBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -155f), new Vector2(760f, 48f));
            hintText = CreateText(hintBg.rectTransform, "Label", "", 22, new Color(0.95f, 0.95f, 1f), TextAnchor.MiddleCenter);
            hintText.rectTransform.anchorMin = Vector2.zero;
            hintText.rectTransform.anchorMax = Vector2.one;
            hintText.rectTransform.offsetMin = Vector2.zero;
            hintText.rectTransform.offsetMax = Vector2.zero;
            hintBg.gameObject.SetActive(false);

            BuildWaveControl();

            // Castle HP sits with the castle at the bottom of the battlefield.
            Image hpBg = CreateImage(safeAreaRect, "CastleHpBar", new Color(0f, 0f, 0f, 0.6f));
            SetAnchored(hpBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(680f, 46f));
            castleHpFillImage = CreateImage(hpBg.rectTransform, "Fill", new Color(0.25f, 0.8f, 0.3f));
            castleHpFill = castleHpFillImage.rectTransform;
            castleHpFill.anchorMin = Vector2.zero;
            castleHpFill.anchorMax = Vector2.one;
            castleHpFill.pivot = new Vector2(0f, 0.5f);
            castleHpFill.offsetMin = new Vector2(2f, 2f);
            castleHpFill.offsetMax = new Vector2(-2f, -2f);
            castleHpText = CreateText(hpBg.rectTransform, "Label", "CASTLE  10 / 10", 24, Color.white, TextAnchor.MiddleCenter);
            castleHpText.rectTransform.anchorMin = Vector2.zero;
            castleHpText.rectTransform.anchorMax = Vector2.one;
            castleHpText.rectTransform.offsetMin = Vector2.zero;
            castleHpText.rectTransform.offsetMax = Vector2.zero;

            // Speed button: top-left (1x -> 1.5x -> 2x)
            Button speedButton = CreateButton(safeAreaRect, "SpeedButton",
                game != null ? FormatSpeed(game.GameSpeed) : "1x",
                new Vector2(95f, 42f), new Vector2(0f, 1f), new Vector2(65f, -38f),
                () =>
                {
                    if (game != null)
                    {
                        game.CycleGameSpeed();
                    }
                });
            speedButtonLabel = speedButton.GetComponentInChildren<Text>();

            // Pause button: top-right
            CreateButton(safeAreaRect, "PauseButton", "Pause", new Vector2(95f, 42f), new Vector2(1f, 1f),
                new Vector2(-65f, -38f), () => { if (game != null) game.TogglePause(); });

            // Wave banner (center)
            bannerText = CreateText(safeAreaRect, "WaveBanner", "", 72, Color.white, TextAnchor.MiddleCenter);
            bannerText.fontStyle = FontStyle.Bold;
            SetAnchored(bannerText.rectTransform, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(1200f, 110f));
            bannerGroup = bannerText.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.alpha = 0f;
            bannerGroup.blocksRaycasts = false;

            BuildBuildMenu();
            BuildTowerPanel();
            pauseGroup = BuildOverlay("PauseMenu", "PAUSED", "", Color.white,
                new (string, UnityEngine.Events.UnityAction)[] {
                    ("Resume", () => { if (game != null) game.TogglePause(); }),
                    ("Restart", () => { if (game != null) game.Restart(); }),
                    ("Main Menu", () => { if (game != null) game.LoadMainMenu(); }) });
            victoryGroup = BuildOverlay("VictoryScreen", "STAGE COMPLETE!", "You defeated the Warlord Boss and cleared Stage 1: Castle Road!", new Color(1f, 0.85f, 0.2f),
                new (string, UnityEngine.Events.UnityAction)[] {
                    ("Play Again", () => { if (game != null) game.Restart(); }),
                    ("Lobby Menu", () => { if (game != null) game.LoadMainMenu(); }) });
            defeatGroup = BuildOverlay("DefeatScreen", "DEFEAT", "The castle has fallen. Hold the line next time!", new Color(0.95f, 0.3f, 0.25f),
                new (string, UnityEngine.Events.UnityAction)[] {
                    ("Retry", () => { if (game != null) game.Restart(); }),
                    ("Main Menu", () => { if (game != null) game.LoadMainMenu(); }) });

            BuildLevelUpPanel();
            BuildAbilityCooldownHUD();
            BuildBattleResultPanel();
        }

        private void BuildLevelUpPanel()
        {
            Image dim = CreateImage(canvasRect, "LevelUpPanel", new Color(0.04f, 0.05f, 0.08f, 0.88f));
            RectTransform rect = dim.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text titleText = CreateText(rect, "Title", "LEVEL UP!", 64, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleCenter);
            titleText.fontStyle = FontStyle.Bold;

            Text subtitleText = CreateText(rect, "Subtitle", "Choose a blessing for your defenders", 26, new Color(0.85f, 0.85f, 0.9f), TextAnchor.MiddleCenter);

            bool isPortrait = Screen.width < Screen.height;
            if (isPortrait)
            {
                SetAnchored(titleText.rectTransform, new Vector2(0.5f, 0.90f), Vector2.zero, new Vector2(1000f, 80f));
                SetAnchored(subtitleText.rectTransform, new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(1000f, 40f));
            }
            else
            {
                SetAnchored(titleText.rectTransform, new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(1200f, 80f));
                SetAnchored(subtitleText.rectTransform, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(1200f, 40f));
            }

            float startX = isPortrait ? 0f : -450f;
            float spacingX = isPortrait ? 0f : 450f;
            float startY = isPortrait ? 260f : -40f;
            float spacingY = isPortrait ? -320f : 0f;
            Vector2 cardSize = isPortrait ? new Vector2(800f, 260f) : new Vector2(380f, 540f);

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                Image cardBg = CreateImage(rect, "Card_" + i, new Color(0.12f, 0.16f, 0.24f, 0.95f));
                RectTransform cardRt = cardBg.rectTransform;
                SetAnchored(cardRt, new Vector2(0.5f, 0.5f), new Vector2(startX + i * spacingX, startY + i * spacingY), cardSize);

                Image border = CreateImage(cardRt, "Border", new Color(0.24f, 0.32f, 0.48f, 0.8f));
                border.rectTransform.anchorMin = Vector2.zero;
                border.rectTransform.anchorMax = Vector2.one;
                border.rectTransform.offsetMin = new Vector2(4f, 4f);
                border.rectTransform.offsetMax = new Vector2(-4f, -4f);
                cardBorders[i] = border;

                Image innerBg = CreateImage(border.rectTransform, "InnerBg", new Color(0.08f, 0.1f, 0.15f, 0.95f));
                innerBg.rectTransform.anchorMin = Vector2.zero;
                innerBg.rectTransform.anchorMax = Vector2.one;
                innerBg.rectTransform.offsetMin = new Vector2(4f, 4f);
                innerBg.rectTransform.offsetMax = new Vector2(-4f, -4f);
                cardInnerBgs[i] = innerBg;

                RectTransform contentRoot = innerBg.rectTransform;

                // Card Type Badge
                cardTypeBadges[i] = CreateImage(contentRoot, "TypeBadge", new Color(0.1f, 0.1f, 0.1f, 0.9f));

                // Title
                cardTitleTexts[i] = CreateText(contentRoot, "Title", "Card Title", 26, new Color(1f, 0.85f, 0.2f), isPortrait ? TextAnchor.MiddleLeft : TextAnchor.UpperCenter);
                cardTitleTexts[i].fontStyle = FontStyle.Bold;
                cardTitleTexts[i].horizontalOverflow = HorizontalWrapMode.Wrap;

                // Description
                cardDescriptionTexts[i] = CreateText(contentRoot, "Description", "Card description...", 19, new Color(0.85f, 0.85f, 0.9f), isPortrait ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
                cardDescriptionTexts[i].horizontalOverflow = HorizontalWrapMode.Wrap;

                // Select Button
                if (isPortrait)
                {
                    SetAnchored(cardTypeBadges[i].rectTransform, new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(140f, 32f));
                    SetAnchored(cardTitleTexts[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(40f, -20f), new Vector2(240f, 100f));
                    SetAnchored(cardDescriptionTexts[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(300f, 0f), new Vector2(280f, 200f));
                    cardButtons[i] = CreateButton(contentRoot, "SelectButton", "SELECT", new Vector2(160f, 64f), new Vector2(1f, 0.5f),
                        new Vector2(-40f, 0f), () => { });
                }
                else
                {
                    SetAnchored(cardTypeBadges[i].rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(180f, 32f));
                    SetAnchored(cardTitleTexts[i].rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(320f, 60f));
                    SetAnchored(cardDescriptionTexts[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(300f, 180f));
                    cardButtons[i] = CreateButton(contentRoot, "SelectButton", "SELECT", new Vector2(240f, 60f), new Vector2(0.5f, 0f),
                        new Vector2(0f, 40f), () => { });
                }

                cardTypeLabels[i] = CreateText(cardTypeBadges[i].rectTransform, "Label", "TYPE", 14, Color.white, TextAnchor.MiddleCenter);
                cardTypeLabels[i].fontStyle = FontStyle.Bold;
                cardTypeLabels[i].rectTransform.anchorMin = Vector2.zero;
                cardTypeLabels[i].rectTransform.anchorMax = Vector2.one;
                cardTypeLabels[i].rectTransform.offsetMin = Vector2.zero;
                cardTypeLabels[i].rectTransform.offsetMax = Vector2.zero;
            }

            rerollButton = CreateButton(rect, "RerollButton", "Reroll (20 Gold)", new Vector2(240f, 56f), new Vector2(0.5f, 0.08f),
                Vector2.zero, OnRerollClicked);
            rerollButtonText = rerollButton.GetComponentInChildren<Text>();

            levelUpPanelGroup = dim.gameObject.AddComponent<CanvasGroup>();
            levelUpPanelGroup.alpha = 0f;
            levelUpPanelGroup.interactable = false;
            levelUpPanelGroup.blocksRaycasts = false;
        }

        private void OnRerollClicked()
        {
            if (CardDraftManager.Instance != null && CardDraftManager.Instance.TryReroll())
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayButton();
                }
            }
        }

        private void BuildBuildMenu()
        {
            buildMenuGroup = CreateBottomPanel("BuildMenu", out RectTransform panel);
            int count = towers != null ? towers.AvailableTowers.Length : 0;
            bool twoRows = count > 3;
            if (twoRows)
            {
                SetAnchored(panel, new Vector2(0.5f, 0f), new Vector2(0f, 125f), new Vector2(760f, 230f));
            }

            CreateText(panel, "Title", "Build Defender", 26, Color.white, TextAnchor.UpperCenter)
                .rectTransform.SetAnchored(new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(400f, 34f));

            int topRowCount = twoRows ? Mathf.CeilToInt(count * 0.5f) : count;
            int bottomRowCount = count - topRowCount;
            float spacing = twoRows ? 230f : 190f;

            for (int i = 0; i < count; i++)
            {
                int index = i;
                int row = twoRows && i >= topRowCount ? 1 : 0;
                int rowIndex = row == 0 ? i : i - topRowCount;
                int rowCount = row == 0 ? topRowCount : bottomRowCount;
                float startX = -(rowCount - 1) * spacing / 2f;
                float y = twoRows ? (row == 0 ? 118f : 48f) : 55f;
                Vector2 buttonSize = twoRows ? new Vector2(210f, 58f) : new Vector2(170f, 74f);
                Button button = CreateButton(panel, "Build_" + i, "", buttonSize, new Vector2(0.5f, 0f),
                    new Vector2(startX + rowIndex * spacing, y), () => OnBuildClicked(index));
                AddBuildButtonPreview(button, index);
                buildButtons.Add(button);
                buildButtonLabels.Add(button.GetComponentInChildren<Text>());
            }

            CreateButton(panel, "Cancel", "X", new Vector2(44f, 44f), new Vector2(1f, 1f),
                new Vector2(-30f, -28f), HideSelectionPanels);
        }

        private static string GetTowerDisplayName(TowerData data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            return string.IsNullOrEmpty(data.displayNameOverride) ? data.towerName : data.displayNameOverride;
        }

        private void BuildTowerPanel()
        {
            towerPanelGroup = CreateBottomPanel("TowerPanel", out RectTransform panel);
            towerPanelTitle = CreateText(panel, "Title", "", 26, Color.white, TextAnchor.UpperCenter);
            towerPanelTitle.rectTransform.SetAnchored(new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(680f, 34f));

            Button upgrade = CreateButton(panel, "Upgrade", "Upgrade", new Vector2(180f, 70f), new Vector2(0.5f, 0f),
                new Vector2(-210f, 55f), OnUpgradeClicked);
            upgradeButton = upgrade;
            upgradeButtonLabel = upgrade.GetComponentInChildren<Text>();

            Button targetBtn = CreateButton(panel, "Target", "Target: ClosestToGoal >", new Vector2(200f, 70f), new Vector2(0.5f, 0f),
                new Vector2(0f, 55f), OnTargetClicked);
            targetButton = targetBtn;
            targetButtonLabel = targetBtn.GetComponentInChildren<Text>();

            sellButton = CreateButton(panel, "Sell", "Sell", new Vector2(180f, 70f), new Vector2(0.5f, 0f),
                new Vector2(210f, 55f), OnSellClicked);
            sellButtonLabel = sellButton.GetComponentInChildren<Text>();

            CreateButton(panel, "Close", "X", new Vector2(44f, 44f), new Vector2(1f, 1f),
                new Vector2(-30f, -28f), HideSelectionPanels);
        }

        private void BuildWaveControl()
        {
            Image bg = CreateImage(safeAreaRect != null ? safeAreaRect : canvasRect, "WaveControl", new Color(0.08f, 0.08f, 0.12f, 0.9f));
            RectTransform panel = bg.rectTransform;
            SetAnchored(panel, new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(560f, 96f));

            waveStatusText = CreateText(panel, "Status", "Next Wave", 24, Color.white, TextAnchor.UpperLeft);
            waveStatusText.fontStyle = FontStyle.Bold;
            SetAnchored(waveStatusText.rectTransform, new Vector2(0f, 1f), new Vector2(22f, -14f), new Vector2(350f, 34f));

            waveCountdownText = CreateText(panel, "Countdown", "Auto starts in 5s", 22, new Color(1f, 0.85f, 0.2f), TextAnchor.UpperLeft);
            SetAnchored(waveCountdownText.rectTransform, new Vector2(0f, 1f), new Vector2(22f, -48f), new Vector2(350f, 30f));

            startWaveButton = CreateButton(panel, "StartNextWaveButton", "Start", new Vector2(150f, 48f), new Vector2(1f, 0.5f),
                new Vector2(-94f, 0f), OnStartNextWaveClicked);

            waveControlGroup = bg.gameObject.AddComponent<CanvasGroup>();
            waveControlGroup.alpha = 0f;
            waveControlGroup.interactable = false;
            waveControlGroup.blocksRaycasts = false;
            startWaveButton.interactable = false;
        }

        private CanvasGroup CreateBottomPanel(string name, out RectTransform panel)
        {
            Image bg = CreateImage(safeAreaRect != null ? safeAreaRect : canvasRect, name, new Color(0.08f, 0.08f, 0.12f, 0.92f));
            panel = bg.rectTransform;
            SetAnchored(panel, new Vector2(0.5f, 0f), new Vector2(0f, 105f), new Vector2(720f, 190f));

            CanvasGroup group = bg.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        private CanvasGroup BuildOverlay(string name, string title, string subtitle, Color titleColor,
            (string label, UnityEngine.Events.UnityAction action)[] buttons)
        {
            Image dim = CreateImage(canvasRect, name, new Color(0f, 0f, 0f, 0.72f));
            RectTransform rect = dim.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text titleText = CreateText(rect, "Title", title, 96, titleColor, TextAnchor.MiddleCenter);
            titleText.fontStyle = FontStyle.Bold;
            SetAnchored(titleText.rectTransform, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(1200f, 130f));

            if (!string.IsNullOrEmpty(subtitle))
            {
                Text subtitleText = CreateText(rect, "Subtitle", subtitle, 28, new Color(0.85f, 0.85f, 0.9f), TextAnchor.MiddleCenter);
                SetAnchored(subtitleText.rectTransform, new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(1200f, 60f));
            }

            float buttonStartAnchorY = string.IsNullOrEmpty(subtitle) ? 0.42f : 0.38f;

            for (int i = 0; i < buttons.Length; i++)
            {
                CreateButton(rect, buttons[i].label, buttons[i].label, new Vector2(260f, 70f), new Vector2(0.5f, buttonStartAnchorY),
                    new Vector2(0f, -i * 85f), buttons[i].action);
            }

            CanvasGroup group = dim.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }
        private void BuildBattleResultPanel()
        {
            // Dim background
            Image dim = CreateImage(canvasRect, "BattleResultPanel", new Color(0.04f, 0.05f, 0.08f, 0.96f));
            RectTransform rect = dim.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Title (Victory/Defeat)
            resultTitleText = CreateText(rect, "Title", "BATTLE RESULT", 72, Color.white, TextAnchor.MiddleCenter);
            resultTitleText.fontStyle = FontStyle.Bold;
            SetAnchored(resultTitleText.rectTransform, new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(1000f, 100f));

            // Subtitle (Wave reached, Battle number)
            resultSubtitleText = CreateText(rect, "Subtitle", "Wave Reached: - | Run: #1", 24, new Color(0.8f, 0.8f, 0.85f), TextAnchor.MiddleCenter);
            SetAnchored(resultSubtitleText.rectTransform, new Vector2(0.5f, 0.80f), Vector2.zero, new Vector2(1000f, 40f));

            bool isPortrait = Screen.width < Screen.height;

            // Content Panel (center)
            Image contentBg = CreateImage(rect, "ContentBg", new Color(0.08f, 0.10f, 0.15f, 0.9f));
            if (isPortrait)
            {
                SetAnchored(contentBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(900f, 760f));
            }
            else
            {
                SetAnchored(contentBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(800f, 380f));
            }

            // Rewards Box
            Text rewardsTitle = CreateText(contentBg.rectTransform, "RewardsTitle", "REWARDS EARNED", 22, new Color(1f, 0.85f, 0.2f), isPortrait ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft);
            rewardsTitle.fontStyle = FontStyle.Bold;

            resultRewardsText = CreateText(contentBg.rectTransform, "RewardsText", "• Gold: 0\n• XP: 0", 17, Color.white, TextAnchor.UpperLeft);

            if (isPortrait)
            {
                SetAnchored(rewardsTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(820f, 40f));
                SetAnchored(resultRewardsText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(700f, 180f));
            }
            else
            {
                SetAnchored(rewardsTitle.rectTransform, new Vector2(0f, 1f), new Vector2(50f, -40f), new Vector2(320f, 40f));
                SetAnchored(resultRewardsText.rectTransform, new Vector2(0f, 1f), new Vector2(50f, -90f), new Vector2(320f, 260f));
            }

            // Divider line
            Image divider = CreateImage(contentBg.rectTransform, "Divider", new Color(0.24f, 0.32f, 0.48f, 0.5f));
            if (isPortrait)
            {
                SetAnchored(divider.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(820f, 2f));
            }
            else
            {
                SetAnchored(divider.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 320f));
            }

            // Damage Report Box
            Text damageTitle = CreateText(contentBg.rectTransform, "DamageTitle", "DAMAGE REPORT", 22, new Color(0.4f, 0.8f, 1f), isPortrait ? TextAnchor.MiddleCenter : TextAnchor.UpperLeft);
            damageTitle.fontStyle = FontStyle.Bold;

            resultDamageReportText = CreateText(contentBg.rectTransform, "DamageText", "Archer: 0 dmg (0%)\nBombardier: 0 dmg (0%)", 20, Color.white, TextAnchor.UpperLeft);

            if (isPortrait)
            {
                SetAnchored(damageTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -310f), new Vector2(820f, 40f));
                SetAnchored(resultDamageReportText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(700f, 360f));
            }
            else
            {
                SetAnchored(damageTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(50f, -40f), new Vector2(320f, 40f));
                SetAnchored(resultDamageReportText.rectTransform, new Vector2(0.5f, 1f), new Vector2(50f, -90f), new Vector2(320f, 260f));
            }

            // OK Button
            if (isPortrait)
            {
                resultOkButton = CreateButton(rect, "OkButton", "OK", new Vector2(260f, 70f), new Vector2(0.5f, 0.12f), new Vector2(-150f, 0f), () => { OnOkClicked(); });
            }
            else
            {
                resultOkButton = CreateButton(rect, "OkButton", "OK", new Vector2(240f, 60f), new Vector2(0.5f, 0.18f), new Vector2(-140f, 0f), () => { OnOkClicked(); });
            }

            // Double Rewards Button (disabled/placeholder)
            if (isPortrait)
            {
                resultDoubleButton = CreateButton(rect, "DoubleButton", "2X REWARDS (AD)", new Vector2(260f, 70f), new Vector2(0.5f, 0.12f), new Vector2(150f, 0f), () => { });
            }
            else
            {
                resultDoubleButton = CreateButton(rect, "DoubleButton", "2X REWARDS (AD)", new Vector2(240f, 60f), new Vector2(0.5f, 0.18f), new Vector2(140f, 0f), () => { });
            }
            resultDoubleButton.interactable = false; // Disabled placeholder
            Text doubleLabel = resultDoubleButton.GetComponentInChildren<Text>();
            if (doubleLabel != null)
            {
                doubleLabel.text = "2X REWARDS (AD)";
                doubleLabel.color = new Color(0.6f, 0.6f, 0.6f);
            }

            resultPanelGroup = dim.gameObject.AddComponent<CanvasGroup>();
            resultPanelGroup.alpha = 0f;
            resultPanelGroup.interactable = false;
            resultPanelGroup.blocksRaycasts = false;
        }

        private void OnOkClicked()
        {
            ShowPanel(resultPanelGroup, false);
            if (game != null)
            {
                game.LoadMainMenu();
            }
        }

        public void ShowBattleResult(bool victory)
        {
            int wave = Mathf.Max(1, waves != null ? waves.CurrentWave : 1);
            int runNumber = Mathf.Max(1, SaveManager.TotalRuns);

            var rewards = RewardCalculator.CalculateRewards(wave);
            rewardsClaimed = SaveManager.TryClaimRunRewards(wave, out int gold, out int xp, out int materials);
            if (rewardsClaimed)
            {
                Debug.Log($"[UIManager] Saved run rewards permanently: Gold +{gold}, XP +{xp}, Materials +{materials}.");
            }
            else
            {
                Debug.Log("[UIManager] Run rewards were already claimed; result screen refresh did not grant duplicates.");
            }

            var damageEntries = new List<DamageReportEntry>();
            float totalDamage = DamageTracker.Instance != null ? DamageTracker.Instance.GetTotalDamage() : 0f;

            if (DamageTracker.Instance != null && DamageTracker.Instance.DamageByHeroId != null)
            {
                foreach (var kvp in DamageTracker.Instance.DamageByHeroId)
                {
                    string id = kvp.Key;
                    float dmg = kvp.Value;
                    float pct = totalDamage > 0f ? (dmg / totalDamage) * 100f : 0f;
                    string name = GetHeroDisplayName(id);
                    damageEntries.Add(new DamageReportEntry(id, name, dmg, pct));
                }
            }

            damageEntries.Sort((a, b) => b.damageDealt.CompareTo(a.damageDealt));

            if (resultTitleText != null)
            {
                resultTitleText.text = victory ? "VICTORY" : "DEFEAT";
                resultTitleText.color = victory ? new Color(1f, 0.85f, 0.2f) : new Color(0.95f, 0.3f, 0.25f);
            }

            if (resultSubtitleText != null)
            {
                resultSubtitleText.text = $"Wave Reached: {wave}  |  Battle: #{runNumber}";
            }

            if (resultRewardsText != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (var r in rewards)
                {
                    sb.AppendLine($"• {r.rewardName}: {r.amount}");
                }

                if (HeroRosterManager.Instance != null && HeroRosterManager.Instance.OwnedHeroIds.Count > 0)
                {
                    List<string> heroNames = new List<string>();
                    foreach (string id in HeroRosterManager.Instance.OwnedHeroIds)
                    {
                        heroNames.Add(GetHeroDisplayName(id));
                    }
                    sb.AppendLine($"\nDefenders: {string.Join(", ", heroNames)}");
                }

                if (RunModifierManager.Instance != null && RunModifierManager.Instance.ActiveCards.Count > 0)
                {
                    List<string> cardNames = new List<string>();
                    foreach (var card in RunModifierManager.Instance.ActiveCards)
                    {
                        cardNames.Add(card.displayName);
                    }
                    sb.AppendLine($"Blessings: {string.Join(", ", cardNames)}");
                }

                resultRewardsText.text = sb.ToString();
            }

            if (resultDamageReportText != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                if (damageEntries.Count == 0)
                {
                    sb.AppendLine("No damage recorded.");
                }
                else
                {
                    foreach (var entry in damageEntries)
                    {
                        sb.AppendLine($"• {entry.displayName}: {entry.damageDealt:N0} dmg ({entry.percentage:F1}%)");
                    }
                }
                resultDamageReportText.text = sb.ToString();
            }

            ShowPanel(resultPanelGroup, true);
        }

        private string GetHeroDisplayName(string heroId)
        {
            var activeHeroes = UnityEngine.Object.FindObjectsByType<HeroAttack>(UnityEngine.FindObjectsSortMode.None);
            foreach (var h in activeHeroes)
            {
                if (h.Definition != null && h.Definition.id == heroId)
                {
                    return h.Definition.displayName;
                }
            }
            if (string.IsNullOrEmpty(heroId)) return "Unknown";
            string formatted = System.Text.RegularExpressions.Regex.Replace(heroId, @"_([a-z])", m => " " + m.Groups[1].Value.ToUpper());
            return char.ToUpper(formatted[0]) + formatted.Substring(1);
        }
        // ------------------------------------------------------------- Helpers

        private static RectTransform CreateRoot(RectTransform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private Text CreateText(RectTransform parent, string name, string content, int size, Color color, TextAnchor anchor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
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
            SetAnchored(bg.rectTransform, anchor, position, size);

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

            Text text = CreateText(bg.rectTransform, "Label", label, 24, Color.white, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        private void AddBuildButtonPreview(Button button, int index)
        {
            EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();
            AddEventTrigger(trigger, EventTriggerType.PointerEnter, () => PreviewBuildRange(index));
            AddEventTrigger(trigger, EventTriggerType.Select, () => PreviewBuildRange(index));
            AddEventTrigger(trigger, EventTriggerType.PointerDown, () => PreviewBuildRange(index));
        }

        private static void AddEventTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private int GetGoldShortfall(int cost)
        {
            int currentGold = economy != null ? economy.Gold : 0;
            return Mathf.Max(0, cost - currentGold);
        }

        private static void SetButtonVisual(Button button, Color color)
        {
            if (button != null && button.targetGraphic != null)
            {
                button.targetGraphic.color = color;
            }
        }

        private void ShowBuildRangeForFirstAvailableTower()
        {
            if (towers == null || selectedSlot == null || towers.AvailableTowers == null)
            {
                HideRangeIndicator();
                return;
            }

            for (int i = 0; i < towers.AvailableTowers.Length; i++)
            {
                TowerData tower = towers.AvailableTowers[i];
                if (tower != null && (unlocks == null || unlocks.IsTowerUnlocked(tower)))
                {
                    ShowBuildRange(tower);
                    return;
                }
            }

            HideRangeIndicator();
        }

        private void PreviewBuildRange(int index)
        {
            if (towers == null || towers.AvailableTowers == null || index < 0 || index >= towers.AvailableTowers.Length)
            {
                return;
            }

            TowerData tower = towers.AvailableTowers[index];
            if (tower == null || (unlocks != null && !unlocks.IsTowerUnlocked(tower)))
            {
                HideRangeIndicator();
                return;
            }

            ShowBuildRange(tower);
        }

        private void ShowBuildRange(TowerData tower)
        {
            if (selectedSlot == null || tower == null || rangeIndicator == null)
            {
                return;
            }

            rangeIndicator.Show(selectedSlot.transform.position, tower.range, tower.projectileTrailColor);
        }

        private void ShowTowerRange(Tower tower)
        {
            if (tower == null || rangeIndicator == null)
            {
                HideRangeIndicator();
                return;
            }

            Color color = tower.Data != null ? tower.Data.projectileTrailColor : Color.white;
            rangeIndicator.Show(tower.transform.position, tower.Range, color);
        }

        private void HideRangeIndicator()
        {
            if (rangeIndicator != null)
            {
                rangeIndicator.Hide();
            }
        }

        private static Color GetHeroRangeColor(WeaponDefinition weapon)
        {
            if (weapon.attackType == AttackType.Splash) return new Color(1f, 0.55f, 0.2f);
            if (weapon.statusEffectType == StatusEffectType.Slow) return new Color(0.5f, 0.85f, 1f);
            if (weapon.statusEffectType == StatusEffectType.Burn) return new Color(1f, 0.3f, 0.1f);
            if (weapon.statusEffectType == StatusEffectType.Shock) return new Color(0.9f, 0.9f, 0.2f);
            return new Color(1f, 0.95f, 0.55f);
        }

        private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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

        private void BuildAbilityCooldownHUD()
        {
            GameObject containerObj = new GameObject("AbilityCooldownHUD", typeof(RectTransform));
            abilityHudContainer = containerObj.GetComponent<RectTransform>();
            abilityHudContainer.SetParent(safeAreaRect, false);
            SetAnchored(abilityHudContainer, new Vector2(1f, 1f), new Vector2(-60f, -145f), new Vector2(90f, 600f));
        }

        private void CreateAbilityCooldownUIItem()
        {
            GameObject rootObj = new GameObject("Slot_" + abilityUiItems.Count, typeof(RectTransform));
            RectTransform rootRt = rootObj.GetComponent<RectTransform>();
            rootRt.SetParent(abilityHudContainer, false);
            SetAnchored(rootRt, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(70f, 70f));

            Image bg = CreateImage(rootRt, "Border", new Color(0.12f, 0.15f, 0.2f, 0.9f));
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.offsetMin = Vector2.zero;
            bg.rectTransform.offsetMax = Vector2.zero;

            Image icon = CreateImage(rootRt, "Icon", Color.white);
            icon.rectTransform.anchorMin = Vector2.zero;
            icon.rectTransform.anchorMax = Vector2.one;
            icon.rectTransform.offsetMin = new Vector2(4f, 4f);
            icon.rectTransform.offsetMax = new Vector2(-4f, -4f);

            Image fill = CreateImage(rootRt, "FillOverlay", new Color(0f, 0f, 0f, 0.65f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = false;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = new Vector2(4f, 4f);
            fill.rectTransform.offsetMax = new Vector2(-4f, -4f);

            Text timerText = CreateText(rootRt, "TimerText", "READY", 14, new Color(0.3f, 1f, 0.3f), TextAnchor.MiddleCenter);
            timerText.fontStyle = FontStyle.Bold;
            timerText.rectTransform.anchorMin = Vector2.zero;
            timerText.rectTransform.anchorMax = Vector2.one;
            timerText.rectTransform.offsetMin = Vector2.zero;
            timerText.rectTransform.offsetMax = Vector2.zero;

            Text lvlText = CreateText(rootRt, "LevelText", "Lv.1", 12, Color.white, TextAnchor.LowerCenter);
            lvlText.fontStyle = FontStyle.Bold;
            lvlText.rectTransform.anchorMin = new Vector2(0f, 0f);
            lvlText.rectTransform.anchorMax = new Vector2(1f, 0f);
            lvlText.rectTransform.anchoredPosition = new Vector2(0f, -12f);
            lvlText.rectTransform.sizeDelta = new Vector2(70f, 18f);

            AbilityCooldownUIItem item = new AbilityCooldownUIItem
            {
                Root = rootObj,
                Background = bg,
                Icon = icon,
                Fill = fill,
                LevelText = lvlText,
                TimerText = timerText,
                HeroId = ""
            };

            abilityUiItems.Add(item);
        }

        private void UpdateAbilityCooldownHUD()
        {
            if (HeroRosterManager.Instance == null) return;

            var slots = HeroRosterManager.Instance.Slots;
            int activeSlotCount = 0;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || !slot.IsOccupied || slot.CurrentHero == null)
                {
                    continue;
                }

                HeroAttack hero = slot.CurrentHero;
                if (!hero.HasSignatureAbility)
                {
                    continue;
                }

                if (activeSlotCount >= abilityUiItems.Count)
                {
                    CreateAbilityCooldownUIItem();
                }

                var uiItem = abilityUiItems[activeSlotCount];
                uiItem.Root.SetActive(true);
                uiItem.HeroId = hero.Definition.id;
                uiItem.Icon.color = GetHeroColor(hero.Definition.id);

                int metaLevel = SaveManager.GetMetaLevel(hero.Definition.id);
                uiItem.LevelText.text = "Lv." + metaLevel;

                float cdRemaining = hero.GetAbilityCooldownRemaining();
                float cdTotal = hero.GetModifiedAbilityCooldown();

                if (cdRemaining > 0f)
                {
                    uiItem.Fill.fillAmount = cdRemaining / cdTotal;
                    uiItem.TimerText.text = Mathf.CeilToInt(cdRemaining).ToString();
                    uiItem.TimerText.color = new Color(1f, 0.4f, 0.4f);
                }
                else
                {
                    uiItem.Fill.fillAmount = 0f;
                    uiItem.TimerText.text = "READY";
                    uiItem.TimerText.color = new Color(0.3f, 1f, 0.3f);
                }

                RectTransform rt = uiItem.Root.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, -activeSlotCount * 78f);

                activeSlotCount++;
            }

            for (int i = activeSlotCount; i < abilityUiItems.Count; i++)
            {
                abilityUiItems[i].Root.SetActive(false);
            }
        }

        private Color GetHeroColor(string heroId)
        {
            switch (heroId)
            {
                case "archer": return new Color(0.35f, 0.85f, 0.25f);
                case "bombardier": return new Color(1f, 0.5f, 0.12f);
                case "frost_mage": return new Color(0.25f, 0.85f, 1f);
                case "fire_mage": return new Color(1f, 0.22f, 0.08f);
                case "electric_engineer": return new Color(0.2f, 0.85f, 1f);
                case "sniper": return new Color(0.75f, 0.4f, 1f);
                default: return Color.white;
            }
        }
    }

    internal sealed class RangeIndicator
    {
        private const int SegmentCount = 96;
        private readonly LineRenderer line;

        private RangeIndicator(LineRenderer line)
        {
            this.line = line;
        }

        public static RangeIndicator Create()
        {
            GameObject go = new GameObject("Tower Range Indicator");
            LineRenderer renderer = go.AddComponent<LineRenderer>();
            renderer.loop = true;
            renderer.useWorldSpace = true;
            renderer.positionCount = SegmentCount;
            renderer.startWidth = 0.08f;
            renderer.endWidth = 0.08f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            go.SetActive(false);
            return new RangeIndicator(renderer);
        }

        public void Show(Vector3 center, float radius, Color color)
        {
            if (radius <= 0f)
            {
                Hide();
                return;
            }

            center.y += 0.08f;
            Color visibleColor = color;
            visibleColor.a = 0.9f;
            line.startColor = visibleColor;
            line.endColor = visibleColor;

            for (int i = 0; i < SegmentCount; i++)
            {
                float angle = i / (float)SegmentCount * Mathf.PI * 2f;
                Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                line.SetPosition(i, point);
            }

            line.gameObject.SetActive(true);
        }

        public void Hide()
        {
            line.gameObject.SetActive(false);
        }
    }

    internal static class RectTransformExtensions
    {
        public static void SetAnchored(this RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
