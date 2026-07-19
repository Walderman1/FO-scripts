// EventTypes.cs
using UnityEngine;

public enum EventTriggerType
{
    None,
    PickupItem,
    EnterLocation,
    EquipItem,
    UseItem,
    DialogueEnd,
    TimeReached,
    GlobalFlag,
    InventoryHas,
    InventoryCount,
    PlayerAction,
    Custom
}

public enum EventActionType
{
    None,
    StartDialogue,
    AddItem,
    RemoveItem,
    SetFlag,
    ClearFlag,
    Teleport,
    ShowUI,
    HideUI,
    PlaySound,
    SpawnObject,
    DestroyObject,
    EnableObject,
    DisableObject,
    CustomFunction,
    Multiple,
    Delay
}

// ✅ ДОБАВЛЯЕМ СЮДА
public enum ExecutionPolicy
{
    ExecuteOnce,        // Выполнить только один раз за всю игру
    ExecuteMultiple,    // Выполнить несколько раз (ограничено maxExecutions)
    ExecutePerSession,  // Выполнить один раз за сессию
    ExecuteEveryTime    // Выполнять всегда, когда срабатывает триггер
}