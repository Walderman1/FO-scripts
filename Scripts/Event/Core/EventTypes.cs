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

public enum ExecutionPolicy
{
    ExecuteOnce,
    ExecuteMultiple,
    ExecutePerSession,
    ExecuteEveryTime
}
