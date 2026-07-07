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
            SetState(GameState.Defeat);
        }

        private void OnAllWavesCleared()
        {
            SetState(GameState.Victory);
        }

        private void SetState(GameState newState)
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
