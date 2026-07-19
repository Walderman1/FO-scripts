// EventAction.cs - УНИВЕРСАЛЬНАЯ ВЕРСИЯ С ПОДДЕРЖКОЙ DELAY У ВСЕХ ДЕЙСТВИЙ
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public abstract class EventAction
{
    public string actionName = "Action";
    public float delay = 0f;

    // Основной метод Execute — теперь сам обрабатывает задержку
    public void Execute(EventContext context)
    {
        if (delay > 0f)
        {
            if (EventManager.Instance != null)
                EventManager.Instance.RunCoroutine(DelayedExecute(context));
            else
                ExecuteImmediate(context);
        }
        else
        {
            ExecuteImmediate(context);
        }
    }

    private IEnumerator DelayedExecute(EventContext context)
    {
        yield return new WaitForSeconds(delay);
        ExecuteImmediate(context);
    }

    // Этот метод переопределяют все наследники
    protected abstract void ExecuteImmediate(EventContext context);
}

// ============ ВСЕ ДЕЙСТВИЯ ============

[System.Serializable]
public class EventActionStartDialogue : EventAction
{
    public int dialogueFileIndex;
    public string dialogueID;

    protected override void ExecuteImmediate(EventContext context)
    {
        Debug.Log($"🔍 Ищем TextBeginner на сцене...");

        TextBeginner textBeginner = Object.FindObjectOfType<TextBeginner>();

        if (textBeginner != null)
        {
            Debug.Log($"✅ TextBeginner найден: {textBeginner.gameObject.name}");
            Debug.Log($"🎯 Вызываем StartDialogueWithFile({dialogueFileIndex})");
            textBeginner.StartDialogueWithFile(dialogueFileIndex);
            Debug.Log($"📜 Команда на запуск диалога отправлена!");
        }
        else
        {
            Debug.LogError("❌❌❌ TextBeginner НЕ НАЙДЕН на сцене! ❌❌❌");
        }
    }
}

[System.Serializable]
public class EventActionAddItem : EventAction
{
    public ItemType itemType;
    public int amount = 1;

    protected override void ExecuteImmediate(EventContext context)
    {
        InventoryUIManager inventory = Object.FindObjectOfType<InventoryUIManager>();
        if (inventory != null)
        {
            for (int i = 0; i < amount; i++)
            {
                inventory.AddItem(itemType);
            }
            Debug.Log($"🎒 Добавлено {amount}x {itemType} в инвентарь");
        }
        else
        {
            Debug.LogWarning("InventoryUIManager не найден!");
        }
    }
}

[System.Serializable]
public class EventActionRemoveItem : EventAction
{
    public ItemType itemType;
    public int amount = 1;

    protected override void ExecuteImmediate(EventContext context)
    {
        Debug.Log($"🗑️ Удаляем {amount}x {itemType} из инвентаря...");

        InventoryUIManager inventory = Object.FindObjectOfType<InventoryUIManager>();
        if (inventory == null)
        {
            Debug.LogWarning("InventoryUIManager не найден!");
            return;
        }

        if (inventory.inventoryGrid == null)
        {
            Debug.LogWarning("inventoryGrid не назначен!");
            return;
        }

        int removedCount = 0;

        foreach (Transform slotTransform in inventory.inventoryGrid.transform)
        {
            InventoryItemMarker marker = slotTransform.GetComponentInChildren<InventoryItemMarker>();
            if (marker == null) continue;

            if (marker.itemType == itemType)
            {
                int toRemove = Mathf.Min(amount - removedCount, marker.count);
                marker.count -= toRemove;
                removedCount += toRemove;

                if (marker.count <= 0)
                {
                    Object.Destroy(marker.gameObject);
                    InventorySlot slot = slotTransform.GetComponent<InventorySlot>();
                    if (slot != null)
                    {
                        slot.isOccupied = false;
                        slot.UpdateSlotText();
                    }
                }
                else
                {
                    marker.UpdateUI();
                    InventorySlot slot = slotTransform.GetComponent<InventorySlot>();
                    if (slot != null) slot.UpdateSlotText();
                }

                if (removedCount >= amount) break;
            }
        }

        if (removedCount > 0)
        {
            Debug.Log($"🗑️ Удалено {removedCount}x {itemType} из инвентаря");
        }
        else
        {
            Debug.Log($"⚠️ Не найдено {itemType} в инвентаре для удаления");
        }

        inventory.UpdateAllSlots();
    }
}

// EventActionSetFlag - ИСПОЛЬЗУЕТ ОБЪЕДИНЕННЫЙ FlagManager
[System.Serializable]
public class EventActionSetFlag : EventAction
{
    public string flagName;
    public bool flagValue = true;

    protected override void ExecuteImmediate(EventContext context)
    {
        // ✅ Используем FlagManager вместо GlobalFlagManager
        if (FlagManager.Instance != null)
        {
            FlagManager.Instance.SetFlag(flagName, flagValue);
            Debug.Log($"🚩 Установлен флаг {flagName} = {flagValue}");
        }
        else
        {
            Debug.LogWarning("FlagManager.Instance is null!");
        }
    }
}

[System.Serializable]
public class EventActionTeleport : EventAction
{
    public string locationName;
    public Vector2 targetPosition;
    public bool usePositionOverride = false;

    protected override void ExecuteImmediate(EventContext context)
    {
        if (SceneSlideTransition.Instance != null)
        {
            SceneSlideTransition.Instance.SwitchToScene(locationName);
            Debug.Log($"📍 Телепорт в: {locationName}");
        }
        else
        {
            Debug.LogWarning("SceneSlideTransition.Instance is null!");
        }
    }
}

