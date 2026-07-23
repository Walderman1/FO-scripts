using UnityEngine;

public class SceneEventController : MonoBehaviour
{
    [Header("Events to execute on scene start")]
    [SerializeField] private GameEvent[] eventsOnAwake;

    [Header("Events to execute on scene enter")]
    [SerializeField] private GameEvent[] eventsOnEnter;

    [Header("Unique scene events (execute once)")]
    [SerializeField] private UniqueSceneEvent[] uniqueEvents;

    [System.Serializable]
    public class UniqueSceneEvent
    {
        public string eventID;
        public GameEvent gameEvent;
        public string sceneName;
    }

    private void Awake()
    {
        Logger.Log(LogModule.Event, $"Инициализация контроллера событий сцены: {gameObject.scene.name}");

        foreach (var evt in eventsOnAwake)
        {
            if (evt != null)
            {
                var context = new EventContext()
                    .WithLocation(gameObject.scene.name)
                    .WithValue("Awake");
                evt.Execute(context);
                Logger.Log(LogModule.Event, $"Выполнено событие Awake: {evt.name}");
            }
        }
    }

    private void Start()
    {
        Logger.Log(LogModule.Event, $"Запуск событий сцены: {gameObject.scene.name}");

        foreach (var evt in eventsOnEnter)
        {
            if (evt != null)
            {
                var context = new EventContext()
                    .WithLocation(gameObject.scene.name)
                    .WithValue("Start");
                evt.Execute(context);
                Logger.Log(LogModule.Event, $"Выполнено событие Start: {evt.name}");
            }
        }

        foreach (var unique in uniqueEvents)
        {
            if (unique.gameEvent != null)
            {
                bool isExecuted = EventStateManager.Instance?.IsExecuted(unique.eventID) ?? false;
                if (!isExecuted)
                {
                    unique.gameEvent.Execute(new EventContext()
                        .WithLocation(gameObject.scene.name));
                    Logger.Log(LogModule.Event, $"Выполнено уникальное событие '{unique.eventID}' в сцене {gameObject.scene.name}");
                }
                else
                {
                    Logger.Log(LogModule.Event, $"Уникальное событие '{unique.eventID}' уже было выполнено ранее");
                }
            }
        }
    }

    private void OnDestroy()
    {
        Logger.Log(LogModule.Event, $"Уничтожение контроллера событий сцены: {gameObject.scene.name}");
    }
}
