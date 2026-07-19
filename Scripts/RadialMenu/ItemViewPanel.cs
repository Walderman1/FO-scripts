using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ItemViewPanel : MonoBehaviour
{
    public static ItemViewPanel Instance;

    private TMP_Text itemNameText;
    private TMP_Text itemDescriptionText;
    private Image itemIconImage;
    private Button backButton;

    // ✅ Для отображения префаба предмета
    [Header("Item Preview")]
    [SerializeField] private Transform itemPreviewContainer; // сюда будет помещаться префаб
    private GameObject currentPreviewItem;

    private CanvasGroup canvasGroup;
    private GameObject panelObject;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        panelObject = gameObject;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // ✅ Поиск контейнера для префаба (по имени "Slot")
        if (itemPreviewContainer == null)
        {
            // Ищем объект с именем "Slot"
            Transform container = transform.Find("Slot");
            if (container == null)
                container = transform.Find("PreviewContainer"); // fallback для обратной совместимости

            if (container == null)
            {
                // Если не нашли — создаём
                GameObject containerGO = new GameObject("Slot");
                containerGO.transform.SetParent(panelObject.transform);

                RectTransform rt = containerGO.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(150, 150);
                rt.anchoredPosition = new Vector2(0, 50);

                // Добавляем Image для фона
                Image bg = containerGO.AddComponent<Image>();
                bg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

                itemPreviewContainer = containerGO.transform;
                Debug.Log("Created Slot automatically");
            }
            else
            {
                itemPreviewContainer = container;
                Debug.Log($"Found Slot: {container.name}");
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
                Debug.Log($"Found ItemNameText: {text.gameObject.name}");
            }

            if (itemDescriptionText == null && (name.Contains("desc") || name.Contains("description")))
            {
                itemDescriptionText = text;
                Debug.Log($"Found ItemDescriptionText: {text.gameObject.name}");
            }
        }

        if (itemNameText == null && allTexts.Length > 0)
        {
            itemNameText = allTexts[0];
        }
        if (itemDescriptionText == null && allTexts.Length > 1)
        {
            itemDescriptionText = allTexts[1];
        }

        Image[] allImages = GetComponentsInChildren<Image>(true);
        foreach (Image img in allImages)
        {
            string name = img.gameObject.name.ToLower();
            if (itemIconImage == null && (name.Contains("icon") || name.Contains("image") || name.Contains("sprite")))
            {
                itemIconImage = img;
                Debug.Log($"Found ItemIconImage: {img.gameObject.name}");
            }
        }

        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in allButtons)
        {
            string name = btn.gameObject.name.ToLower();
            if (backButton == null && (name.Contains("back") || name.Contains("close") || name.Contains("exit") || name.Contains("назад")))
            {
                backButton = btn;
                Debug.Log($"Found BackButton: {btn.gameObject.name}");
            }
        }

        if (backButton == null && allButtons.Length > 0)
        {
            backButton = allButtons[0];
            Debug.Log($"Fallback: Using {allButtons[0].gameObject.name} as BackButton");
        }
    }

    private void Update()
    {
        if (IsPanelVisible() && Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverPanel())
            {
                HidePanel();
            }
        }
    }

    private bool IsPointerOverPanel()
    {
        if (EventSystem.current == null) return false;

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
        if (item == null) return;

        ItemData data = ItemDatabase.Instance?.GetItemData(item.itemType);
        if (data == null) return;

        // Отображаем префаб предмета
        if (data.uiPrefab != null && itemPreviewContainer != null)
        {
            if (currentPreviewItem != null)
            {
                Destroy(currentPreviewItem);
                currentPreviewItem = null;
            }

            currentPreviewItem = Instantiate(data.uiPrefab, itemPreviewContainer);

            // Настраиваем RectTransform
            RectTransform rt = currentPreviewItem.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.sizeDelta = new Vector2(150, 150);
            }

            // ✅ ОТКЛЮЧАЕМ InventoryItemMarker
            InventoryItemMarker marker = currentPreviewItem.GetComponent<InventoryItemMarker>();
            if (marker != null)
            {
                marker.enabled = false;
                Debug.Log("InventoryItemMarker disabled on preview item");
            }

            // ✅ Также отключаем на всех дочерних объектах
            InventoryItemMarker[] markers = currentPreviewItem.GetComponentsInChildren<InventoryItemMarker>(true);
            foreach (InventoryItemMarker m in markers)
            {
                m.enabled = false;
            }

            // ✅ ПОДНИМАЕМ ПРЕДМЕТ ВВЕРХ ИЕРАРХИИ
            currentPreviewItem.transform.SetAsFirstSibling();

            // Включаем все Image
            Image[] images = currentPreviewItem.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                img.enabled = true;
            }

            // Включаем все TMP_Text
            TMP_Text[] texts = currentPreviewItem.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text txt in texts)
            {
                txt.enabled = true;
            }

            Debug.Log($"Item preview created: {data.uiPrefab.name}");
        }

        // Заполняем текстовую информацию
        if (itemNameText != null)
            itemNameText.text = data.itemName;

        if (itemDescriptionText != null)
            itemDescriptionText.text = data.description;

        if (itemIconImage != null)
        {
            itemIconImage.sprite = data.icon;
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

        Debug.Log("ItemViewPanel показана");
    }

    public void HidePanel()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        // ✅ Удаляем префаб при закрытии
        if (currentPreviewItem != null)
        {
            Destroy(currentPreviewItem);
            currentPreviewItem = null;
        }

        Debug.Log("ItemViewPanel скрыта");
    }

    public bool IsPanelVisible()
    {
        return panelObject.activeSelf && canvasGroup != null && canvasGroup.alpha > 0.5f;
    }
}