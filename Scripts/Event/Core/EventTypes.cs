using UnityEngine;

public enum EventTriggerType
{
    [InspectorName("Нет")] None,
    [InspectorName("Подбор предмета")] PickupItem,
    [InspectorName("Вход в локацию")] EnterLocation,
    [InspectorName("Экипировка предмета")] EquipItem,
    [InspectorName("Использование предмета")] UseItem,
    [InspectorName("Окончание диалога")] DialogueEnd,
    [InspectorName("Достижение времени")] TimeReached,
    [InspectorName("Глобальный флаг")] GlobalFlag,
    [InspectorName("Есть предмет")] InventoryHas,
    [InspectorName("Количество предметов")] InventoryCount,
    [InspectorName("Действие игрока")] PlayerAction,
    [InspectorName("Пользовательский")] Custom
}

public enum EventActionType
{
    [InspectorName("Нет")] None,
    [InspectorName("Начать диалог")] StartDialogue,
    [InspectorName("Добавить предмет")] AddItem,
    [InspectorName("Удалить предмет")] RemoveItem,
    [InspectorName("Установить флаг")] SetFlag,
    [InspectorName("Очистить флаг")] ClearFlag,
    [InspectorName("Телепорт")] Teleport,
    [InspectorName("Показать UI")] ShowUI,
    [InspectorName("Скрыть UI")] HideUI,
    [InspectorName("Воспроизвести звук")] PlaySound,
    [InspectorName("Создать объект")] SpawnObject,
    [InspectorName("Удалить объект")] DestroyObject,
    [InspectorName("Включить объект")] EnableObject,
    [InspectorName("Выключить объект")] DisableObject,
    [InspectorName("Кастомная функция")] CustomFunction,
    [InspectorName("Группа действий")] Multiple,
    [InspectorName("Задержка")] Delay
}

public enum ExecutionPolicy
{
    [InspectorName("Один раз")] ExecuteOnce,
    [InspectorName("Несколько раз")] ExecuteMultiple,
    [InspectorName("Раз за сессию")] ExecutePerSession,
    [InspectorName("Каждый раз")] ExecuteEveryTime
}