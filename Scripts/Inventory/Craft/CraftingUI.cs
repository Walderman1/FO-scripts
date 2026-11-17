using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using VContainer;

public class CraftingUI : MonoBehaviour
{
    [Header("=== ЗАВИСИМОСТИ ===")]
    [SerializeField] private string craftSlotsName = "CraftSlots";
    [SerializeField] private string goButtonName = "GoButton";
    [SerializeField] private string craftSearchName = "CraftSearch";

    [Header("=== ПРЕФАБЫ ===")]
    [SerializeField] private GameObject craftSlotPrefab;
    [SerializeField] private string craftSlotPrefabPath = "UI/Prefab/Buttons/CraftSlots";
    [SerializeField] private GameObject searchDropdownPrefab;
    [SerializeField] private string searchDropdownPrefabPath = "UI/Prefab/SearchDropdown";
    [SerializeField] private GameObject searchEntryPrefab;
    [SerializeField] private string searchEntryPrefabPath = "UI/Prefab/Entry_Null";

    [Header("=== НАСТРОЙКИ ===")]
    [SerializeField] private int maxCraftSlots = 6;
    [SerializeField] private int maxSearchLength = 30;
    [SerializeField] private int minSearchChars = 1;
    [SerializeField] private int maxSearchResults = 5;

    [Header("=== ССЫЛКИ ===")]
    [SerializeField] private Transform craftSlotsContainer;
    [SerializeField] private Button goButton;
    [SerializeField] private TMP_InputField craftSearchInput;

    [Inject] private InventoryUIManager _inventoryUIManager;
    [Inject] private CraftingSystem _craftingSystem;
    [Inject] private TooltipManager _tooltipManager;
    [Inject] private ItemDatabase _itemDatabase;
    [Inject] private CraftingDatabase _craftingDatabase;
    [Inject] private CraftingProgress _craftingProgress;
    [Inject] private MagicSpellPanel _magicSpellPanel;

    private CanvasGroup canvasGroup;
    private List<CraftSlotUI> activeSlots = new();
    private bool isOpen = false;

    private GameObject searchDropdown;
    private List<CraftingRecipe> currentSearchResults = new();

    public bool IsOpen => isOpen;

    private InventoryUIManager Inventory
    {
        get
        {
            if (_inventoryUIManager != null) return _inventoryUIManager;
            return SceneInjector.Instance?.CurrentSceneScope?.Resolve<InventoryUIManager>();
        }
    }

    private CraftingSystem Crafting
    {
        get
        {
            if (_craftingSystem != null) return _craftingSystem;
            return GameLifetimeScope.Current?.Container?.Resolve<CraftingSystem>();
        }
    }

    private ItemDatabase Items
    {
        get
        {
            if (_itemDatabase != null) return _itemDatabase;
            return GameLifetimeScope.Current?.Container?.Resolve<ItemDatabase>();
        }
    }

    private CraftingDatabase Database
    {
        get
        {
            if (_craftingDatabase != null) return _craftingDatabase;
            return GameLifetimeScope.Current?.Container?.Resolve<CraftingDatabase>();
        }
    }

    private CraftingProgress Progress
    {
        get
        {
            if (_craftingProgress != null) return _craftingProgress;
            return GameLifetimeScope.Current?.Container?.Resolve<CraftingProgress>();
        }
    }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        SetVisible(false);

