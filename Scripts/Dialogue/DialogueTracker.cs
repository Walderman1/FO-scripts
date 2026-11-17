using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueStatus
{
    public string dialogueId;
    public bool isCompleted;
    public int timesPlayed;
    public string lastPlayed;
    public string nextDialogueId;
    public bool isLocked;

    public DialogueStatus() { }

    public DialogueStatus(string id)
    {
        dialogueId = id;
        isCompleted = false;
        timesPlayed = 0;
        lastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        nextDialogueId = "";
        isLocked = false;
    }
}

[DefaultExecutionOrder(-9100)]
public class DialogueTracker : MonoBehaviour
{
    private Dictionary<string, DialogueStatus> dialogueStatuses = new Dictionary<string, DialogueStatus>();

    private void Awake()
    {
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public void MarkDialogueCompleted(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId)) return;

        if (!dialogueStatuses.ContainsKey(dialogueId))
            dialogueStatuses[dialogueId] = new DialogueStatus(dialogueId);

        dialogueStatuses[dialogueId].isCompleted = true;
        dialogueStatuses[dialogueId].timesPlayed++;
        dialogueStatuses[dialogueId].lastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        Logger.Log(LogModule.Dialogue, $"Диалог '{dialogueId}' отмечен как завершённый");
    }

    public void RegisterDialogue(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId)) return;
        if (!dialogueStatuses.ContainsKey(dialogueId))
            dialogueStatuses[dialogueId] = new DialogueStatus(dialogueId);
    }

    public bool IsDialogueCompleted(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId)) return false;
        return dialogueStatuses.TryGetValue(dialogueId, out var s) && s.isCompleted;
    }

    public int GetDialoguePlayCount(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId)) return 0;
        return dialogueStatuses.TryGetValue(dialogueId, out var s) ? s.timesPlayed : 0;
    }

    public bool IsDialogueLocked(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId)) return true;
        if (!dialogueStatuses.TryGetValue(dialogueId, out var s)) return false;
        return s.isLocked;
    }

    public void SetDialogueLock(string dialogueId, bool locked)
    {
        if (string.IsNullOrEmpty(dialogueId)) return;
        if (!dialogueStatuses.ContainsKey(dialogueId))
            dialogueStatuses[dialogueId] = new DialogueStatus(dialogueId);
        dialogueStatuses[dialogueId].isLocked = locked;
    }

    public void SetNextDialogue(string dialogueId, string nextDialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId)) return;
        if (!dialogueStatuses.ContainsKey(dialogueId))
            dialogueStatuses[dialogueId] = new DialogueStatus(dialogueId);
        dialogueStatuses[dialogueId].nextDialogueId = nextDialogueId;
    }

    public string GetNextDialogue(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId)) return "";
        return dialogueStatuses.TryGetValue(dialogueId, out var s) ? s.nextDialogueId : "";
    }

    public void ResetDialogue(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId)) return;
        if (dialogueStatuses.TryGetValue(dialogueId, out var s))
        {
            s.isCompleted = false;
            s.isLocked = false;
            s.timesPlayed = 0;
        }
    }

    public void ClearAllData() => dialogueStatuses.Clear();

    public List<DialogueStatusEntry> ExportState()
    {
        var result = new List<DialogueStatusEntry>();
        foreach (var kvp in dialogueStatuses)
        {
            var s = kvp.Value;
            result.Add(new DialogueStatusEntry
            {
                dialogueId = s.dialogueId,
                isCompleted = s.isCompleted,
                timesPlayed = s.timesPlayed,
                lastPlayed = s.lastPlayed ?? "",
                nextDialogueId = s.nextDialogueId ?? "",
                isLocked = s.isLocked
            });
        }
        return result;
    }

    public void ImportState(List<DialogueStatusEntry> data)
    {
        dialogueStatuses.Clear();
        if (data == null) return;

        foreach (var e in data)
        {
            if (e == null || string.IsNullOrEmpty(e.dialogueId)) continue;
            dialogueStatuses[e.dialogueId] = new DialogueStatus
            {
                dialogueId = e.dialogueId,
                isCompleted = e.isCompleted,
                timesPlayed = e.timesPlayed,
                lastPlayed = e.lastPlayed ?? "",
                nextDialogueId = e.nextDialogueId ?? "",
                isLocked = e.isLocked
            };
        }
    }

    public void SaveStatuses() { }
    public void LoadStatuses() { }

    public void DebugLogAllStatuses()
    {
        Logger.Log(LogModule.Dialogue, "=== СТАТУСЫ ДИАЛОГОВ ===");
        foreach (var kvp in dialogueStatuses)
        {
            var s = kvp.Value;
            Logger.Log(LogModule.Dialogue, $"  {s.dialogueId}: завершён={s.isCompleted}, сыгран={s.timesPlayed}, заблокирован={s.isLocked}");
        }
    }
}