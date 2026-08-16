using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Stonehold
{
    /// <summary>
    /// Central manager for spawning and recycling floating combat damage numbers and elemental synergy popups.
    /// Uses a zero-allocation pool on a dedicated WorldSpace Canvas.
    /// </summary>
    public class FloatingCombatTextManager : MonoBehaviour
    {
        private static FloatingCombatTextManager instance;

        public static FloatingCombatTextManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<FloatingCombatTextManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("FloatingCombatTextManager", typeof(FloatingCombatTextManager));
                        instance = go.GetComponent<FloatingCombatTextManager>();
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        [Header("Pool Settings")]
        [SerializeField] private int initialPoolSize = 40;
        [SerializeField] private bool showDamageNumbers = true;
        [SerializeField] private bool showElementalPopups = true;

        private Canvas worldCanvas;
        private Font uiFont;
        private readonly Queue<FloatingCombatTextItem> pool = new Queue<FloatingCombatTextItem>(64);
        private readonly List<FloatingCombatTextItem> allItems = new List<FloatingCombatTextItem>(64);

        public int ActiveCount => allItems.Count - pool.Count;
        public int TotalPoolCount => allItems.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ResolveFont();
            BuildWorldCanvas();
            WarmPool(initialPoolSize);

            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void SubscribeEvents()
        {
            Enemy.AnyDamagedDetailed += OnEnemyDamagedDetailed;
            StatusEffectController.OnElementalReaction += OnElementalReaction;
        }

        private void UnsubscribeEvents()
        {
            Enemy.AnyDamagedDetailed -= OnEnemyDamagedDetailed;
            StatusEffectController.OnElementalReaction -= OnElementalReaction;
        }

        private void ResolveFont()
        {
            if (uiFont != null) return;
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (uiFont == null) uiFont = Resources.Load<Font>("Fonts/Cinzel-SemiBold");
            if (uiFont == null) uiFont = Resources.Load<Font>("Cinzel-SemiBold");
            if (uiFont == null) uiFont = Resources.Load<Font>("Arial");
            if (uiFont == null)
            {
                var allFonts = Resources.FindObjectsOfTypeAll<Font>();
                if (allFonts != null && allFonts.Length > 0) uiFont = allFonts[0];
            }
        }

        private void BuildWorldCanvas()
        {
            worldCanvas = GetComponent<Canvas>();
            if (worldCanvas == null)
            {
                worldCanvas = gameObject.AddComponent<Canvas>();
            }
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingOrder = 50;

            if (GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 10;
            }
        }

        private void WarmPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                CreateNewItem();
            }
        }

        private FloatingCombatTextItem CreateNewItem()
        {
            GameObject go = new GameObject($"FloatingText_{allItems.Count}", typeof(RectTransform));
            go.transform.SetParent(transform, false);

            FloatingCombatTextItem item = go.AddComponent<FloatingCombatTextItem>();
            item.SetupComponents(uiFont, this);
            go.SetActive(false);

            allItems.Add(item);
            pool.Enqueue(item);
            return item;
        }

        public FloatingCombatTextItem GetItem()
        {
            FloatingCombatTextItem item;
            if (pool.Count > 0)
            {
                item = pool.Dequeue();
            }
            else
            {
                item = CreateNewItem();
                pool.Dequeue(); // remove since it was added to pool in CreateNewItem
            }
            return item;
        }

        public void ReturnToPool(FloatingCombatTextItem item)
        {
            if (item != null && !pool.Contains(item))
            {
                pool.Enqueue(item);
            }
        }

        private void OnEnemyDamagedDetailed(Enemy enemy, float amount, bool isCrit)
        {
            if (!showDamageNumbers || enemy == null || amount < 1f) return;
            SpawnDamageText(enemy.transform.position, amount, isCrit);
        }

        private void OnElementalReaction(ElementalReactionType reaction, Vector3 worldPos, string heroId)
        {
            if (!showElementalPopups || reaction == ElementalReactionType.None) return;
            SpawnReactionText(worldPos, reaction, heroId);
        }

        /// <summary>
        /// Spawns a floating damage number above an enemy.
        /// </summary>
        public void SpawnDamageText(Vector3 worldPos, float amount, bool isCrit)
        {
            FloatingCombatTextItem item = GetItem();
            int displayDamage = Mathf.RoundToInt(amount);
            string message = isCrit ? $"CRIT! {displayDamage}" : $"{displayDamage}";
            Color color = isCrit ? new Color(1f, 0.85f, 0.2f, 1f) : new Color(0.96f, 0.95f, 0.93f, 1f);
            float scale = isCrit ? 1.35f : 1.0f;
            float duration = isCrit ? 0.95f : 0.75f;

            item.Spawn(message, worldPos, color, scale, isCrit, duration);
        }

        /// <summary>
        /// Spawns a colorful banner for triggered elemental synergy reactions (Thermal Shock, Overload, Shatter).
        /// </summary>
        public void SpawnReactionText(Vector3 worldPos, ElementalReactionType reaction, string heroId = null)
        {
            FloatingCombatTextItem item = GetItem();
            string message;
            Color color;

            switch (reaction)
            {
                case ElementalReactionType.ThermalShock:
                    message = "🔥 THERMAL SHOCK!";
                    color = new Color(1f, 0.45f, 0.15f, 1f);
                    break;
                case ElementalReactionType.Overload:
                    message = "⚡ OVERLOAD!";
                    color = new Color(0.35f, 0.92f, 1f, 1f);
                    break;
                case ElementalReactionType.Shatter:
                    message = "❄️ SHATTER!";
                    color = new Color(0.40f, 0.85f, 1f, 1f);
                    break;
                case ElementalReactionType.CorrosiveBlast:
                    message = "🧪💥 CORROSIVE BLAST!";
                    color = new Color(0.35f, 1f, 0.2f, 1f);
                    break;
                case ElementalReactionType.Neurotoxin:
                    message = "🧪⚡ NEUROTOXIN!";
                    color = new Color(0.65f, 0.4f, 1f, 1f);
                    break;
                case ElementalReactionType.BrittleBlight:
                    message = "🧪❄️ BRITTLE BLIGHT!";
                    color = new Color(0.2f, 0.95f, 0.8f, 1f);
                    break;
                default:
                    message = reaction.ToString().ToUpper();
                    color = Color.white;
                    break;
            }

            Vector3 elevatedPos = worldPos + Vector3.up * 0.4f;
            item.Spawn(message, elevatedPos, color, 1.4f, true, 1.1f);
        }

        /// <summary>
        /// Spawns custom floating text (e.g. gold popups, level up, status tags).
        /// </summary>
        public void SpawnCustomText(Vector3 worldPos, string message, Color color, float scaleMultiplier = 1f, bool bounce = false)
        {
            FloatingCombatTextItem item = GetItem();
            item.Spawn(message, worldPos, color, scaleMultiplier, bounce, 0.85f);
        }

        public void ClearAll()
        {
            for (int i = 0; i < allItems.Count; i++)
            {
                if (allItems[i] != null && allItems[i].IsActive)
                {
                    allItems[i].Recycle();
                }
            }
        }
    }
}