        CacheReferences();
        LoadPrefabs();
        SetupGoButton();
        SetupSearch();
    }

    public bool IsPointerOverPanel(Vector2 screenPos)
    {
        if (!isOpen) return false;
        RectTransform rt = transform as RectTransform;
        if (rt == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, null);
    }

    private void CacheReferences()
    {
        if (craftSlotsContainer == null)
            craftSlotsContainer = FindChildByName(transform, craftSlotsName);

        if (goButton == null)
        {
            var goBtnObj = FindChildByName(transform, goButtonName);
            if (goBtnObj != null) goButton = goBtnObj.GetComponent<Button>();
        }

        if (craftSearchInput == null)
        {
            var searchObj = FindChildByName(transform, craftSearchName);
            if (searchObj != null)
                craftSearchInput = searchObj.GetComponent<TMP_InputField>()
                                ?? searchObj.GetComponentInChildren<TMP_InputField>();
        }
    }

    private void LoadPrefabs()
    {
        if (searchDropdownPrefab == null)
            searchDropdownPrefab = Resources.Load<GameObject>(searchDropdownPrefabPath);
        if (searchEntryPrefab == null)
            searchEntryPrefab = Resources.Load<GameObject>(searchEntryPrefabPath);
        if (craftSlotPrefab == null)
            craftSlotPrefab = Resources.Load<GameObject>(craftSlotPrefabPath);
    }

    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindChildByName(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void SetupGoButton()
    {
        if (goButton != null) goButton.onClick.AddListener(OnGoButtonClicked);
    }

    private void SetupSearch()
    {
        if (craftSearchInput == null) return;
        craftSearchInput.characterLimit = maxSearchLength;
        craftSearchInput.onValueChanged.AddListener(OnSearchChanged);
        craftSearchInput.onSubmit.AddListener(OnSearchSubmit);
    }

    public void Toggle() => SetVisible(!isOpen);

    public void SetVisible(bool visible)
    {
        isOpen = visible;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }

        if (!visible)
        {
            ClearCraftSlots();
            currentSearchResults.Clear();
            HideSearchDropdown();
            if (craftSearchInput != null) craftSearchInput.SetTextWithoutNotify("");
        }
        else
        {
            UICoordinator.BeforeOpen(UIKind.Craft);
        }
    }

    public bool TryAddItemToCraft(string itemId, int count = 1)
    {
        if (!isOpen) return false;

        var so = Items?.GetItem(itemId);
        if (so == null) return false;

        int alreadyInCraft = 0;
        foreach (var slot in activeSlots)
            if (slot.ItemId == itemId) alreadyInCraft += slot.Count;

        var inventory = Inventory;
        int inInventory = inventory != null ? inventory.GetItemCount(so) : 0;

        int canAdd = inInventory - alreadyInCraft;
        if (canAdd <= 0) return false;

        int toAdd = Mathf.Min(count, canAdd);

        foreach (var slot in activeSlots)
        {
            if (slot.ItemId == itemId)
            {
                slot.AddCount(toAdd);
                return true;
            }
        }

        if (activeSlots.Count >= maxCraftSlots) return false;

        CreateCraftSlot(so, toAdd);
        return true;
    }

    private void CreateCraftSlot(ItemSO so, int count)
    {
        if (craftSlotPrefab == null) return;

        var slotObj = Instantiate(craftSlotPrefab, craftSlotsContainer);
        slotObj.name = $"CraftSlot_{so.id}";
        slotObj.transform.localScale = Vector3.one;

        var slotUI = slotObj.GetComponent<CraftSlotUI>();
        if (slotUI == null) slotUI = slotObj.AddComponent<CraftSlotUI>();

        slotUI.Initialize(so, count, this);
        activeSlots.Add(slotUI);
    }

    public void RemoveSlot(CraftSlotUI slot)
    {
        if (slot == null) return;
        activeSlots.Remove(slot);
        Destroy(slot.gameObject);
    }

    private void ClearCraftSlots()
    {
        foreach (var slot in activeSlots)
            if (slot != null) Destroy(slot.gameObject);
        activeSlots.Clear();
    }

    private void OnGoButtonClicked()
    {
        Logger.LogVerbose(LogModule.Craft, $"GoButton clicked, slots={activeSlots.Count}");

        if (activeSlots.Count == 0) return;

        var input = new Dictionary<string, int>();
        foreach (var slot in activeSlots)
            input[slot.ItemId] = slot.Count;

        var craftingSystem = Crafting;
        if (craftingSystem == null)
        {
            Logger.LogError(LogModule.Craft, "CraftingSystem null");
            return;
        }

        string resultId;
        int resultCount;
        var recipe = craftingSystem.TryCraft(input, out resultId, out resultCount);

        Logger.LogVerbose(LogModule.Craft, $"TryCraft result={(recipe != null ? recipe.name : "null")} resultId={resultId} count={resultCount}");

        if (recipe == null) return;

        var inventory = Inventory;
        if (inventory == null) return;

        if (!craftingSystem.CanCraft(recipe, inventory))
        {
            Logger.LogVerbose(LogModule.Craft, "CanCraft=false");
            return;
        }

        int freeSpace = inventory.GetFreeSpaceFor(resultId, resultCount);
        if (freeSpace < resultCount)
        {
            _tooltipManager?.ShowTooltip($"Инвентарь полон для {resultId} x{resultCount}", Input.mousePosition);
            return;
        }

        if (!craftingSystem.ConsumeIngredients(recipe, inventory))
        {
            Logger.LogVerbose(LogModule.Craft, "ConsumeIngredients failed");
            return;
        }

        int added = inventory.AddItem(resultId, resultCount);
        Logger.LogVerbose(LogModule.Craft, $"AddItem: {resultId} x{added}");

        if (added < resultCount)
        {
            foreach (var ing in recipe.ingredients)
            {
                if (ing == null) continue;
                string id = ing.GetId();
                if (string.IsNullOrEmpty(id) || ing.count <= 0) continue;
                inventory.AddItem(id, ing.count);
            }
            return;
        }

        ClearCraftSlots();
    }

    private void OnSearchChanged(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < minSearchChars)
        {
            currentSearchResults.Clear();
            HideSearchDropdown();
            return;
        }

        var db = Database;
        var progress = Progress;
        if (db == null) return;

        var knownPool = db.GetAllRecipes()
            .Where(r => progress != null && progress.IsKnown(r))
            .ToList();

        string q = query.Trim().ToLowerInvariant();
        currentSearchResults = knownPool
            .Where(r =>
                r.GetDisplayName().ToLowerInvariant().Contains(q) ||
                r.name.ToLowerInvariant().Contains(q))
            .Take(maxSearchResults)
            .ToList();

        ShowSearchDropdown(currentSearchResults);
    }

    private void OnSearchSubmit(string query)
    {
        if (currentSearchResults.Count > 0)
            SelectRecipe(currentSearchResults[0]);
    }

    private void ShowSearchDropdown(List<CraftingRecipe> results)
    {
        HideSearchDropdown();
        if (results.Count == 0) return;
        if (craftSearchInput == null || searchDropdownPrefab == null) return;

        searchDropdown = Instantiate(searchDropdownPrefab, craftSearchInput.transform);
        searchDropdown.name = "SearchDropdown";
        searchDropdown.transform.localScale = Vector3.one;
        searchDropdown.transform.SetAsLastSibling();

        var rt = searchDropdown.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -2);
            rt.sizeDelta = new Vector2(0, 28 * results.Count);
        }

        foreach (var recipe in results)
            CreateSearchEntry(recipe);
    }

    private void CreateSearchEntry(CraftingRecipe recipe)
    {
        if (searchEntryPrefab == null) return;

        var entry = Instantiate(searchEntryPrefab, searchDropdown.transform);
        entry.name = "Entry_" + recipe.name;
        entry.transform.localScale = Vector3.one;

        var btn = entry.GetComponent<Button>();
        if (btn == null) btn = entry.GetComponentInChildren<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectRecipe(recipe));
        }

        var text = entry.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = recipe.GetDisplayName();
            text.raycastTarget = false;
        }
    }

    private void SelectRecipe(CraftingRecipe recipe)
    {
        ClearCraftSlots();
        currentSearchResults.Clear();
        HideSearchDropdown();
        if (craftSearchInput != null) craftSearchInput.SetTextWithoutNotify("");

        var inventory = Inventory;
        if (inventory == null) return;

        foreach (var ing in recipe.ingredients)
        {
            if (ing == null) continue;
            string id = ing.GetId();
            if (string.IsNullOrEmpty(id)) continue;

            int have = inventory.GetItemCount(id);
            int take = Mathf.Min(have, ing.count);
            if (take > 0) TryAddItemToCraft(id, take);
        }
    }

    private void HideSearchDropdown()
    {
        if (searchDropdown != null)
        {
            Destroy(searchDropdown);
            searchDropdown = null;
        }
    }
}