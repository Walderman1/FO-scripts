using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftSlotUI : MonoBehaviour
{
    [SerializeField] private string iconObjectName = "Icon";
    [SerializeField] private string countObjectName = "Count";

    [Header("Цвета")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color missingCountColor = new Color(1f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color missingIconColor = new Color(1f, 0.55f, 0.55f, 0.75f);

    private Image iconImage;
    private TMP_Text countText;

    private int count = 1;
    private CraftingUI parentUI;
    private bool lastEnough = true;
    private bool hasRefreshed = false;

    public ItemSO ItemSO { get; private set; }
    public string ItemId => ItemSO != null ? ItemSO.id : "";
    public int Count => count;

    private void Awake()
    {
        CacheReferences();
    }

    private void Update()
    {
        Refresh();
    }

    private void CacheReferences()
    {
        Transform iconT = transform.Find(iconObjectName);
        if (iconT != null) iconImage = iconT.GetComponent<Image>();
        if (iconImage == null) iconImage = GetComponentInChildren<Image>();

        Transform countT = transform.Find(countObjectName);
        if (countT != null) countText = countT.GetComponent<TMP_Text>();
        if (countText == null) countText = GetComponentInChildren<TMP_Text>();
    }

    public void Initialize(ItemSO so, int cnt, CraftingUI parent)
    {
        ItemSO = so;
        count = cnt;
        parentUI = parent;

        if (iconImage == null) CacheReferences();

        if (so != null && so.icon != null && iconImage != null)
        {
            iconImage.sprite = so.icon;
            iconImage.color = normalColor;
            iconImage.preserveAspect = true;
        }

        UpdateCountText();

        var btn = GetComponent<Button>();
        if (btn == null) btn = gameObject.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnSlotClicked);

        lastEnough = true;
        hasRefreshed = false;
        Refresh();
    }

    public void Refresh()
    {
        var inventory = GameReferences.Instance?.InventoryUIManager;
        if (inventory == null || ItemSO == null) return;

        int available = inventory.GetItemCount(ItemSO);
        bool enough = available >= count;

        if (hasRefreshed && enough == lastEnough) return;

        hasRefreshed = true;
        lastEnough = enough;

        if (countText != null)
            countText.color = enough ? normalColor : missingCountColor;

        if (iconImage != null)
            iconImage.color = enough ? normalColor : missingIconColor;
    }

    private void OnSlotClicked()
    {
        count--;
        if (count <= 0)
        {
            parentUI?.RemoveSlot(this);
        }
        else
        {
            UpdateCountText();
            Refresh();
        }
    }

    public void AddCount(int amount)
    {
        count += amount;
        UpdateCountText();
        Refresh();
    }

    private void UpdateCountText()
    {
        if (countText != null)
            countText.text = count > 1 ? count.ToString() : "";
    }
}