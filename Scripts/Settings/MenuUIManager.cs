using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using VContainer;

public class MenuUIManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private MenuUIConfig config;

    [Inject] private SettingsManager _settingsManager;
    [Inject] private GlobalControl _globalControl;

    private BackgroundManager backgroundManager;
    private PanelManager panelManager;
    private UIBuilder uiBuilder;

    private GameObject panelPrefab, buttonPrefab, closeButtonPrefab, backButtonPrefab;

    private GameObject menuCanvas;
    private TMP_FontAsset defaultFont;
    private bool isTransitioning = false;
    private bool isBuilt = false;

    private GameObject exitOverlay;
    private GameObject exitTitle;
    private GameObject exitBackButton;
    private GameObject exitQuitButton;

    private Button continueButton;
    private Button loadButton;

    private static GameObject uiRoot;

    public MenuUIConfig GetConfig() => config;

    private void Awake()
    {
        if (config == null)
            config = Resources.Load<MenuUIConfig>("Configs/MenuUIConfig");

        if (config == null)
            config = ScriptableObject.CreateInstance<MenuUIConfig>();
    }

    private void Start()
    {
        int buildIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        if (buildIndex != 0)
        {
            Logger.LogWarning(LogModule.Menu,
                $"MenuUIManager найден в игровой сцене '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}' — меню не будет построено");
            enabled = false;
            return;
        }

        BuildIfNeeded();
    }

    private void BuildIfNeeded()
    {
        if (isBuilt && menuCanvas != null && !menuCanvas.Equals(null)) return;

        EnsureEventSystem();
        LoadPrefabs();
        defaultFont = Resources.Load<TMP_FontAsset>(config.defaultFontPath);

        _settingsManager?.ApplySettings();

        CreateCanvas();
        InitializeManagers();
        BuildMenu();

        isBuilt = true;
    }

    private void OnDestroy()
    {
        backgroundManager?.Cleanup();
        panelManager?.Cleanup();

        if (exitOverlay != null) { Destroy(exitOverlay); exitOverlay = null; }
        if (exitTitle != null) { Destroy(exitTitle); exitTitle = null; }
        if (exitBackButton != null) { Destroy(exitBackButton); exitBackButton = null; }
        if (exitQuitButton != null) { Destroy(exitQuitButton); exitQuitButton = null; }

        continueButton = null;
        loadButton = null;
        isBuilt = false;
    }

    public void ShowMenuUI()
    {
        BuildIfNeeded();

        if (menuCanvas != null && !menuCanvas.Equals(null))
            menuCanvas.SetActive(true);

        isTransitioning = false;

        if (exitOverlay != null) exitOverlay.SetActive(false);
        if (exitTitle != null) exitTitle.SetActive(false);
        if (exitBackButton != null) exitBackButton.SetActive(false);
        if (exitQuitButton != null) exitQuitButton.SetActive(false);

        if (panelManager != null && panelManager.MainPanel != null)
        {
            panelManager.MainPanel.SetActive(true);
            RectTransform rt = panelManager.MainPanel.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = Vector2.zero;
            panelManager.ShowPanel(panelManager.MainPanel);
        }

        if (backgroundManager != null)
        {
            var mainBg = backgroundManager.GetMainBackground();
            if (mainBg != null)
                backgroundManager.ShowBackground(mainBg);
        }

        UpdateContinueButtonState();
    }

    public void HideMenuUI()
    {
        if (menuCanvas != null && !menuCanvas.Equals(null))
            menuCanvas.SetActive(false);
    }

    private void InitializeManagers()
    {
        if (backgroundManager == null)
            backgroundManager = gameObject.AddComponent<BackgroundManager>();
        if (uiBuilder == null)
            uiBuilder = gameObject.AddComponent<UIBuilder>();
        if (panelManager == null)
            panelManager = gameObject.AddComponent<PanelManager>();

        backgroundManager.Initialize(config, GetUIRoot().transform);
        uiBuilder.Initialize(config, defaultFont, buttonPrefab);
        panelManager.Initialize(config, menuCanvas.transform, backgroundManager,
                               panelPrefab, buttonPrefab, defaultFont, uiBuilder);
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
        es.transform.SetParent(GetUIRoot().transform);
    }

    private GameObject GetUIRoot()
    {
        if (uiRoot != null)
        {
            try
            {
                if (uiRoot == null || uiRoot.Equals(null)) uiRoot = null;
            }
            catch { uiRoot = null; }
        }

        if (uiRoot == null)
        {
            uiRoot = new GameObject("UIRoot_Persistent");
            DontDestroyOnLoad(uiRoot);
        }

        return uiRoot;
    }

    private void LoadPrefabs()
    {
        panelPrefab = Resources.Load<GameObject>(config.panelPrefabPath);
        buttonPrefab = Resources.Load<GameObject>(config.buttonPrefabPath);
        closeButtonPrefab = Resources.Load<GameObject>(config.closeButtonPrefabPath);
        backButtonPrefab = Resources.Load<GameObject>(config.backButtonPrefabPath);
    }

    private void CreateCanvas()
    {
        var root = GetUIRoot();

        if (menuCanvas != null && !menuCanvas.Equals(null))
        {
            if (menuCanvas.transform.parent != root.transform)
                menuCanvas.transform.SetParent(root.transform, false);
            return;
        }

        var existing = root.transform.Find(config.canvasName);
        if (existing != null)
        {
            menuCanvas = existing.gameObject;
            return;
        }

        menuCanvas = new GameObject(config.canvasName) { transform = { parent = root.transform } };

        Canvas canvas = menuCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;

        Camera cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();

        if (cam != null)
        {
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        canvas.sortingOrder = 1000;

        CanvasScaler scaler = menuCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        if (menuCanvas.GetComponent<GraphicRaycaster>() == null)
            menuCanvas.AddComponent<GraphicRaycaster>();
    }

    private void BuildMenu()
    {
        backgroundManager.CreateBackgrounds();
        panelManager.CreateAllPanels();
        CreateButtons();
        SetupPanelsContent();
        panelManager.UpdateMainPanelSize();
        panelManager.ShowPanel(panelManager.MainPanel);
        backgroundManager.ShowBackground(backgroundManager.GetMainBackground());
        CreateExitUI();
    }

    private void SetupPanelsContent()
    {
        panelManager.SetupMemoriesContent(
            setupBackButton: (parent) => uiBuilder.CreateBackButton(parent, ReturnToMainMenu)
        );
    }

    private void CreateExitUI()
    {
        exitOverlay = new GameObject(config.exitOverlayName);
        exitOverlay.transform.SetParent(menuCanvas.transform, false);
        exitOverlay.SetActive(false);

        RectTransform overlayRt = exitOverlay.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.sizeDelta = Vector2.zero;

        Image overlayImg = exitOverlay.AddComponent<Image>();
        overlayImg.color = config.overlayColor;
        overlayImg.raycastTarget = true;

        exitTitle = new GameObject(config.exitTitleName);
        exitTitle.transform.SetParent(menuCanvas.transform, false);
        exitTitle.SetActive(false);

        RectTransform titleRt = exitTitle.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(600, 80);
        titleRt.anchoredPosition = new Vector2(0, config.exitTitleYOffset);

        TextMeshProUGUI titleTmp = exitTitle.AddComponent<TextMeshProUGUI>();
        titleTmp.text = config.exitConfirmTitleText;
        titleTmp.fontSize = 42;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        titleTmp.raycastTarget = false;
        titleTmp.fontStyle = FontStyles.Bold;
        if (defaultFont != null) titleTmp.font = defaultFont;

        GameObject backButtonPrefabLocal = Resources.Load<GameObject>(config.trixieBackButtonPath);
        if (backButtonPrefabLocal != null)
        {
            exitBackButton = Instantiate(backButtonPrefabLocal, menuCanvas.transform);
            exitBackButton.name = config.exitBackButtonName;
            exitBackButton.SetActive(false);

            RectTransform backRt = exitBackButton.GetComponent<RectTransform>();
            if (backRt != null)
            {
                backRt.anchorMin = new Vector2(0f, 0.5f);
                backRt.anchorMax = new Vector2(0f, 0.5f);
                backRt.pivot = new Vector2(0f, 0.5f);
                backRt.anchoredPosition = new Vector2(200, config.exitButtonsYOffset);
                backRt.sizeDelta = config.exitButtonSize;
            }

            TMP_Text backText = exitBackButton.GetComponentInChildren<TMP_Text>();
            if (backText != null)
            {
                backText.text = config.backToMenuText;
                backText.fontSize = 32;
                backText.alignment = TextAlignmentOptions.Center;
                backText.color = Color.white;
                backText.fontStyle = FontStyles.Bold;
            }

            Button backButton = exitBackButton.GetComponent<Button>();
            if (backButton != null) backButton.onClick.AddListener(() => CloseExitPanel());
        }
        else
        {
            exitBackButton = uiBuilder.CreateButton(menuCanvas.transform, config.backToMenuText, CloseExitPanel);
            exitBackButton.SetActive(false);
            RectTransform backRt = exitBackButton.GetComponent<RectTransform>();
            if (backRt != null)
            {
                backRt.anchorMin = new Vector2(0f, 0.5f);
                backRt.anchorMax = new Vector2(0f, 0.5f);
                backRt.pivot = new Vector2(0f, 0.5f);
                backRt.anchoredPosition = new Vector2(200, config.exitButtonsYOffset);
                backRt.sizeDelta = config.exitButtonSize;
            }
        }

        GameObject exitButtonPrefab = Resources.Load<GameObject>(config.trixieExitButtonPath);
        if (exitButtonPrefab != null)
        {
            exitQuitButton = Instantiate(exitButtonPrefab, menuCanvas.transform);
            exitQuitButton.name = config.exitQuitButtonName;
            exitQuitButton.SetActive(false);

            RectTransform exitRt = exitQuitButton.GetComponent<RectTransform>();
            if (exitRt != null)
            {
                exitRt.anchorMin = new Vector2(1f, 0.5f);
                exitRt.anchorMax = new Vector2(1f, 0.5f);
                exitRt.pivot = new Vector2(1f, 0.5f);
                exitRt.anchoredPosition = new Vector2(-200, config.exitButtonsYOffset);
                exitRt.sizeDelta = config.exitButtonSize;
            }

            TMP_Text exitText = exitQuitButton.GetComponentInChildren<TMP_Text>();
            if (exitText != null)
            {
                exitText.text = config.quitButtonText;
                exitText.fontSize = 32;
                exitText.alignment = TextAlignmentOptions.Center;
                exitText.color = Color.white;
                exitText.fontStyle = FontStyles.Bold;
            }

            Button exitButton = exitQuitButton.GetComponent<Button>();
            if (exitButton != null) exitButton.onClick.AddListener(() => OnQuit());
        }
        else
        {
            exitQuitButton = uiBuilder.CreateButton(menuCanvas.transform, config.quitButtonText, OnQuit);
            exitQuitButton.SetActive(false);
            RectTransform exitRt = exitQuitButton.GetComponent<RectTransform>();
            if (exitRt != null)
            {
                exitRt.anchorMin = new Vector2(1f, 0.5f);
                exitRt.anchorMax = new Vector2(1f, 0.5f);
                exitRt.pivot = new Vector2(1f, 0.5f);
                exitRt.anchoredPosition = new Vector2(-200, config.exitButtonsYOffset);
                exitRt.sizeDelta = config.exitButtonSize;
            }
        }
    }

    private void OpenExitPanel()
    {
        if (isTransitioning) return;
        CompleteCurrentBackgroundAnimation();
        panelManager.HideAllPanels();
        backgroundManager.HideAllBackgrounds();
        exitOverlay.SetActive(true);
        exitTitle.SetActive(true);
        exitBackButton.SetActive(true);
        exitQuitButton.SetActive(true);
        StartCoroutine(AnimateExitPanel(true));
    }

    private void CloseExitPanel()
    {
        if (isTransitioning) return;
        StartCoroutine(AnimateExitPanel(false));
    }

    private IEnumerator AnimateExitPanel(bool isOpening)
    {
        isTransitioning = true;

        RectTransform titleRt = exitTitle.GetComponent<RectTransform>();
        RectTransform backRt = exitBackButton.GetComponent<RectTransform>();
        RectTransform quitRt = exitQuitButton.GetComponent<RectTransform>();

        float startYTitle = config.exitTitleStartY;
        float endYTitle = config.exitTitleEndY;
        float startYButtons = config.exitButtonsStartY;
        float endYButtons = config.exitButtonsEndY;

        if (!isOpening)
        {
            float temp = startYTitle;
            startYTitle = endYTitle;
            endYTitle = temp;
            temp = startYButtons;
            startYButtons = endYButtons;
            endYButtons = temp;
        }

        float elapsed = 0f;
        Vector2 titleStart = new Vector2(0, startYTitle);
        Vector2 titleEnd = new Vector2(0, endYTitle);
        Vector2 backStart = new Vector2(200, startYButtons);
        Vector2 backEnd = new Vector2(200, endYButtons);
        Vector2 quitStart = new Vector2(-200, startYButtons);
        Vector2 quitEnd = new Vector2(-200, endYButtons);

        if (isOpening)
        {
            titleRt.anchoredPosition = titleStart;
            backRt.anchoredPosition = backStart;
            quitRt.anchoredPosition = quitStart;
        }

        float duration = config.exitTransitionDuration;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smooth = t * t * (3f - 2f * t);
            titleRt.anchoredPosition = Vector2.Lerp(titleStart, titleEnd, smooth);
            backRt.anchoredPosition = Vector2.Lerp(backStart, backEnd, smooth);
            quitRt.anchoredPosition = Vector2.Lerp(quitStart, quitEnd, smooth);
            yield return null;
        }

        titleRt.anchoredPosition = titleEnd;
        backRt.anchoredPosition = backEnd;
        quitRt.anchoredPosition = quitEnd;

        if (!isOpening)
        {
            exitOverlay.SetActive(false);
            exitTitle.SetActive(false);
            exitBackButton.SetActive(false);
            exitQuitButton.SetActive(false);
            yield return StartCoroutine(ShowMainMenu());
        }

        isTransitioning = false;
    }

    private IEnumerator ShowMainMenu()
    {
        GameObject mainPanel = panelManager.MainPanel;
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
            RectTransform mainRt = mainPanel.GetComponent<RectTransform>();
            if (mainRt != null)
            {
                Vector2 startPos = new Vector2(mainRt.anchoredPosition.x, -config.panelTransitionOffset);
                Vector2 endPos = new Vector2(mainRt.anchoredPosition.x, 0);
                mainRt.anchoredPosition = startPos;
                float elapsed = 0f;
                while (elapsed < config.panelTransitionDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / config.panelTransitionDuration;
                    float smooth = t * t * (3f - 2f * t);
                    mainRt.anchoredPosition = Vector2.Lerp(startPos, endPos, smooth);
                    yield return null;
                }
                mainRt.anchoredPosition = endPos;
            }
            panelManager.ShowPanel(mainPanel);
        }

        GameObject mainBg = backgroundManager.GetMainBackground();
        if (mainBg != null)
        {
            mainBg.SetActive(true);
            mainBg.transform.position = new Vector3(0, -config.backgroundOffset / 100f, 10f);
            Vector3 startPos = mainBg.transform.position;
            Vector3 endPos = new Vector3(0, 0, 10f);
            float elapsed = 0f;
            while (elapsed < config.backgroundTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / config.backgroundTransitionDuration;
                float smooth = t * t * (3f - 2f * t);
                mainBg.transform.position = Vector3.Lerp(startPos, endPos, smooth);
                yield return null;
            }
            mainBg.transform.position = endPos;
            backgroundManager.ShowBackground(mainBg);
        }
    }

    private void CompleteCurrentBackgroundAnimation()
    {
        GameObject currentBg = backgroundManager.GetCurrentBackground();
        if (currentBg == null) return;

        FindingOneselfAnimation anim = currentBg.GetComponentInChildren<FindingOneselfAnimation>();
        if (anim != null) anim.CompleteAnimationImmediate();
        else CheckAllBackgroundsForAnimation();
    }

    private void CheckAllBackgroundsForAnimation()
    {
        GameObject[] backgrounds = new GameObject[]
        {
            backgroundManager.GetMainBackground(),
            backgroundManager.GetSettingsBackground(),
            backgroundManager.GetMemoriesBackground()
        };

        foreach (GameObject bg in backgrounds)
        {
            if (bg != null && bg.activeSelf)
            {
                FindingOneselfAnimation anim = bg.GetComponentInChildren<FindingOneselfAnimation>();
                if (anim != null) { anim.CompleteAnimationImmediate(); break; }
            }
        }
    }

    private void CreateButtons()
    {
        if (panelManager.MainPanel == null) return;

        string[] buttonNames = new string[]
        {
            config.newGameButtonText,
            config.continueButtonText,
            config.loadButtonText,
            config.memoriesButtonText,
            config.settingsButtonText,
            config.exitButtonText
        };

        for (int i = 0; i < buttonNames.Length; i++)
        {
            string name = buttonNames[i];
            System.Action onClick = null;

            if (i == 0) onClick = () => ShowNewGameConfirmation();
            else if (i == 1) onClick = () => StartCoroutine(AnimateAndStartGame(false));
            else if (i == 2) onClick = () => OpenLoadPanel();
            else if (i == 3) onClick = () => OpenMemoriesPanel();
            else if (i == 4) onClick = () => OpenSettingsPanel();
            else if (i == 5) onClick = () => OpenExitPanel();

            GameObject btn = uiBuilder.CreateButton(panelManager.MainPanel.transform, name, onClick);

            if (i == 1) continueButton = btn.GetComponent<Button>();
            else if (i == 2) loadButton = btn.GetComponent<Button>();
        }

        UpdateContinueButtonState();
    }

    public void StartNewGameFromMenu()
    {
        StartCoroutine(AnimateAndStartGame(true));
    }

    public void LoadGameFromMenu()
    {
        StartCoroutine(AnimateAndStartGame(false));
    }

    private IEnumerator AnimateAndStartGame(bool isNewGame)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        GameObject currentPanel = panelManager.CurrentPanel;
        GameObject currentBg = backgroundManager.GetCurrentBackground();

        if (currentPanel != null)
        {
            RectTransform panelRt = currentPanel.GetComponent<RectTransform>();
            if (panelRt != null)
            {
                Vector2 startPos = panelRt.anchoredPosition;
                Vector2 endPos = startPos + Vector2.up * config.panelTransitionOffset;
                float elapsed = 0f;
                while (elapsed < config.panelTransitionDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / config.panelTransitionDuration;
                    float smooth = t * t * (3f - 2f * t);
                    panelRt.anchoredPosition = Vector2.Lerp(startPos, endPos, smooth);
                    yield return null;
                }
                panelRt.anchoredPosition = endPos;
            }
            currentPanel.SetActive(false);
        }

        if (currentBg != null)
        {
            Vector3 startPos = currentBg.transform.position;
            Vector3 endPos = startPos + new Vector3(0, config.backgroundOffset / 100f, 0);
            float elapsed = 0f;
            while (elapsed < config.backgroundTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / config.backgroundTransitionDuration;
                float smooth = t * t * (3f - 2f * t);
                currentBg.transform.position = Vector3.Lerp(startPos, endPos, smooth);
                yield return null;
            }
            currentBg.transform.position = endPos;
            currentBg.SetActive(false);
        }

        yield return new WaitForSeconds(0.1f);

        isTransitioning = false;

        if (isNewGame) OnNewGame();
        else OnContinue();
    }

    private void OpenSettingsPanel()
    {
        if (isTransitioning) return;
        CompleteCurrentBackgroundAnimation();

        var settingsUI = _globalControl?.GameSettings;
        if (settingsUI == null) return;

        settingsUI.EnsureCreated();
        if (settingsUI.PanelInstance == null) return;

        settingsUI.OnClose = ReturnToMainMenu;
        settingsUI.PrepareForOpen();

        RectTransform targetRt = settingsUI.PanelInstance.GetComponent<RectTransform>();
        if (targetRt != null) targetRt.anchoredPosition = new Vector2(config.panelTransitionOffset, 0);

        StartCoroutine(SwitchPanelWithBackground(
            settingsUI.PanelInstance,
            Vector2.left,
            backgroundManager.GetSettingsBackground(),
            new Vector2(-config.backgroundOffset, 0)
        ));
    }

    private void OpenMemoriesPanel()
    {
        if (isTransitioning) return;
        CompleteCurrentBackgroundAnimation();
        StartCoroutine(SwitchPanelWithBackground(
            panelManager.MemoriesPanel,
            Vector2.right,
            backgroundManager.GetMemoriesBackground(),
            new Vector2(config.backgroundOffset, 0)
        ));
    }

    private IEnumerator SwitchPanelWithBackground(GameObject targetPanel, Vector2 direction,
                                                  GameObject targetBackground, Vector2 bgStartOffset)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        if (panelManager.CurrentPanel == null) { isTransitioning = false; yield break; }

        GameObject currentPanel = panelManager.CurrentPanel;
        RectTransform currentRt = currentPanel.GetComponent<RectTransform>();
        if (currentRt == null) { isTransitioning = false; yield break; }

        Vector2 startPos = currentRt.anchoredPosition;
        Vector2 endPos = startPos + direction * config.panelTransitionOffset;
        float elapsed = 0f;

        while (elapsed < config.panelTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / config.panelTransitionDuration;
            float smooth = t * t * (3f - 2f * t);
            currentRt.anchoredPosition = Vector2.Lerp(startPos, endPos, smooth);
            yield return null;
        }

        panelManager.SwitchPanel(null);

        if (targetBackground != null && targetBackground != backgroundManager.GetCurrentBackground())
        {
            yield return StartCoroutine(backgroundManager.AnimateBackgrounds(
                backgroundManager.GetCurrentBackground(),
                targetBackground,
                direction,
                bgStartOffset));
        }

        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
            RectTransform targetRt = targetPanel.GetComponent<RectTransform>();
            if (targetRt != null)
            {
                Vector2 startNew = -direction * config.panelTransitionOffset;
                Vector2 endNew = Vector2.zero;
                targetRt.anchoredPosition = startNew;
                elapsed = 0f;
                while (elapsed < config.panelTransitionDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / config.panelTransitionDuration;
                    float smooth = t * t * (3f - 2f * t);
                    targetRt.anchoredPosition = Vector2.Lerp(startNew, endNew, smooth);
                    yield return null;
                }
                targetRt.anchoredPosition = endNew;
            }
            panelManager.SwitchPanel(targetPanel);
        }

        isTransitioning = false;
    }

    private void ReturnToMainMenu()
    {
        if (isTransitioning) return;
        CompleteCurrentBackgroundAnimation();
        StartCoroutine(SwitchPanelWithBackground(
            panelManager.MainPanel,
            Vector2.right,
            backgroundManager.GetMainBackground(),
            new Vector2(-config.backgroundOffset, 0)
        ));
    }

    private void OnNewGame()
    {
        if (_globalControl == null) return;
        _globalControl.StartNewGame();
    }

    private void OnContinue()
    {
        if (_globalControl == null) return;
        _globalControl.ContinueGame();
    }

    private void OnQuit()
    {
        _globalControl?.QuitGame();
    }

    public void UpdateContinueButtonState()
    {
        bool hasAnySave = GamePaths.HasAnySave();
        if (continueButton != null) continueButton.interactable = hasAnySave;
        if (loadButton != null) loadButton.interactable = hasAnySave;
    }

    public void RefreshMenuButtons()
    {
        UpdateContinueButtonState();
    }

    private void ShowNewGameConfirmation()
    {
        ShowConfirmation("Начать новую игру? Несохраненный прогресс будет потерян.",
            () => {
                var panel = gameObject.GetComponent<SaveSelectionPanel>();
                if (panel == null) panel = gameObject.AddComponent<SaveSelectionPanel>();
                panel.Show(true, menuCanvas.transform);
            }, null);
    }

    private void OpenLoadPanel()
    {
        var panel = gameObject.GetComponent<SaveSelectionPanel>();
        if (panel == null) panel = gameObject.AddComponent<SaveSelectionPanel>();
        panel.Show(false, menuCanvas.transform);
    }

    private void ShowConfirmation(string message, System.Action onYes, System.Action onNo)
    {
        var overlay = new GameObject("ConfirmationOverlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(menuCanvas.transform, false);
        var rt = overlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = overlay.GetComponent<Image>();
        img.color = config.overlayColor;

        GameObject bigPanelPrefab = Resources.Load<GameObject>("UI/Prefab/Panels/BigPanel");
        GameObject cancelButtonPrefab = Resources.Load<GameObject>("UI/Prefab/Buttons/CancelButton");

        if (bigPanelPrefab == null || cancelButtonPrefab == null) { Destroy(overlay); return; }

        GameObject panel = Instantiate(bigPanelPrefab, overlay.transform);
        panel.name = "ConfirmationPanel";
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(600, 300);
        panelRt.anchoredPosition = Vector2.zero;

        var panelImg = panel.GetComponent<Image>();
        if (panelImg != null) panelImg.color = Color.white;

        var textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(panel.transform, false);
        var textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.1f, 0.4f);
        textRt.anchorMax = new Vector2(0.9f, 0.9f);
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = message;
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;
        if (defaultFont != null) tmp.font = defaultFont;

        var btnRow = new GameObject("Buttons", typeof(RectTransform));
        btnRow.transform.SetParent(panel.transform, false);
        var btnRowRt = btnRow.GetComponent<RectTransform>();
        btnRowRt.anchorMin = new Vector2(0.5f, 0);
        btnRowRt.anchorMax = new Vector2(0.5f, 0);
        btnRowRt.sizeDelta = new Vector2(400, 60);
        btnRowRt.anchoredPosition = new Vector2(0, 70);

        var yesBtnObj = Instantiate(cancelButtonPrefab, btnRow.transform);
        var yesRt = yesBtnObj.GetComponent<RectTransform>();
        yesRt.anchorMin = new Vector2(0.3f, 0.5f);
        yesRt.anchorMax = new Vector2(0.3f, 0.5f);
        yesRt.sizeDelta = new Vector2(150, 50);
        yesRt.anchoredPosition = Vector2.zero;
        var yesBtn = yesBtnObj.GetComponent<Button>() ?? yesBtnObj.AddComponent<Button>();
        var yesTmp = yesBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (yesTmp != null) { yesTmp.text = "Да"; yesTmp.color = Color.black; yesTmp.fontSize = 24; if (defaultFont != null) yesTmp.font = defaultFont; }
        yesBtn.onClick.AddListener(() => { Destroy(overlay); onYes?.Invoke(); });

        var noBtnObj = Instantiate(cancelButtonPrefab, btnRow.transform);
        var noRt = noBtnObj.GetComponent<RectTransform>();
        noRt.anchorMin = new Vector2(0.7f, 0.5f);
        noRt.anchorMax = new Vector2(0.7f, 0.5f);
        noRt.sizeDelta = new Vector2(150, 50);
        noRt.anchoredPosition = Vector2.zero;
        var noBtn = noBtnObj.GetComponent<Button>() ?? noBtnObj.AddComponent<Button>();
        var noTmp = noBtnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (noTmp != null) { noTmp.text = "Нет"; noTmp.color = Color.black; noTmp.fontSize = 24; if (defaultFont != null) noTmp.font = defaultFont; }
        noBtn.onClick.AddListener(() => { Destroy(overlay); onNo?.Invoke(); });
    }
}