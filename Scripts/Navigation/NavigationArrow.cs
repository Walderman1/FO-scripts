using UnityEngine;

public class NavigationArrow : MonoBehaviour
{
    [Header("Search")]
    [SerializeField] private string rootTag = "NavigationArrow";
    [SerializeField] private string rootName = "NavigationArrow";

    [Header("Child Names")]
    [SerializeField] private string leftName = "Left";
    [SerializeField] private string rightName = "Right";
    [SerializeField] private string upName = "Up";
    [SerializeField] private string downName = "Down";

    [Header("Settings")]
    [SerializeField] private bool hideInMenuScene = true;

    private Transform root;
    private GameObject leftArrow;
    private GameObject rightArrow;
    private GameObject upArrow;
    private GameObject downArrow;

    private bool isSubscribed = false;
    private bool initialized = false;

    private ClairvoyanceAbility clairvoyance;
    private bool clairvoyanceSubscribed = false;

    private void Awake()
    {
        root = FindRoot();
        if (root == null)
        {
            Logger.LogWarning(LogModule.Navigation, "NavigationArrow: корневой объект не найден");
            return;
        }

        leftArrow = FindChildRecursive(root, leftName)?.gameObject;
        rightArrow = FindChildRecursive(root, rightName)?.gameObject;
        upArrow = FindChildRecursive(root, upName)?.gameObject;
        downArrow = FindChildRecursive(root, downName)?.gameObject;

        if (leftArrow == null) Logger.LogWarning(LogModule.Navigation, $"NavigationArrow: '{leftName}' не найден");
        if (rightArrow == null) Logger.LogWarning(LogModule.Navigation, $"NavigationArrow: '{rightName}' не найден");
        if (upArrow == null) Logger.LogWarning(LogModule.Navigation, $"NavigationArrow: '{upName}' не найден");
        if (downArrow == null) Logger.LogWarning(LogModule.Navigation, $"NavigationArrow: '{downName}' не найден");

        root.gameObject.SetActive(false);
        HideAllArrows();
    }

    private void Start()
    {
        if (root == null) return;
        StartCoroutine(SubscribeAndInit());
    }

    private void OnDestroy()
    {
        Unsubscribe();
        UnsubscribeFromClairvoyance();
    }

    private Transform FindRoot()
    {
        try
        {
            GameObject tagged = GameObject.FindGameObjectWithTag(rootTag);
            if (tagged != null) return tagged.transform;
        }
        catch (UnityException) { }

        GameObject byName = GameObject.Find(rootName);
        if (byName != null) return byName.transform;

        return null;
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChildRecursive(parent.GetChild(i), name);
            if (result != null) return result;
        }

        return null;
    }

    private System.Collections.IEnumerator SubscribeAndInit()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (GameReferences.Instance?.SceneSlideTransition == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        var sst = GameReferences.Instance?.SceneSlideTransition;
        if (sst == null)
        {
            Logger.LogWarning(LogModule.Navigation, "NavigationArrow: SceneSlideTransition не найден");
            yield break;
        }

        sst.OnSceneChanged -= HandleSceneChanged;
        sst.OnSceneChanged += HandleSceneChanged;
        isSubscribed = true;
        initialized = true;

        StartCoroutine(SubscribeToClairvoyance());
    }

    private System.Collections.IEnumerator SubscribeToClairvoyance()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (clairvoyance == null)
            {
                clairvoyance = GameReferences.Instance?.ClairvoyanceAbility;

                if (clairvoyance == null)
                {
                    var am = GameReferences.Instance?.AbilityManager;
                    if (am != null)
                        clairvoyance = am.GetAbility<ClairvoyanceAbility>();
                }
            }

            if (clairvoyance != null) break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (clairvoyance == null)
        {
            Logger.LogWarning(LogModule.Navigation, "NavigationArrow: ClairvoyanceAbility не найден");
            yield break;
        }

        clairvoyance.OnStateChanged -= HandleClairvoyanceChanged;
        clairvoyance.OnStateChanged += HandleClairvoyanceChanged;
        clairvoyanceSubscribed = true;

        Logger.LogVerbose(LogModule.Navigation, "NavigationArrow подписан на ClairvoyanceAbility");
    }

    private void UnsubscribeFromClairvoyance()
    {
        if (!clairvoyanceSubscribed) return;

        if (clairvoyance != null)
        {
            clairvoyance.OnStateChanged -= HandleClairvoyanceChanged;
        }

        clairvoyanceSubscribed = false;
    }

    private void HandleClairvoyanceChanged(bool active)
    {
        if (active) ShowRootAndRefresh();
        else HideRoot();
    }

    private void Unsubscribe()
    {
        if (!isSubscribed) return;

        var sst = GameReferences.Instance?.SceneSlideTransition;
        if (sst != null)
        {
            sst.OnSceneChanged -= HandleSceneChanged;
        }

        isSubscribed = false;
    }

    private void HandleSceneChanged(GameObject currentScene)
    {
        if (clairvoyance == null || !clairvoyance.IsActive)
        {
            HideRoot();
            return;
        }

        ShowRootAndRefresh();
    }

    private void ShowRootAndRefresh()
    {
        if (root == null) return;

        if (hideInMenuScene)
        {
            int idx = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
            if (idx == 0)
            {
                HideRoot();
                return;
            }
        }

        GameObject currentScene = GameReferences.Instance?.SceneSlideTransition?.GetCurrentScene();
        if (currentScene == null)
        {
            HideRoot();
            return;
        }

        LocationNeighbors neighbors = currentScene.GetComponent<LocationNeighbors>();
        if (neighbors == null)
        {
            HideRoot();
            return;
        }

        bool hasLeft = !string.IsNullOrEmpty(neighbors.LeftNeighbor);
        bool hasRight = !string.IsNullOrEmpty(neighbors.RightNeighbor);
        bool hasUp = !string.IsNullOrEmpty(neighbors.UpNeighbor);
        bool hasDown = !string.IsNullOrEmpty(neighbors.DownNeighbor);

        if (!root.gameObject.activeSelf)
            root.gameObject.SetActive(true);

        SetArrowActive(leftArrow, hasLeft);
        SetArrowActive(rightArrow, hasRight);
        SetArrowActive(upArrow, hasUp);
        SetArrowActive(downArrow, hasDown);

        Logger.LogVerbose(LogModule.Navigation,
            $"NavigationArrow обновлён для {currentScene.name}: L={hasLeft}, R={hasRight}, U={hasUp}, D={hasDown}");
    }

    private void HideRoot()
    {
        HideAllArrows();

        if (root != null && root.gameObject.activeSelf)
            root.gameObject.SetActive(false);
    }

    private void SetArrowActive(GameObject arrow, bool active)
    {
        if (arrow == null) return;
        if (arrow.activeSelf != active)
            arrow.SetActive(active);
    }

    private void HideAllArrows()
    {
        SetArrowActive(leftArrow, false);
        SetArrowActive(rightArrow, false);
        SetArrowActive(upArrow, false);
        SetArrowActive(downArrow, false);
    }

    public void ForceRefresh()
    {
        if (!initialized) return;
        if (clairvoyance == null || !clairvoyance.IsActive) return;
        ShowRootAndRefresh();
    }
}