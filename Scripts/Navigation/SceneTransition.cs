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
            Instance = this;
        else
            Destroy(gameObject);
    }

    // ✅ Затемнение (Fade Out)
    public IEnumerator FadeOut(float duration)
    {
        if (fadePanel == null) yield break;

        float elapsed = 0f;
        float startAlpha = fadePanel.alpha;

        fadePanel.blocksRaycasts = true;
        fadePanel.interactable = true;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            fadePanel.alpha = Mathf.Lerp(startAlpha, 1f, t);
            yield return null;
        }

        fadePanel.alpha = 1f;
    }

    // ✅ Просветление (Fade In)
    public IEnumerator FadeIn(float duration)
    {
        if (fadePanel == null) yield break;

        float elapsed = 0f;
        float startAlpha = fadePanel.alpha;

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
    }

    // ✅ Затемнение → действие → просветление
    public IEnumerator FadeOutAndIn(float duration, System.Action onMiddleAction = null)
    {
        yield return StartCoroutine(FadeOut(duration));
        yield return new WaitForSeconds(holdDuration);

        onMiddleAction?.Invoke();

        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeIn(duration));
    }

    // ✅ Затемнение → загрузка сцены → просветление
    public IEnumerator TransitionToScene(string sceneName, float duration)
    {
        yield return StartCoroutine(FadeOut(duration));
        yield return new WaitForSeconds(holdDuration);

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);

        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeIn(duration));
    }

    // ✅ Затемнение → загрузка сцены по индексу → просветление
    public IEnumerator TransitionToScene(int sceneIndex, float duration)
    {
        yield return StartCoroutine(FadeOut(duration));
        yield return new WaitForSeconds(holdDuration);

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneIndex);

        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(FadeIn(duration));
    }

    // ✅ Мгновенное затемнение (без анимации)
    public void SetBlack()
    {
        if (fadePanel != null)
        {
            fadePanel.alpha = 1f;
            fadePanel.blocksRaycasts = true;
            fadePanel.interactable = true;
        }
    }

    // ✅ Мгновенное просветление (без анимации)
    public void SetTransparent()
    {
        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;
            fadePanel.blocksRaycasts = false;
            fadePanel.interactable = false;
        }
    }
}
