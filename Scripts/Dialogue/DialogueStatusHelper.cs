using DialogueSystem;
using UnityEngine;

public class DialogueStatusHelper : MonoBehaviour
{
    private DialogueTracker Tracker => GameReferences.Instance?.DialogueTracker;

    public void CompleteDialogue(string dialogueId)
    {
        var tracker = Tracker;
        if (tracker == null)
        {
            Logger.LogWarning(LogModule.Dialogue, "DialogueTracker недоступен");
            return;
        }

        tracker.MarkDialogueCompleted(dialogueId);
        Logger.Log(LogModule.Dialogue, $"✅ Диалог '{dialogueId}' завершён через Helper");
    }

    public void UnlockDialogue(string dialogueId)
    {
        var tracker = Tracker;
        if (tracker == null) return;

        tracker.SetDialogueLock(dialogueId, false);
        Logger.Log(LogModule.Dialogue, $"🔓 Диалог '{dialogueId}' разблокирован через Helper");
    }

    public void LockDialogue(string dialogueId)
    {
        var tracker = Tracker;
        if (tracker == null) return;

        tracker.SetDialogueLock(dialogueId, true);
        Logger.Log(LogModule.Dialogue, $"🔒 Диалог '{dialogueId}' заблокирован через Helper");
    }

    public bool IsDialogueCompleted(string dialogueId)
    {
        return Tracker?.IsDialogueCompleted(dialogueId) ?? false;
    }

    public bool IsDialogueAvailable(string dialogueId)
    {
        return !(Tracker?.IsDialogueLocked(dialogueId) ?? true);
    }

    public int GetPlayCount(string dialogueId)
    {
        return Tracker?.GetDialoguePlayCount(dialogueId) ?? 0;
    }

    public void DebugAllStatuses()
    {
        Tracker?.DebugLogAllStatuses();
    }

    public void ClearAllData()
    {
        var tracker = Tracker;
        if (tracker == null) return;

        tracker.ClearAllData();
        Logger.LogWarning(LogModule.Dialogue, "⚠️ Все данные диалогов очищены!");
    }
}