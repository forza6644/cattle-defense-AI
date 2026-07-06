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

        private float masterVolume = 1f;
        private float musicVolume = 0.6f;
        private float sfxVolume = 0.9f;

        private Castle hookedCastle;
        private GameManager hookedGame;

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
        }

        public void PlayPlace() { if (library != null) PlaySfx(library.place); }
        public void PlayUpgrade() { if (library != null) PlaySfx(library.upgrade); }

        public void PlayMusic(AudioClip clip, bool loop)
        {
            if (musicSource == null || clip == null)
            {
                return;
            }

            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = musicVolume;
            musicSource.Play();
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
                hookedCastle.HealthChanged += OnCastleDamaged;
            }

            hookedGame = FindFirstObjectByType<GameManager>();
            if (hookedGame != null)
            {
                hookedGame.StateChanged += OnStateChanged;
            }
        }

        private void Unhook()
        {
            if (hookedCastle != null)
            {
                hookedCastle.HealthChanged -= OnCastleDamaged;
                hookedCastle = null;
            }

            if (hookedGame != null)
            {
                hookedGame.StateChanged -= OnStateChanged;
                hookedGame = null;
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

        private void OnCastleDamaged()
        {
            if (library != null)
            {
                PlaySfx(library.castleDamage);
            }
        }

        private void OnStateChanged(GameState state)
        {
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
