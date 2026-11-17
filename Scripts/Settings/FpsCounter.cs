using UnityEngine;
using UnityEngine.UI;

public class FpsCounter : MonoBehaviour
{
    private Text fpsText;
    private Canvas canvas;

    private float deltaTime = 0f;
    private float updateInterval = 0.5f;
    private float accumulator = 0f;
    private int frames = 0;
    private bool isVisible = false;

    private void Awake()
    {
        EnsureCanvas();
    }

    private void EnsureCanvas()
    {
        if (canvas != null) return;

        var existing = GameObject.Find("FpsCanvas");
        if (existing != null)
        {
            canvas = existing.GetComponent<Canvas>();
            if (canvas != null)
            {
                fpsText = existing.GetComponentInChildren<Text>(true);
                if (fpsText != null) return;
            }
            Destroy(existing);
        }

        var canvasGO = new GameObject("FpsCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(null);
        if (Application.isPlaying) DontDestroyOnLoad(canvasGO);

        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        if (canvasGO.GetComponent<GraphicRaycaster>() == null)
            canvasGO.AddComponent<GraphicRaycaster>();

        var textGO = new GameObject("FpsText", typeof(RectTransform));
        textGO.transform.SetParent(canvasGO.transform, false);

        var rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(250f, 60f);

        fpsText = textGO.AddComponent<Text>();
        fpsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fpsText.font == null) fpsText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        fpsText.fontSize = 24;
        fpsText.color = Color.yellow;
        fpsText.alignment = TextAnchor.UpperRight;
        fpsText.text = "FPS: 0";

        canvasGO.SetActive(isVisible);
    }

    public void Initialize(bool startVisible)
    {
        EnsureCanvas();
        SetVisible(startVisible);
    }

    public void SetVisible(bool visible)
    {
        EnsureCanvas();
        isVisible = visible;
        if (canvas != null) canvas.gameObject.SetActive(visible);
    }

    private void Update()
    {
        if (!isVisible) return;

        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        accumulator += Time.unscaledDeltaTime;
        frames++;

        if (accumulator >= updateInterval)
        {
            float fps = frames / accumulator;
            float ms = deltaTime * 1000f;

            if (fpsText != null)
                fpsText.text = $"{fps:F0} FPS\n{ms:F1} ms";

            accumulator = 0f;
            frames = 0;
        }
    }

    private void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }
}