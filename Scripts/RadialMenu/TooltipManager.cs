using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class TooltipManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private string tooltipPrefabPath = "UI/Prefab/Panels/TooltipPanel";

    [Header("Настройки")]
    [SerializeField] private float showDelay = 0.5f;
    [SerializeField] private float offsetY = 0f;
    [SerializeField] private Color titleColor = new Color(1f, 0.84f, 0f);

    [Inject] private SettingsManager _settingsManager;
    [Inject] private GameReferences _refs;

    private GameReferences Refs => _refs;
    private SettingsManager SM => _settingsManager;

    private RectTransform panelRect;
    private RectTransform canvasRect;
    private bool isVisible = false;

    private Coroutine showRoutine;
    private string pendingText;

    private void Awake()
    {
        EnsureUI();
        HideTooltip();
    }

    private void Update()
    {
        if (isVisible) FollowMouse();
    }

    private void EnsureUI()
    {
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        }

        if (canvas == null) return;

        canvasRect = canvas.transform as RectTransform;

        if (tooltipPanel == null)
        {
            GameObject prefab = Resources.Load<GameObject>(tooltipPrefabPath);
            if (prefab != null)
            {
                tooltipPanel = Instantiate(prefab, canvas.transform);
                tooltipPanel.name = "TooltipPanel";
            }
            else return;
        }

        CanvasGroup cg = tooltipPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = tooltipPanel.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        panelRect = tooltipPanel.GetComponent<RectTransform>();
        if (panelRect == null) return;

        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);

        if (tooltipText == null)
            tooltipText = tooltipPanel.GetComponentInChildren<TMP_Text>(true);
    }

    private void FollowMouse()
    {
        if (panelRect == null || canvasRect == null) return;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (cam == null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = Camera.main;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, Input.mousePosition, cam, out Vector2 localPoint))
            return;

        float x = localPoint.x;
        float y = localPoint.y + offsetY;

        Vector2 panelSize = panelRect.rect.size;
        Vector2 canvasSize = canvasRect.rect.size;
        float halfW = canvasSize.x * 0.5f;
        float halfH = canvasSize.y * 0.5f;

        if (x + panelSize.x * 0.5f > halfW) x = halfW - panelSize.x * 0.5f;
        if (x - panelSize.x * 0.5f < -halfW) x = -halfW + panelSize.x * 0.5f;
        if (y + panelSize.y > halfH) y = halfH - panelSize.y;
        if (y < -halfH) y = -halfH;

        panelRect.anchoredPosition = new Vector2(x, y);
    }

    public void ShowTooltip(string text, Vector2 position)
    {
        var sm = SM;
        if (sm != null && sm.Settings != null && !sm.Settings.showTooltips) return;

        if (tooltipPanel == null || tooltipText == null)
        {
            EnsureUI();
            if (tooltipPanel == null || tooltipText == null) return;
        }

        pendingText = text;

        if (showRoutine != null) StopCoroutine(showRoutine);

        if (showDelay <= 0f) ShowImmediate();
        else showRoutine = StartCoroutine(ShowDelayed());
    }

    private System.Collections.IEnumerator ShowDelayed()
    {
        yield return new WaitForSecondsRealtime(showDelay);
        showRoutine = null;
        if (string.IsNullOrEmpty(pendingText)) yield break;
        ShowImmediate();
    }

    private void ShowImmediate()
    {
        if (string.IsNullOrEmpty(pendingText)) return;

        if (tooltipText != null)
        {
            string hexColor = ColorUtility.ToHtmlStringRGB(titleColor);

            string formattedText = pendingText
                .Replace("<b>", $"<color=#{hexColor}><b>")
                .Replace("</b>", "</b></color>");

            tooltipText.text = formattedText;
            tooltipText.alignment = TextAlignmentOptions.Center;
        }

        tooltipPanel.SetActive(true);
        tooltipPanel.transform.SetAsLastSibling();
        isVisible = true;

        FollowMouse();
    }

    public void HideTooltip()
    {
        pendingText = null;

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        if (tooltipPanel != null) tooltipPanel.SetActive(false);
        isVisible = false;
    }
}