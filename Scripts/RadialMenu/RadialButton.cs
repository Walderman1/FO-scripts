using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RadialButton : MonoBehaviour
{
    public string buttonName;
    public int buttonIndex;
    public RadialMenu.RadialAction buttonAction;

    public System.Action<string, int> onClick;

    private TMP_Text textComponent;
    private Button button;

    private void Awake()
    {
        textComponent = GetComponentInChildren<TMP_Text>();
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(() => onClick?.Invoke(buttonName, buttonIndex));
        }

        Logger.Log(LogModule.RadialMenu, $"RadialButton инициализирован: {buttonName}");
    }

    public void Init(string name, int index)
    {
        buttonName = name;
        buttonIndex = index;
        if (textComponent != null)
        {
            textComponent.text = name;
        }

        Logger.Log(LogModule.RadialMenu, $"RadialButton инициализирован с именем: {name}, индекс: {index}");
    }

    public void SetAction(RadialMenu.RadialAction action)
    {
        buttonAction = action;
        Logger.Log(LogModule.RadialMenu, $"Для кнопки {buttonName} установлено действие: {action}");
    }
}
