using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

public class WorldItemTracker : MonoBehaviour
{
    private HashSet<string> takenItemIds = new HashSet<string>();
    private List<DroppedItemData> droppedItems = new List<DroppedItemData>();
    private Dictionary<string, PickupItem> allSceneItems = new Dictionary<string, PickupItem>();
    private Dictionary<string, MovedItemData> movedItems = new Dictionary<string, MovedItemData>();

    [Inject] private ItemDatabase _itemDatabase;

    private void Awake()
    {
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        PickupItem.OnPickedUp -= HandleItemPickedUp;
        PickupItem.OnPickedUp += HandleItemPickedUp;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        PickupItem.OnPickedUp -= HandleItemPickedUp;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().buildIndex == 0) return;
        StartCoroutine(RefreshRoutine());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0) return;
        StartCoroutine(RefreshRoutine());
    }

    private IEnumerator RefreshRoutine()
    {
        yield return null;
        yield return null;

        if (SceneManager.GetActiveScene().buildIndex == 0) yield break;

        RefreshSceneItems();
        ApplySavedState();
    }

    public List<MovedItemData> GetMovedItems() => new List<MovedItemData>(movedItems.Values);

    public void RegisterMovedItem(string uniqueId, string locationName, Vector3 localPos)
    {
        if (string.IsNullOrEmpty(uniqueId)) return;

        movedItems[uniqueId] = new MovedItemData
        {
            uniqueId = uniqueId,
            locationName = locationName ?? "",
            localPosX = localPos.x,
            localPosY = localPos.y
        };
    }

    public void UpdateMovedItemPosition(string uniqueId, Vector3 localPos)
    {
        if (string.IsNullOrEmpty(uniqueId)) return;
        if (movedItems.TryGetValue(uniqueId, out var data))
        {
            data.localPosX = localPos.x;
            data.localPosY = localPos.y;
        }
    }


    public void RefreshSceneItems()
    {
        allSceneItems.Clear();
        var items = FindObjectsByType<PickupItem>(FindObjectsInactive.Include);
        var usedIds = new HashSet<string>();

        foreach (var item in items)
        {
            if (item == null) continue;

            string id = item.UniqueSaveID;
            if (string.IsNullOrEmpty(id))
            {
                id = BuildPathId(item);
                item.SetUniqueId(id);
            }

            if (usedIds.Contains(id))
            {
                int counter = 2;
                string candidate;
                do { candidate = id + "#" + counter; counter++; }
                while (usedIds.Contains(candidate));
                id = candidate;
            }

            usedIds.Add(id);
            allSceneItems[id] = item;
        }
    }

    private string BuildPathId(PickupItem item)
    {
        var sb = new System.Text.StringBuilder();
        var t = item.transform;
        while (t != null)
        {
            sb.Insert(0, "/" + t.name);
            t = t.parent;
        }
        return item.gameObject.scene.name + sb.ToString();
    }

    private void HandleItemPickedUp(PickupItem item)
    {
        if (item == null) return;

        string id = null;
        foreach (var kvp in allSceneItems)
        {
            if (kvp.Value == item) { id = kvp.Key; break; }
        }

        if (!string.IsNullOrEmpty(id))
        {
            takenItemIds.Add(id);
            droppedItems.RemoveAll(d => d.uniqueId == id);
        }
    }

    public void UpdateDroppedItemPosition(string uniqueId, Vector3 localPos)
    {
        var data = droppedItems.Find(d => d.uniqueId == uniqueId);
        if (data != null)
        {
            data.localPosX = localPos.x;
            data.localPosY = localPos.y;
        }
    }

    public void RegisterDroppedItem(ItemSO so, string locationName, Vector3 localPos, int count)
    {
        var data = new DroppedItemData
        {
            uniqueId = System.Guid.NewGuid().ToString(),
            itemSOId = so.id,
            locationName = locationName,
            localPosX = localPos.x,
            localPosY = localPos.y,
            count = count
        };
        droppedItems.Add(data);
    }

    public void ApplySavedState()
    {
        foreach (var data in movedItems.Values)
        {
            if (!allSceneItems.TryGetValue(data.uniqueId, out PickupItem item)) continue;
            if (item == null) continue;

            GameObject location = FindLocationByName(data.locationName);
            if (location != null)
            {
                item.transform.SetParent(location.transform, false);
                item.transform.localPosition = new Vector3(data.localPosX, data.localPosY, 0f);
            }
            else
            {
                item.transform.position = new Vector3(data.localPosX, data.localPosY, 0f);
            }
        }

        foreach (var id in takenItemIds)
        {
            if (allSceneItems.TryGetValue(id, out PickupItem item))
                if (item != null) item.gameObject.SetActive(false);
        }

        foreach (var data in droppedItems)
        {
            if (!allSceneItems.ContainsKey(data.uniqueId))
                SpawnDroppedItem(data);
        }
    }

    private void SpawnDroppedItem(DroppedItemData data)
    {
        if (SceneManager.GetActiveScene().buildIndex == 0) return;

        var so = _itemDatabase?.GetItem(data.itemSOId);
        if (so == null || so.worldPrefab == null) return;

        GameObject location = FindLocationByName(data.locationName);
        GameObject obj = Instantiate(so.worldPrefab);

        if (location != null)
        {
            obj.transform.SetParent(location.transform, false);
            obj.transform.localPosition = new Vector3(data.localPosX, data.localPosY, 0f);
            obj.transform.SetAsFirstSibling();
        }
        else
        {
            obj.transform.position = new Vector3(data.localPosX, data.localPosY, 0f);
        }

        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in renderers) sr.sortingOrder = 50;

        SceneInjector.InjectInto(obj);

        PickupItem pickup = obj.GetComponent<PickupItem>();
        if (pickup != null)
        {
            pickup.itemSO = so;
            pickup.SetCount(data.count);
            pickup.SetUniqueId(data.uniqueId);
            allSceneItems[data.uniqueId] = pickup;
        }
    }

    private GameObject FindLocationByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        var sceneCount = SceneManager.sceneCount;
        for (int s = 0; s < sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;

            var roots = scene.GetRootGameObjects();
            int len = roots.Length;
            for (int i = 0; i < len; i++)
            {
                var root = roots[i];
                if (root == null) continue;
                if (root.name == name && root.CompareTag("Location")) return root;

                var found = FindByNameRecursive(root.transform, name);
                if (found != null) return found;
            }
        }
        return null;
    }

    private GameObject FindByNameRecursive(Transform parent, string name)
    {
        if (parent.name == name && parent.CompareTag("Location")) return parent.gameObject;

        int count = parent.childCount;
        for (int i = 0; i < count; i++)
        {
            var found = FindByNameRecursive(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    public List<string> GetTakenItemIds() => new List<string>(takenItemIds);
    public List<DroppedItemData> GetDroppedItems() => droppedItems;

    public void SetData(List<string> takenIds, List<DroppedItemData> dropped, List<MovedItemData> moved)
    {
        takenItemIds.Clear();
        if (takenIds != null)
            foreach (string id in takenIds) takenItemIds.Add(id);

        droppedItems.Clear();
        if (dropped != null) droppedItems.AddRange(dropped);

        movedItems.Clear();
        if (moved != null)
            foreach (var m in moved)
                if (m != null && !string.IsNullOrEmpty(m.uniqueId)) movedItems[m.uniqueId] = m;

        RefreshSceneItems();
        ApplySavedState();
    }
}