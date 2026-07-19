using UnityEngine;

public class NavigationArrow : MonoBehaviour
{
    public static NavigationArrow Instance;

    [Header("Settings")]
    public bool enableKeyboardArrows = true;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        if (!enableKeyboardArrows) return;

        // ⬅️➡️⬆️⬇️ Стрелки на клавиатуре
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            TryNavigate(Vector2.right);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            TryNavigate(Vector2.left);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            TryNavigate(Vector2.up);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            TryNavigate(Vector2.down);
        }
    }

    public void TryNavigate(Vector2 direction)
    {
        // Находим текущую активную локацию
        GameObject currentLocation = FindCurrentLocation();

        if (currentLocation == null)
        {
            Debug.LogWarning("No active location found!");
            return;
        }

        // Получаем соседей
        LocationNeighbors neighbors = currentLocation.GetComponent<LocationNeighbors>();

        if (neighbors == null)
        {
            Debug.LogWarning($"No LocationNeighbors found on {currentLocation.name}");
            return;
        }

        // Определяем целевую локацию
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
            // ✅ Передаём направление в переход
            SceneSlideTransition.Instance?.SwitchToScene(targetLocation.name, direction);
        }
        else
        {
            Debug.Log("No location in this direction!");
        }
    }

    private GameObject FindCurrentLocation()
    {
        GameObject[] locations = GameObject.FindGameObjectsWithTag("Location");
        foreach (GameObject loc in locations)
        {
            if (loc.activeSelf)
                return loc;
        }
        return null;
    }
}