using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestListItem : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;

    private QuestInstance quest;
    private System.Action onClick;

    public void Initialize(QuestInstance questInstance, System.Action onSelect)
    {
        quest = questInstance;
        onClick = onSelect;

        if (titleText != null)
            titleText.text = questInstance.QuestName;
        else
            Logger.LogWarning(LogModule.QuestSystem, "titleText не назначен в QuestListItem");

        if (progressText != null)
            progressText.text = questInstance.GetProgressText();
        else
            Logger.LogWarning(LogModule.QuestSystem, "progressText не назначен в QuestListItem");

        if (iconImage != null && questInstance.Icon != null)
            iconImage.sprite = questInstance.Icon;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onSelect?.Invoke());
        }

        Logger.Log(LogModule.QuestSystem, $"Инициализирован элемент списка квеста: {questInstance.QuestName}");
    }
}
