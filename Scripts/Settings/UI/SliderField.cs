using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderField : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private string valueFormat = "{0:F0}";

    [Header("Засечки")]
    [SerializeField] private bool generateTicks = true;
    [SerializeField] private int tickCount = 11;
    [SerializeField] private Vector2 tickSize = new Vector2(2f, 15f);
    [SerializeField] private Color tickColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);

    private System.Action<float> onValueChanged;
    private bool suppress;
    private bool ticksGenerated = false;

    private void Init()
    {
        if (slider == null) slider = GetComponent<Slider>() ?? GetComponentInChildren<Slider>(true);

        if (valueText == null)
        {
            var allTexts = GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in allTexts)
                if (t.name.ToLower().Contains("value")) { valueText = t; break; }
        }
    }

    private void Awake()
    {
        Init();
    }

    public void Setup(string label, float min, float max, float value, bool wholeNumbers, System.Action<float> callback)
    {
        Init();

        if (labelText != null) labelText.text = label;

        onValueChanged = callback;
        suppress = true;

        if (slider != null)
        {
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            slider.value = value;

            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(HandleValueChanged);

            if (generateTicks && !ticksGenerated)
            {
                GenerateTicks();
                ticksGenerated = true;
            }
        }
        else
        {
            Logger.LogError(LogModule.UI, "SliderField: компонент Slider не найден!");
        }

        RefreshValueLabel(value);
        suppress = false;
    }

    private void GenerateTicks()
    {
        if (slider == null || tickCount < 2) return;

        Transform parentForTicks = slider.transform;
        var fillArea = slider.transform.Find("Fill Area");
        if (fillArea != null) parentForTicks = fillArea;

        for (int i = 0; i < tickCount; i++)
        {
            GameObject tick = new GameObject($"Tick_{i}", typeof(RectTransform), typeof(Image));
            tick.transform.SetParent(parentForTicks, false);

            var rt = tick.GetComponent<RectTransform>();
            float pct = i / (float)(tickCount - 1);

            rt.anchorMin = new Vector2(pct, 0.5f);
            rt.anchorMax = new Vector2(pct, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = tickSize;

            var img = tick.GetComponent<Image>();
            img.color = tickColor;
            img.raycastTarget = false;

            tick.transform.SetAsFirstSibling();
        }
    }

    public void SetValueSilent(float value)
    {
        suppress = true;
        if (slider != null) slider.value = value;
        RefreshValueLabel(value);
        suppress = false;
    }

    private void HandleValueChanged(float value)
    {
        RefreshValueLabel(value);
        if (suppress) return;
        onValueChanged?.Invoke(value);
    }

    private void RefreshValueLabel(float value)
    {
        if (valueText != null)
            valueText.text = string.Format(valueFormat, value);
    }
}