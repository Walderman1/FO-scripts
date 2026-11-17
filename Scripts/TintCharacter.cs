using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class TintCharacter : MonoBehaviour
{
    [Tooltip("Насколько темнее фона (0.9 = на 10% темнее)")]
    [Range(0.5f, 1f)] public float darkenFactor = 0.9f;

    [Tooltip("Скорость перехода цвета. 10 ≈ 0.25 сек до 90%, 5 ≈ 0.5 сек, 15 ≈ 0.15 сек")]
    public float transitionSpeed = 10f;

    [Tooltip("Игнорировать эти SpriteRenderer (например, эффекты или UI над головой)")]
    public SpriteRenderer[] ignoredRenderers;

    [Tooltip("Интервал обновления кэша источников света (сек)")]
    [SerializeField] private float refreshInterval = 2f;

    private SpriteRenderer[] renderers;
    private Color targetColor = Color.white;

    private Light2D[] cachedLights;
    private float nextLightRefresh;

    private float nextColorUpdate;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (ignoredRenderers != null && ignoredRenderers.Length > 0)
        {
            var filtered = new System.Collections.Generic.List<SpriteRenderer>();
            foreach (var sr in renderers)
            {
                bool skip = false;
                foreach (var ig in ignoredRenderers)
                    if (sr == ig) { skip = true; break; }
                if (!skip) filtered.Add(sr);
            }
            renderers = filtered.ToArray();
        }

        RefreshLightsCache();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshLightsCache();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        cachedLights = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshLightsCache();
    }

    private void RefreshLightsCache()
    {
        cachedLights = FindObjectsByType<Light2D>();
        nextLightRefresh = Time.time + refreshInterval;
    }

    void Update()
    {
        if (Time.time >= nextLightRefresh)
            RefreshLightsCache();

        Color ambient = ComputeLightingAt(transform.position);

        float lum = 0.299f * ambient.r + 0.587f * ambient.g + 0.114f * ambient.b;
        Color hue = lum > 0.001f ? ambient / lum : Color.white;

        targetColor = new Color(
            hue.r * darkenFactor,
            hue.g * darkenFactor,
            hue.b * darkenFactor,
            1f
        );

        float t = 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime);

        foreach (var sr in renderers)
        {
            if (sr == null) continue;

            Color current = sr.color;
            Color next = Color.Lerp(current, targetColor, t);

            if (next != current)
                sr.color = next;
        }
    }

    Color ComputeLightingAt(Vector2 pos)
    {
        if (cachedLights == null || cachedLights.Length == 0)
            return Color.white;

        Color total = Color.black;
        bool anyActive = false;

        for (int i = 0; i < cachedLights.Length; i++)
        {
            var light = cachedLights[i];
            if (light == null || !light.enabled) continue;

            anyActive = true;
            float contribution = 0f;

            switch (light.lightType)
            {
                case Light2D.LightType.Global:
                    contribution = 1f;
                    break;

                case Light2D.LightType.Point:
                    contribution = PointFalloff(pos, light.transform.position, light.pointLightOuterRadius);
                    break;

                case Light2D.LightType.Freeform:
                    contribution = 0.7f;
                    break;
            }

            total += light.color * light.intensity * contribution;
        }

        if (!anyActive)
            return Color.white;

        return total;
    }

    float PointFalloff(Vector2 pos, Vector2 lightPos, float radius)
    {
        float d = Vector2.Distance(pos, lightPos);
        if (d >= radius) return 0f;
        float t = 1f - d / radius;
        return t * t;
    }

    [ContextMenu("Show Renderers")]
    void ShowRenderers()
    {
        foreach (var sr in renderers)
            Logger.Log(LogModule.UI, sr != null ? sr.name : "null");
    }
}