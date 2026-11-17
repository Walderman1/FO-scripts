using DialogueSystem;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class LocationNeighbors : MonoBehaviour
{
    [Header("Neighbors")]
    [SerializeField] private string left;
    [SerializeField] private string right;
    [SerializeField] private string up;
    [SerializeField] private string down;

    [Header("Search Settings")]
    [Tooltip("Максимальное расстояние до соседа. 0 = без ограничения")]
    [SerializeField] private float maxNeighborDistance = 20f;

    [Tooltip("Минимальное совпадение направления (cos угла). 0.7 ≈ 45°, 0.9 ≈ 25°")]
    [SerializeField, Range(0f, 1f)] private float directionAlignment = 0.7f;

    [Header("Gizmo Settings")]
    [SerializeField] private bool drawNeighborGizmos = true;
    [SerializeField] private Color horizontalGizmoColor = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color verticalGizmoColor = new Color(1f, 0.9f, 0.2f);

    [Header("References")]
    [SerializeField] private GameObject locationReference;
    [SerializeField] private DialogueData dialogueConfig;

    private Dictionary<string, string> neighbors = new Dictionary<string, string>();
    private bool hasSearched = false;

    public string LeftNeighbor => left;
    public string RightNeighbor => right;
    public string UpNeighbor => up;
    public string DownNeighbor => down;

    public string leftNeighbor => left;
    public string rightNeighbor => right;
    public string upNeighbor => up;
    public string downNeighbor => down;
    [Inject] private DialogueData _injectedConfig;

    private void Start()
    {
        if (dialogueConfig == null)
            dialogueConfig = _injectedConfig;

        if (dialogueConfig == null)
            Logger.LogWarning(LogModule.Navigation, "DialogueConfig не найден");

        FindNeighbors();
        FindLocationReference();
    }

    public void FindNeighbors()
    {
        if (hasSearched) return;
        hasSearched = true;

        if (!string.IsNullOrEmpty(left) || !string.IsNullOrEmpty(right) ||
            !string.IsNullOrEmpty(up) || !string.IsNullOrEmpty(down))
        {
            neighbors.Clear();
            if (!string.IsNullOrEmpty(left)) neighbors["Left"] = left;
            if (!string.IsNullOrEmpty(right)) neighbors["Right"] = right;
            if (!string.IsNullOrEmpty(up)) neighbors["Up"] = up;
            if (!string.IsNullOrEmpty(down)) neighbors["Down"] = down;

            Logger.Log(LogModule.Navigation,
                $"Соседи (вручную) для {gameObject.name}: L={left}, R={right}, U={up}, D={down}");
            return;
        }

        string locationTag = dialogueConfig != null ? dialogueConfig.LocationTag : "Location";
        GameObject[] allLocations = GameObject.FindGameObjectsWithTag(locationTag);

        Vector3[] dirVectors = { Vector3.left, Vector3.right, Vector3.up, Vector3.down };
        float[] closestDist = { float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue };
        string[] closestName = { null, null, null, null };

        Vector3 myPos = transform.position;

        foreach (GameObject loc in allLocations)
        {
            if (loc == gameObject) continue;

            Vector3 toLoc = loc.transform.position - myPos;
            float distance = toLoc.magnitude;

            if (distance < 0.01f) continue;
            if (maxNeighborDistance > 0f && distance > maxNeighborDistance) continue;

            Vector3 dir = toLoc / distance;

            int bestIdx = -1;
            float bestDot = directionAlignment;
            for (int i = 0; i < 4; i++)
            {
                float dot = Vector3.Dot(dir, dirVectors[i]);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestIdx = i;
                }
            }

            if (bestIdx < 0) continue;

            if (distance < closestDist[bestIdx])
            {
                closestDist[bestIdx] = distance;
                closestName[bestIdx] = loc.name;
            }
        }

        left = closestName[0];
        right = closestName[1];
        up = closestName[2];
        down = closestName[3];

        neighbors.Clear();
        if (!string.IsNullOrEmpty(left)) neighbors["Left"] = left;
        if (!string.IsNullOrEmpty(right)) neighbors["Right"] = right;
        if (!string.IsNullOrEmpty(up)) neighbors["Up"] = up;
        if (!string.IsNullOrEmpty(down)) neighbors["Down"] = down;

        Logger.LogVerbose(LogModule.Navigation,
            $"Соседи для {gameObject.name}: L={left}, R={right}, U={up}, D={down}");
    }

    public void ForceRefreshNeighbors()
    {
        hasSearched = false;
        FindNeighbors();
    }

    public void FindLocationReference()
    {
        if (locationReference != null) return;

        string locationRefTag = dialogueConfig != null ? dialogueConfig.LocationReferenceTag : "LocationReference";

        foreach (Transform child in transform)
        {
            if (child.CompareTag(locationRefTag))
            {
                locationReference = child.gameObject;
                Logger.LogVerbose(LogModule.Navigation, $"LocationReference найден по тегу в дочерних: {locationReference.name}");
                return;
            }
        }

        Transform refTransform = transform.Find(locationRefTag);
        if (refTransform != null)
        {
            locationReference = refTransform.gameObject;
            Logger.LogVerbose(LogModule.Navigation, $"LocationReference найден по имени в дочерних: {locationReference.name}");
            return;
        }

        GameObject foundRef = GameObject.FindGameObjectWithTag(locationRefTag);
        if (foundRef != null)
        {
            locationReference = foundRef;
            Logger.LogVerbose(LogModule.Navigation, $"LocationReference найден по тегу в сцене: {locationReference.name}");
            return;
        }

        Logger.LogWarning(LogModule.Navigation, $"LocationReference не найден для {gameObject.name}");
    }

    public string GetNeighbor(string direction)
    {
        if (neighbors.TryGetValue(direction, out string neighborName))
            return neighborName;
        return null;
    }

    public GameObject GetLocationReference()
    {
        if (locationReference == null)
            FindLocationReference();
        return locationReference;
    }

    public Dictionary<string, string> GetAllNeighbors() => neighbors;

    public string GetLeft() => left;
    public string GetRight() => right;
    public string GetUp() => up;
    public string GetDown() => down;

    public void SetLeft(string value) { left = value; }
    public void SetRight(string value) { right = value; }
    public void SetUp(string value) { up = value; }
    public void SetDown(string value) { down = value; }

    private void OnDrawGizmosSelected()
    {
        if (!drawNeighborGizmos) return;

        Vector3 from = transform.position;

        DrawNeighborGizmo(from, left, horizontalGizmoColor, Vector3.left);
        DrawNeighborGizmo(from, right, horizontalGizmoColor, Vector3.right);
        DrawNeighborGizmo(from, up, verticalGizmoColor, Vector3.up);
        DrawNeighborGizmo(from, down, verticalGizmoColor, Vector3.down);
    }

    private void DrawNeighborGizmo(Vector3 from, string neighborName, Color color, Vector3 defaultDir)
    {
        if (string.IsNullOrEmpty(neighborName)) return;

        GameObject target = GameObject.Find(neighborName);
        Vector3 to = target != null ? target.transform.position : from + defaultDir * 5f;

        Gizmos.color = color;
        Gizmos.DrawLine(from, to);
        Gizmos.DrawWireSphere(to, 0.4f);

        Vector3 mid = (from + to) * 0.5f;
        Gizmos.DrawWireSphere(mid, 0.15f);
    }

    private static GameObject FindInScene(string name)
    {
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in all)
        {
            if (go == null) continue;
            if (go.name != name) continue;
            if (!go.scene.IsValid()) continue;
            return go;
        }
        return null;
    }
}