using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [Header("Тайминги")]
    [SerializeField] private float minDisplayTime = 0.5f;
    [SerializeField] private float fadeDuration = 0.3f;

    private CanvasGroup canvasGroup;
    private RectTransform progressFillRt;
    private TMP_Text statusText;
    private TMP_Text titleText;
    private float showStartedAt;
    private float currentProgress;
    private bool isVisible;

    public bool IsVisible => isVisible;

    public static bool ShowEnabled
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool("LoadingScreen.ShowEnabled", true);
#else
            return true;
#endif
        }
        set
        {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetBool("LoadingScreen.ShowEnabled", value);
#endif
        }
    }

    public static float SpeedPercent
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetFloat("LoadingScreen.SpeedPercent", 100f);
#else
            return 100f;
#endif
        }
        set
        {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetFloat("LoadingScreen.SpeedPercent",
                Mathf.Clamp(value, 0f, 100f));
#endif
        }
    }

    public static float GetArtificialDelay()
    {
        float sp = SpeedPercent;
        if (sp >= 100f) return 0f;
        return (100f - sp) * 0.2f;
    }

#if UNITY_EDITOR
    public static void ResetPrefs()
    {
        UnityEditor.EditorPrefs.DeleteKey("LoadingScreen.ShowEnabled");
        UnityEditor.EditorPrefs.DeleteKey("LoadingScreen.SpeedPercent");
    }
#endif

    private void Awake()
    {
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        CreateUI();
    }

    private void CreateUI()
    {
        var canvasObj = new GameObject("LoadingCanvas", typeof(RectTransform));
        canvasObj.transform.SetParent(null);
        DontDestroyOnLoad(canvasObj);

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        canvas.overrideSorting = true;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        var canvasRt = canvasObj.GetComponent<RectTransform>();
        canvasRt.anchorMin = Vector2.zero;
        canvasRt.anchorMax = Vector2.one;
        canvasRt.offsetMin = Vector2.zero;
        canvasRt.offsetMax = Vector2.zero;
        canvasRt.localScale = Vector3.one;

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        var bg = new GameObject("BG", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvasObj.transform, false);
        var bgImg = bg.GetComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.08f, 1f);
        bgImg.raycastTarget = true;
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        titleText = CreateText(canvasObj.transform, "Title", "Загрузка...", 48, new Vector2(0, 80));
        statusText = CreateText(canvasObj.transform, "Status", "Подготовка", 24, new Vector2(0, -80));

        var barBg = new GameObject("BarBg", typeof(RectTransform), typeof(Image));
        barBg.transform.SetParent(canvasObj.transform, false);
        var barBgImg = barBg.GetComponent<Image>();
        barBgImg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        barBgImg.raycastTarget = false;
        var barRt = barBg.GetComponent<RectTransform>();
        barRt.anchorMin = new Vector2(0.25f, 0.5f);
        barRt.anchorMax = new Vector2(0.75f, 0.5f);
        barRt.pivot = new Vector2(0.5f, 0.5f);
        barRt.anchoredPosition = new Vector2(0, -120);
        barRt.sizeDelta = new Vector2(0, 14);

        var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(barBg.transform, false);
        var fillImg = fillObj.GetComponent<Image>();
        fillImg.color = new Color(0.3f, 0.7f, 1f);
        fillImg.raycastTarget = false;
        fillImg.type = Image.Type.Simple;

        progressFillRt = fillObj.GetComponent<RectTransform>();
        progressFillRt.pivot = new Vector2(0f, 0.5f);
        progressFillRt.anchorMin = Vector2.zero;
        progressFillRt.anchorMax = Vector2.one;
        progressFillRt.offsetMin = Vector2.zero;
        progressFillRt.offsetMax = Vector2.zero;
        progressFillRt.localScale = new Vector3(0f, 1f, 1f);

        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null)
        {
            titleText.font = font;
            statusText.font = font;
        }
    }

    private TMP_Text CreateText(Transform parent, string name, string text, int size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(1000, 60);
        return tmp;
    }

    public void Show(string title = "Загрузка...")
    {
        if (!ShowEnabled) return;
        if (canvasGroup == null) return;

        showStartedAt = Time.unscaledTime;
        isVisible = true;

        if (titleText != null) titleText.text = title;
        SetProgress(0f, "Подготовка...");

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        canvasGroup.gameObject.transform.SetAsLastSibling();
    }

    public void SetProgress(float progress01, string status)
    {
        currentProgress = Mathf.Clamp01(progress01);

        if (progressFillRt != null)
            progressFillRt.localScale = new Vector3(currentProgress, 1f, 1f);

        if (statusText != null)
            statusText.text = status;

        Logger.LogVerbose(LogModule.Save,
            $"[LoadingScreen] Progress={currentProgress:P0} '{status}'");
    }

    public float GetCurrentProgress() => currentProgress;

    public void Hide() => StartCoroutine(HideAsync());

    public IEnumerator HideAsync()
    {
        if (!isVisible || canvasGroup == null) yield break;

        float elapsedVisible = Time.unscaledTime - showStartedAt;
        if (elapsedVisible < minDisplayTime)
            yield return new WaitForSecondsRealtime(minDisplayTime - elapsedVisible);

        float t = 0f;
        float start = canvasGroup.alpha;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 0f, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        isVisible = false;
    }

    public void HideInstant()
    {
        if (canvasGroup == null) return;
        StopAllCoroutines();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        isVisible = false;
    }

    public void ShowInstant() => Show();

    public void DumpState()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[LoadingScreen] === DUMP ===");
        sb.AppendLine($"  go={gameObject.name}, ShowEnabled={ShowEnabled}, Speed={SpeedPercent}%");
        sb.AppendLine($"  isVisible={isVisible}, currentProgress={currentProgress:P0}");

        if (canvasGroup == null)
        {
            sb.AppendLine("  canvasGroup = NULL");
        }
        else
        {
            sb.AppendLine($"  cg.alpha={canvasGroup.alpha:F2}, blocks={canvasGroup.blocksRaycasts}");
            sb.AppendLine($"  cg.activeSelf={canvasGroup.gameObject.activeSelf}");
        }

        if (progressFillRt != null)
        {
            sb.AppendLine($"  fill.parent={progressFillRt.parent?.name}");
            sb.AppendLine($"  fill.anchors min={progressFillRt.anchorMin} max={progressFillRt.anchorMax}");
            sb.AppendLine($"  fill.localScale={progressFillRt.localScale}");
            sb.AppendLine($"  fill.sizeDelta={progressFillRt.sizeDelta}");
            sb.AppendLine($"  fill.position={progressFillRt.position}");
        }
        else
        {
            sb.AppendLine("  progressFillRt = NULL");
        }

        Logger.Log(LogModule.Editor, sb.ToString());
    }

    public void ForceTestShow(float seconds = 3f)
    {
        Show("TEST");
        StartCoroutine(AutoHide(seconds));
    }

    private IEnumerator AutoHide(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        yield return HideAsync();
    }
}