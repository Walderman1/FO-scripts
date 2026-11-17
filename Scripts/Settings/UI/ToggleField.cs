using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToggleField : MonoBehaviour
{
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Toggle toggle;
    [SerializeField] private Image background;

    [SerializeField] private Color onColor = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color offColor = new Color(0.3f, 0.3f, 0.3f);

    private System.Action<bool> onValueChanged;
    private bool suppress;

    private void Init()
    {
        if (toggle == null) toggle = GetComponent<Toggle>() ?? GetComponentInChildren<Toggle>(true);
        if (background == null && toggle != null) background = toggle.targetGraphic as Image;
        if (labelText == null) labelText = GetComponentInChildren<TMP_Text>(true);
    }

    private void Awake()
    {
        Init();
    }

    public void Setup(string label, bool value, System.Action<bool> callback)
    {
        Init();

        if (labelText != null) labelText.text = label;

        onValueChanged = callback;
        suppress = true;

        if (toggle != null)
        {
            toggle.isOn = value;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(HandleValueChanged);
        }
        else
        {
            Logger.LogError(LogModule.UI, "ToggleField: компонент Toggle не найден на префабе!");
        }

        RefreshVisual(value);
        suppress = false;
    }

    public void SetValueSilent(bool value)
    {
        suppress = true;
        if (toggle != null) toggle.isOn = value;
        RefreshVisual(value);
        suppress = false;
    }

    private void HandleValueChanged(bool value)
    {
        RefreshVisual(value);
        if (suppress) return;
        onValueChanged?.Invoke(value);
    }

    private void RefreshVisual(bool value)
    {
        if (background != null)
            background.color = value ? onColor : offColor;
    }
}