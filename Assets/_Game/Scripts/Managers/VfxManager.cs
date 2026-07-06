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

        private readonly Dictionary<GameObject, Queue<ParticleSystem>> pools = new Dictionary<GameObject, Queue<ParticleSystem>>();
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

        public void PlayExplosion(Vector3 pos) => Play(explosionPrefab, pos);
        public void PlayFrost(Vector3 pos) => Play(frostPrefab, pos);
        public void PlayHit(Vector3 pos) => Play(hitPrefab, pos);
        public void PlayPlace(Vector3 pos) => Play(placePrefab, pos);
        public void PlayUpgrade(Vector3 pos) => Play(upgradePrefab, pos);

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
            if (prefab == null)
            {
                return;
            }

            ParticleSystem instance = Get(prefab);
            instance.transform.position = position;
            instance.gameObject.SetActive(true);
            instance.Clear();
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
            return go.GetComponent<ParticleSystem>();
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
