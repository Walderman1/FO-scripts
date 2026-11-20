using DialogueSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class RadialMenu : MonoBehaviour
{
    [Header("Роль меню")]
    [Tooltip("Определяет назначение меню. Должно совпадать с тегом объекта:\n" +
         "• Main — тег «RadialMenu», быстрое меню панелей (I/E/J/настройки), открывается ПКМ\n" +
         "• Choice — тег «ChoicePanel», только для вариантов диалогов, не открывается вручную\n" +
         "• Context — тег «ContextMenu», действия над предметом, открывается ПКМ по слоту")]
    [SerializeField] private RadialMenuRole role = RadialMenuRole.Main;

    public RadialMenuRole Role => role;

    public enum RadialMenuRole
    {
        [InspectorName("Быстрое меню панелей")] Main,
        [InspectorName("Выбор в диалоге")] Choice,
        [InspectorName("Действия с предметом")] Context
    }
    [Header("Префабы кнопок")]
    [SerializeField] private GameObject longButtonPrefab;
    [SerializeField] private GameObject shortButtonPrefab;

    [Header("Настройки")]
    public int buttonCount = 0;
    public float radius = 250f;
    public List<string> buttonNames;
    public System.Action onMenuClosed;

    public List<RadialAction> buttonActions;

    [Header("Тип меню")]
    public MenuType menuType = MenuType.Circle;

    public enum MenuType { Circle, Fan, Vertical, Horizontal }

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
        None, Talk, Take, Use, Examine, Move, UseMagic,
        OpenInventory, OpenEquipment, Cancel, Settings, Exit,
        Menu, Choice, Split, Equip,
    }

    public enum RadialMenuMode { Default, Choice, Context }

    private List<RadialButton> buttons = new List<RadialButton>();
    private readonly Dictionary<GameObject, Stack<RadialButton>> buttonPools = new Dictionary<GameObject, Stack<RadialButton>>();
    private readonly Dictionary<RadialButton, GameObject> buttonPrefabMap = new Dictionary<RadialButton, GameObject>();
    private CanvasGroup canvasGroup;
    public TextBeginner textBeginner;

    public System.Action<RadialAction, string, int> onButtonClick;

    private InventoryItemMarker currentItem;
    private RadialMenuMode currentMode = RadialMenuMode.Default;
    private System.Action<int> onChoiceSelected;

    private List<string> defaultButtonNames;
    private List<RadialAction> defaultButtonActions;
    private int defaultButtonCount;

    public bool IsVisible()
    {
        return canvasGroup != null && canvasGroup.alpha > 0.5f;
    }

    public void ClearButtons()
    {
        foreach (RadialButton btn in buttons)
            ReturnButton(btn);
        buttons.Clear();
    }

    private void Start()
    {
        ValidateRoleTag();
        LoadPrefabsFromResources();
        CacheComponents();
        Logger.LogVerbose(LogModule.RadialMenu, $"Start menu={name}");
        LoadPrefabsFromResources();
        CacheComponents();

        defaultButtonNames = new List<string>(buttonNames);
        defaultButtonActions = new List<RadialAction>(buttonActions);
        defaultButtonCount = buttonCount;

        CreateMenu();
        Hide();
    }

    private void ValidateRoleTag()
    {
        string expected = role switch
        {
            RadialMenuRole.Main => "RadialMenu",
            RadialMenuRole.Choice => "ChoicePanel",
            RadialMenuRole.Context => "ContextMenu",
            _ => null
        };

        if (expected == null) return;

        string actual = "?";
        try { actual = gameObject.tag; } catch (UnityException) { }

        if (actual != expected)
        {
            Logger.LogWarning(LogModule.RadialMenu,
                $"[{gameObject.name}] Роль '{role}' ожидает тег '{expected}', а стоит '{actual}'. " +
                $"RadialMenuRegistry не сможет корректно определить меню.");
        }
    }

    private void LoadPrefabsFromResources()
    {
        if (longButtonPrefab == null)
        {
            longButtonPrefab = Resources.Load<GameObject>("UI/Prefab/Buttons/LongButton");
            if (longButtonPrefab == null)
            {
                Logger.LogWarning(LogModule.RadialMenu, $"longButtonPrefab не назначен и не найден: Resources/UI/Prefab/Buttons/LongButton. Создан fallback.");
                longButtonPrefab = CreateLongButtonPrefab();
            }
        }

        if (shortButtonPrefab == null)
        {
            shortButtonPrefab = Resources.Load<GameObject>("UI/Prefab/Buttons/ShortButton");
            if (shortButtonPrefab == null)
            {
                Logger.LogWarning(LogModule.RadialMenu, $"shortButtonPrefab не назначен и не найден: Resources/UI/Prefab/Buttons/ShortButton. Создан fallback.");
                shortButtonPrefab = CreateShortButtonPrefab();
            }
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
        Logger.LogVerbose(LogModule.RadialMenu, $"CacheComponents menu={name} canvasGroup={(canvasGroup != null)}");
    }

    private void ClearMenu()
    {
        foreach (RadialButton btn in buttons)
        {
            if (btn != null && btn.gameObject != null)
                Destroy(btn.gameObject);
        }
        buttons.Clear();
    }

    public void CreateMenu()
    {
        ClearMenu();

        if (buttonCount <= 0)
        {
            if (gameObject.tag != "ChoicePanel")
                Logger.LogVerbose(LogModule.RadialMenu, $"RadialMenu ({gameObject.name}): buttonCount is 0, no menu created");
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
        float angleStep = (fanEndAngle - fanStartAngle) / Mathf.Max(1, buttonCount - 1);
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

    private RadialButton GetButton(GameObject prefab)
    {
        if (prefab == null) return null;

        if (!buttonPools.TryGetValue(prefab, out var stack))
        {
            stack = new Stack<RadialButton>();
            buttonPools[prefab] = stack;
        }

        if (stack.Count > 0)
        {
            RadialButton pooled = stack.Pop();
            if (pooled != null)
            {
                pooled.gameObject.SetActive(true);
                return pooled;
            }
        }

        GameObject go = Instantiate(prefab, transform);
        RadialButton radialBtn = go.GetComponent<RadialButton>();
        if (radialBtn == null) radialBtn = go.AddComponent<RadialButton>();
        return radialBtn;
    }

    private void ReturnButton(RadialButton btn)
    {
        if (btn == null || btn.gameObject == null) return;

        btn.onClick -= OnButtonClick;
        btn.gameObject.SetActive(false);
        btn.transform.SetParent(transform, false);

        if (buttonPrefabMap.TryGetValue(btn, out var prefab) && prefab != null)
        {
            if (!buttonPools.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<RadialButton>();
                buttonPools[prefab] = stack;
            }
            stack.Push(btn);
        }
        else
        {
            Destroy(btn.gameObject);
        }

        buttonPrefabMap.Remove(btn);
    }

    private void CreateButton(Vector2 position, int index, GameObject prefab)
    {
        RadialButton radialBtn = GetButton(prefab);
        if (radialBtn == null) return;

        GameObject btn = radialBtn.gameObject;
        btn.transform.localPosition = position;
        btn.transform.localScale = Vector3.one;

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
            {
                textComponent.gameObject.SetActive(true);
                textComponent.text = btnName;
            }
        }

        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt != null)
            rt.rotation = Quaternion.identity;

        radialBtn.Init(btnName, index);
        radialBtn.SetAction(btnAction);
        radialBtn.onClick -= OnButtonClick;
        radialBtn.onClick += OnButtonClick;

        buttons.Add(radialBtn);
        buttonPrefabMap[radialBtn] = prefab;
    }

    private void OnButtonClick(string name, int index)
    {
        RadialAction action = RadialAction.None;
        if (index < buttonActions.Count)
            action = buttonActions[index];

        Logger.LogVerbose(LogModule.RadialMenu, $"OnButtonClick name={name} index={index} action={action} mode={currentMode} hasListener={(onButtonClick != null)} handlerInstance={(RadialMenuActionHandler.Instance != null)} hasCurrentItem={(currentItem != null)}");

        if (currentMode == RadialMenuMode.Choice)
        {
            Logger.LogVerbose(LogModule.RadialMenu, "choice mode → onChoiceSelected");
            onChoiceSelected?.Invoke(index);
            currentMode = RadialMenuMode.Default;
            Hide();
            return;
        }

        if (currentItem != null)
        {
            if (action == RadialAction.None)
                action = GetActionFromButtonName(name);

            Logger.LogVerbose(LogModule.RadialMenu, $"context item, action={action}");
            var controller = SceneInjector.Instance?.CurrentSceneScope?.Resolve<RadialMenuController>();
            controller?.HandleContextAction(currentItem, action);
            currentItem = null;

            // Действие контекстного меню уже выполнено (например «Выбросить» → DropItem).
            // Пробрасывать его дальше в RadialMenuActionHandler нельзя: там Exit = QuitGame,
            // из-за чего игра закрывалась при выбрасывании любого предмета.
            Hide();

            var contextController = SceneInjector.Instance?.CurrentSceneScope?.Resolve<RadialMenuController>();
            contextController?.CloseAllMenus();
            return;
        }

        if (onButtonClick != null)
        {
            Logger.LogVerbose(LogModule.RadialMenu, "→ onButtonClick listeners");
            onButtonClick(action, name, index);
        }
        else if (RadialMenuActionHandler.Instance != null)
        {
            Logger.LogVerbose(LogModule.RadialMenu, "→ RadialMenuActionHandler.Instance");
            RadialMenuActionHandler.Instance.ProcessAction(action, name, index);
        }
        else
        {
            Logger.LogError(LogModule.RadialMenu, "No listener AND handler Instance == null");
        }

        var ctrl = SceneInjector.Instance?.CurrentSceneScope?.Resolve<RadialMenuController>();
        ctrl?.CloseAllMenus();
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
        Logger.LogVerbose(LogModule.RadialMenu, $"SetChoiceMode menu={name} choices={choices.Count}");
        currentMode = RadialMenuMode.Choice;

        buttonNames = new List<string>(choices);
        buttonActions = new List<RadialAction>();
        for (int i = 0; i < choices.Count; i++)
            buttonActions.Add(RadialAction.Choice);

        buttonCount = choices.Count;
        this.onChoiceSelected = onChoiceSelected;

        CreateMenu();
        ShowAtCenter();

        transform.SetAsLastSibling();

        Logger.Log(LogModule.RadialMenu, $"[{gameObject.name}] SetChoiceMode: {choices.Count} variants");
    }

    public void SetCurrentItem(InventoryItemMarker item)
    {
        Logger.LogVerbose(LogModule.RadialMenu, $"SetCurrentItem menu={name} item={(item != null ? item.name : "null")}");
        currentItem = item;
    }

    public void SetContextMenu(List<string> names, List<RadialAction> actions)
    {
        Logger.LogVerbose(LogModule.RadialMenu, $"SetContextMenu menu={name} buttons={names.Count}");
        currentMode = RadialMenuMode.Context;

        buttonNames = new List<string>(names);
        buttonActions = new List<RadialAction>(actions);
        buttonCount = names.Count;

        while (buttonActions.Count < buttonCount)
            buttonActions.Add(RadialAction.None);

        CreateMenu();
    }

    public void RestoreDefaultMenu()
    {
        currentMode = RadialMenuMode.Default;
        buttonNames = defaultButtonNames;
        buttonActions = defaultButtonActions;
        buttonCount = defaultButtonCount;
        CreateMenu();
    }

    public RadialAction GetActionAtIndex(int index)
    {
        if (index >= 0 && index < buttonActions.Count)
            return buttonActions[index];
        return RadialAction.None;
    }

    public void ShowAtCenter()
    {
        Logger.LogVerbose(LogModule.RadialMenu, $"ShowAtCenter menu={name}");
        Vector2 centerScreen = new Vector2(Screen.width / 2, Screen.height / 2);
        SetPosition(centerScreen);
        Show();
    }

    public void ShowAtPosition(Vector2 screenPosition)
    {
        Logger.LogVerbose(LogModule.RadialMenu, $"ShowAtPosition menu={name} pos={screenPosition}");
        SetPosition(screenPosition);
        Show();
    }

    private void SetPosition(Vector2 screenPosition)
    {
        RectTransform parentRect = transform.parent as RectTransform;
        if (parentRect == null)
        {
            Logger.LogError(LogModule.RadialMenu, $"[{gameObject.name}] Parent is not RectTransform!");
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Logger.LogError(LogModule.RadialMenu, $"[{gameObject.name}] No Canvas in parent!");
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPosition,
            canvas.worldCamera,
            out Vector2 localPoint
        );
        transform.localPosition = localPoint;
    }

    public void Show()
    {
        Logger.LogVerbose(LogModule.RadialMenu, $"Show menu={name} buttons={buttons.Count} canvasGroup={(canvasGroup != null)}");

        if (canvasGroup == null)
        {
            Logger.LogError(LogModule.RadialMenu, $"[{gameObject.name}] canvasGroup is null!");
            return;
        }

        gameObject.SetActive(true);

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        transform.SetAsLastSibling();
    }

    public void Hide()
    {
        Logger.LogVerbose(LogModule.RadialMenu, $"Hide menu={name}");
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        onMenuClosed?.Invoke();
    }

    private void OnDestroy()
    {
        foreach (var stack in buttonPools.Values)
        {
            foreach (var btn in stack)
                if (btn != null) Destroy(btn.gameObject);
            stack.Clear();
        }
        buttonPools.Clear();
        buttonPrefabMap.Clear();
        buttons.Clear();
    }
}