using System.Collections;
using UnityEngine;
using VContainer;

public class EquipmentUIController : MonoBehaviour
{
    [Header("Ссылки (заполнить, если компонент добавлен на сцене)")]
    [SerializeField] private CanvasGroup equipmentCanvasGroup;

    [Header("Поиск по тегу")]
    [SerializeField] private string equipmentPanelTag = "EquipmentPanel";
    [SerializeField] private string equipmentPanelName = "EquipmentPanel";
    [SerializeField] private float fadeDuration = 0.3f;

    private bool isOpen = false;
    private Coroutine fadeCoroutine;

    public bool IsOpen => isOpen;

    public void Initialize()
    {
        Logger.LogVerbose(LogModule.Core, "EquipmentUIController инициализирован");
    }

    public bool IsPointerOverPanel(Vector2 screenPos)
    {
        if (!isOpen) return false;
        if (equipmentCanvasGroup == null) return false;

        RectTransform rt = equipmentCanvasGroup.transform as RectTransform;
        if (rt == null) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, null);
    }

    public void SetPanel(CanvasGroup panel)
    {
        equipmentCanvasGroup = panel;

        if (equipmentCanvasGroup == null)
        {
            Logger.LogWarning(LogModule.Core, "EquipmentUIController.SetPanel(null): панель не назначена");
            return;
        }

        equipmentCanvasGroup.alpha = 0f;
        equipmentCanvasGroup.blocksRaycasts = false;
        equipmentCanvasGroup.interactable = false;
        isOpen = false;
    }

    public void FindPanel()
    {
        if (equipmentCanvasGroup != null) return;

        GameObject panel = null;

        try { panel = GameObject.FindGameObjectWithTag(equipmentPanelTag); }
        catch (UnityException)
        {
            Logger.LogWarning(LogModule.Core, $"EquipmentUIController: тег '{equipmentPanelTag}' не определён в проекте");
        }

        if (panel == null && !string.IsNullOrEmpty(equipmentPanelName))
        {
            panel = GameObject.Find(equipmentPanelName);
            if (panel != null)
                Logger.LogWarning(LogModule.Core, $"EquipmentUIController: панель найдена по имени '{equipmentPanelName}', а не по тегу");
        }

        if (panel == null)
        {
            Logger.LogWarning(LogModule.Core, $"EquipmentUIController: панель экипировки не найдена (тег '{equipmentPanelTag}', имя '{equipmentPanelName}')");
            return;
        }

        equipmentCanvasGroup = panel.GetComponent<CanvasGroup>();
        if (equipmentCanvasGroup == null)
        {
            Logger.LogWarning(LogModule.Core, $"EquipmentUIController: на '{panel.name}' отсутствует CanvasGroup");
            return;
        }

        equipmentCanvasGroup.alpha = 0f;
        equipmentCanvasGroup.blocksRaycasts = false;
        equipmentCanvasGroup.interactable = false;
        isOpen = false;
    }

    public void Toggle()
    {
        if (equipmentCanvasGroup == null)
        {
            FindPanel();
            if (equipmentCanvasGroup == null) return;
        }

        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (equipmentCanvasGroup == null)
        {
            FindPanel();
            if (equipmentCanvasGroup == null) return;
        }

        UICoordinator.BeforeOpen(UIKind.Equipment);

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(Fade(1f));
    }

    public void Close()
    {
        if (equipmentCanvasGroup == null) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(Fade(0f));
    }

    public void ResetState()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        equipmentCanvasGroup = null;
        isOpen = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = equipmentCanvasGroup.alpha;
        float elapsed = 0f;

        if (targetAlpha >= 1f)
        {
            equipmentCanvasGroup.blocksRaycasts = true;
            equipmentCanvasGroup.interactable = true;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            equipmentCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        equipmentCanvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            equipmentCanvasGroup.blocksRaycasts = false;
            equipmentCanvasGroup.interactable = false;
        }

        isOpen = targetAlpha >= 1f;
        fadeCoroutine = null;
    }
}