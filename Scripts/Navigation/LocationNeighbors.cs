using UnityEngine;
using System.Collections.Generic;

public class LocationNeighbors : MonoBehaviour
{
    [Header("Auto Detection")]
    [SerializeField] private float detectionRadius = 30f;

    [Header("Location Reference")]
    [SerializeField] private GameObject locationReference;

    public GameObject leftNeighbor;
    public GameObject rightNeighbor;
    public GameObject upNeighbor;
    public GameObject downNeighbor;

    private void Start()
    {
        FindNeighbors();
        FindLocationReference();

        if (transform.position != Vector3.zero)
        {
            this.enabled = false;
        }
    }

    private void FindLocationReference()
    {
        if (locationReference != null)
        {
            Debug.Log($"📍 LocationReference назначен вручную: {locationReference.name}");
            return;
        }

        // Поиск по имени в дочерних
        Transform childByName = transform.Find("LocationReference");
        if (childByName != null)
        {
            locationReference = childByName.gameObject;
            Debug.Log($"📍 LocationReference найден по имени в дочерних: {locationReference.name}");
            return;
        }

        // Поиск по тегу в дочерних
        foreach (Transform child in transform)
        {
            if (child.CompareTag("LocationReference"))
            {
                locationReference = child.gameObject;
                Debug.Log($"📍 LocationReference найден по тегу в дочерних: {locationReference.name}");
                return;
            }
        }

        // Поиск по тегу глобально (если дочерний)
        GameObject foundByTag = GameObject.FindGameObjectWithTag("LocationReference");
        if (foundByTag != null && foundByTag.transform.IsChildOf(transform))
        {
            locationReference = foundByTag;
            Debug.Log($"📍 LocationReference найден по тегу глобально: {locationReference.name}");
            return;
        }

        // Поиск по имени глобально (если дочерний)
        GameObject foundByName = GameObject.Find("LocationReference");
        if (foundByName != null && foundByName.transform.IsChildOf(transform))
        {
            locationReference = foundByName;
            Debug.Log($"📍 LocationReference найден по имени глобально: {locationReference.name}");
            return;
        }

        Debug.LogWarning($"⚠️ LocationReference не найден для {gameObject.name}");
    }

    private void FindNeighbors()
    {
        GameObject[] allLocations = GameObject.FindGameObjectsWithTag("Location");

        Dictionary<string, GameObject> candidates = new Dictionary<string, GameObject>();
        Dictionary<string, float> distances = new Dictionary<string, float>();

        candidates["left"] = null;
        candidates["right"] = null;
        candidates["up"] = null;
        candidates["down"] = null;

        distances["left"] = float.MaxValue;
        distances["right"] = float.MaxValue;
        distances["up"] = float.MaxValue;
        distances["down"] = float.MaxValue;

        foreach (GameObject loc in allLocations)
        {
            if (loc == gameObject) continue;

            Vector3 direction = loc.transform.position - transform.position;
            float distance = direction.magnitude;

            if (distance > detectionRadius) continue;

            Vector3 normalized = direction.normalized;

            if (Mathf.Abs(normalized.x) > Mathf.Abs(normalized.y))
            {
                if (normalized.x > 0)
                {
                    if (distance < distances["right"])
                    {
                        distances["right"] = distance;
                        candidates["right"] = loc;
                    }
                }
                else
                {
                    if (distance < distances["left"])
                    {
                        distances["left"] = distance;
                        candidates["left"] = loc;
                    }
                }
            }
            else
            {
                if (normalized.y > 0)
                {
                    if (distance < distances["up"])
                    {
                        distances["up"] = distance;
                        candidates["up"] = loc;
                    }
                }
                else
                {
                    if (distance < distances["down"])
                    {
                        distances["down"] = distance;
                        candidates["down"] = loc;
                    }
                }
            }
        }

        leftNeighbor = candidates["left"];
        rightNeighbor = candidates["right"];
        upNeighbor = candidates["up"];
        downNeighbor = candidates["down"];

        Debug.Log($"📍 {gameObject.name}: Left={leftNeighbor?.name ?? "None"}, Right={rightNeighbor?.name ?? "None"}, Up={upNeighbor?.name ?? "None"}, Down={downNeighbor?.name ?? "None"}");

        // ✅ Проверка на null перед использованием
        if (locationReference != null)
        {
            Debug.Log($"📍 {gameObject.name}: LocationReference={locationReference.name}");
        }
        else
        {
            Debug.Log($"📍 {gameObject.name}: LocationReference=None");
        }
    }

    public GameObject GetLocationReference()
    {
        return locationReference;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        if (leftNeighbor != null)
            Gizmos.DrawLine(transform.position, leftNeighbor.transform.position);

        Gizmos.color = Color.green;
        if (rightNeighbor != null)
            Gizmos.DrawLine(transform.position, rightNeighbor.transform.position);

        Gizmos.color = Color.yellow;
        if (upNeighbor != null)
            Gizmos.DrawLine(transform.position, upNeighbor.transform.position);

        Gizmos.color = Color.red;
        if (downNeighbor != null)
            Gizmos.DrawLine(transform.position, downNeighbor.transform.position);

        Gizmos.color = Color.magenta;
        if (locationReference != null)
            Gizmos.DrawLine(transform.position, locationReference.transform.position);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}