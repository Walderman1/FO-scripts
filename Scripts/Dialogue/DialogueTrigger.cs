using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public int dialogueFileIndex = 1;

    private void Awake()
    {
        Logger.Log(LogModule.Dialogue, $"DialogueTrigger инициализирован на {gameObject.name}, индекс файла: {dialogueFileIndex}");
    }

    private void OnDestroy()
    {
        Logger.Log(LogModule.Dialogue, $"DialogueTrigger уничтожен на {gameObject.name}");
    }
}
