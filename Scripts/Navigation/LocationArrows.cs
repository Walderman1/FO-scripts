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
    }

    private void CreateArrows()
    {
        LocationNeighbors neighbors = GetComponent<LocationNeighbors>();

        if (neighbors == null) return;

        float offset = 2f;

        if (neighbors.leftNeighbor != null)
            InstantiateArrow(leftArrowPrefab, Vector3.left * offset, Quaternion.identity);

        if (neighbors.rightNeighbor != null)
            InstantiateArrow(rightArrowPrefab, Vector3.right * offset, Quaternion.identity);

        if (neighbors.upNeighbor != null)
            InstantiateArrow(upArrowPrefab, Vector3.up * offset, Quaternion.identity);

        if (neighbors.downNeighbor != null)
            InstantiateArrow(downArrowPrefab, Vector3.down * offset, Quaternion.identity);
    }

    private void InstantiateArrow(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject arrow = Instantiate(prefab, transform.position + position, rotation);
        arrow.transform.SetParent(transform);
    }
}