using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ItemViewPanel : MonoBehaviour
{
    private TMP_Text itemNameText;
    private TMP_Text itemDescriptionText;
    private Image itemIconImage;
    private Button backButton;

    [Header("Item Preview")]
    [SerializeField] private Transform itemPreviewContainer;
    private GameObject currentPreviewItem;

    private CanvasGroup canvasGroup;
    private GameObject panelObject;

    private void Awake()
    {
        panelObject = gameObject;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (itemPreviewContainer == null)
        {
            Transform container = transform.Find("Slot");
            if (container == null)
                container = transform.Find("PreviewContainer");

            if (container == null)
            {
                GameObject containerGO = new GameObject("Slot");
                containerGO.transform.SetParent(panelObject.transform);

                RectTransform rt = containerGO.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(150, 150);
                rt.anchoredPosition = new Vector2(0, 50);

                Image bg = containerGO.AddComponent<Image>();
                bg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

                itemPreviewContainer = containerGO.transform;
                Logger.Log(LogModule.RadialMenu, "Слот создан автоматически");
            }
            else
            {
                itemPreviewContainer = container;
                Logger.LogVerbose(LogModule.RadialMenu, $"Найден слот: {container.name}");
            }
        }

        FindAllComponents();

        panelObject.SetActive(true);
        HidePanel();

        if (backButton != null)
        {
            backButton.onClick.AddListener(HidePanel);
        }
    }

    private void FindAllComponents()
    {
        TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in allTexts)
        {
            string name = text.gameObject.name.ToLower();

            if (itemNameText == null && (name.Contains("name") || name.Contains("title")))
            {
                itemNameText = text;
                Logger.LogVerbose(LogModule.RadialMenu, $"Найден ItemNameText: {text.gameObject.name}");
            }

            if (itemDescriptionText == null && (name.Contains("desc") || name.Contains("description")))
            {
                itemDescriptionText = text;
                Logger.LogVerbose(LogModule.RadialMenu, $"Найден ItemDescriptionText: {text.gameObject.name}");
            }
        }

        if (itemNameText == null && allTexts.Length > 0)
        {
            itemNameText = allTexts[0];
            Logger.Log(LogModule.RadialMenu, $"Запасной вариант: {allTexts[0].gameObject.name} как ItemNameText");
        }
        if (itemDescriptionText == null && allTexts.Length > 1)
        {
            itemDescriptionText = allTexts[1];
            Logger.Log(LogModule.RadialMenu, $"Запасной вариант: {allTexts[1].gameObject.name} как ItemDescriptionText");
        }

        Image[] allImages = GetComponentsInChildren<Image>(true);
        foreach (Image img in allImages)
        {
            string name = img.gameObject.name.ToLower();
            if (itemIconImage == null && (name.Contains("icon") || name.Contains("image") || name.Contains("sprite")))
            {
                itemIconImage = img;
                Logger.Log(LogModule.RadialMenu, $"Найден ItemIconImage: {img.gameObject.name}");
            }
        }

        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in allButtons)
        {
            string name = btn.gameObject.name.ToLower();
            if (backButton == null && (name.Contains("back") || name.Contains("close") || name.Contains("exit") || name.Contains("назад")))
            {
                backButton = btn;
                Logger.LogVerbose(LogModule.RadialMenu, $"Найден BackButton: {btn.gameObject.name}");
            }
        }

        if (backButton == null && allButtons.Length > 0)
        {
            backButton = allButtons[0];
            Logger.LogVerbose(LogModule.RadialMenu, $"Запасной вариант: {allButtons[0].gameObject.name} как BackButton");
        }
    }

    private void Update()
    {
        if (IsPanelVisible() && Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverPanel())
            {
                HidePanel();
                Logger.Log(LogModule.RadialMenu, "Клик вне панели - скрытие");
            }
        }
    }

    private bool IsPointerOverPanel()
    {
        if (EventSystem.current == null)
        {
            Logger.LogWarning(LogModule.RadialMenu, "EventSystem.current отсутствует");
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            if (result.gameObject == panelObject || result.gameObject.transform.IsChildOf(panelObject.transform))
            {
                return true;
            }
        }

        return false;
    }

    public void ShowItem(InventoryItemMarker item)
    {
        if (item == null)
        {
            Logger.LogWarning(LogModule.RadialMenu, "Попытка показать null предмет");
            return;
        }

        var so = item.ItemSO;
        if (so == null)
        {
            Logger.LogWarning(LogModule.RadialMenu, "У предмета нет ItemSO");
            return;
        }

        if (so.uiPrefab != null && itemPreviewContainer != null)
        {
            if (currentPreviewItem != null)
            {
                Destroy(currentPreviewItem);
                currentPreviewItem = null;
            }

            currentPreviewItem = Instantiate(so.uiPrefab, itemPreviewContainer);

            RectTransform rt = currentPreviewItem.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(150, 150);
            }

            InventoryItemMarker[] markers = currentPreviewItem.GetComponentsInChildren<InventoryItemMarker>(true);
            foreach (InventoryItemMarker m in markers)
                m.enabled = false;

            currentPreviewItem.transform.SetAsFirstSibling();

            Image[] images = currentPreviewItem.GetComponentsInChildren<Image>(true);
            foreach (Image img in images) img.enabled = true;

            TMP_Text[] texts = currentPreviewItem.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text txt in texts) txt.enabled = true;
        }

        if (itemNameText != null)
            itemNameText.text = so.itemName;

        if (itemDescriptionText != null)
            itemDescriptionText.text = so.description;

        if (itemIconImage != null)
        {
            itemIconImage.sprite = so.icon;
            itemIconImage.preserveAspect = true;
        }

        ShowPanel();
    }

    public void ShowPanel()
    {
        if (!panelObject.activeSelf)
        {
            panelObject.SetActive(true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        Logger.LogVerbose(LogModule.RadialMenu, "ItemViewPanel показана");
    }

    public void HidePanel()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (currentPreviewItem != null)
        {
            Destroy(currentPreviewItem);
            currentPreviewItem = null;
        }

        Logger.LogVerbose(LogModule.RadialMenu, "ItemViewPanel скрыта");
    }

    public bool IsPanelVisible()
    {
        return panelObject.activeSelf && canvasGroup != null && canvasGroup.alpha > 0.5f;
    }
}