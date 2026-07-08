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

        public GameState State { get; private set; } = GameState.Playing;

        private Castle castle;
        private WaveManager waveManager;

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            Application.targetFrameRate = 60;
            if (GetComponent<RunProgressionManager>() == null)
            {
                gameObject.AddComponent<RunProgressionManager>();
            }
            if (GetComponent<RunModifierManager>() == null)
            {
                gameObject.AddComponent<RunModifierManager>();
            }
            else
            {
                GetComponent<RunModifierManager>().ClearModifiers();
            }
            if (GetComponent<CardDraftManager>() == null)
            {
                gameObject.AddComponent<CardDraftManager>();
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

            // Debug triggers: V for Victory screen, G for Defeat screen
            if (Keyboard.current != null)
            {
                if (Keyboard.current.vKey.wasPressedThisFrame)
                {
                    Debug.Log("[GameManager] Debug Victory triggered via V key.");
                    SetState(GameState.Victory);
                }
                else if (Keyboard.current.gKey.wasPressedThisFrame)
                {
                    Debug.Log("[GameManager] Debug Defeat triggered via G key.");
                    SetState(GameState.Defeat);
                }
            }
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

        /// <summary>Reloads the current scene for a fresh run.</summary>
        public void Restart()
        {
            Time.timeScale = 1f;
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
            var towers = FindFirstObjectByType<TowerManager>();
            if (towers != null && towers.Config != null && towers.Config.draftRunMode)
            {
                int waveReached = SaveManager.BestWave;
                if (waveReached < 1) waveReached = 1;
                int metaGoldEarned = waveReached * 15;
                SaveManager.AddMetaGold(metaGoldEarned);
                Debug.Log($"Defeat! Highest Wave: {waveReached}. Awarded {metaGoldEarned} Meta Gold.");
            }

            SaveManager.RecordLoss();
            SetState(GameState.Defeat);
        }

        private void OnAllWavesCleared()
        {
            var towers = FindFirstObjectByType<TowerManager>();
            if (towers != null && towers.Config != null && towers.Config.draftRunMode)
            {
                int metaGoldEarned = 300;
                SaveManager.AddMetaGold(metaGoldEarned);
                Debug.Log($"Victory! Awarded {metaGoldEarned} Meta Gold.");
            }

            SaveManager.RecordWin();
            if (SaveManager.SelectedStageIndex == 0)
            {
                SaveManager.CompleteStage1();
            }
            SetState(GameState.Victory);
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
            Time.timeScale = newState == GameState.Playing ? 1f : 0f;
            StateChanged?.Invoke(newState);
        }
    }
}
