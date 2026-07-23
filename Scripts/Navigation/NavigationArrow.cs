using UnityEngine;

public class NavigationArrow : MonoBehaviour
{
    public static NavigationArrow Instance;

    [Header("Settings")]
    public bool enableKeyboardArrows = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Logger.Log(LogModule.Navigation, "NavigationArrow инициализирован");
        }
        else
        {
            Logger.Log(LogModule.Navigation, "Уничтожение дублирующего NavigationArrow");
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!enableKeyboardArrows) return;

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Logger.Log(LogModule.Navigation, "Нажата стрелка вправо");
            TryNavigate(Vector2.right);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Logger.Log(LogModule.Navigation, "Нажата стрелка влево");
            TryNavigate(Vector2.left);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Logger.Log(LogModule.Navigation, "Нажата стрелка вверх");
            TryNavigate(Vector2.up);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Logger.Log(LogModule.Navigation, "Нажата стрелка вниз");
            TryNavigate(Vector2.down);
        }
    }

    public void TryNavigate(Vector2 direction)
    {
        GameObject currentLocation = FindCurrentLocation();

        if (currentLocation == null)
        {
            Logger.LogWarning(LogModule.Navigation, "Активная локация не найдена");
            return;
        }

        LocationNeighbors neighbors = currentLocation.GetComponent<LocationNeighbors>();

        if (neighbors == null)
        {
            Logger.LogWarning(LogModule.Navigation, $"LocationNeighbors не найден на {currentLocation.name}");
            return;
        }

        GameObject targetLocation = null;

        if (direction == Vector2.right)
            targetLocation = neighbors.rightNeighbor;
        else if (direction == Vector2.left)
            targetLocation = neighbors.leftNeighbor;
        else if (direction == Vector2.up)
            targetLocation = neighbors.upNeighbor;
        else if (direction == Vector2.down)
            targetLocation = neighbors.downNeighbor;

        if (targetLocation != null)
        {
            Logger.Log(LogModule.Navigation, $"Переход из {currentLocation.name} в {targetLocation.name} по направлению {direction}");
            SceneSlideTransition.Instance?.SwitchToScene(targetLocation.name, direction);
        }
        else
        {
            Logger.Log(LogModule.Navigation, $"Нет локации в направлении {direction} из {currentLocation.name}");
        }
    }

    private GameObject FindCurrentLocation()
    {
        GameObject[] locations = GameObject.FindGameObjectsWithTag("Location");
        foreach (GameObject loc in locations)
        {
            if (loc.activeSelf)
            {
                Logger.Log(LogModule.Navigation, $"Найдена активная локация: {loc.name}");
                return loc;
            }
        }
        Logger.LogWarning(LogModule.Navigation, "Активная локация не найдена");
        return null;
    }
}
