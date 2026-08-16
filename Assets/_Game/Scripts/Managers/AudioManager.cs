using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Stonehold
{
    /// <summary>
    /// Persistent audio system. Bootstraps itself once (no per-scene wiring), plays
    /// looping music and pooled one-shot SFX, and mixes three buses — Master, Music,
    /// SFX — whose 0..1 levels are exposed to the settings UI and saved to PlayerPrefs.
    /// Auto-hooks enemy death, gold, castle damage and victory/defeat music from
    /// gameplay events. Clips come from a SoundLibrary asset (data-driven).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const int SfxVoices = 10;
        private const string PrefMaster = "vol_master";
        private const string PrefMusic = "vol_music";
        private const string PrefSfx = "vol_sfx";

        [SerializeField] private SoundLibrary library;

        private AudioSource musicSource;
        private AudioSource[] sfxSources;
        private int sfxIndex;
        private readonly Dictionary<AudioClip, float> lastPlayTime = new Dictionary<AudioClip, float>();

        private float masterVolume = 1f;
        private float musicVolume = 0.6f;
        private float sfxVolume = 0.9f;

        private Castle hookedCastle;
        private GameManager hookedGame;
        private WaveManager hookedWaves;

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
            {
                return;
            }

            AudioManager prefab = Resources.Load<AudioManager>("AudioManager");
            if (prefab != null)
            {
                Instantiate(prefab).name = "AudioManager";
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildSources();
            LoadPrefs();
            ApplyVolumes();

            Enemy.AnyKilled += OnEnemyKilled;
            StatusEffectController.OnElementalReaction += OnElementalReaction;
            SceneManager.sceneLoaded += OnSceneLoaded;
            HookScene();

            // sceneLoaded does not fire for the scene already open when we bootstrap,
            // so kick off the ambient track for the current scene here.
            if (library != null)
            {
                PlayMusic(library.musicGameplay, true);
            }
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            Enemy.AnyKilled -= OnEnemyKilled;
            StatusEffectController.OnElementalReaction -= OnElementalReaction;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Unhook();
            Instance = null;
        }

        private void BuildSources()
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;

            sfxSources = new AudioSource[SfxVoices];
            for (int i = 0; i < SfxVoices; i++)
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                sfxSources[i] = src;
            }
        }

        // -------------------------------------------------------------- Volumes

        public void SetMasterVolume(float v) { masterVolume = Mathf.Clamp01(v); ApplyVolumes(); Save(); }
        public void SetMusicVolume(float v) { musicVolume = Mathf.Clamp01(v); ApplyVolumes(); Save(); }
        public void SetSfxVolume(float v) { sfxVolume = Mathf.Clamp01(v); ApplyVolumes(); Save(); }

        private void ApplyVolumes()
        {
            AudioListener.volume = masterVolume;
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }

        private void LoadPrefs()
        {
            masterVolume = PlayerPrefs.GetFloat(PrefMaster, 1f);
            musicVolume = PlayerPrefs.GetFloat(PrefMusic, 0.6f);
            sfxVolume = PlayerPrefs.GetFloat(PrefSfx, 0.9f);
        }

        private void Save()
        {
            PlayerPrefs.SetFloat(PrefMaster, masterVolume);
            PlayerPrefs.SetFloat(PrefMusic, musicVolume);
            PlayerPrefs.SetFloat(PrefSfx, sfxVolume);
        }

        // ------------------------------------------------------------- Playback

        public void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
        {
            if (clip == null || sfxSources == null)
            {
                return;
            }

            // Prevent spam: if the same clip is played within 0.05 seconds (50ms), skip it!
            if (lastPlayTime.TryGetValue(clip, out float lastTime))
            {
                if (Time.time - lastTime < 0.05f)
                {
                    return;
                }
            }
            lastPlayTime[clip] = Time.time;

            AudioSource src = sfxSources[sfxIndex];
            sfxIndex = (sfxIndex + 1) % SfxVoices;
            src.pitch = pitch;
            src.PlayOneShot(clip, sfxVolume * volumeScale);
        }

        public void PlayButton()
        {
            if (library != null)
            {
                PlaySfx(library.button, 0.8f);
            }
        }

        public void PlayHeroShot(string heroId)
        {
            if (library == null) return;
            float pitch = Random.Range(0.9f, 1.1f);
            switch (heroId)
            {
                case "archer":
                    if (library.arrowHit != null)
                    {
                        PlaySfx(library.arrowHit, 0.35f, pitch * 1.35f);
                    }
                    break;
                case "bombardier":
                    if (library.cannonExplosion != null)
                    {
                        PlaySfx(library.cannonExplosion, 0.42f, pitch * 0.75f);
                    }
                    break;
                case "frost_mage":
                    if (library.frostHit != null)
                    {
                        PlaySfx(library.frostHit, 0.38f, pitch * 1.4f);
                    }
                    break;
                case "fire_mage":
                    if (library.cannonExplosion != null)
                    {
                        PlaySfx(library.cannonExplosion, 0.32f, pitch * 1.6f);
                    }
                    break;
                case "electric_engineer":
                    if (library.button != null)
                    {
                        PlaySfx(library.button, 0.35f, pitch * 1.8f);
                    }
                    break;
                case "sniper":
                    if (library.arrowHit != null)
                    {
                        PlaySfx(library.arrowHit, 0.75f, pitch * 0.65f);
                    }
                    break;
                case "plague_doctor":
                    if (library.frostHit != null)
                    {
                        PlaySfx(library.frostHit, 0.40f, pitch * 1.55f);
                    }
                    break;
                case "radiant_paladin":
                    if (library.upgrade != null)
                    {
                        PlaySfx(library.upgrade, 0.45f, pitch * 1.25f);
                    }
                    break;
                case "shadow_assassin":
                    if (library.arrowHit != null)
                    {
                        PlaySfx(library.arrowHit, 0.45f, pitch * 1.65f);
                    }
                    break;
                case "storm_druid":
                    if (library.frostHit != null)
                    {
                        PlaySfx(library.frostHit, 0.45f, pitch * 1.15f);
                    }
                    break;
            }
        }


        public void PlayHeroImpact(string heroId, bool isAbility = false)
        {
            if (library == null) return;

            float volume = isAbility ? 1.0f : 0.6f;
            float pitch = Random.Range(0.9f, 1.1f);

            switch (heroId)
            {
                case "archer":
                    if (library.arrowHit != null)
                    {
                        PlaySfx(library.arrowHit, volume * 0.8f, pitch * 1.05f);
                    }
                    break;
                case "bombardier":
                    if (library.cannonExplosion != null)
                    {
                        PlaySfx(library.cannonExplosion, volume * 0.55f, pitch * 0.85f);
                    }
                    break;
                case "frost_mage":
                    if (library.frostHit != null)
                    {
                        PlaySfx(library.frostHit, volume * 0.75f, pitch * 1.15f);
                    }
                    break;
                case "fire_mage":
                    if (library.cannonExplosion != null)
                    {
                        PlaySfx(library.cannonExplosion, volume * 0.5f, pitch * 1.25f);
                    }
                    break;
                case "electric_engineer":
                    // Play a quick high-pitched spark sound
                    if (library.button != null)
                    {
                        PlaySfx(library.button, volume * 0.45f, pitch * 1.6f);
                    }
                    break;
                case "sniper":
                    // Loud heavy arrow snap/sniper hit
                    if (library.arrowHit != null)
                    {
                        PlaySfx(library.arrowHit, volume * 0.95f, pitch * 0.72f);
                    }
                    break;
                case "plague_doctor":
                    if (library.frostHit != null)
                    {
                        PlaySfx(library.frostHit, volume * 0.7f, pitch * 1.3f);
                    }
                    break;
                case "radiant_paladin":
                    if (library.cannonExplosion != null)
                    {
                        PlaySfx(library.cannonExplosion, volume * 0.75f, pitch * 1.1f);
                    }
                    break;
                case "shadow_assassin":
                    if (library.arrowHit != null)
                    {
                        PlaySfx(library.arrowHit, volume * 0.9f, pitch * 1.4f);
                    }
                    break;
                case "storm_druid":
                    if (library.cannonExplosion != null)
                    {
                        PlaySfx(library.cannonExplosion, volume * 0.75f, pitch * 1.35f);
                    }
                    break;
                default:
                    // Fallback
                    PlayImpact(false, false);
                    break;
            }
        }

        public void PlayAbilityCast(string heroId)
        {
            if (library == null) return;
            float pitch = Random.Range(0.95f, 1.05f);
            switch (heroId)
            {
                case "fire_mage":
                    PlaySfx(library.cannonExplosion, 0.7f, pitch * 1.5f);
                    break;
                case "frost_mage":
                    PlaySfx(library.frostHit, 0.8f, pitch * 0.9f);
                    break;
                case "electric_engineer":
                    PlaySfx(library.upgrade, 0.6f, pitch * 1.4f);
                    break;
                case "bombardier":
                    PlaySfx(library.cannonExplosion, 0.5f, pitch * 0.7f);
                    break;
                case "sniper":
                    PlaySfx(library.upgrade, 0.7f, pitch * 1.8f);
                    break;
                case "archer":
                    PlaySfx(library.arrowHit, 0.8f, pitch * 1.3f);
                    break;
                case "plague_doctor":
                    PlaySfx(library.cannonExplosion, 0.65f, pitch * 1.4f);
                    break;
                case "radiant_paladin":
                    PlaySfx(library.upgrade, 0.85f, pitch * 1.3f);
                    break;
                case "shadow_assassin":
                    PlaySfx(library.arrowHit, 0.95f, pitch * 1.6f);
                    break;
                case "storm_druid":
                    PlaySfx(library.frostHit, 0.9f, pitch * 1.1f);
                    break;
            }
        }

        public void PlayCastleAbilitySfx(string abilityId)
        {
            if (library == null) return;
            switch (abilityId)
            {
                case "castle_meteor_strike":
                    PlaySfx(library.cannonExplosion, 1.0f, 0.65f);
                    PlaySfx(library.castleDamage, 0.7f, 0.85f);
                    break;
                case "castle_kinetic_shield":
                    PlaySfx(library.upgrade, 0.9f, 1.2f);
                    break;
                case "castle_gold_overflow":
                    PlaySfx(library.gold, 1.0f, 1.1f);
                    break;
                case "castle_overcharge":
                    PlaySfx(library.upgrade, 1.0f, 1.6f);
                    break;
            }
        }

        public void PlayLevelUp()
        {
            if (library != null)
            {
                PlaySfx(library.upgrade, 1.0f, 0.95f);
                PlaySfx(library.gold, 0.8f, 1.15f);
            }
        }

        public void PlayImpact(bool splash, bool frost)
        {
            if (library == null)
            {
                return;
            }

            if (splash)
            {
                PlaySfx(library.cannonExplosion);
            }
            else if (frost)
            {
                PlaySfx(library.frostHit);
            }
            else
            {
                AudioClip clip = library.arrowHit != null ? library.arrowHit : library.button;
                if (clip != null)
                {
                    float targetPitch = library.arrowHit != null ? Random.Range(0.9f, 1.15f) : Random.Range(1.3f, 1.6f);
                    float targetVol = library.arrowHit != null ? 0.6f : 0.4f;
                    PlaySfx(clip, targetVol, targetPitch);
                }
            }
        }

        public void PlayPlace() { if (library != null) PlaySfx(library.place); }
        public void PlayUpgrade() { if (library != null) PlaySfx(library.upgrade); }

        private Coroutine musicCrossfadeRoutine;

        public void PlayMusic(AudioClip clip, bool loop)
        {
            if (musicSource == null || clip == null)
            {
                return;
            }

            if (musicCrossfadeRoutine != null)
            {
                StopCoroutine(musicCrossfadeRoutine);
                musicCrossfadeRoutine = null;
            }

            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = masterVolume * musicVolume;
            musicSource.Play();
        }

        public void FadeMusicTo(AudioClip newClip, float duration = 1.0f, bool loop = true)
        {
            if (musicSource == null) return;
            if (musicCrossfadeRoutine != null)
            {
                StopCoroutine(musicCrossfadeRoutine);
            }
            musicCrossfadeRoutine = StartCoroutine(CrossfadeMusicRoutine(newClip, duration, loop));
        }

        private System.Collections.IEnumerator CrossfadeMusicRoutine(AudioClip newClip, float duration, bool loop)
        {
            if (musicSource == null) yield break;
            float startVol = musicSource.volume;
            float halfDuration = Mathf.Max(0.05f, duration * 0.5f);

            // Fade out
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / halfDuration);
                yield return null;
            }

            musicSource.volume = 0f;
            if (newClip != null)
            {
                musicSource.clip = newClip;
                musicSource.loop = loop;
                musicSource.Play();

                // Fade in
                elapsed = 0f;
                float targetVol = masterVolume * musicVolume;
                while (elapsed < halfDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    musicSource.volume = Mathf.Lerp(0f, targetVol, elapsed / halfDuration);
                    yield return null;
                }
                musicSource.volume = targetVol;
            }
            else
            {
                musicSource.Stop();
            }
            musicCrossfadeRoutine = null;
        }

        private void OnElementalReaction(ElementalReactionType reaction, Vector3 worldPos, string heroId)
        {
            PlayElementalReactionSfx(reaction);
        }

        public void PlayElementalReactionSfx(ElementalReactionType reaction)
        {
            if (library == null) return;

            switch (reaction)
            {
                case ElementalReactionType.ThermalShock:
                    if (library.frostHit != null)
                    {
                        PlaySfx(library.frostHit, 0.65f, 1.4f);
                    }
                    if (library.cannonExplosion != null)
                    {
                        PlaySfx(library.cannonExplosion, 0.45f, 1.8f);
                    }
                    break;
                case ElementalReactionType.Overload:
                    if (library.button != null)
                    {
                        PlaySfx(library.button, 0.7f, 1.9f);
                    }
                    if (library.cannonExplosion != null)
                    {
                        PlaySfx(library.cannonExplosion, 0.5f, 1.5f);
                    }
                    break;
                case ElementalReactionType.Shatter:
                    if (library.arrowHit != null)
                    {
                        PlaySfx(library.arrowHit, 0.8f, 1.5f);
                    }
                    if (library.frostHit != null)
                    {
                        PlaySfx(library.frostHit, 0.6f, 1.6f);
                    }
                    break;
                case ElementalReactionType.CorrosiveBlast:
                    if (library.cannonExplosion != null) PlaySfx(library.cannonExplosion, 0.7f, 1.3f);
                    if (library.frostHit != null) PlaySfx(library.frostHit, 0.5f, 0.7f);
                    break;
                case ElementalReactionType.Neurotoxin:
                    if (library.button != null) PlaySfx(library.button, 0.8f, 1.7f);
                    if (library.arrowHit != null) PlaySfx(library.arrowHit, 0.7f, 1.4f);
                    break;
                case ElementalReactionType.BrittleBlight:
                    if (library.frostHit != null) PlaySfx(library.frostHit, 0.8f, 1.2f);
                    if (library.enemyDeath != null) PlaySfx(library.enemyDeath, 0.5f, 1.5f);
                    break;
            }
        }

        // --------------------------------------------------------------- Hooks

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            HookScene();

            if (library == null)
            {
                return;
            }

            // Both scenes share the ambient loop; victory/defeat override it later.
            PlayMusic(library.musicGameplay, true);
        }

        private void HookScene()
        {
            Unhook();

            hookedCastle = FindFirstObjectByType<Castle>();
            if (hookedCastle != null)
            {
                hookedCastle.DamageTaken += OnCastleDamaged;
            }

            hookedGame = FindFirstObjectByType<GameManager>();
            if (hookedGame != null)
            {
                hookedGame.StateChanged += OnStateChanged;
            }

            hookedWaves = FindFirstObjectByType<WaveManager>();
            if (hookedWaves != null)
            {
                hookedWaves.WaveStarted += OnWaveStarted;
                hookedWaves.WaveCleared += OnWaveCleared;
            }
        }

        private void Unhook()
        {
            if (hookedCastle != null)
            {
                hookedCastle.DamageTaken -= OnCastleDamaged;
                hookedCastle = null;
            }

            if (hookedGame != null)
            {
                hookedGame.StateChanged -= OnStateChanged;
                hookedGame = null;
            }

            if (hookedWaves != null)
            {
                hookedWaves.WaveStarted -= OnWaveStarted;
                hookedWaves.WaveCleared -= OnWaveCleared;
                hookedWaves = null;
            }
        }

        private void OnWaveStarted(int waveNumber, WaveData wave)
        {
            if (library != null)
            {
                if (hookedWaves != null && waveNumber == hookedWaves.TotalWaves)
                {
                    // Boss wave started! Deep bass rumble SFX and dynamic crossfade to boss BGM
                    PlaySfx(library.cannonExplosion, 1.0f, 0.45f);
                    PlaySfx(library.castleDamage, 0.8f, 0.55f);
                    if (library.musicBoss != null)
                    {
                        FadeMusicTo(library.musicBoss, 1.2f, true);
                    }
                }
                else
                {
                    AudioClip clip = library.waveStart != null ? library.waveStart : library.upgrade;
                    PlaySfx(clip, 1.0f);
                    if (waveNumber == 1 && library.musicGameplay != null)
                    {
                        FadeMusicTo(library.musicGameplay, 0.8f, true);
                    }
                }
            }
        }

        private void OnWaveCleared(int waveNumber, WaveData wave)
        {
            if (library != null)
            {
                AudioClip clip = library.waveClear != null ? library.waveClear : library.gold;
                PlaySfx(clip, 1.0f);
            }
        }

        private void OnEnemyKilled(Enemy enemy, int gold)
        {
            if (library == null)
            {
                return;
            }

            PlaySfx(library.enemyDeath, 0.9f, Random.Range(0.95f, 1.08f));
            PlaySfx(library.gold, 0.5f, Random.Range(1.0f, 1.12f));
        }

        private bool isLowHealthTension;
        private Coroutine tensionPitchRoutine;

        public void SetLowHealthTension(bool active)
        {
            if (isLowHealthTension == active) return;
            isLowHealthTension = active;

            if (tensionPitchRoutine != null) StopCoroutine(tensionPitchRoutine);
            tensionPitchRoutine = StartCoroutine(SmoothPitchTension(active ? 1.08f : 1.0f));

            if (active && library != null && library.castleDamage != null)
            {
                PlaySfx(library.castleDamage, 0.7f, 0.5f);
            }
        }

        private System.Collections.IEnumerator SmoothPitchTension(float targetPitch)
        {
            if (musicSource == null) yield break;
            float startPitch = musicSource.pitch;
            float elapsed = 0f;
            float dur = 0.6f;

            while (elapsed < dur)
            {
                if (musicSource == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                musicSource.pitch = Mathf.Lerp(startPitch, targetPitch, elapsed / dur);
                yield return null;
            }

            if (musicSource != null) musicSource.pitch = targetPitch;
            tensionPitchRoutine = null;
        }

        private void OnCastleDamaged(int damage)
        {
            if (library != null)
            {
                PlaySfx(library.castleDamage);
            }

            if (hookedCastle != null && hookedCastle.MaxHealth > 0)
            {
                float ratio = (float)hookedCastle.CurrentHealth / hookedCastle.MaxHealth;
                SetLowHealthTension(ratio <= 0.30f);
            }
        }

        private void OnStateChanged(GameState state)
        {
            SetLowHealthTension(false);
            if (library == null)
            {
                return;
            }

            if (state == GameState.Victory)
            {
                PlayMusic(library.musicVictory, false);
            }
            else if (state == GameState.Defeat)
            {
                PlayMusic(library.musicDefeat, false);
            }
        }
    }
}
