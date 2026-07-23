using UnityEngine;

public class LocationArrows : MonoBehaviour
{
    [Header("Arrow Prefabs")]
    public GameObject leftArrowPrefab;
    public GameObject rightArrowPrefab;
    public GameObject upArrowPrefab;
    public GameObject downArrowPrefab;

    private void Start()
    {
        CreateArrows();
        Logger.Log(LogModule.Navigation, "LocationArrows инициализирован");
    }

    private void CreateArrows()
    {
        LocationNeighbors neighbors = GetComponent<LocationNeighbors>();

        if (neighbors == null)
        {
            Logger.LogWarning(LogModule.Navigation, "LocationNeighbors не найден на объекте");
            return;
        }

        float offset = 2f;

        if (neighbors.leftNeighbor != null)
        {
            InstantiateArrow(leftArrowPrefab, Vector3.left * offset, Quaternion.identity);
            Logger.Log(LogModule.Navigation, $"Создана стрелка влево для {gameObject.name}");
        }

        if (neighbors.rightNeighbor != null)
        {
            InstantiateArrow(rightArrowPrefab, Vector3.right * offset, Quaternion.identity);
            Logger.Log(LogModule.Navigation, $"Создана стрелка вправо для {gameObject.name}");
        }

        if (neighbors.upNeighbor != null)
        {
            InstantiateArrow(upArrowPrefab, Vector3.up * offset, Quaternion.identity);
            Logger.Log(LogModule.Navigation, $"Создана стрелка вверх для {gameObject.name}");
        }

        if (neighbors.downNeighbor != null)
        {
            InstantiateArrow(downArrowPrefab, Vector3.down * offset, Quaternion.identity);
            Logger.Log(LogModule.Navigation, $"Создана стрелка вниз для {gameObject.name}");
        }
    }

    private void InstantiateArrow(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Logger.LogWarning(LogModule.Navigation, $"Префаб стрелки не назначен для {gameObject.name}");
            return;
        }

        GameObject arrow = Instantiate(prefab, transform.position + position, rotation);
        arrow.transform.SetParent(transform);
    }
}
