using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Stonehold
{
    /// <summary>
    /// Central game flow for a run: owns the GameState, handles pause (Esc or UI),
    /// reacts to defeat (castle destroyed) and victory (all waves cleared), and
    /// restarts the run. Pausing sets Time.timeScale to 0 so gameplay freezes.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        /// <summary>Raised whenever the game state changes.</summary>
        public event Action<GameState> StateChanged;

        /// <summary>Raised whenever the gameplay speed multiplier changes.</summary>
        public event Action<float> GameSpeedChanged;

        public GameState State { get; private set; } = GameState.Playing;

        /// <summary>
        /// Player-selectable gameplay speed multiplier applied while Playing.
        /// Only ever 1x / 1.5x / 2x - 0x is never a selectable speed; freezing is a
        /// side effect of the Paused/LevelUp/Victory/Defeat states, not a speed mode.
        /// </summary>
        public float GameSpeed { get; private set; } = 1f;

        private static readonly float[] SpeedSteps = { 1f, 1.5f, 2f };

        private Castle castle;
        private WaveManager waveManager;
        private bool runResultRecorded;

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            Application.targetFrameRate = 60;
            SaveManager.BeginRunRewardSession();
            runResultRecorded = false;

            if (GetComponent<SceneReferenceValidator>() == null)
            {
                gameObject.AddComponent<SceneReferenceValidator>();
            }
            if (GetComponent<HeroRosterManager>() == null)
            {
                gameObject.AddComponent<HeroRosterManager>();
            }
            if (GetComponent<RunProgressionManager>() == null)
            {
                gameObject.AddComponent<RunProgressionManager>();
            }
            if (GetComponent<StagePresentationController>() == null)
            {
                gameObject.AddComponent<StagePresentationController>();
            }
            if (EnemyPoolManager.Instance == null && FindAnyObjectByType<EnemyPoolManager>() == null)
            {
                gameObject.AddComponent<EnemyPoolManager>();
            }
            if (RunModifierManager.Instance == null)
            {
                GameObject rmGo = new GameObject("RunModifierManager", typeof(RunModifierManager));
                DontDestroyOnLoad(rmGo);
            }
            else
            {
                RunModifierManager.Instance.ClearModifiers();
            }
            if (CardDraftManager.Instance == null)
            {
                GameObject cdGo = new GameObject("CardDraftManager", typeof(CardDraftManager));
                DontDestroyOnLoad(cdGo);
            }
            CardDraftManager.Instance?.ResetForRun();

            if (FindAnyObjectByType<MetaUpgradeManager>() == null)
            {
                GameObject managerGo = new GameObject("MetaUpgradeManager", typeof(MetaUpgradeManager));
                DontDestroyOnLoad(managerGo);
            }
        }

        private void Start()
        {
            castle = FindFirstObjectByType<Castle>();
            waveManager = FindFirstObjectByType<WaveManager>();

            if (castle != null)
            {
                castle.Defeated += OnCastleDefeated;
            }

            if (waveManager != null)
            {
                waveManager.AllWavesCleared += OnAllWavesCleared;
                waveManager.WaveStarted += OnWaveStarted;
                waveManager.WaveCleared += OnWaveCleared;
            }
        }

        private void OnDestroy()
        {
            if (castle != null)
            {
                castle.Defeated -= OnCastleDefeated;
            }

            if (waveManager != null)
            {
                waveManager.AllWavesCleared -= OnAllWavesCleared;
                waveManager.WaveStarted -= OnWaveStarted;
                waveManager.WaveCleared -= OnWaveCleared;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Debug triggers: V for Victory screen, G for Defeat screen
            if (Keyboard.current != null)
            {
                if (Keyboard.current.vKey.wasPressedThisFrame)
                {
                    Debug.Log("[GameManager] Debug Victory triggered via V key.");
                    CompleteRun(true);
                }
                else if (Keyboard.current.gKey.wasPressedThisFrame)
                {
                    Debug.Log("[GameManager] Debug Defeat triggered via G key.");
                    CompleteRun(false);
                }
                else if (Keyboard.current.mKey.wasPressedThisFrame)
                {
                    Debug.Log("[GameManager] Debug: Added 500 Gold via M key.");
                    SaveManager.AddMetaGold(500);
                }
                else if (Keyboard.current.rKey.wasPressedThisFrame)
                {
                    Debug.Log("[GameManager] Debug: Reset all progress via R key.");
                    SaveManager.ResetAll();
                }
            }
#endif
        }

        public void TogglePause()
        {
            if (State == GameState.Playing)
            {
                SetState(GameState.Paused);
            }
            else if (State == GameState.Paused)
            {
                SetState(GameState.Playing);
            }
        }

        /// <summary>
        /// Sets the gameplay speed multiplier. The live clock is only touched while
        /// Playing; Paused/LevelUp/Victory/Defeat stay frozen at timeScale 0 so the
        /// selected speed is remembered but never un-freezes a paused game.
        /// </summary>
        public void SetGameSpeed(float speed)
        {
            GameSpeed = speed;
            if (State == GameState.Playing)
            {
                Time.timeScale = GameSpeed;
            }
            GameSpeedChanged?.Invoke(GameSpeed);
        }

        /// <summary>Cycles 1x -> 1.5x -> 2x -> 1x and returns the new multiplier.</summary>
        public float CycleGameSpeed()
        {
            int index = 0;
            for (int i = 0; i < SpeedSteps.Length; i++)
            {
                if (Mathf.Approximately(SpeedSteps[i], GameSpeed))
                {
                    index = i;
                    break;
                }
            }

            int next = (index + 1) % SpeedSteps.Length;
            SetGameSpeed(SpeedSteps[next]);
            return GameSpeed;
        }

        /// <summary>Reloads the current scene for a fresh run.</summary>
        public void Restart()
        {
            Time.timeScale = 1f;
            EnemyPoolManager.Instance?.DespawnAllActive();
            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeToScene(SceneManager.GetActiveScene().name);
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        /// <summary>Returns to the main menu scene.</summary>
        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            if (SceneFader.Instance != null)
            {
                SceneFader.Instance.FadeToScene("MainMenu");
            }
            else
            {
                SceneManager.LoadScene("MainMenu");
            }
        }

        private void OnCastleDefeated()
        {
            CompleteRun(false);
        }

        private void OnAllWavesCleared()
        {
            CompleteRun(true);
        }

        private void OnWaveStarted(int waveNumber, WaveData wave)
        {
            SaveManager.UpdateBestWave(waveNumber);
        }

        private void OnWaveCleared(int waveNumber, WaveData wave)
        {
            SaveManager.UpdateBestWave(waveNumber);
        }

        public void SetState(GameState newState)
        {
            if (State == newState)
            {
                return;
            }

            State = newState;
            // Restore the player's selected speed on resume; any non-Playing state freezes.
            Time.timeScale = newState == GameState.Playing ? GameSpeed : 0f;
            StateChanged?.Invoke(newState);
        }

        private void CompleteRun(bool victory)
        {
            if (runResultRecorded)
            {
                SetState(victory ? GameState.Victory : GameState.Defeat);
                return;
            }

            runResultRecorded = true;
            EnemyPoolManager.Instance?.DespawnAllActive();

            if (victory)
            {
                SaveManager.RecordWin();
                SaveManager.CompleteStage(SaveManager.SelectedStageIndex);
                SetState(GameState.Victory);
            }
            else
            {
                SaveManager.RecordLoss();
                SetState(GameState.Defeat);
            }
        }
    }
}
