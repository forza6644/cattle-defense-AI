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
    /// font, responsive CanvasScaler): HUD (gold, wave, castle HP), enemy health
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
        private Text castleHpText;
        private RectTransform castleHpFill;
        private Image castleHpFillImage;
        private CanvasGroup waveControlGroup;
        private Text waveStatusText;
        private Text waveCountdownText;
        private Button startWaveButton;

        // Banner
        private CanvasGroup bannerGroup;
        private Text bannerText;

        // Panels
        private CanvasGroup buildMenuGroup;
        private CanvasGroup towerPanelGroup;
        private CanvasGroup pauseGroup;
        private CanvasGroup victoryGroup;
        private CanvasGroup defeatGroup;
        private Text towerPanelTitle;
        private Text upgradeButtonLabel;
        private Text sellButtonLabel;
        private Text targetButtonLabel;
        private Button upgradeButton;
        private Button targetButton;
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
        private RunProgressionManager progression;

        private Text hintText;
        private Image hintBg;
        private float hintTimer;
        private bool hasShownTargetingHint;

        // Floating text + enemy bars
        private RectTransform barsRoot;
        private RectTransform floatingRoot;
        private readonly Queue<Text> floatingPool = new Queue<Text>();
        private readonly List<RectTransform> barBackgrounds = new List<RectTransform>();
        private readonly List<RectTransform> barFills = new List<RectTransform>();
        private readonly List<Image> barFillImages = new List<Image>();

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

        private void Awake()
        {
            Instance = this;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void Start()
        {
            economy = EconomyManager.Instance;
            waves = FindFirstObjectByType<WaveManager>();
            castle = FindFirstObjectByType<Castle>();
            game = GameManager.Instance;
            towers = FindFirstObjectByType<TowerManager>();
            unlocks = UnlockManager.Instance != null ? UnlockManager.Instance : FindFirstObjectByType<UnlockManager>();
            cam = Camera.main;
            rangeIndicator = RangeIndicator.Create();

            BuildUI();

            progression = RunProgressionManager.Instance != null ? RunProgressionManager.Instance : FindFirstObjectByType<RunProgressionManager>();
            if (progression != null)
            {
                progression.XpChanged += OnXpChanged;
                progression.ShowLevelUpDraft += OnShowLevelUpDraft;
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
            if (game != null) game.StateChanged += OnStateChanged;
            if (unlocks != null)
            {
                unlocks.UnlocksChanged += RefreshBuildMenu;
                unlocks.TowerUnlocked += OnTowerUnlocked;
            }

            Enemy.AnyDamaged += OnEnemyDamaged;
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
                progression.ShowLevelUpDraft -= OnShowLevelUpDraft;
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
            if (game != null) game.StateChanged -= OnStateChanged;
            if (unlocks != null)
            {
                unlocks.UnlocksChanged -= RefreshBuildMenu;
                unlocks.TowerUnlocked -= OnTowerUnlocked;
            }

            Enemy.AnyDamaged -= OnEnemyDamaged;
            Enemy.AnyKilled -= OnEnemyKilled;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ------------------------------------------------------------------ HUD

        private void RefreshGold()
        {
            goldText.text = "Gold: " + (economy != null ? economy.Gold : 0);
            RefreshBuildMenu();
            RefreshTowerPanel();
        }

        private void RefreshCastleHealth()
        {
            if (castle == null)
            {
                return;
            }

            float pct = castle.MaxHealth > 0 ? (float)castle.CurrentHealth / castle.MaxHealth : 0f;
            castleHpFill.localScale = new Vector3(pct, 1f, 1f);
            castleHpFillImage.color = Color.Lerp(new Color(0.85f, 0.2f, 0.2f), new Color(0.25f, 0.8f, 0.3f), pct);
            castleHpText.text = castle.CurrentHealth + " / " + castle.MaxHealth;
        }

        private void OnWaveStarted(int number, WaveData wave)
        {
            ShowPanel(waveControlGroup, false);
            waveText.text = "Wave " + number + "/" + waves.TotalWaves;
            if (waves != null && number == waves.TotalWaves)
            {
                bannerText.color = new Color(1f, 0.25f, 0.2f); // High-contrast Red for Boss
                ShowBanner("!!! FINAL BOSS WAVE !!!");
            }
            else
            {
                bannerText.color = Color.white;
                ShowBanner("Wave " + number + " - " + wave.waveLabel);
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
            waveText.text = "Next: Wave " + number + "/" + waves.TotalWaves;
            waveStatusText.text = "Next: " + wave.waveLabel;
            OnWaveCountdownChanged(secondsRemaining);
            if (startWaveButton != null)
            {
                startWaveButton.interactable = true;
            }

            ShowPanel(waveControlGroup, true);

            if (number == 12)
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

        private void OnWaveCountdownChanged(float secondsRemaining)
        {
            waveCountdownText.text = "Auto starts in " + Mathf.CeilToInt(secondsRemaining) + "s";
        }

        private void OnWaveCountdownFinished()
        {
            if (startWaveButton != null)
            {
                startWaveButton.interactable = false;
            }

            ShowPanel(waveControlGroup, false);
        }

        private void OnWaveCleared(int number, WaveData wave)
        {
            waveText.text = "Wave " + number + "/" + waves.TotalWaves + " cleared";
        }

        private void OnWaveClearBonusAwarded(int waveNumber, int amount)
        {
            waveText.text = "Wave " + waveNumber + "/" + waves.TotalWaves + " cleared  +" + amount + "g";
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

        private void OnEnemyDamaged(Enemy enemy, float amount)
        {
            SpawnFloatingText("-" + Mathf.RoundToInt(amount), enemy.transform.position + Vector3.up * 0.8f,
                new Color(1f, 0.55f, 0.2f), 30);
        }

        private void OnEnemyKilled(Enemy enemy, int gold)
        {
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

        // ---------------------------------------------------------- Enemy bars

        private void LateUpdate()
        {
            var enemies = EnemyManager.All;

            while (barBackgrounds.Count < enemies.Count)
            {
                CreateEnemyBar();
            }

            for (int i = 0; i < barBackgrounds.Count; i++)
            {
                Enemy enemy = i < enemies.Count ? enemies[i] : null;
                Vector2 local = default;
                float pct = enemy != null && enemy.MaxHealth > 0f ? Mathf.Clamp01(enemy.CurrentHealth / enemy.MaxHealth) : 0f;

                // Only show a bar for enemies that exist, are damaged, and are on-screen.
                bool used = enemy != null
                    && enemy.MaxHealth > 0f
                    && pct < 0.999f
                    && TryWorldToCanvas(enemy.transform.position + Vector3.up * 1.4f, out local);

                barBackgrounds[i].gameObject.SetActive(used);
                if (!used)
                {
                    continue;
                }

                barBackgrounds[i].anchoredPosition = local;
                barFills[i].localScale = new Vector3(pct, 1f, 1f);
                barFillImages[i].color = pct > 0.5f
                    ? Color.Lerp(new Color(0.95f, 0.85f, 0.2f), new Color(0.35f, 0.85f, 0.3f), (pct - 0.5f) * 2f)
                    : Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.95f, 0.85f, 0.2f), pct * 2f);
            }
        }

        private void CreateEnemyBar()
        {
            Image bg = CreateImage(barsRoot, "EnemyBarBg", new Color(0f, 0f, 0f, 0.65f));
            RectTransform bgRect = bg.rectTransform;
            bgRect.anchorMin = bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(64f, 9f);

            Image fill = CreateImage(bgRect, "Fill", new Color(0.9f, 0.25f, 0.25f));
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = new Vector2(1.5f, 1.5f);
            fillRect.offsetMax = new Vector2(-1.5f, -1.5f);

            bg.gameObject.SetActive(false);
            barBackgrounds.Add(bgRect);
            barFills.Add(fillRect);
            barFillImages.Add(fill);
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
            selectedSlot = null;
            RefreshTowerPanel();
            ShowPanel(towerPanelGroup, true);
            ShowPanel(buildMenuGroup, false);
            ShowTowerRange(tower);

            if (!hasShownTargetingHint)
            {
                hasShownTargetingHint = true;
                ShowHint("Use Target mode to choose how this defender picks enemies.");
            }
        }

        public void HideSelectionPanels()
        {
            selectedSlot = null;
            selectedTower = null;
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

        private void RefreshTowerPanel()
        {
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
                targetButtonLabel.text = "Target:\n" + selectedTower.CurrentTargetingMode.ToString() + " >";
                targetButtonLabel.fontSize = 20;
            }
        }

        private void OnUpgradeClicked()
        {
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

        private void OnStateChanged(GameState state)
        {
            HideSelectionPanels();
            ShowPanel(pauseGroup, state == GameState.Paused);
            ShowPanel(victoryGroup, state == GameState.Victory);
            ShowPanel(defeatGroup, state == GameState.Defeat);
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

        private void OnShowLevelUpDraft(RunProgressionManager.CardChoice[] choices)
        {
            if (choices == null || choices.Length < 3)
            {
                return;
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

                if (cardButtons[i] != null)
                {
                    cardButtons[i].onClick.RemoveAllListeners();
                    cardButtons[i].onClick.AddListener(() =>
                    {
                        if (progression != null)
                        {
                            progression.ApplyChoice(choices[index]);
                        }
                    });
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
            RectTransform rt = group.transform as RectTransform;
            bool overlay = rt != null && rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one;
            float startAlpha = group.alpha;

            Vector3 startScale = group.transform.localScale;
            if (!overlay && targetAlpha > 0.5f && startAlpha < 0.5f)
            {
                startScale = Vector3.one * 0.9f; // pop in from slightly small
            }

            for (float t = 0f; t < PanelFadeSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / PanelFadeSeconds;
                group.alpha = Mathf.Lerp(startAlpha, targetAlpha, k);
                if (!overlay)
                {
                    group.transform.localScale = Vector3.Lerp(startScale, Vector3.one, k);
                }

                yield return null;
            }

            group.alpha = targetAlpha;
            if (!overlay)
            {
                group.transform.localScale = Vector3.one;
            }
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

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Draw order: enemy bars underneath, floating text, HUD, panels on top.
            barsRoot = CreateRoot(canvasRect, "EnemyBars");
            floatingRoot = CreateRoot(canvasRect, "FloatingText");

            // Gold (top-left)
            goldText = CreateText(canvasRect, "GoldText", "Gold: 0", 40, new Color(1f, 0.85f, 0.2f), TextAnchor.UpperLeft);
            SetAnchored(goldText.rectTransform, new Vector2(0f, 1f), new Vector2(25f, -20f), new Vector2(400f, 60f));

            // XP Bar (top-left, below GoldText)
            Image xpBg = CreateImage(canvasRect, "XpBarBg", new Color(0f, 0f, 0f, 0.6f));
            SetAnchored(xpBg.rectTransform, new Vector2(0f, 1f), new Vector2(175f, -95f), new Vector2(300f, 26f));
            xpFillImage = CreateImage(xpBg.rectTransform, "XpFill", new Color(0.6f, 0.25f, 0.85f));
            xpFill = xpFillImage.rectTransform;
            xpFill.anchorMin = Vector2.zero;
            xpFill.anchorMax = Vector2.one;
            xpFill.pivot = new Vector2(0f, 0.5f);
            xpFill.offsetMin = new Vector2(2f, 2f);
            xpFill.offsetMax = new Vector2(-2f, -2f);
            xpText = CreateText(xpBg.rectTransform, "XpText", "Lv.1  XP: 0 / 100", 18, Color.white, TextAnchor.MiddleCenter);
            xpText.rectTransform.anchorMin = Vector2.zero;
            xpText.rectTransform.anchorMax = Vector2.one;
            xpText.rectTransform.offsetMin = Vector2.zero;
            xpText.rectTransform.offsetMax = Vector2.zero;

            // Wave counter (top-center)
            waveText = CreateText(canvasRect, "WaveText", "Wave -", 40, Color.white, TextAnchor.UpperCenter);
            SetAnchored(waveText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(400f, 60f));

            // Hint panel background (top-center, below wave counter)
            hintBg = CreateImage(canvasRect, "HintPanel", new Color(0.08f, 0.1f, 0.15f, 0.85f));
            SetAnchored(hintBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(900f, 48f));
            hintText = CreateText(hintBg.rectTransform, "Label", "", 22, new Color(0.95f, 0.95f, 1f), TextAnchor.MiddleCenter);
            hintText.rectTransform.anchorMin = Vector2.zero;
            hintText.rectTransform.anchorMax = Vector2.one;
            hintText.rectTransform.offsetMin = Vector2.zero;
            hintText.rectTransform.offsetMax = Vector2.zero;
            hintBg.gameObject.SetActive(false);

            BuildWaveControl();

            // Castle HP bar (top-right)
            Image hpBg = CreateImage(canvasRect, "CastleHpBar", new Color(0f, 0f, 0f, 0.6f));
            SetAnchored(hpBg.rectTransform, new Vector2(1f, 1f), new Vector2(-190f, -35f), new Vector2(320f, 34f));
            castleHpFillImage = CreateImage(hpBg.rectTransform, "Fill", new Color(0.25f, 0.8f, 0.3f));
            castleHpFill = castleHpFillImage.rectTransform;
            castleHpFill.anchorMin = Vector2.zero;
            castleHpFill.anchorMax = Vector2.one;
            castleHpFill.pivot = new Vector2(0f, 0.5f);
            castleHpFill.offsetMin = new Vector2(2f, 2f);
            castleHpFill.offsetMax = new Vector2(-2f, -2f);
            castleHpText = CreateText(hpBg.rectTransform, "Label", "10 / 10", 22, Color.white, TextAnchor.MiddleCenter);
            castleHpText.rectTransform.anchorMin = Vector2.zero;
            castleHpText.rectTransform.anchorMax = Vector2.one;
            castleHpText.rectTransform.offsetMin = Vector2.zero;
            castleHpText.rectTransform.offsetMax = Vector2.zero;

            // Pause button (under the HP bar)
            CreateButton(canvasRect, "PauseButton", "Pause", new Vector2(120f, 44f), new Vector2(1f, 1f),
                new Vector2(-90f, -90f), () => { if (game != null) game.TogglePause(); });

            // Wave banner (center)
            bannerText = CreateText(canvasRect, "WaveBanner", "", 72, Color.white, TextAnchor.MiddleCenter);
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
            SetAnchored(titleText.rectTransform, new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(1200f, 80f));

            Text subtitleText = CreateText(rect, "Subtitle", "Choose a blessing for your defenders", 26, new Color(0.85f, 0.85f, 0.9f), TextAnchor.MiddleCenter);
            SetAnchored(subtitleText.rectTransform, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(1200f, 40f));

            float startX = -450f;
            float spacing = 450f;
            float cardY = -40f;
            Vector2 cardSize = new Vector2(380f, 540f);

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                Image cardBg = CreateImage(rect, "Card_" + i, new Color(0.12f, 0.16f, 0.24f, 0.95f));
                RectTransform cardRt = cardBg.rectTransform;
                SetAnchored(cardRt, new Vector2(0.5f, 0.5f), new Vector2(startX + i * spacing, cardY), cardSize);

                Image border = CreateImage(cardRt, "Border", new Color(0.24f, 0.32f, 0.48f, 0.8f));
                border.rectTransform.anchorMin = Vector2.zero;
                border.rectTransform.anchorMax = Vector2.one;
                border.rectTransform.offsetMin = new Vector2(4f, 4f);
                border.rectTransform.offsetMax = new Vector2(-4f, -4f);

                Image innerBg = CreateImage(border.rectTransform, "InnerBg", new Color(0.08f, 0.1f, 0.15f, 0.95f));
                innerBg.rectTransform.anchorMin = Vector2.zero;
                innerBg.rectTransform.anchorMax = Vector2.one;
                innerBg.rectTransform.offsetMin = new Vector2(4f, 4f);
                innerBg.rectTransform.offsetMax = new Vector2(-4f, -4f);

                RectTransform contentRoot = innerBg.rectTransform;

                cardTitleTexts[i] = CreateText(contentRoot, "Title", "Card Title", 28, new Color(1f, 0.85f, 0.2f), TextAnchor.UpperCenter);
                cardTitleTexts[i].fontStyle = FontStyle.Bold;
                SetAnchored(cardTitleTexts[i].rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(320f, 60f));

                cardDescriptionTexts[i] = CreateText(contentRoot, "Description", "Card description...", 20, new Color(0.85f, 0.85f, 0.9f), TextAnchor.MiddleCenter);
                cardDescriptionTexts[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                SetAnchored(cardDescriptionTexts[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(300f, 220f));

                cardButtons[i] = CreateButton(contentRoot, "SelectButton", "Select", new Vector2(240f, 60f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 40f), () => { });
            }

            levelUpPanelGroup = dim.gameObject.AddComponent<CanvasGroup>();
            levelUpPanelGroup.alpha = 0f;
            levelUpPanelGroup.interactable = false;
            levelUpPanelGroup.blocksRaycasts = false;
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

            Button sell = CreateButton(panel, "Sell", "Sell", new Vector2(180f, 70f), new Vector2(0.5f, 0f),
                new Vector2(210f, 55f), OnSellClicked);
            sellButtonLabel = sell.GetComponentInChildren<Text>();

            CreateButton(panel, "Close", "X", new Vector2(44f, 44f), new Vector2(1f, 1f),
                new Vector2(-30f, -28f), HideSelectionPanels);
        }

        private void BuildWaveControl()
        {
            Image bg = CreateImage(canvasRect, "WaveControl", new Color(0.08f, 0.08f, 0.12f, 0.9f));
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
            Image bg = CreateImage(canvasRect, name, new Color(0.08f, 0.08f, 0.12f, 0.92f));
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

        private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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
