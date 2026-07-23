using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InventoryUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] public GameObject inventoryGrid;
    [SerializeField] private GameObject itemSlotPrefab;

    [Header("Inventory Settings")]
    [SerializeField] private int inventorySize = 30;
    [SerializeField] private float fadeDuration = 0.3f;

    private CanvasGroup canvasGroup;
    private bool isInventoryOpen = false;
    private Coroutine fadeCoroutine = null;
    private bool isAnimating = false;

    private void Awake()
    {
        if (inventoryGrid == null)
        {
            GridLayoutGroup grid = GetComponentInChildren<GridLayoutGroup>();
            if (grid != null)
            {
                inventoryGrid = grid.gameObject;
                Logger.Log(LogModule.Inventory, "InventoryGrid найден автоматически");
            }
            else
            {
                Logger.LogError(LogModule.Inventory, "InventoryGrid не найден");
            }
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        isInventoryOpen = false;

        Logger.Log(LogModule.Inventory, "InventoryUIManager инициализирован");
    }

    private void Start()
    {
        ClearInventory();
        CreateInventorySlots();
        Logger.Log(LogModule.Inventory, $"Создано {inventorySize} слотов инвентаря");
    }

    public void OpenInventory()
    {
        if (!isInventoryOpen && !isAnimating)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(1f, fadeDuration));
            Logger.Log(LogModule.Inventory, "Инвентарь открыт");
        }
    }

    public void CloseInventory()
    {
        if (isInventoryOpen && !isAnimating)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(0f, fadeDuration));

            MenuManager.Instance?.CloseAllMenus();

            Logger.Log(LogModule.Inventory, "Инвентарь закрыт");
        }
    }

    public void ToggleInventory()
    {
        if (isAnimating) return;

        if (isInventoryOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    private IEnumerator FadeCanvasGroup(float targetAlpha, float duration)
    {
        isAnimating = true;
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        if (targetAlpha >= 1f)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        isInventoryOpen = (targetAlpha >= 1f);
        isAnimating = false;
        fadeCoroutine = null;
    }

    public bool IsInventoryOpen => isInventoryOpen;

    public void UpdateAllSlots()
    {
        if (inventoryGrid == null)
        {
            Logger.LogWarning(LogModule.Inventory, "InventoryGrid не найден для обновления слотов");
            return;
        }

        InventorySlot[] slots = inventoryGrid.GetComponentsInChildren<InventorySlot>();
        foreach (InventorySlot slot in slots)
        {
            InventoryItemMarker marker = slot.GetComponentInChildren<InventoryItemMarker>();
            slot.isOccupied = marker != null;
            slot.UpdateSlotText();
        }

        Logger.Log(LogModule.Inventory, $"Обновлено {slots.Length} слотов инвентаря");
    }

    private void ClearInventory()
    {
        if (inventoryGrid == null) return;

        int count = inventoryGrid.transform.childCount;
        foreach (Transform child in inventoryGrid.transform)
        {
            Destroy(child.gameObject);
        }

        if (count > 0)
        {
            Logger.Log(LogModule.Inventory, $"Очищено {count} слотов инвентаря");
        }
    }

    private void CreateInventorySlots()
    {
        if (inventoryGrid == null)
        {
            Logger.LogWarning(LogModule.Inventory, "InventoryGrid не найден для создания слотов");
            return;
        }

        if (itemSlotPrefab == null)
        {
            Logger.LogWarning(LogModule.Inventory, "itemSlotPrefab не назначен");
            return;
        }

        for (int i = 0; i < inventorySize; i++)
        {
            GameObject newSlot = Instantiate(itemSlotPrefab, inventoryGrid.transform);
            newSlot.name = $"Slot_{i}";

            if (newSlot.GetComponent<InventorySlot>() == null)
            {
                newSlot.AddComponent<InventorySlot>();
            }
        }
    }

    public void AddItem(ItemType itemType)
    {
        ItemData data = ItemDatabase.Instance.GetItemData(itemType);
        if (data == null)
        {
            Logger.LogError(LogModule.Inventory, $"Нет данных для предмета: {itemType}");
            return;
        }

        if (!data.canStack)
        {
            AddToEmptySlot(itemType, data);
            Logger.Log(LogModule.Inventory, $"Добавлен нестекаемый предмет {itemType}");
            return;
        }

        foreach (Transform child in inventoryGrid.transform)
        {
            InventoryItemMarker marker = child.GetComponentInChildren<InventoryItemMarker>();

            if (marker != null && marker.itemType == itemType)
            {
                if (marker.count < data.maxStack)
                {
                    marker.AddCount(1);
                    InventorySlot slot = child.GetComponent<InventorySlot>();
                    if (slot != null) slot.UpdateSlotText();
                    Logger.Log(LogModule.Inventory, $"Добавлен предмет {itemType} в существующий стек ({marker.count})");
                    return;
                }
            }
        }

        AddToEmptySlot(itemType, data);
        Logger.Log(LogModule.Inventory, $"Добавлен предмет {itemType} в пустой слот");
    }

    private void AddToEmptySlot(ItemType itemType, ItemData data)
    {
        if (inventoryGrid == null)
        {
            Logger.LogWarning(LogModule.Inventory, "InventoryGrid не найден для добавления предмета");
            return;
        }

        foreach (Transform child in inventoryGrid.transform)
        {
            InventoryItemMarker marker = child.GetComponentInChildren<InventoryItemMarker>();

            if (marker == null)
            {
                if (data.uiPrefab == null)
                {
                    Logger.LogWarning(LogModule.Inventory, $"uiPrefab для {itemType} не назначен");
                    return;
                }

                GameObject newItem = Instantiate(data.uiPrefab, child);
                newItem.transform.localPosition = Vector3.zero;
                newItem.transform.localScale = Vector3.one;

                InventoryItemMarker newMarker = newItem.GetComponent<InventoryItemMarker>();
                if (newMarker != null)
                {
                    newMarker.itemType = itemType;
                    newMarker.count = 1;
                    newMarker.UpdateUI();
                }

                InventorySlot slot = child.GetComponent<InventorySlot>();
                if (slot != null) slot.UpdateSlotText();
                return;
            }
        }

        Logger.Log(LogModule.Inventory, "Инвентарь полон");
    }
}
