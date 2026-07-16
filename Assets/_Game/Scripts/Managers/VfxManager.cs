using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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

        private readonly Queue<LineRenderer> impactRingPool = new Queue<LineRenderer>();
        private const int MaxImpactRings = 12;
        private int activeImpactRings;
        private readonly Queue<LineRenderer> abilityTracePool = new Queue<LineRenderer>();
        private Transform castle;
        private Castle hookedCastle;
        private Material abilityTraceMaterial;

        private Renderer[] castleRenderers;
        private Color[] castleBaseColors;
        private Coroutine castleFlashRoutine;
        private readonly Dictionary<StatusEffectType, Queue<ParticleSystem>> statusParticlePools = new Dictionary<StatusEffectType, Queue<ParticleSystem>>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            hookedCastle = FindFirstObjectByType<Castle>();
            castle = hookedCastle != null ? hookedCastle.transform : null;

            Enemy.AnyKilled += OnEnemyKilled;
            if (hookedCastle != null)
            {
                hookedCastle.DamageTaken += OnCastleHit;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged += OnStateChanged;
            }

            WaveManager waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.WaveStarted += OnWaveStarted;
            }
        }

        private void OnDestroy()
        {
            Enemy.AnyKilled -= OnEnemyKilled;
            if (hookedCastle != null)
            {
                hookedCastle.DamageTaken -= OnCastleHit;
                hookedCastle = null;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StateChanged -= OnStateChanged;
            }

            WaveManager waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.WaveStarted -= OnWaveStarted;
            }

            if (Instance == this)
            {
                Instance = null;
            }

            if (abilityTraceMaterial != null)
            {
                Destroy(abilityTraceMaterial);
            }
        }

        // -------------------------------------------------------- Public Play API


        public void PlayExplosion(Vector3 pos) => PlayExplosion(pos, false);
        public void PlayExplosion(Vector3 pos, bool shakeCamera)
        {
            Color color = new Color(1f, 0.48f, 0.08f, 1f);
            Play(explosionPrefab, pos, color, 1.25f);
            PlayImpactRing(pos, color, 1.15f, 0.28f, 0.14f);
            if (shakeCamera && CameraRig.Instance != null)
            {
                CameraRig.Instance.Shake(0.45f);
            }
        }


        public void PlayFrost(Vector3 pos, float scale = 1.1f)
        {
            Color color = new Color(0.25f, 0.86f, 1f, 1f);
            Play(frostPrefab, pos, color, scale);
            PlayImpactRing(pos, color, 0.85f * (scale / 1.1f), 0.24f * (scale / 1.1f), 0.1f * (scale / 1.1f));
        }
        public void PlayBurn(Vector3 pos) => Play(explosionPrefab, pos, new Color(1f, 0.18f, 0.03f, 1f));
        public void PlayShock(Vector3 pos) => Play(hitPrefab, pos, new Color(1f, 0.95f, 0.1f, 1f));
        public void PlayHit(Vector3 pos) => Play(hitPrefab, pos, Color.white);
        public void PlayHit(Vector3 pos, Color color) => Play(hitPrefab, pos, color);
        public void PlayPlace(Vector3 pos) => Play(placePrefab, pos);
        public void PlayUpgrade(Vector3 pos) => Play(upgradePrefab, pos);

        public void PlayCriticalImpact(Vector3 pos)
        {
            Color goldColor = new Color(1f, 0.85f, 0.2f, 1f);
            Play(hitPrefab, pos, goldColor, 1.4f);
            PlayImpactRing(pos, goldColor, 0.8f, 0.22f, 0.12f);
        }

        public void PlayCastleRegenFeedback(Vector3 pos)
        {
            Play(upgradePrefab, pos, new Color(0.3f, 1f, 0.3f, 1f), 1.5f);
        }

        public void PlayFireImpact(Vector3 pos) => PlayFireImpact(pos, false);
        public void PlayFireImpact(Vector3 pos, bool shakeCamera)
        {
            Color color = new Color(1f, 0.3f, 0.03f, 1f);
            Play(explosionPrefab, pos, color, 0.9f);
            PlayImpactRing(pos, color, 0.95f, 0.26f, 0.12f);
            if (shakeCamera && CameraRig.Instance != null)
            {
                CameraRig.Instance.Shake(0.25f);
            }
        }


        public void PlayShockImpact(Vector3 pos)
        {
            Color color = new Color(0.16f, 0.72f, 1f, 1f);
            Play(hitPrefab, pos, color, 1.15f);
            PlayImpactRing(pos, color, 0.72f, 0.18f, 0.08f);
        }


        public void PlaySniperImpact(Vector3 pos)
        {
            Color color = new Color(0.82f, 0.32f, 1f, 1f);
            Play(hitPrefab, pos, color, 0.72f);
            PlayImpactRing(pos, color, 0.42f, 0.14f, 0.06f);
        }


        public void PlayHeroMuzzle(Vector3 pos, string heroId)
        {
            float scale;
            switch (heroId)
            {
                case "bombardier": scale = 0.82f; break;
                case "sniper": scale = 0.38f; break;
                case "fire_mage": scale = 0.62f; break;
                default: scale = 0.5f; break;
            }

            Play(hitPrefab, pos, GetHeroColor(heroId), scale);
        }


        public void PlayHeroAbilityCast(Vector3 pos, string heroId)
        {
            Color color = GetHeroColor(heroId);
            float radius = heroId == "bombardier" || heroId == "fire_mage" ? 1.1f : 0.88f;
            Play(upgradePrefab != null ? upgradePrefab : hitPrefab, pos, color, 1.25f);
            PlayImpactRing(pos, color, radius, 0.34f, 0.13f);
        }

        public void PlayAbilityTrace(Vector3 start, Vector3 end, string heroId, float width = 0.1f)
        {
            LineRenderer trace = GetAbilityTrace();
            Color color = GetHeroColor(heroId);
            trace.startColor = color;
            Color endColor = color;
            endColor.a = 0.65f;
            trace.endColor = endColor;
            trace.startWidth = width;
            trace.endWidth = width * 0.45f;

            if (heroId == "electric_engineer")
            {
                int segments = 5;
                trace.positionCount = segments + 1;
                trace.SetPosition(0, start);
                Vector3 dir = end - start;
                Vector3 normal = Vector3.Cross(dir, Vector3.up).normalized;
                if (normal.sqrMagnitude < 0.01f) normal = Vector3.up;

                for (int i = 1; i < segments; i++)
                {
                    float t = (float)i / segments;
                    Vector3 point = Vector3.Lerp(start, end, t);
                    float offsetScale = 0.25f;
                    float offset = Random.Range(-offsetScale, offsetScale);
                    Vector3 perpendicular = (i % 2 == 0 ? 1f : -1f) * normal * offset;
                    perpendicular += Vector3.up * Random.Range(-0.1f, 0.1f);
                    trace.SetPosition(i, point + perpendicular);
                }
                trace.SetPosition(segments, end);
            }
            else
            {
                trace.positionCount = 2;
                trace.SetPosition(0, start);
                trace.SetPosition(1, end);
            }

            trace.enabled = true;
            StartCoroutine(ReturnAbilityTrace(trace));
        }

        public static Color GetHeroColor(string heroId)
        {
            switch (heroId)
            {
                case "archer": return new Color(0.45f, 0.95f, 0.3f, 1f);
                case "bombardier": return new Color(1f, 0.48f, 0.1f, 1f);
                case "frost_mage": return new Color(0.25f, 0.85f, 1f, 1f);
                case "fire_mage": return new Color(1f, 0.2f, 0.04f, 1f);
                case "electric_engineer": return new Color(0.12f, 0.72f, 1f, 1f);
                case "sniper": return new Color(0.82f, 0.32f, 1f, 1f);
                default: return Color.white;
            }
        }

        // ----------------------------------------------------------- Event hooks

        private void OnEnemyKilled(Enemy enemy, int gold)
        {
            Vector3 pos = enemy.transform.position;
            Play(deathPrefab, pos);
            Play(goldPrefab, pos + Vector3.up * 0.5f);
        }

        private void OnCastleHit(int damage)
        {
            if (castle != null)
            {
                Play(castleHitPrefab, castle.position + Vector3.up * 1f);
            }

            if (castleFlashRoutine != null)
            {
                StopCoroutine(castleFlashRoutine);
            }
            castleFlashRoutine = StartCoroutine(FlashCastleWall());

            if (CameraRig.Instance != null)
            {
                CameraRig.Instance.Shake(0.4f);
            }
        }

        private void OnStateChanged(GameState state)
        {
            Vector3 pos = castle != null ? castle.position + Vector3.up * 3f : Vector3.up * 3f;
            if (state == GameState.Victory)
            {
                StartCoroutine(PlayVictorySequence(pos));
            }
            else if (state == GameState.Defeat)
            {
                StartCoroutine(PlayDefeatSequence(pos));
            }
        }

        private void OnWaveStarted(int waveNumber, WaveData wave)
        {
            WaveManager waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null && waveNumber == waveManager.TotalWaves)
            {
                // Boss wave started! Play dramatic entrance presentation
                GameObject spawnPoint = GameObject.Find("SpawnPoint");
                Vector3 spawnPos = spawnPoint != null ? spawnPoint.transform.position : new Vector3(0f, 0f, 15f);

                Play(explosionPrefab, spawnPos, new Color(0.9f, 0.15f, 0.15f, 1f), 2.5f);
                PlayImpactRing(spawnPos, new Color(0.9f, 0.15f, 0.15f, 1f), 4.5f, 0.8f, 0.4f);

                if (CameraRig.Instance != null)
                {
                    CameraRig.Instance.Shake(0.8f);
                }
            }
        }

        private IEnumerator FlashCastleWall()
        {
            if (castleRenderers == null)
            {
                GameObject wall = GameObject.Find("CastleWall") ?? GameObject.Find("CastleWall_Stone");
                GameObject gate = GameObject.Find("CastleGate_Wood");
                List<Renderer> rendList = new List<Renderer>();
                if (wall != null) rendList.AddRange(wall.GetComponentsInChildren<Renderer>());
                if (gate != null) rendList.AddRange(gate.GetComponentsInChildren<Renderer>());

                castleRenderers = rendList.ToArray();
                castleBaseColors = new Color[castleRenderers.Length];
                for (int i = 0; i < castleRenderers.Length; i++)
                {
                    Material shared = castleRenderers[i].sharedMaterial;
                    castleBaseColors[i] = shared != null && shared.HasProperty("_BaseColor")
                        ? shared.GetColor("_BaseColor")
                        : Color.white;
                }
            }

            if (castleRenderers == null || castleRenderers.Length == 0)
            {
                yield break;
            }

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            float duration = 0.25f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float f = Mathf.Clamp01(1f - (elapsed / duration));
                for (int i = 0; i < castleRenderers.Length; i++)
                {
                    if (castleRenderers[i] == null) continue;
                    castleRenderers[i].GetPropertyBlock(mpb);
                    mpb.SetColor("_BaseColor", Color.Lerp(castleBaseColors[i], new Color(1f, 0.2f, 0.2f, 1f), f * 0.55f));
                    castleRenderers[i].SetPropertyBlock(mpb);
                }
                yield return null;
            }

            // Restore base colors
            for (int i = 0; i < castleRenderers.Length; i++)
            {
                if (castleRenderers[i] == null) continue;
                castleRenderers[i].SetPropertyBlock(null);
            }
        }

        private IEnumerator PlayVictorySequence(Vector3 center)
        {
            // Spawn initial victory flag/ring
            Play(victoryPrefab, center, null, 1f, true);

            // Multiple celebratory fireworks sequences
            for (int i = 0; i < 5; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-5f, 5f), Random.Range(1f, 4f), Random.Range(-2f, 2f));
                Color fwColor = GetHeroColor(i == 0 ? "archer" : i == 1 ? "fire_mage" : i == 2 ? "frost_mage" : i == 3 ? "electric_engineer" : "sniper");
                Play(victoryPrefab, center + offset, fwColor * Random.Range(0.8f, 1.2f), Random.Range(1f, 1.5f), true);
                yield return new WaitForSecondsRealtime(0.45f);
            }
        }

        private IEnumerator PlayDefeatSequence(Vector3 center)
        {
            if (CameraRig.Instance != null)
            {
                CameraRig.Instance.Shake(0.9f);
            }

            // Play consecutive heavy dark explosions along the castle wall
            float[] wallX = { -5.5f, -3.3f, -1.1f, 1.1f, 3.3f, 5.5f };
            foreach (float x in wallX)
            {
                Vector3 pos = new Vector3(x, 1f, -4.5f);
                Play(defeatPrefab, pos, new Color(0.18f, 0.18f, 0.18f, 1f), 1.6f, true);
                yield return new WaitForSecondsRealtime(0.15f);
            }
        }

        // ------------------------------------------------------ Status Effects Pooling

        public ParticleSystem GetStatusEffectParticle(StatusEffectType type, Transform parent, bool isFreeze)
        {
            if (!statusParticlePools.TryGetValue(type, out Queue<ParticleSystem> pool))
            {
                pool = new Queue<ParticleSystem>();
                statusParticlePools[type] = pool;
            }

            ParticleSystem ps = null;
            while (pool.Count > 0)
            {
                ps = pool.Dequeue();
                if (ps != null)
                {
                    break;
                }
            }

            if (ps == null)
            {
                GameObject prefab = null;
                switch (type)
                {
                    case StatusEffectType.Slow:
                        prefab = frostPrefab;
                        break;
                    case StatusEffectType.Burn:
                        prefab = explosionPrefab;
                        break;
                    case StatusEffectType.Shock:
                        prefab = hitPrefab;
                        break;
                    case StatusEffectType.Stun:
                        prefab = hitPrefab;
                        break;
                    default:
                        prefab = hitPrefab;
                        break;
                }

                if (prefab == null)
                {
                    return null;
                }

                GameObject go = Instantiate(prefab, transform);
                ps = go.GetComponent<ParticleSystem>();
                if (!prefabDefaults.ContainsKey(prefab))
                {
                    prefabDefaults[prefab] = new EffectDefaults
                    {
                        startColor = ps.main.startColor,
                        scale = go.transform.localScale
                    };
                }
            }

            ps.transform.SetParent(parent, false);
            ps.transform.localPosition = new Vector3(0f, type == StatusEffectType.Stun ? 1.5f : 0.5f, 0f);
            ps.transform.localRotation = Quaternion.identity;
            ps.transform.localScale = Vector3.one * (type == StatusEffectType.Stun ? 0.35f : 0.5f);
            ps.gameObject.SetActive(true);

            PooledParticleState poolState = PooledParticleState.GetOrCreate(ps);
            poolState.PrepareForPlay(false);
            ConfigureStatusParticleSystem(ps, type, isFreeze);
            ps.Play();
            return ps;
        }

        public void ReturnStatusEffectParticle(StatusEffectType type, ParticleSystem ps)
        {
            if (ps == null) return;
            PooledParticleState poolState = PooledParticleState.GetOrCreate(ps);
            if (!poolState.TryReturnCurrent(transform))
            {
                return;
            }

            if (!statusParticlePools.TryGetValue(type, out Queue<ParticleSystem> pool))
            {
                pool = new Queue<ParticleSystem>();
                statusParticlePools[type] = pool;
            }
            pool.Enqueue(ps);
        }

        private void ConfigureStatusParticleSystem(ParticleSystem ps, StatusEffectType type, bool isFreeze)
        {
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startSize = type == StatusEffectType.Stun ? 0.16f : 0.22f;
            main.startLifetime = type == StatusEffectType.Stun ? 0.9f : 0.7f;

            var emission = ps.emission;
            emission.rateOverTime = type == StatusEffectType.Stun ? 5f : isFreeze ? 9f : 4f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = type == StatusEffectType.Stun ? ParticleSystemShapeType.Circle : ParticleSystemShapeType.Box;
            if (type == StatusEffectType.Stun)
            {
                shape.radius = 0.35f;
                shape.arc = 360f;
            }
            else
            {
                shape.scale = new Vector3(0.5f, 0.8f, 0.5f);
            }

            switch (type)
            {
                case StatusEffectType.Slow:
                    main.startColor = isFreeze
                        ? new ParticleSystem.MinMaxGradient(new Color(0.12f, 0.68f, 1f, 0.85f))
                        : new ParticleSystem.MinMaxGradient(new Color(0.42f, 0.88f, 1f, 0.72f));
                    break;
                case StatusEffectType.Burn:
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.34f, 0.04f, 0.82f));
                    break;
                case StatusEffectType.Shock:
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.94f, 0.16f, 0.85f));
                    break;
                case StatusEffectType.Stun:
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.92f, 0.92f, 0.68f, 0.65f));
                    break;
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
            Play(prefab, position, color, scale, false);
        }

        private void Play(GameObject prefab, Vector3 position, Color? color, float scale, bool useUnscaledTime)
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
            PooledParticleState poolState = PooledParticleState.GetOrCreate(instance);
            int activationId = poolState.PrepareForPlay(useUnscaledTime);
            ParticleSystem.MainModule main = instance.main;
            main.startColor = color.HasValue
                ? new ParticleSystem.MinMaxGradient(color.Value)
                : defaults.startColor;
            instance.Play();
            StartCoroutine(ReturnWhenDone(prefab, instance, activationId, useUnscaledTime));
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

        private LineRenderer GetAbilityTrace()
        {
            if (abilityTracePool.Count > 0)
            {
                return abilityTracePool.Dequeue();
            }

            GameObject go = new GameObject("HeroAbilityTrace");
            go.transform.SetParent(transform, false);
            LineRenderer trace = go.AddComponent<LineRenderer>();
            trace.useWorldSpace = true;
            trace.positionCount = 2;
            trace.alignment = LineAlignment.View;
            trace.textureMode = LineTextureMode.Stretch;
            trace.numCapVertices = 2;
            trace.numCornerVertices = 2;
            trace.shadowCastingMode = ShadowCastingMode.Off;
            trace.receiveShadows = false;
            trace.sharedMaterial = GetAbilityTraceMaterial();
            trace.enabled = false;
            return trace;
        }

        private Material GetAbilityTraceMaterial()
        {
            if (abilityTraceMaterial != null)
            {
                return abilityTraceMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            abilityTraceMaterial = new Material(shader)
            {
                name = "Runtime Hero Ability Trace",
                renderQueue = (int)RenderQueue.Transparent
            };
            if (abilityTraceMaterial.HasProperty("_BaseColor"))
            {
                abilityTraceMaterial.SetColor("_BaseColor", Color.white);
            }
            if (abilityTraceMaterial.HasProperty("_Surface"))
            {
                abilityTraceMaterial.SetFloat("_Surface", 1f);
            }
            if (abilityTraceMaterial.HasProperty("_ZWrite"))
            {
                abilityTraceMaterial.SetFloat("_ZWrite", 0f);
            }
            return abilityTraceMaterial;
        }

        private IEnumerator ReturnAbilityTrace(LineRenderer trace)
        {
            yield return new WaitForSeconds(0.14f);
            if (trace == null)
            {
                yield break;
            }

            trace.enabled = false;
            abilityTracePool.Enqueue(trace);
        }

        private IEnumerator ReturnWhenDone(GameObject prefab, ParticleSystem instance, int activationId, bool useUnscaledTime)
        {
            ParticleSystem.MainModule main = instance.main;
            float life = main.duration + main.startLifetime.constantMax;
            if (useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(life);
            }
            else
            {
                yield return new WaitForSeconds(life);
            }

            if (instance == null)
            {
                yield break;
            }

            PooledParticleState poolState = PooledParticleState.GetOrCreate(instance);
            if (!poolState.TryReturn(activationId, transform))
            {
                yield break;
            }

            if (!pools.TryGetValue(prefab, out Queue<ParticleSystem> pool))
            {
                pool = new Queue<ParticleSystem>();
                pools[prefab] = pool;
            }

            pool.Enqueue(instance);
        }



        private IEnumerator AnimateImpactRing(LineRenderer ring, Color color, float radius, float duration, float width)
        {
            float elapsed = 0f;
            while (elapsed < duration && ring != null)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float currentRadius = Mathf.Lerp(radius * 0.16f, radius, t);
                Color faded = color;
                faded.a = 1f - t;
                ring.startColor = faded;
                ring.endColor = faded;
                ring.startWidth = Mathf.Lerp(width, 0.01f, t);
                ring.endWidth = ring.startWidth;

                for (int i = 0; i < ring.positionCount; i++)
                {
                    float angle = (Mathf.PI * 2f * i) / ring.positionCount;
                    ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * currentRadius, 0f, Mathf.Sin(angle) * currentRadius));
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (ring != null)
            {
                ring.enabled = false;
                impactRingPool.Enqueue(ring);
            }

            activeImpactRings = Mathf.Max(0, activeImpactRings - 1);
        }



        private LineRenderer GetImpactRing()
        {
            if (impactRingPool.Count > 0)
            {
                return impactRingPool.Dequeue();
            }

            GameObject go = new GameObject("HeroImpactRing");
            go.transform.SetParent(transform, false);
            LineRenderer ring = go.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 32;
            ring.alignment = LineAlignment.View;
            ring.textureMode = LineTextureMode.Stretch;
            ring.numCapVertices = 2;
            ring.numCornerVertices = 2;
            ring.shadowCastingMode = ShadowCastingMode.Off;
            ring.receiveShadows = false;
            ring.sharedMaterial = GetAbilityTraceMaterial();
            ring.enabled = false;
            return ring;
        }



        private void PlayImpactRing(Vector3 position, Color color, float radius, float duration, float width)
        {
            if (activeImpactRings >= MaxImpactRings)
            {
                return;
            }

            LineRenderer ring = GetImpactRing();
            ring.transform.position = position + Vector3.up * 0.08f;
            ring.startColor = color;
            ring.endColor = color;
            ring.startWidth = width;
            ring.endWidth = width;
            ring.enabled = true;
            activeImpactRings++;
            StartCoroutine(AnimateImpactRing(ring, color, radius, duration, width));
        }
}
}
