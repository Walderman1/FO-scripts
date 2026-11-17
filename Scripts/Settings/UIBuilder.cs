using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIBuilder : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private MenuUIConfig config;
    [SerializeField] private TMP_FontAsset defaultFont;

    [SerializeField] private GameObject buttonPrefab;

    private bool isInitialized = false;

    public void Initialize(MenuUIConfig config, TMP_FontAsset defaultFont, GameObject buttonPrefab)
    {
        this.config = config;
        this.defaultFont = defaultFont;
        this.buttonPrefab = buttonPrefab;
        isInitialized = true;
    }

    public GameObject CreateButton(Transform parent, string name, System.Action onClick = null)
    {
        if (!isInitialized || buttonPrefab == null) return null;

        GameObject btn = Instantiate(buttonPrefab, parent);
        btn.name = name + "Button";

        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = config.buttonSize;
        }

        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = true;
            img.color = Color.white;
        }

        Button button = btn.GetComponent<Button>();
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            colors.selectedColor = new Color(0.85f, 0.85f, 0.85f);
            button.colors = colors;

            if (onClick != null)
                button.onClick.AddListener(() => onClick());
        }

        TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = name;
            tmp.fontSize = config.buttonFontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
        }

        return btn;
    }

    public GameObject CreateBackButton(Transform parent, System.Action onClick, Vector2? position = null, Vector2? size = null)
    {
        GameObject backBtn = new GameObject("BackButton");
        backBtn.transform.SetParent(parent, false);

        RectTransform backRt = backBtn.AddComponent<RectTransform>();
        backRt.anchorMin = new Vector2(0.5f, 0f);
        backRt.anchorMax = new Vector2(0.5f, 0f);
        backRt.pivot = new Vector2(0.5f, 0f);
        backRt.sizeDelta = size ?? new Vector2(200, 50);
        backRt.anchoredPosition = position ?? new Vector2(0, 30);

        Image backImg = backBtn.AddComponent<Image>();
        backImg.color = Color.white;
        backImg.raycastTarget = true;

        Button backButton = backBtn.AddComponent<Button>();
        ColorBlock colors = backButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        backButton.colors = colors;

        if (onClick != null) backButton.onClick.AddListener(() => onClick());

        GameObject backTextObj = new GameObject("Text");
        backTextObj.transform.SetParent(backBtn.transform, false);
        RectTransform backTextRt = backTextObj.AddComponent<RectTransform>();
        backTextRt.anchorMin = Vector2.zero;
        backTextRt.anchorMax = Vector2.one;
        backTextRt.offsetMin = Vector2.zero;
        backTextRt.offsetMax = Vector2.zero;

        TextMeshProUGUI backText = backTextObj.AddComponent<TextMeshProUGUI>();
        backText.text = config.backButtonText;
        backText.fontSize = config.buttonFontSize;
        backText.alignment = TextAlignmentOptions.Center;
        backText.color = Color.black;
        backText.raycastTarget = false;
        if (defaultFont != null) backText.font = defaultFont;

        return backBtn;
    }

    public GameObject CreatePlaceholderText(Transform parent, string text, Vector2? position = null, Vector2? size = null)
    {
        GameObject placeholder = new GameObject("PlaceholderText");
        placeholder.transform.SetParent(parent, false);

        RectTransform rt = placeholder.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size ?? config.placeholderSize;
        rt.anchoredPosition = position ?? Vector2.zero;

        TextMeshProUGUI tmp = placeholder.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = config.placeholderFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = config.placeholderTextColor;
        tmp.raycastTarget = false;
        if (defaultFont != null) tmp.font = defaultFont;

        return placeholder;
    }
}