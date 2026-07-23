using UnityEngine;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance;

    [Header("Settings")]
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float holdDuration = 0.2f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Logger.Log(LogModule.Navigation, "SceneTransition инициализирован");
        }
        else
        {
            Logger.Log(LogModule.Navigation, "Уничтожение дублирующего SceneTransition");
            Destroy(gameObject);
        }
    }

    public IEnumerator FadeOut(float duration)
    {
        if (fadePanel == null)
        {
            Logger.LogWarning(LogModule.Navigation, "fadePanel не назначен, затемнение невозможно");
            yield break;
        }

        float elapsed = 0f;
        float startAlpha = fadePanel.alpha;

        fadePanel.blocksRaycasts = true;
        fadePanel.interactable = true;

        Logger.Log(LogModule.Navigation, $"Начало затемнения на {duration} секунд");

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            fadePanel.alpha = Mathf.Lerp(startAlpha, 1f, t);
            yield return null;
        }

        fadePanel.alpha = 1f;
        Logger.Log(LogModule.Navigation, "Затемнение завершено");
    }

    public IEnumerator FadeIn(float duration)
    {
        if (fadePanel == null)
        {
            Logger.LogWarning(LogModule.Navigation, "fadePanel не назначен, просветление невозможно");
            yield break;
        }

        float elapsed = 0f;
        float startAlpha = fadePanel.alpha;

        Logger.Log(LogModule.Navigation, $"Начало просветления на {duration} секунд");

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            fadePanel.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        fadePanel.alpha = 0f;
        fadePanel.blocksRaycasts = false;
        fadePanel.interactable = false;

        Logger.Log(LogModule.Navigation, "Просветление завершено");
    }

    public IEnumerator FadeOutAndIn(float duration, System.Action onMiddleAction = null)
    {
        Logger.Log(LogModule.Navigation, "Начало последовательности затемнение-действие-просветление");

        yield return StartCoroutine(FadeOut(duration));
        yield return new WaitForSeconds(holdDuration);

        onMiddleAction?.Invoke();
        Logger.Log(LogModule.Navigation, "Выполнено промежуточное действие");

        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeIn(duration));

        Logger.Log(LogModule.Navigation, "Последовательность затемнение-действие-просветление завершена");
    }

    public IEnumerator TransitionToScene(string sceneName, float duration)
    {
        Logger.Log(LogModule.Navigation, $"Начало перехода на сцену {sceneName}");

        yield return StartCoroutine(FadeOut(duration));
        yield return new WaitForSeconds(holdDuration);

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        Logger.Log(LogModule.Navigation, $"Загружена сцена {sceneName}");

        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeIn(duration));

        Logger.Log(LogModule.Navigation, $"Переход на сцену {sceneName} завершён");
    }

    public IEnumerator TransitionToScene(int sceneIndex, float duration)
    {
        Logger.Log(LogModule.Navigation, $"Начало перехода на сцену с индексом {sceneIndex}");

        yield return StartCoroutine(FadeOut(duration));
        yield return new WaitForSeconds(holdDuration);

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneIndex);
        Logger.Log(LogModule.Navigation, $"Загружена сцена с индексом {sceneIndex}");

        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeIn(duration));

        Logger.Log(LogModule.Navigation, $"Переход на сцену с индексом {sceneIndex} завершён");
    }

    public void SetBlack()
    {
        if (fadePanel != null)
        {
            fadePanel.alpha = 1f;
            fadePanel.blocksRaycasts = true;
            fadePanel.interactable = true;
            Logger.Log(LogModule.Navigation, "Установлен чёрный экран (мгновенно)");
        }
        else
        {
            Logger.LogWarning(LogModule.Navigation, "fadePanel не назначен, установка чёрного экрана невозможна");
        }
    }

    public void SetTransparent()
    {
        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;
            fadePanel.blocksRaycasts = false;
            fadePanel.interactable = false;
            Logger.Log(LogModule.Navigation, "Установлен прозрачный экран (мгновенно)");
        }
        else
        {
            Logger.LogWarning(LogModule.Navigation, "fadePanel не назначен, установка прозрачного экрана невозможна");
        }
    }
}
