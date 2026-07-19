using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIBuilder : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private MenuUIConfig config;
    [SerializeField] private TMP_FontAsset defaultFont;

    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private GameObject sliderContainerPrefab;
    [SerializeField] private GameObject togglePrefab;
    [SerializeField] private GameObject tabPrefab;

    private bool isInitialized = false;

    public void Initialize(MenuUIConfig config, TMP_FontAsset defaultFont,
                          GameObject buttonPrefab, GameObject sliderContainerPrefab,
                          GameObject togglePrefab, GameObject tabPrefab)
    {
        this.config = config;
        this.defaultFont = defaultFont;
        this.buttonPrefab = buttonPrefab;
        this.sliderContainerPrefab = sliderContainerPrefab;
        this.togglePrefab = togglePrefab;
        this.tabPrefab = tabPrefab;
        isInitialized = true;
    }

    #region Buttons

    public GameObject CreateButton(Transform parent, string name, System.Action onClick = null, bool isSmall = false)
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
            rt.sizeDelta = isSmall ? config.smallButtonSize : config.buttonSize;
        }

        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = true;
            img.color = config.buttonNormalColor;
        }

        Button button = btn.GetComponent<Button>();
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = config.buttonNormalColor;
            colors.highlightedColor = config.buttonHighlightedColor;
            colors.pressedColor = config.buttonPressedColor;
            colors.selectedColor = config.buttonSelectedColor;
            button.colors = colors;

            if (onClick != null)
                button.onClick.AddListener(() => onClick());
        }

        TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = name;
            tmp.fontSize = isSmall ? config.smallButtonFontSize : config.buttonFontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
        }

        return btn;
    }

    public GameObject CreateSmallButton(Transform parent, string name, System.Action onClick = null)
    {
        return CreateButton(parent, name, onClick, true);
    }

    #endregion

    #region Labels

    public GameObject CreateSettingLabel(Transform parent, string text, Vector2 position)
    {
        GameObject label = new GameObject("Label_" + text);
        label.transform.SetParent(parent, false);

        RectTransform rt = label.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = config.labelSize;
        rt.anchoredPosition = position;

        TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = config.labelFontSize;
        tmp.alignment = TextAlignmentOptions.MidlineRight;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        if (defaultFont != null) tmp.font = defaultFont;

        return label;
    }

    public GameObject CreateTitle(Transform parent, string text, Vector2? position = null)
    {
        GameObject title = new GameObject("Title");
        title.transform.SetParent(parent, false);

        RectTransform rt = title.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = config.titleSize;
        rt.anchoredPosition = position ?? new Vector2(0, config.titleYOffset);

        TextMeshProUGUI tmp = title.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = config.titleFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.fontStyle = FontStyles.Bold;
        if (defaultFont != null) tmp.font = defaultFont;

        return title;
    }

    #endregion

    #region Sliders

    public GameObject CreateSlider(Transform parent, Vector2 position, float minValue, float maxValue, float defaultValue,
                                   System.Action<float> onValueChanged = null)
    {
        if (!isInitialized || sliderContainerPrefab == null) return null;

        GameObject sliderContainer = Instantiate(sliderContainerPrefab, parent);
        sliderContainer.name = "SliderContainer";

        RectTransform containerRt = sliderContainer.GetComponent<RectTransform>();
        if (containerRt != null)
        {
            containerRt.anchorMin = new Vector2(0.5f, 0.5f);
            containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.pivot = new Vector2(0.5f, 0.5f);
            containerRt.anchoredPosition = position;
            containerRt.sizeDelta = config.sliderSize;
        }

        Slider slider = sliderContainer.GetComponentInChildren<Slider>();
        if (slider != null)
        {
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = defaultValue;
            slider.wholeNumbers = true;

            if (onValueChanged != null)
                slider.onValueChanged.AddListener((value) => onValueChanged(value));
        }

        TMP_Text[] texts = sliderContainer.GetComponentsInChildren<TMP_Text>();
        foreach (TMP_Text text in texts)
        {
            if (text.name == "ValueLabel" || text.name == "Label")
            {
                text.text = defaultValue.ToString();

                if (slider != null)
                {
                    Slider localSlider = slider;
                    TMP_Text localText = text;
                    slider.onValueChanged.AddListener((float value) =>
                    {
                        localText.text = value.ToString("F0");
                    });
                }
            }
        }

        return sliderContainer;
    }

    public GameObject CreateVolumeSlider(Transform parent, Vector2 position)
    {
        if (!isInitialized || sliderContainerPrefab == null) return null;

        GameObject sliderContainer = Instantiate(sliderContainerPrefab, parent);
        sliderContainer.name = "VolumeSliderContainer";

        RectTransform containerRt = sliderContainer.GetComponent<RectTransform>();
        if (containerRt != null)
        {
            containerRt.anchorMin = new Vector2(0.5f, 0.5f);
            containerRt.anchorMax = new Vector2(0.5f, 0.5f);
            containerRt.pivot = new Vector2(0.5f, 0.5f);
            containerRt.anchoredPosition = position;
            containerRt.sizeDelta = config.sliderSize;
        }

        Slider slider = sliderContainer.GetComponentInChildren<Slider>();
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 10f;
            slider.value = AudioListener.volume * 10f;
            slider.wholeNumbers = true;

            slider.onValueChanged.AddListener((float value) =>
            {
                float volume = value / 10f;
                AudioListener.volume = volume;
                PlayerPrefs.SetFloat("Volume", volume);
                PlayerPrefs.Save();
            });
        }

        TMP_Text[] texts = sliderContainer.GetComponentsInChildren<TMP_Text>();
        foreach (TMP_Text text in texts)
        {
            if (text.name == "ValueLabel" || text.name == "Label")
            {
                text.text = (AudioListener.volume * 10f).ToString("F0");

                if (slider != null)
                {
                    Slider localSlider = slider;
                    TMP_Text localText = text;
                    slider.onValueChanged.AddListener((float value) =>
                    {
                        localText.text = value.ToString("F0");
                    });
                }
            }
        }

        bool hasMarks = false;
        foreach (Transform child in sliderContainer.transform)
        {
            if (child.name.StartsWith("Mark_"))
            {
                hasMarks = true;
                break;
            }
        }

        if (!hasMarks)
        {
            for (int i = 0; i <= 10; i++)
            {
                GameObject mark = new GameObject("Mark_" + i);
                mark.transform.SetParent(sliderContainer.transform, false);
                RectTransform markRt = mark.AddComponent<RectTransform>();
                markRt.anchorMin = new Vector2(0.5f, 0.5f);
                markRt.anchorMax = new Vector2(0.5f, 0.5f);
                markRt.pivot = new Vector2(0.5f, 0.5f);

                float xPos = -140f + (i * 28f);
                markRt.anchoredPosition = new Vector2(xPos, -18);
                markRt.sizeDelta = new Vector2(20, 15);

                TextMeshProUGUI tmp = mark.AddComponent<TextMeshProUGUI>();
                tmp.text = i.ToString();
                tmp.fontSize = 14;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = config.markTextColor;
                tmp.raycastTarget = false;
                if (defaultFont != null) tmp.font = defaultFont;
            }
        }

        return sliderContainer;
    }

    #endregion

    #region Toggles

    public GameObject CreateToggle(Transform parent, Vector2 position, bool defaultValue, System.Action<bool> onValueChanged = null)
    {
        if (!isInitialized || togglePrefab == null) return null;

        GameObject toggleObj = Instantiate(togglePrefab, parent);
        toggleObj.name = "Toggle";

        RectTransform rt = toggleObj.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = config.toggleSize;
        }

        Toggle toggle = toggleObj.GetComponent<Toggle>();
        if (toggle != null) toggle.isOn = defaultValue;

        Image bg = toggleObj.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = defaultValue ? config.toggleOnColor : config.toggleOffColor;
        }

        RectTransform handleRt = null;
        foreach (Transform child in toggleObj.transform)
        {
            Image img = child.GetComponent<Image>();
            if (img != null)
            {
                handleRt = child.GetComponent<RectTransform>();
                if (handleRt != null)
                {
                    float offset = config.toggleHandleOffset;
                    handleRt.anchoredPosition = defaultValue ? new Vector2(offset, 0) : new Vector2(2, 0);
                }
                break;
            }
        }

        if (toggle != null && bg != null)
        {
            toggle.onValueChanged.AddListener((bool value) =>
            {
                bg.color = value ? config.toggleOnColor : config.toggleOffColor;
                if (handleRt != null)
                {
                    float offset = config.toggleHandleOffset;
                    handleRt.anchoredPosition = value ? new Vector2(offset, 0) : new Vector2(2, 0);
                }
                onValueChanged?.Invoke(value);
            });
        }

        return toggleObj;
    }

    #endregion

    #region Dropdowns

    public GameObject CreateDropdown(Transform parent, Vector2 position, string[] options, System.Action<int> onValueChanged = null)
    {
        GameObject dropdownObj = new GameObject("Dropdown");
        dropdownObj.transform.SetParent(parent, false);

        RectTransform rt = dropdownObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = config.dropdownSize;
        rt.anchoredPosition = position;

        Image bg = dropdownObj.AddComponent<Image>();
        bg.color = config.dropdownBgColor;
        bg.raycastTarget = true;

        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
        dropdown.options.Clear();
        foreach (string opt in options)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(opt));
        }
        dropdown.value = 0;

        if (onValueChanged != null)
            dropdown.onValueChanged.AddListener((value) => onValueChanged(value));

        GameObject template = new GameObject("Template");
        template.transform.SetParent(dropdownObj.transform, false);
        template.SetActive(false);
        RectTransform templateRt = template.AddComponent<RectTransform>();
        templateRt.anchorMin = new Vector2(0f, 1f);
        templateRt.anchorMax = new Vector2(1f, 1f);
        templateRt.pivot = new Vector2(0.5f, 1f);
        templateRt.sizeDelta = new Vector2(0, 0);
        templateRt.anchoredPosition = new Vector2(0, 2);
        template.AddComponent<Image>().color = config.dropdownBgColor;

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(template.transform, false);
        RectTransform viewportRt = viewport.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = new Vector2(2, 2);
        viewportRt.offsetMax = new Vector2(-2, -2);
        viewport.AddComponent<Image>().color = config.dropdownItemColor;
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRt = content.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = new Vector2(0, config.dropdownItemHeight * options.Length);

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childControlHeight = true;
        vlg.spacing = 2;

        foreach (string opt in options)
        {
            GameObject item = new GameObject("Item_" + opt);
            item.transform.SetParent(content.transform, false);
            RectTransform itemRt = item.AddComponent<RectTransform>();
            itemRt.sizeDelta = new Vector2(0, config.dropdownItemHeight);

            Image itemBg = item.AddComponent<Image>();
            itemBg.color = config.dropdownItemColor;

            Toggle toggle = item.AddComponent<Toggle>();
            toggle.isOn = false;
            toggle.targetGraphic = itemBg;

            GameObject itemText = new GameObject("ItemText");
            itemText.transform.SetParent(item.transform, false);
            RectTransform itemTextRt = itemText.AddComponent<RectTransform>();
            itemTextRt.anchorMin = Vector2.zero;
            itemTextRt.anchorMax = Vector2.one;
            itemTextRt.offsetMin = new Vector2(10, 0);
            itemTextRt.offsetMax = new Vector2(-10, 0);

            TextMeshProUGUI tmp = itemText.AddComponent<TextMeshProUGUI>();
            tmp.text = opt;
            tmp.fontSize = config.dropdownFontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            if (defaultFont != null) tmp.font = defaultFont;
            tmp.raycastTarget = false;
        }

        dropdown.template = templateRt;
        dropdown.itemText = dropdownObj.GetComponentInChildren<TMP_Text>();

        return dropdownObj;
    }

    #endregion

    #region Tabs

    public GameObject CreateTabButton(Transform parent, string name, System.Action onClick = null)
    {
        if (!isInitialized || tabPrefab == null) return null;

        GameObject tab = Instantiate(tabPrefab, parent);
        tab.name = name + "Tab";

        LayoutElement le = tab.GetComponent<LayoutElement>() ?? tab.AddComponent<LayoutElement>();
        le.minWidth = 100;
        le.flexibleWidth = 1;

        TMP_Text tmp = tab.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = name;
            tmp.fontSize = config.tabFontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            if (defaultFont != null) tmp.font = defaultFont;
        }

        Button button = tab.GetComponent<Button>();
        if (button != null && onClick != null)
        {
            button.onClick.AddListener(() => onClick());
        }

        Image bg = tab.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = config.tabNormalColor;
            bg.raycastTarget = true;
        }

        return tab;
    }

    public GameObject CreateTabContainer(Transform parent, Vector2 size, Vector2 position)
    {
        GameObject container = new GameObject("TabsContainer");
        container.transform.SetParent(parent, false);

        RectTransform rt = container.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.spacing = config.tabSpacing;
        layout.padding = new RectOffset(10, 10, 0, 0);

        return container;
    }

    public GameObject CreateTabContentContainer(Transform parent)
    {
        GameObject container = new GameObject("TabContentContainer");
        container.transform.SetParent(parent, false);

        RectTransform rt = container.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 0.05f);
        rt.anchorMax = new Vector2(0.95f, 0.9f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, -40);
        rt.sizeDelta = Vector2.zero;

        return container;
    }

    public GameObject CreateTabContent(Transform parent, string name)
    {
        GameObject tab = new GameObject(name);
        tab.transform.SetParent(parent, false);
        tab.SetActive(false);

        RectTransform rt = tab.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return tab;
    }

    #endregion

    #region Back Buttons

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
        backImg.color = config.buttonNormalColor;
        backImg.raycastTarget = true;

        Button backButton = backBtn.AddComponent<Button>();
        if (onClick != null)
            backButton.onClick.AddListener(() => onClick());

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
        backText.color = Color.white;
        backText.raycastTarget = false;
        if (defaultFont != null) backText.font = defaultFont;

        return backBtn;
    }

    #endregion

    #region Placeholder

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

    #endregion
}