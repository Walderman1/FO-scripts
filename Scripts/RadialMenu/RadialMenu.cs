using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RadialMenu : MonoBehaviour
{
    [Header("Настройки")]
    public int buttonCount = 0;
    public float radius = 250f;
    public List<string> buttonNames;
    public System.Action onMenuClosed;

    public List<RadialAction> buttonActions;

    [Header("Тип меню")]
    public MenuType menuType = MenuType.Circle;

    public enum MenuType
    {
        Circle,
        Fan,
        Vertical,
        Horizontal
    }

    [Header("Настройки веера")]
    public float fanStartAngle = -90f;
    public float fanEndAngle = 90f;
    public float fanRadius = 200f;

    [Header("Настройки списка")]
    public float itemSpacing = 60f;
    public float horizontalSpacing = 100f;
    public Vector2 listOffset = Vector2.zero;
    public bool useIcons = false;
    public List<Sprite> buttonIcons;

    public enum RadialAction
    {
        None,
        Talk,
        Take,
        Use,
        Examine,
        Move,
        UseMagic,
        OpenInventory,
        OpenEquipment,
        Cancel,
        Settings,
        Exit,
        Menu,
        Choice,
        Split,
        Equip,
    }

    public enum RadialMenuMode
    {
        Default,
        Choice,
        Context
    }

    private List<RadialButton> buttons = new List<RadialButton>();
    private CanvasGroup canvasGroup;
    public TextBeginner textBeginner;

    private GameObject longButtonPrefab;
    private GameObject shortButtonPrefab;

    public System.Action<RadialAction, string, int> onButtonClick;

    private InventoryItemMarker currentItem;
    private RadialMenuMode currentMode = RadialMenuMode.Default;
    private System.Action<int> onChoiceSelected;

    private List<string> defaultButtonNames;
    private List<RadialAction> defaultButtonActions;
    private int defaultButtonCount;

    private void Start()
    {
        LoadPrefabsFromResources();
        CacheComponents();

        defaultButtonNames = new List<string>(buttonNames);
        defaultButtonActions = new List<RadialAction>(buttonActions);
        defaultButtonCount = buttonCount;

        CreateMenu();
        Hide();

        Logger.Log(LogModule.RadialMenu, $"RadialMenu инициализирован с {buttonCount} кнопками, тип: {menuType}");
    }

    private void LoadPrefabsFromResources()
    {
        longButtonPrefab = Resources.Load<GameObject>("UI/Prefab/Buttons/LongButton");
        shortButtonPrefab = Resources.Load<GameObject>("UI/Prefab/Buttons/ShortButton");

        if (longButtonPrefab == null)
        {
            Logger.LogWarning(LogModule.RadialMenu, "LongButton префаб не найден, создание запасного");
            longButtonPrefab = CreateLongButtonPrefab();
        }
        if (shortButtonPrefab == null)
        {
            Logger.LogWarning(LogModule.RadialMenu, "ShortButton префаб не найден, создание запасного");
            shortButtonPrefab = CreateShortButtonPrefab();
        }
    }

    private GameObject CreateLongButtonPrefab()
    {
        GameObject btn = new GameObject("LongButton");
        RectTransform rt = btn.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 50);
        Image img = btn.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        img.raycastTarget = true;
        Button button = btn.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        button.colors = colors;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btn.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10, 5);
        textRt.offsetMax = new Vector2(-10, -5);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Button";
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.font = GetDefaultFont();

        Logger.Log(LogModule.RadialMenu, "Создан запасной LongButton префаб");
        return btn;
    }

    private GameObject CreateShortButtonPrefab()
    {
        GameObject btn = new GameObject("ShortButton");
        RectTransform rt = btn.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80, 80);
        Image img = btn.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        img.raycastTarget = true;
        Button button = btn.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        button.colors = colors;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btn.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(5, 5);
        textRt.offsetMax = new Vector2(-5, -5);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "B";
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.font = GetDefaultFont();

        Logger.Log(LogModule.RadialMenu, "Создан запасной ShortButton префаб");
        return btn;
    }

    private TMP_FontAsset GetDefaultFont()
    {
        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    private void CacheComponents()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void ClearMenu()
    {
        foreach (RadialButton btn in buttons)
        {
            if (btn != null && btn.gameObject != null)
                Destroy(btn.gameObject);
        }
        buttons.Clear();
        Logger.Log(LogModule.RadialMenu, "Меню очищено");
    }

    public void CreateMenu()
    {
        ClearMenu();

        if (buttonCount <= 0)
        {
            Logger.LogWarning(LogModule.RadialMenu, "buttonCount равен 0, меню не создано");
            return;
        }

        switch (menuType)
        {
            case MenuType.Circle:
                if (longButtonPrefab == null) return;
                CreateCircleMenu();
                break;
            case MenuType.Fan:
                if (longButtonPrefab == null) return;
                CreateFanMenu();
                break;
            case MenuType.Vertical:
                if (longButtonPrefab == null) return;
                CreateVerticalMenu();
                break;
            case MenuType.Horizontal:
                if (shortButtonPrefab == null) return;
                CreateHorizontalMenu();
                break;
        }

        Logger.Log(LogModule.RadialMenu, $"Создано меню с {buttonCount} кнопками, тип: {menuType}");
    }

    private void CreateCircleMenu()
    {
        float angleStep = 360f / buttonCount;
        for (int i = 0; i < buttonCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector2 position = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            CreateButton(position, i, longButtonPrefab);
        }
    }

    private void CreateFanMenu()
    {
        float angleStep = (fanEndAngle - fanStartAngle) / (buttonCount - 1);
        for (int i = 0; i < buttonCount; i++)
        {
            float angle = (fanStartAngle + i * angleStep) * Mathf.Deg2Rad;
            Vector2 position = new Vector2(Mathf.Cos(angle) * fanRadius, Mathf.Sin(angle) * fanRadius);
            CreateButton(position, i, longButtonPrefab);
        }
    }

    private void CreateVerticalMenu()
    {
        float totalHeight = (buttonCount - 1) * itemSpacing;
        float startY = totalHeight / 2f;
        for (int i = 0; i < buttonCount; i++)
        {
            float y = startY - (i * itemSpacing);
            Vector2 position = new Vector2(listOffset.x, y + listOffset.y);
            CreateButton(position, i, longButtonPrefab);
        }
    }

    private void CreateHorizontalMenu()
    {
        float spacing = (horizontalSpacing > 0) ? horizontalSpacing : itemSpacing;
        float totalWidth = (buttonCount - 1) * spacing;
        float startX = -totalWidth / 2f;
        for (int i = 0; i < buttonCount; i++)
        {
            float x = startX + (i * spacing);
            Vector2 position = new Vector2(x + listOffset.x, listOffset.y);
            CreateButton(position, i, shortButtonPrefab);
        }
    }

    private void CreateButton(Vector2 position, int index, GameObject prefab)
    {
        GameObject btn = Instantiate(prefab, transform);
        btn.transform.localPosition = position;

        string btnName = (index < buttonNames.Count) ? buttonNames[index] : $"Item {index + 1}";
        RadialAction btnAction = (index < buttonActions.Count) ? buttonActions[index] : RadialAction.None;

        if (menuType == MenuType.Horizontal && useIcons)
        {
            Image iconImage = btn.GetComponent<Image>();
            if (iconImage != null && index < buttonIcons.Count)
            {
                iconImage.sprite = buttonIcons[index];
                iconImage.preserveAspect = true;
            }

            TMP_Text textComponent = btn.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
                textComponent.gameObject.SetActive(false);
        }
        else
        {
            TMP_Text textComponent = btn.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
                textComponent.text = btnName;
        }

        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt != null)
            rt.rotation = Quaternion.identity;

        RadialButton radialBtn = btn.GetComponent<RadialButton>();
        if (radialBtn == null)
            radialBtn = btn.AddComponent<RadialButton>();
        radialBtn.Init(btnName, index);
        radialBtn.SetAction(btnAction);
        radialBtn.onClick += OnButtonClick;
        buttons.Add(radialBtn);
    }

    public bool IsVisible()
    {
        return canvasGroup != null && canvasGroup.alpha > 0.5f;
    }

    private void OnButtonClick(string name, int index)
    {
        RadialAction action = RadialAction.None;
        if (index < buttonActions.Count)
            action = buttonActions[index];

        Logger.Log(LogModule.RadialMenu, $"Выбрана кнопка: {name} (индекс {index}, действие {action})");

        if (currentMode == RadialMenuMode.Choice)
        {
            onChoiceSelected?.Invoke(index);
            currentMode = RadialMenuMode.Default;
            Hide();
            return;
        }

        if (currentItem != null)
        {
            if (action == RadialAction.None)
            {
                action = GetActionFromButtonName(name);
            }

            MenuManager.Instance?.HandleContextAction(currentItem, action);
            currentItem = null;
        }

        onButtonClick?.Invoke(action, name, index);

        MenuManager.Instance?.CloseAllMenus();
    }

    private RadialAction GetActionFromButtonName(string buttonName)
    {
        if (buttonName.Contains("Съесть") || buttonName.Contains("Выпить") ||
            buttonName.Contains("Использовать"))
            return RadialAction.Use;
        if (buttonName.Contains("Разделить"))
            return RadialAction.Split;
        if (buttonName.Contains("Подробнее") || buttonName.Contains("Осмотреть"))
            return RadialAction.Examine;
        if (buttonName.Contains("Выбросить"))
            return RadialAction.Exit;
        if (buttonName.Contains("Экипировать") || buttonName.Contains("Снять"))
            return RadialAction.Equip;
        return RadialAction.None;
    }

    public void SetChoiceMode(List<string> choices, System.Action<int> onChoiceSelected)
    {
        currentMode = RadialMenuMode.Choice;

        buttonNames = new List<string>(choices);
        buttonActions = new List<RadialAction>();
        for (int i = 0; i < choices.Count; i++)
        {
            buttonActions.Add(RadialAction.Choice);
        }
        buttonCount = choices.Count;

        this.onChoiceSelected = onChoiceSelected;

        CreateMenu();
        ShowAtCenter();

        Logger.Log(LogModule.RadialMenu, $"Установлен режим выбора с {choices.Count} вариантами");
    }

    public void SetCurrentItem(InventoryItemMarker item)
    {
        currentItem = item;
        Logger.Log(LogModule.RadialMenu, $"Установлен текущий предмет для контекстного меню: {item?.itemType}");
    }

    public void SetContextMenu(List<string> names, List<RadialAction> actions)
    {
        currentMode = RadialMenuMode.Context;

        buttonNames = new List<string>(names);
        buttonActions = new List<RadialAction>(actions);
        buttonCount = names.Count;

        while (buttonActions.Count < buttonCount)
        {
            buttonActions.Add(RadialAction.None);
        }

        CreateMenu();

        Logger.Log(LogModule.RadialMenu, $"Установлено контекстное меню с {names.Count} кнопками");
    }

    public void RestoreDefaultMenu()
    {
        currentMode = RadialMenuMode.Default;
        buttonNames = defaultButtonNames;
        buttonActions = defaultButtonActions;
        buttonCount = defaultButtonCount;
        CreateMenu();

        Logger.Log(LogModule.RadialMenu, "Восстановлено стандартное меню");
    }

    public RadialAction GetActionAtIndex(int index)
    {
        if (index >= 0 && index < buttonActions.Count)
            return buttonActions[index];
        return RadialAction.None;
    }

    public void ShowAtCenter()
    {
        Vector2 centerScreen = new Vector2(Screen.width / 2, Screen.height / 2);
        SetPosition(centerScreen);
        Show();
        Logger.Log(LogModule.RadialMenu, "Меню показано по центру");
    }

    public void ShowAtPosition(Vector2 screenPosition)
    {
        SetPosition(screenPosition);
        Show();
        Logger.Log(LogModule.RadialMenu, $"Меню показано по позиции: {screenPosition}");
    }

    private void SetPosition(Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform,
            screenPosition,
            GetComponentInParent<Canvas>().worldCamera,
            out Vector2 localPoint
        );
        transform.localPosition = localPoint;
    }

    public void Show()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
        transform.SetAsLastSibling();
        Logger.Log(LogModule.RadialMenu, "Меню показано");
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        onMenuClosed?.Invoke();
        Logger.Log(LogModule.RadialMenu, "Меню скрыто");
    }

    private void OnDestroy()
    {
        ClearMenu();
        Logger.Log(LogModule.RadialMenu, "RadialMenu уничтожен");
    }
}
