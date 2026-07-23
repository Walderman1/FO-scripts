using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

public class FindingOneselfAnimation : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private MenuUIConfig config;

    private class LetterData
    {
        public GameObject letterObject;
        public Light2D light;
        public SpriteRenderer spriteRenderer;
        public Color originalColor;
        public float originalIntensity;
        public bool isFading = false;
        public Coroutine fadeCoroutine;
        public Coroutine flickerCoroutine;
    }

    private List<LetterData> allLetters = new List<LetterData>();
    private SpriteRenderer trixieSpriteRenderer;
    private Light2D trixieLight;
    private Color trixieOriginalColor;
    private float trixieOriginalLightIntensity;

    private bool isAnimationComplete = false;
    private bool isAnimationStopped = false;
    private bool isTrixieComplete = false;
    private Coroutine mainAnimationCoroutine;
    private Coroutine trixieCoroutine;

    private void Start()
    {
        if (config == null)
        {
            config = Resources.Load<MenuUIConfig>("Configs/MenuUIConfig");
            if (config == null)
                Logger.LogError(LogModule.Menu, "MenuUIConfig не найден");
        }

        FindTrixieObject();
        FindAllLetters();
        mainAnimationCoroutine = StartCoroutine(AnimateLetters());

        Logger.Log(LogModule.Menu, "FindingOneselfAnimation запущен");
    }

    public void CompleteAnimationImmediate()
    {
        if (isAnimationComplete) return;

        isAnimationStopped = true;

        if (mainAnimationCoroutine != null)
        {
            StopCoroutine(mainAnimationCoroutine);
            mainAnimationCoroutine = null;
        }

        ShowAllLettersImmediate();

        foreach (LetterData data in allLetters)
        {
            if (data.flickerCoroutine != null)
            {
                StopCoroutine(data.flickerCoroutine);
                data.flickerCoroutine = null;
            }
            if (data.fadeCoroutine != null)
            {
                StopCoroutine(data.fadeCoroutine);
                data.fadeCoroutine = null;
            }
        }

        ShowTrixieImmediate();

        isAnimationComplete = true;
        isTrixieComplete = true;

        Logger.Log(LogModule.Menu, "Анимация завершена мгновенно");
    }

    public bool IsAnimationComplete()
    {
        return isAnimationComplete;
    }

    private void FindTrixieObject()
    {
        Transform menuBackground = transform.parent;
        GameObject trixieObject = null;

        if (menuBackground != null)
        {
            trixieObject = menuBackground.Find("Trixie")?.gameObject;
        }

        if (trixieObject == null)
        {
            trixieObject = GameObject.Find("Trixie");
        }

        if (trixieObject != null)
        {
            Logger.Log(LogModule.Menu, $"Объект Trixie найден: {trixieObject.name}");

            trixieSpriteRenderer = trixieObject.GetComponent<SpriteRenderer>();

            if (trixieSpriteRenderer != null)
            {
                trixieOriginalColor = trixieSpriteRenderer.color;
                Color transparentColor = trixieOriginalColor;
                transparentColor.a = 0f;
                trixieSpriteRenderer.color = transparentColor;
                Logger.Log(LogModule.Menu, $"Trixie SpriteRenderer: альфа установлена на 0 (исходная альфа: {trixieOriginalColor.a})");
            }
            else
            {
                Logger.LogWarning(LogModule.Menu, "Объект Trixie найден, но SpriteRenderer отсутствует");
            }

            trixieLight = trixieObject.GetComponent<Light2D>();

            if (trixieLight != null)
            {
                trixieOriginalLightIntensity = trixieLight.intensity;

                if (trixieOriginalLightIntensity <= 0f)
                {
                    Logger.LogWarning(LogModule.Menu, $"Интенсивность света Trixie равна {trixieOriginalLightIntensity}, установка значения по умолчанию: {config.trixieDefaultLightIntensity}");
                    trixieOriginalLightIntensity = config.trixieDefaultLightIntensity;
                }

                trixieLight.intensity = 0f;
                trixieLight.enabled = true;

                Logger.Log(LogModule.Menu, $"Trixie Light2D: интенсивность установлена на 0, будет восстановлена до: {trixieOriginalLightIntensity}");
            }
            else
            {
                Logger.LogWarning(LogModule.Menu, "Объект Trixie найден, но Light2D отсутствует");
            }
        }
        else
        {
            Logger.LogWarning(LogModule.Menu, "Объект Trixie не найден");
        }
    }

    private void FindAllLetters()
    {
        foreach (Transform word in transform)
        {
            foreach (Transform letter in word)
            {
                LetterData data = new LetterData();
                data.letterObject = letter.gameObject;

                data.light = letter.GetComponentInChildren<Light2D>();
                data.spriteRenderer = letter.GetComponentInChildren<SpriteRenderer>();

                if (data.light != null)
                {
                    data.originalIntensity = data.light.intensity;

                    if (data.originalIntensity <= 0f)
                    {
                        Logger.LogWarning(LogModule.Menu, $"Буква '{letter.name}': интенсивность света равна {data.originalIntensity}, установка значения по умолчанию: {config.intensityMax}");
                        data.originalIntensity = config.intensityMax;
                    }

                    data.light.intensity = 0f;

                    Logger.Log(LogModule.Menu, $"Буква '{letter.name}': свет найден, будет восстановлен до интенсивности: {data.originalIntensity}");
                }

                if (data.spriteRenderer != null)
                {
                    data.originalColor = data.spriteRenderer.color;

                    Color transparentColor = data.originalColor;
                    transparentColor.a = 0f;
                    data.spriteRenderer.color = transparentColor;

                    Logger.Log(LogModule.Menu, $"Буква '{letter.name}': SpriteRenderer найден, альфа установлена на 0");
                }

                if (data.light != null || data.spriteRenderer != null)
                {
                    allLetters.Add(data);
                }
            }
        }

        Logger.Log(LogModule.Menu, $"Найдено {allLetters.Count} букв с компонентами");
    }

    private IEnumerator AnimateLetters()
    {
        for (int i = 0; i < allLetters.Count; i++)
        {
            if (isAnimationStopped) yield break;

            LetterData data = allLetters[i];
            data.fadeCoroutine = StartCoroutine(FadeInLetter(data));

            yield return new WaitForSeconds(config.letterRevealDelay);
        }

        if (isAnimationStopped) yield break;

        yield return new WaitForSeconds(0.3f);

        if (isAnimationStopped) yield break;

        foreach (LetterData data in allLetters)
        {
            if (isAnimationStopped) yield break;

            if (data.light != null)
            {
                data.flickerCoroutine = StartCoroutine(RandomFlicker(data.light, data));
            }
        }

        if (isAnimationStopped) yield break;

        yield return new WaitForSeconds(config.trixieStartDelay);

        if (isAnimationStopped) yield break;

        if (trixieSpriteRenderer != null || trixieLight != null)
        {
            trixieCoroutine = StartCoroutine(FadeInTrixie());
            while (trixieCoroutine != null && !isAnimationStopped)
            {
                yield return null;
            }
        }

        isAnimationComplete = true;
        isTrixieComplete = true;
        Logger.Log(LogModule.Menu, "Полная анимация завершена");
    }

    private IEnumerator FadeInLetter(LetterData data)
    {
        if (isAnimationStopped) yield break;

        List<Coroutine> coroutines = new List<Coroutine>();

        if (data.spriteRenderer != null)
        {
            coroutines.Add(StartCoroutine(FadeSpriteRenderer(data.spriteRenderer, data.originalColor, config.letterFadeDuration)));
        }

        if (data.light != null)
        {
            coroutines.Add(StartCoroutine(FadeLightIntensity(data.light, data.originalIntensity, config.letterFadeDuration, data)));
        }

        foreach (Coroutine c in coroutines)
        {
            if (c != null)
            {
                yield return c;
                if (isAnimationStopped) yield break;
            }
        }
    }

    private IEnumerator FadeSpriteRenderer(SpriteRenderer spriteRenderer, Color originalColor, float duration)
    {
        if (spriteRenderer == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (isAnimationStopped)
            {
                Color finalColor = originalColor;
                finalColor.a = 1f;
                spriteRenderer.color = finalColor;
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Color newColor = originalColor;
            newColor.a = Mathf.Lerp(0f, 1f, t);

            spriteRenderer.color = newColor;

            yield return null;
        }

        Color finalColor2 = originalColor;
        finalColor2.a = 1f;
        spriteRenderer.color = finalColor2;
    }

    private IEnumerator FadeLightIntensity(Light2D light, float targetIntensity, float duration, LetterData data = null)
    {
        if (light == null) yield break;

        light.enabled = true;
        float startIntensity = light.intensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (isAnimationStopped)
            {
                light.intensity = targetIntensity;
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            light.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);

            yield return null;
        }

        light.intensity = targetIntensity;
    }

    private IEnumerator FadeInTrixie()
    {
        Logger.Log(LogModule.Menu, "Начало появления Trixie");

        if (trixieSpriteRenderer != null)
        {
            Logger.Log(LogModule.Menu, "Появление Trixie SpriteRenderer");

            float elapsed = 0f;
            float startAlpha = 0f;
            float targetAlpha = 1f;

            while (elapsed < config.trixieFadeInDuration)
            {
                if (isAnimationStopped)
                {
                    Color finalColor = trixieOriginalColor;
                    finalColor.a = 1f;
                    trixieSpriteRenderer.color = finalColor;
                    Logger.Log(LogModule.Menu, "Trixie SpriteRenderer: принудительно показан");
                    break;
                }

                elapsed += Time.deltaTime;
                float t = elapsed / config.trixieFadeInDuration;

                Color newColor = trixieOriginalColor;
                newColor.a = Mathf.Lerp(startAlpha, targetAlpha, t);

                trixieSpriteRenderer.color = newColor;

                yield return null;
            }

            if (!isAnimationStopped)
            {
                Color finalColor = trixieOriginalColor;
                finalColor.a = 1f;
                trixieSpriteRenderer.color = finalColor;
                Logger.Log(LogModule.Menu, $"Trixie SpriteRenderer: альфа = {trixieSpriteRenderer.color.a} (полностью видим)");
            }
        }
        else
        {
            Logger.LogWarning(LogModule.Menu, "Trixie SpriteRenderer отсутствует, невозможно выполнить появление");
        }

        if (isAnimationStopped)
        {
            if (trixieLight != null)
            {
                trixieLight.intensity = trixieOriginalLightIntensity;
                trixieLight.enabled = true;
            }
            isTrixieComplete = true;
            yield break;
        }

        yield return new WaitForSeconds(0.2f);

        if (isAnimationStopped) yield break;

        if (trixieLight != null)
        {
            Logger.Log(LogModule.Menu, $"Появление Trixie Light2D от 0 до {trixieOriginalLightIntensity}");

            yield return StartCoroutine(FadeLightIntensity(trixieLight, trixieOriginalLightIntensity, config.trixieLightFadeInDuration));

            Logger.Log(LogModule.Menu, $"Trixie Light2D: интенсивность = {trixieLight.intensity} (полностью освещён)");
        }
        else
        {
            Logger.LogWarning(LogModule.Menu, "Trixie Light2D отсутствует, невозможно выполнить появление света");
        }

        isTrixieComplete = true;
        Logger.Log(LogModule.Menu, "Появление Trixie завершено");
    }

    private IEnumerator RandomFlicker(Light2D light, LetterData data)
    {
        while (true)
        {
            if (light == null || isAnimationStopped || data == null) yield break;

            yield return new WaitForSeconds(Random.Range(0.3f, 1.0f));

            if (isAnimationStopped || light == null) yield break;

            float target = Random.Range(config.flickerMin, config.flickerMax);

            if (data.fadeCoroutine != null)
            {
                StopCoroutine(data.fadeCoroutine);
            }
            data.fadeCoroutine = StartCoroutine(FadeLightIntensity(light, target, config.flickerSpeed, data));
        }
    }

    public void HideAllLettersImmediate()
    {
        foreach (LetterData data in allLetters)
        {
            if (data.spriteRenderer != null)
            {
                Color transparentColor = data.originalColor;
                transparentColor.a = 0f;
                data.spriteRenderer.color = transparentColor;
            }

            if (data.light != null)
            {
                data.light.intensity = 0f;
            }
        }
        Logger.Log(LogModule.Menu, "Все буквы скрыты мгновенно");
    }

    public void ShowAllLettersImmediate()
    {
        foreach (LetterData data in allLetters)
        {
            if (data.spriteRenderer != null)
            {
                Color fullColor = data.originalColor;
                fullColor.a = 1f;
                data.spriteRenderer.color = fullColor;
            }

            if (data.light != null)
            {
                data.light.intensity = data.originalIntensity;
                data.light.enabled = true;
            }
        }
        Logger.Log(LogModule.Menu, "Все буквы показаны мгновенно");
    }

    public void HideTrixieImmediate()
    {
        if (trixieSpriteRenderer != null)
        {
            Color transparentColor = trixieOriginalColor;
            transparentColor.a = 0f;
            trixieSpriteRenderer.color = transparentColor;
        }

        if (trixieLight != null)
        {
            trixieLight.intensity = 0f;
        }
        Logger.Log(LogModule.Menu, "Trixie скрыта мгновенно");
    }

    public void ShowTrixieImmediate()
    {
        if (trixieSpriteRenderer != null)
        {
            Color fullColor = trixieOriginalColor;
            fullColor.a = 1f;
            trixieSpriteRenderer.color = fullColor;
        }

        if (trixieLight != null)
        {
            trixieLight.intensity = trixieOriginalLightIntensity;
            trixieLight.enabled = true;
        }
        Logger.Log(LogModule.Menu, "Trixie показана мгновенно");
    }

    public bool IsTrixieComplete()
    {
        return isTrixieComplete;
    }
}
