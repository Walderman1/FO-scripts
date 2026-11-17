using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySortDropdown : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color selectedTextColor = new Color(1f, 0.8f, 0.2f);

    private CanvasGroup rootCanvasGroup;
    private InventoryUIManager inventory;

    private Button mainButton;
    private TMP_Text mainButtonText;
    private Transform optionsContainer;
    private CanvasGroup optionsCanvasGroup;

    private Dictionary<InventorySortMode, Button> optionButtons = new Dictionary<InventorySortMode, Button>();
    private Dictionary<InventorySortMode, TMP_Text> optionTexts = new Dictionary<InventorySortMode, TMP_Text>();

    private void Awake()
    {
        rootCanvasGroup = GetComponent<CanvasGroup>();
        if (rootCanvasGroup == null)
            rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        rootCanvasGroup.alpha = 0f;
        rootCanvasGroup.blocksRaycasts = false;
        rootCanvasGroup.interactable = false;
    }

    private void Start()
    {
        inventory = GameReferences.Instance?.InventoryUIManager;
        if (inventory == null)
        {
            Logger.LogWarning(LogModule.UI, "InventorySortDropdown: InventoryUIManager не найден");
            return;
        }

        CacheReferences();

        inventory.OnSortModeChanged += HandleSortModeChanged;
        inventory.OnVisibilityChanged += HandleVisibilityChanged;

        UpdateSelection(inventory.CurrentSortMode);

        if (inventory.IsInventoryOpen)
            SetVisible(true, true);
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnSortModeChanged -= HandleSortModeChanged;
            inventory.OnVisibilityChanged -= HandleVisibilityChanged;
        }
        CloseDropdown();
    }

    private void CacheReferences()
    {
        Transform mainBtnTrans = transform.Find("MainButton");
        if (mainBtnTrans != null)
        {
            mainButton = mainBtnTrans.GetComponent<Button>();
            mainButtonText = mainBtnTrans.GetComponentInChildren<TMP_Text>();
            if (mainButton != null)
            {
                mainButton.onClick.RemoveAllListeners();
                mainButton.onClick.AddListener(ToggleDropdown);
            }
        }

        optionsContainer = transform.Find("OptionsContainer");
        if (optionsContainer != null)
        {
            optionsCanvasGroup = optionsContainer.GetComponent<CanvasGroup>();
            if (optionsCanvasGroup == null)
                optionsCanvasGroup = optionsContainer.gameObject.AddComponent<CanvasGroup>();

            optionsCanvasGroup.alpha = 0f;
            optionsCanvasGroup.blocksRaycasts = false;
            optionsCanvasGroup.interactable = false;

            var modes = (InventorySortMode[])System.Enum.GetValues(typeof(InventorySortMode));
            foreach (var mode in modes)
            {
                Transform optTrans = optionsContainer.Find($"Option_{mode}");
                if (optTrans != null)
                {
                    var btn = optTrans.GetComponent<Button>();
                    var txt = optTrans.GetComponentInChildren<TMP_Text>();

                    if (btn != null)
                    {
                        var capturedMode = mode;
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() =>
                        {
                            if (inventory != null) inventory.SetSortMode(capturedMode);
                            UpdateSelection(capturedMode);
                            CloseDropdown();
                        });
                        optionButtons[mode] = btn;
                    }

                    if (txt != null)
                    {
                        txt.text = GetModeName(mode);
                        optionTexts[mode] = txt;
                    }
                }
            }
        }
    }

    private void ToggleDropdown()
    {
        if (optionsCanvasGroup == null) return;

        if (optionsCanvasGroup.alpha > 0f) CloseDropdown();
        else OpenDropdown();
    }

    private void OpenDropdown()
    {
        if (optionsCanvasGroup == null) return;

        optionsCanvasGroup.alpha = 1f;
        optionsCanvasGroup.blocksRaycasts = true;
        optionsCanvasGroup.interactable = true;
    }

    private void CloseDropdown()
    {
        if (optionsCanvasGroup == null) return;

        optionsCanvasGroup.alpha = 0f;
        optionsCanvasGroup.blocksRaycasts = false;
        optionsCanvasGroup.interactable = false;
    }

    private void HandleSortModeChanged(InventorySortMode mode)
    {
        UpdateSelection(mode);
    }

    private void HandleVisibilityChanged(bool isOpen)
    {
        SetVisible(isOpen);
        if (!isOpen) CloseDropdown();
    }

    private void UpdateSelection(InventorySortMode mode)
    {
        if (mainButtonText != null)
            mainButtonText.text = GetModeName(mode);

        foreach (var kvp in optionTexts)
        {
            kvp.Value.color = kvp.Key == mode ? selectedTextColor : normalTextColor;
        }
    }

    private void SetVisible(bool visible, bool instant = false)
    {
        if (rootCanvasGroup == null) return;

        if (instant)
        {
            rootCanvasGroup.alpha = visible ? 1f : 0f;
            rootCanvasGroup.blocksRaycasts = visible;
            rootCanvasGroup.interactable = visible;
            return;
        }

        StopAllCoroutines();
        StartCoroutine(FadeRootRoutine(visible ? 1f : 0f));
    }

    private IEnumerator FadeRootRoutine(float targetAlpha)
    {
        float startAlpha = rootCanvasGroup.alpha;
        float elapsed = 0f;

        if (targetAlpha > 0f)
        {
            rootCanvasGroup.blocksRaycasts = true;
            rootCanvasGroup.interactable = true;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            rootCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        rootCanvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            rootCanvasGroup.blocksRaycasts = false;
            rootCanvasGroup.interactable = false;
        }
    }

    private string GetModeName(InventorySortMode mode)
    {
        switch (mode)
        {
            case InventorySortMode.Category: return "Категория";
            case InventorySortMode.Name: return "Название";
            case InventorySortMode.Count: return "Количество";
            case InventorySortMode.Priority: return "Приоритет";
            default: return mode.ToString();
        }
    }
}