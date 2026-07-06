using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Stonehold
{
    /// <summary>
    /// Persistent full-screen fader for smooth scene transitions. Bootstraps itself,
    /// fades in from black on every scene load, and offers FadeToScene so buttons can
    /// fade out before loading. Runs on unscaled time so it still works while paused
    /// or on the timeScale-0 victory/defeat screens.
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        public static SceneFader Instance { get; private set; }

        private const float FadeDuration = 0.4f;

        private CanvasGroup group;
        private bool busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance == null)
            {
                new GameObject("SceneFader").AddComponent<SceneFader>();
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
            Build();
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(Fade(1f, 0f, false));
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
            }
        }

        private void Build()
        {
            GameObject canvasObject = new GameObject("FaderCanvas", typeof(Canvas), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            GameObject imageObject = new GameObject("Black", typeof(Image), typeof(CanvasGroup));
            imageObject.transform.SetParent(canvasObject.transform, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = Color.black;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            group = imageObject.GetComponent<CanvasGroup>();
            group.alpha = 1f;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(Fade(1f, 0f, false));
        }

        /// <summary>Fade to black, then load the given scene.</summary>
        public void FadeToScene(string sceneName)
        {
            if (!busy)
            {
                StartCoroutine(FadeAndLoad(sceneName));
            }
        }

        private IEnumerator FadeAndLoad(string sceneName)
        {
            busy = true;
            group.blocksRaycasts = true;
            yield return Fade(0f, 1f, true);
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        private IEnumerator Fade(float from, float to, bool block)
        {
            if (group == null)
            {
                yield break;
            }

            group.blocksRaycasts = block;
            float t = 0f;
            while (t < FadeDuration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, t / FadeDuration);
                yield return null;
            }

            group.alpha = to;
            group.blocksRaycasts = to > 0.5f;
            busy = false;
        }
    }
}
