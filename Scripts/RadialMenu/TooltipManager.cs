using UnityEngine;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [Header("UI Elements")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Logger.Log(LogModule.UI, "TooltipManager инициализирован");
        }
        else
        {
            Logger.Log(LogModule.UI, "Уничтожение дублирующего TooltipManager");
            Destroy(gameObject);
        }

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    private void Update()
    {
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            Vector2 mousePos = Input.mousePosition;
            tooltipPanel.transform.position = mousePos + new Vector2(15, -15);
        }
    }

    public void ShowTooltip(string text, Vector2 position)
    {
        if (tooltipPanel == null)
        {
            Logger.LogWarning(LogModule.UI, "tooltipPanel не назначен");
            return;
        }

        if (tooltipText == null)
        {
            Logger.LogWarning(LogModule.UI, "tooltipText не назначен");
            return;
        }

        tooltipText.text = text;
        tooltipPanel.transform.position = position + new Vector2(15, -15);
        tooltipPanel.SetActive(true);

        Logger.Log(LogModule.UI, $"Показан тултип: {text}");
    }

    public void HideTooltip()
    {
        if (tooltipPanel == null)
        {
            Logger.LogWarning(LogModule.UI, "tooltipPanel не назначен");
            return;
        }

        tooltipPanel.SetActive(false);
        Logger.Log(LogModule.UI, "Тултип скрыт");
    }
}
