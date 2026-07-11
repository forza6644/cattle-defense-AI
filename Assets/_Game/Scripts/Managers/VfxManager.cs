using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Stonehold
{
    /// <summary>
    /// Central visual-effects spawner. Owns a small pool per effect prefab so bursts
    /// never allocate/Destroy at runtime, exposes a Play API for gameplay code
    /// (projectile impacts, tower place/upgrade), and auto-plays death, gold, castle
    /// hit and victory/defeat effects by listening to existing gameplay events.
    /// Effect prefabs are assigned in the Inspector (data-driven, swap for art later).
    /// </summary>
    public class VfxManager : MonoBehaviour
    {
        public static VfxManager Instance { get; private set; }

        [Header("Impact")]
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField] private GameObject frostPrefab;
        [SerializeField] private GameObject hitPrefab;

        [Header("Enemy")]
        [SerializeField] private GameObject deathPrefab;
        [SerializeField] private GameObject goldPrefab;

        [Header("Towers")]
        [SerializeField] private GameObject placePrefab;
        [SerializeField] private GameObject upgradePrefab;

        [Header("Castle / Run")]
        [SerializeField] private GameObject castleHitPrefab;
        [SerializeField] private GameObject victoryPrefab;
        [SerializeField] private GameObject defeatPrefab;

        private struct EffectDefaults
        {
            public ParticleSystem.MinMaxGradient startColor;
            public Vector3 scale;
        }

        private readonly Dictionary<GameObject, Queue<ParticleSystem>> pools = new Dictionary<GameObject, Queue<ParticleSystem>>();
        private readonly Dictionary<GameObject, EffectDefaults> prefabDefaults = new Dictionary<GameObject, EffectDefaults>();
        private Transform castle;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Castle castleComponent = FindFirstObjectByType<Castle>();
            castle = castleComponent != null ? castleComponent.transform : null;

            Enemy.AnyKilled += OnEnemyKilled;
            if (castleComponent != null)
            {
                castleComponent.HealthChanged += OnCastleHit;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged += OnStateChanged;
            }
        }

        private void OnDestroy()
        {
            Enemy.AnyKilled -= OnEnemyKilled;
            Castle castleComponent = FindFirstObjectByType<Castle>();
            if (castleComponent != null)
            {
                castleComponent.HealthChanged -= OnCastleHit;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnStateChanged;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // -------------------------------------------------------- Public Play API

        public void PlayExplosion(Vector3 pos)
        {
            Play(explosionPrefab, pos, null, 1.25f);
            if (CameraRig.Instance != null)
            {
                CameraRig.Instance.Shake(0.4f);
            }
        }

        public void PlayFrost(Vector3 pos) => Play(frostPrefab, pos, new Color(0.3f, 0.85f, 1f, 1f), 1.1f);
        public void PlayBurn(Vector3 pos) => Play(explosionPrefab, pos, new Color(1f, 0.18f, 0.03f, 1f));
        public void PlayShock(Vector3 pos) => Play(hitPrefab, pos, new Color(1f, 0.95f, 0.1f, 1f));
        public void PlayHit(Vector3 pos) => Play(hitPrefab, pos, Color.white);
        public void PlayHit(Vector3 pos, Color color) => Play(hitPrefab, pos, color);
        public void PlayPlace(Vector3 pos) => Play(placePrefab, pos);
        public void PlayUpgrade(Vector3 pos) => Play(upgradePrefab, pos);

        public void PlayFireImpact(Vector3 pos)
        {
            Play(explosionPrefab, pos, new Color(1f, 0.35f, 0.05f, 1f), 0.85f);
            if (CameraRig.Instance != null)
            {
                CameraRig.Instance.Shake(0.2f);
            }
        }

        public void PlayShockImpact(Vector3 pos)
        {
            Play(hitPrefab, pos, new Color(1f, 0.95f, 0.15f, 1f), 1.15f);
        }

        public void PlaySniperImpact(Vector3 pos)
        {
            Play(hitPrefab, pos, new Color(0.85f, 0.3f, 1f, 1f), 0.7f);
        }

        // ----------------------------------------------------------- Event hooks

        private void OnEnemyKilled(Enemy enemy, int gold)
        {
            Vector3 pos = enemy.transform.position;
            Play(deathPrefab, pos);
            Play(goldPrefab, pos + Vector3.up * 0.5f);
        }

        private void OnCastleHit()
        {
            if (castle != null)
            {
                Play(castleHitPrefab, castle.position + Vector3.up * 1f);
            }

            if (CameraRig.Instance != null)
            {
                CameraRig.Instance.Shake(0.5f);
            }
        }

        private void OnStateChanged(GameState state)
        {
            Vector3 pos = castle != null ? castle.position + Vector3.up * 3f : Vector3.up * 3f;
            if (state == GameState.Victory)
            {
                Play(victoryPrefab, pos);
            }
            else if (state == GameState.Defeat)
            {
                Play(defeatPrefab, pos);
            }
        }

        // -------------------------------------------------------------- Pooling

        private void Play(GameObject prefab, Vector3 position)
        {
            Play(prefab, position, null, 1f);
        }

        private void Play(GameObject prefab, Vector3 position, Color? color)
        {
            Play(prefab, position, color, 1f);
        }

        private void Play(GameObject prefab, Vector3 position, Color? color, float scale)
        {
            if (prefab == null)
            {
                return;
            }

            ParticleSystem instance = Get(prefab);
            EffectDefaults defaults = prefabDefaults[prefab];

            // Pooled instances are shared across effect types, so every reuse must
            // restore the prefab's original color/scale before applying overrides -
            // otherwise a fire tint leaks into later untinted explosions or hits.
            instance.transform.position = position;
            instance.transform.localScale = defaults.scale * scale;
            instance.gameObject.SetActive(true);
            instance.Clear();
            ParticleSystem.MainModule main = instance.main;
            main.startColor = color.HasValue
                ? new ParticleSystem.MinMaxGradient(color.Value)
                : defaults.startColor;
            instance.Play();
            StartCoroutine(ReturnWhenDone(prefab, instance));
        }

        private ParticleSystem Get(GameObject prefab)
        {
            if (pools.TryGetValue(prefab, out Queue<ParticleSystem> pool) && pool.Count > 0)
            {
                return pool.Dequeue();
            }

            GameObject go = Instantiate(prefab, transform);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();

            // Capture the prefab's untouched color/scale the first time we see it,
            // before any Play() override can mutate the instance.
            if (!prefabDefaults.ContainsKey(prefab))
            {
                prefabDefaults[prefab] = new EffectDefaults
                {
                    startColor = ps.main.startColor,
                    scale = go.transform.localScale
                };
            }

            return ps;
        }

        private IEnumerator ReturnWhenDone(GameObject prefab, ParticleSystem instance)
        {
            ParticleSystem.MainModule main = instance.main;
            float life = main.duration + main.startLifetime.constantMax;
            yield return new WaitForSeconds(life);

            if (instance == null)
            {
                yield break;
            }

            instance.Stop();
            instance.gameObject.SetActive(false);

            if (!pools.TryGetValue(prefab, out Queue<ParticleSystem> pool))
            {
                pool = new Queue<ParticleSystem>();
                pools[prefab] = pool;
            }

            pool.Enqueue(instance);
        }
    }
}
