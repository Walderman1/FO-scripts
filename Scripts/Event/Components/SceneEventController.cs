using System.Collections;
using UnityEngine;
using VContainer;

public class SceneEventController : MonoBehaviour
{
    [Header("Events to execute on scene start")]
    [SerializeField] private GameEvent[] eventsOnAwake;
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

    [Inject] private EventStateManager _eventStateManager;
    [Inject] private IObjectResolver _injectedContainer;

    private IObjectResolver Container => _injectedContainer ?? SceneInjector.Instance?.CurrentSceneScope;

    private void Start()
    {
        StartCoroutine(RunEventsWhenReady());
    }

    private IEnumerator RunEventsWhenReady()
    {
        float timeout = 5f;
        float elapsed = 0f;
        while (Container == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        var container = Container;
        if (container == null)
        {
            Logger.LogWarning(LogModule.Event, "SceneEventController: контейнер не получен, события пропущены");
            yield break;
        }

        foreach (var evt in eventsOnAwake)
        {
            if (evt == null) continue;
            var context = new EventContext()
                .WithContainer(container)
                .WithLocation(gameObject.scene.name)
                .WithValue("Awake");
            evt.Execute(context);
        }

        foreach (var evt in eventsOnEnter)
        {
            if (evt == null) continue;
            var context = new EventContext()
                .WithContainer(container)
                .WithLocation(gameObject.scene.name)
                .WithValue("Start");
            evt.Execute(context);
        }

        foreach (var unique in uniqueEvents)
        {
            if (unique?.gameEvent == null) continue;
            bool isExecuted = _eventStateManager?.IsExecuted(unique.eventID) ?? false;
            if (!isExecuted)
            {
                unique.gameEvent.Execute(new EventContext()
                    .WithContainer(container)
                    .WithLocation(gameObject.scene.name));
            }
        }
    }
}