[System.Serializable]
public class EventActionMultiple : EventAction
{
    public List<EventAction> subActions = new List<EventAction>();

    protected override void ExecuteImmediate(EventContext context)
    {
        foreach (var action in subActions)
        {
            if (action != null)
            {
                action.Execute(context);
            }
        }
    }
}

[System.Serializable]
public class EventActionPlaySound : EventAction
{
    public AudioClip soundClip;
    public float volume = 1f;

    protected override void ExecuteImmediate(EventContext context)
    {
        if (soundClip != null)
        {
            AudioSource.PlayClipAtPoint(soundClip, Vector3.zero, volume);
            Debug.Log($"🔊 Воспроизведён звук: {soundClip.name}");
        }
        else
        {
            Debug.LogWarning("soundClip не назначен!");
        }
    }
}

[System.Serializable]
public class EventActionSpawnObject : EventAction
{
    public GameObject prefabToSpawn;
    public Vector3 spawnPosition;
    public Transform parentTransform;

    protected override void ExecuteImmediate(EventContext context)
    {
        if (prefabToSpawn != null)
        {
            Vector3 position = spawnPosition;
            if (context != null && context.TargetObject != null && parentTransform == null)
            {
                position = context.TargetObject.transform.position;
            }

            GameObject obj = Object.Instantiate(prefabToSpawn, position, Quaternion.identity);
            if (parentTransform != null)
            {
                obj.transform.SetParent(parentTransform);
            }
            Debug.Log($"📦 Создан объект: {prefabToSpawn.name}");
        }
        else
        {
            Debug.LogWarning("prefabToSpawn не назначен!");
        }
    }
}

[System.Serializable]
public class EventActionDestroyObject : EventAction
{
    public GameObject targetObject;
    public float destroyDelay = 0f;

    protected override void ExecuteImmediate(EventContext context)
    {
        if (targetObject != null)
        {
            Object.Destroy(targetObject, destroyDelay);
            Debug.Log($"💥 Удалён объект: {targetObject.name} (через {destroyDelay}с)");
        }
        else
        {
            Debug.LogWarning("targetObject не назначен!");
        }
    }
}

[System.Serializable]
public class EventActionEnableObject : EventAction
{
    public GameObject targetObject;
    public bool enable = true;

    protected override void ExecuteImmediate(EventContext context)
    {
        if (targetObject != null)
        {
            targetObject.SetActive(enable);
            Debug.Log($"🔛 {(enable ? "Включён" : "Выключён")} объект: {targetObject.name}");
        }
        else
        {
            Debug.LogWarning("targetObject не назначен!");
        }
    }
}

[System.Serializable]
public class EventActionDisableObject : EventAction
{
    public GameObject targetObject;

    protected override void ExecuteImmediate(EventContext context)
    {
        if (targetObject != null)
        {
            targetObject.SetActive(false);
            Debug.Log($"🔛 Выключён объект: {targetObject.name}");
        }
        else
        {
            Debug.LogWarning("targetObject не назначен!");
        }
    }
}

[System.Serializable]
public class EventActionEnableEvent : EventAction
{
    public string eventID;
    public bool enable = true;

    protected override void ExecuteImmediate(EventContext context)
    {
        if (enable)
        {
            EventStateManager.Instance?.ResetEvent(eventID);
            Debug.Log($"🔓 Событие '{eventID}' включено (сброшено состояние)");
        }
        else
        {
            EventStateManager.Instance?.MarkExecuted(eventID);
            Debug.Log($"🔒 Событие '{eventID}' выключено (отмечено как выполненное)");
        }
    }
}

[System.Serializable]
public class EventActionDisableEvent : EventAction
{
    public string eventID;

    protected override void ExecuteImmediate(EventContext context)
    {
        EventStateManager.Instance?.MarkExecuted(eventID);
        Debug.Log($"🔒 Событие '{eventID}' выключено");
    }
}

[System.Serializable]
public class EventActionCheckEvent : EventAction
{
    public string eventID;
    public bool requiredState = true;
    public EventAction actionIfTrue;
    public EventAction actionIfFalse;

    protected override void ExecuteImmediate(EventContext context)
    {
        bool state = EventStateManager.Instance?.IsExecuted(eventID) ?? false;

        if (state == requiredState)
        {
            actionIfTrue?.Execute(context);
            Debug.Log($"✅ Событие '{eventID}' соответствует состоянию ({requiredState})");
        }
        else
        {
            actionIfFalse?.Execute(context);
            Debug.Log($"❌ Событие '{eventID}' НЕ соответствует состоянию ({requiredState})");
        }
    }
}

[System.Serializable]
public class EventActionResetEvent : EventAction
{
    public string eventID;

    protected override void ExecuteImmediate(EventContext context)
    {
        EventStateManager.Instance?.ResetEvent(eventID);
        Debug.Log($"🔄 Событие '{eventID}' сброшено");
    }
}

[System.Serializable]
public class EventActionSaveState : EventAction
{
    public string customKey;
    public string value;

    protected override void ExecuteImmediate(EventContext context)
    {
        if (!string.IsNullOrEmpty(customKey))
        {
            PlayerPrefs.SetString(customKey, value);
            PlayerPrefs.Save();
            Debug.Log($"💾 Сохранено состояние: {customKey} = {value}");
        }
        else
        {
            Debug.LogWarning("customKey не указан!");
        }
    }
}