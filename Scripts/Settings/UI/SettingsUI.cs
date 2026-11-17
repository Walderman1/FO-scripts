using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;

public class SettingsUI : MonoBehaviour
{
    [Header("Префабы")]
    [SerializeField] private string settingsPanelPath = "UI/Prefab/Panels/SettingsPanel";
    [SerializeField] private GameObject titlePrefab;

    [Inject] private MenuUIConfig _menuConfig;

    private GameObject panelInstance;
    private GameObject settingsCanvasObj;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private TabManager tabManager;
    private bool isOpen;
    private bool isBuilt = false;

    public bool IsOpen => isOpen;
    public GameObject PanelInstance => panelInstance;
    public System.Action OnClose;

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void PrepareForOpen()
    {
        EnsureCreated();
        if (panelInstance == null) return;

        SceneInjector.InjectInto(panelInstance);

        UICoordinator.BeforeOpen(UIKind.Settings);

        if (settingsCanvasObj != null)
        {
            settingsCanvasObj.SetActive(true);
            settingsCanvasObj.transform.SetAsLastSibling();

            Camera cam = Camera.main;
            if (cam == null) cam = FindAnyObjectByType<Camera>();

            if (cam != null && canvas != null) canvas.worldCamera = cam;
        }

        if (panelInstance != null)
        {
            RectTransform rt = panelInstance.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = Vector2.zero;
            panelInstance.SetActive(true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        if (!isBuilt && tabManager != null)
        {
            tabManager.Build();
            isBuilt = true;
        }

        isOpen = true;
    }

    public void Open() => PrepareForOpen();

    public void Close()
    {
        if (panelInstance != null)
        {
            if (OnClose != null)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.interactable = false;
                }
                var callback = OnClose;
                OnClose = null;
                callback.Invoke();
            }
            else
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.interactable = false;
                }
                panelInstance.SetActive(false);
            }
        }
        isOpen = false;
    }

    public void EnsureCreated()
    {
        if (panelInstance != null) return;

        var prefab = Resources.Load<GameObject>(settingsPanelPath);
        if (prefab == null) return;

        settingsCanvasObj = new GameObject("SettingsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        settingsCanvasObj.transform.SetParent(null);
        DontDestroyOnLoad(settingsCanvasObj);

        canvas = settingsCanvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.planeDistance = 10f;
        canvas.sortingOrder = 2000;

        Camera cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();

        if (cam != null) canvas.worldCamera = cam;
        else canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = settingsCanvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        panelInstance = Instantiate(prefab, settingsCanvasObj.transform);
        panelInstance.name = "SettingsPanel";

        var panelImage = panelInstance.GetComponent<Image>();
        if (panelImage != null) panelImage.color = new Color32(90, 255, 255, 255);

        canvasGroup = panelInstance.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = panelInstance.AddComponent<CanvasGroup>();

        panelInstance.SetActive(false);

        var rt = panelInstance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.125f, 0.125f);
            rt.anchorMax = new Vector2(0.875f, 0.875f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        if (titlePrefab == null) titlePrefab = Resources.Load<GameObject>("UI/Prefab/Title");

        if (titlePrefab != null)
        {
            var titleGo = Instantiate(titlePrefab, panelInstance.transform);
            titleGo.name = "Title_Settings";
            var txt = titleGo.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = "Настройки";
            titleGo.transform.SetAsFirstSibling();
        }

        tabManager = panelInstance.GetComponentInChildren<TabManager>(true);
        if (tabManager == null) return;

        var backBtnTrans = panelInstance.transform.Find("BackButton");
        Button backBtn = backBtnTrans != null ? backBtnTrans.GetComponent<Button>() : null;

        if (backBtn == null)
        {
            var allBtns = panelInstance.GetComponentsInChildren<Button>(true);
            foreach (var b in allBtns)
                if (b.name.ToLower().Contains("back") || b.name.ToLower().Contains("close"))
                { backBtn = b; break; }
        }

        if (backBtn == null)
        {
            GameObject backBtnObj = new GameObject("BackButton");
            backBtnObj.transform.SetParent(panelInstance.transform, false);

            RectTransform backRt = backBtnObj.AddComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0.5f, 0f);
            backRt.anchorMax = new Vector2(0.5f, 0f);
            backRt.pivot = new Vector2(0.5f, 0f);
            backRt.anchoredPosition = new Vector2(0, 30f);
            backRt.sizeDelta = new Vector2(200, 50);

            Image img = backBtnObj.AddComponent<Image>();
            img.color = _menuConfig != null ? _menuConfig.buttonNormalColor : new Color(0.2f, 0.2f, 0.2f);

            backBtn = backBtnObj.AddComponent<Button>();
            backBtn.targetGraphic = img;

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(backBtnObj.transform, false);
            RectTransform txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "Назад";
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }

        if (backBtn != null) backBtn.onClick.AddListener(Close);
    }

    public void HidePanel() => Close();
}