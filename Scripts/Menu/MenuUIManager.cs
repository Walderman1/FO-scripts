using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MenuUIManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private MenuUIConfig config;

    private BackgroundManager backgroundManager;
    private PanelManager panelManager;
    private UIBuilder uiBuilder;
    private TabManager tabManager;

    private GameObject panelPrefab, bigPanelPrefab, buttonPrefab, closeButtonPrefab, backButtonPrefab;
    private GameObject sliderContainerPrefab, togglePrefab, tabPrefab;

    private GameObject menuCanvas;
    private TMP_FontAsset defaultFont;
    private bool isFullscreen = true;
    private bool isTransitioning = false;

    private GameObject exitOverlay;
    private GameObject exitTitle;
    private GameObject exitBackButton;
    private GameObject exitQuitButton;

    private static GameObject uiRoot;

    private void Awake()
    {
        config = Resources.Load<MenuUIConfig>("Configs/MenuUIConfig") ?? ScriptableObject.CreateInstance<MenuUIConfig>();
    }

    private void Start()
    {
        EnsureEventSystem();
        LoadPrefabs();
        defaultFont = Resources.Load<TMP_FontAsset>(config.defaultFontPath);
        isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        ApplyFullscreen();

        CreateCanvas();
        InitializeManagers();
        BuildMenu();
    }

    private void OnDestroy()
    {
        if (uiRoot != null && uiRoot == gameObject) uiRoot = null;
        backgroundManager?.Cleanup();
        panelManager?.Cleanup();
        tabManager?.Cleanup();
    }

    private void InitializeManagers()
    {
        backgroundManager = gameObject.AddComponent<BackgroundManager>();
        backgroundManager.Initialize(config, GetUIRoot().transform);

        uiBuilder = gameObject.AddComponent<UIBuilder>();
        uiBuilder.Initialize(config, defaultFont, buttonPrefab, sliderContainerPrefab, togglePrefab, tabPrefab);

        panelManager = gameObject.AddComponent<PanelManager>();
        panelManager.Initialize(config, menuCanvas.transform, backgroundManager,
                               panelPrefab, bigPanelPrefab, buttonPrefab, defaultFont, uiBuilder);

        tabManager = gameObject.AddComponent<TabManager>();
        tabManager.Initialize(config, uiBuilder, isFullscreen, OnFullscreenToggle, OnResetProgress);
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

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
            uiRoot = GameObject.Find(config.uiRootName) ?? new GameObject(config.uiRootName);
        }
        return uiRoot;
    }

    private void LoadPrefabs()
    {
        panelPrefab = Resources.Load<GameObject>(config.panelPrefabPath);
        bigPanelPrefab = Resources.Load<GameObject>(config.bigPanelPrefabPath);
        buttonPrefab = Resources.Load<GameObject>(config.buttonPrefabPath);
        closeButtonPrefab = Resources.Load<GameObject>(config.closeButtonPrefabPath);
        backButtonPrefab = Resources.Load<GameObject>(config.backButtonPrefabPath);
        sliderContainerPrefab = Resources.Load<GameObject>(config.sliderContainerPrefabPath);
        togglePrefab = Resources.Load<GameObject>(config.togglePrefabPath);
        tabPrefab = Resources.Load<GameObject>(config.tabPrefabPath);
    }

    private void CreateCanvas()
    {
        menuCanvas = new GameObject(config.canvasName) { transform = { parent = GetUIRoot().transform } };

        Canvas canvas = menuCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
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
        panelManager.SetupSettingsContent(
            setupTabs: (parent) => tabManager.CreateTabs(parent),
            setupBackButton: (parent) => uiBuilder.CreateBackButton(parent, ReturnToMainMenu)
        );

        panelManager.SetupMemoriesContent(
            setupBackButton: (parent) => uiBuilder.CreateBackButton(parent, ReturnToMainMenu)
        );
    }

    #region Exit UI

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

        GameObject backButtonPrefab = Resources.Load<GameObject>(config.trixieBackButtonPath);
        if (backButtonPrefab != null)
        {
            exitBackButton = Instantiate(backButtonPrefab, menuCanvas.transform);
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
            if (backButton != null)
            {
                backButton.onClick.AddListener(() => CloseExitPanel());
            }
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
            if (exitButton != null)
            {
                exitButton.onClick.AddListener(() => OnQuit());
            }
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

    #endregion

    #region Background Animation Control

    private void CompleteCurrentBackgroundAnimation()
    {
        GameObject currentBg = backgroundManager.GetCurrentBackground();
        if (currentBg == null) return;

        FindingOneselfAnimation anim = currentBg.GetComponentInChildren<FindingOneselfAnimation>();

        if (anim != null)
        {
            Debug.Log($"Completing animation on {currentBg.name}");
            anim.CompleteAnimationImmediate();
        }
        else
        {
            CheckAllBackgroundsForAnimation();
        }
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
                if (anim != null)
                {
                    Debug.Log($"Completing animation on active background: {bg.name}");
                    anim.CompleteAnimationImmediate();
                    break;
                }
            }
        }
    }

    #endregion

    #region Buttons Creation

    private void CreateButtons()
    {
        if (panelManager.MainPanel == null) return;

        string[] buttonNames = new string[]
        {
            config.newGameButtonText,
            config.continueButtonText,
            config.memoriesButtonText,
            config.settingsButtonText,
            config.exitButtonText
        };

        foreach (string name in buttonNames)
        {
            System.Action onClick = null;
            switch (name)
            {
                case "Новая игра":
                    onClick = () => StartCoroutine(AnimateAndStartGame(true));
                    break;
                case "Продолжить":
                    onClick = () => StartCoroutine(AnimateAndStartGame(false));
                    break;
                case "Воспоминания":
                    onClick = () => OpenMemoriesPanel();
                    break;
                case "Настройки":
                    onClick = () => OpenSettingsPanel();
                    break;
                case "Выход":
                    onClick = () => OpenExitPanel();
                    break;
            }

            GameObject btn = uiBuilder.CreateButton(panelManager.MainPanel.transform, name, onClick);
        }
    }

    #endregion

    #region Game Start Animation

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

        if (isNewGame)
        {
            OnNewGame();
        }
        else
        {
            OnContinue();
        }
    }

    #endregion

    #region Panel Opening Methods

    private void OpenSettingsPanel()
    {
        if (isTransitioning) return;

        CompleteCurrentBackgroundAnimation();

        StartCoroutine(SwitchPanelWithBackground(
            panelManager.SettingsPanel,
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

    #endregion

    #region Panel Switching with Animation

    private IEnumerator SwitchPanelWithBackground(GameObject targetPanel, Vector2 direction,
                                                  GameObject targetBackground, Vector2 bgStartOffset)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        if (panelManager.CurrentPanel == null)
        {
            isTransitioning = false;
            yield break;
        }

        GameObject currentPanel = panelManager.CurrentPanel;
        RectTransform currentRt = currentPanel.GetComponent<RectTransform>();
        if (currentRt == null)
        {
            isTransitioning = false;
            yield break;
        }

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

    #endregion

    #region Return to Main Menu

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

    #endregion

    #region Event Handlers

    private void OnFullscreenToggle(bool value)
    {
        isFullscreen = value;
        ApplyFullscreen();
        PlayerPrefs.SetInt("Fullscreen", value ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyFullscreen() => Screen.fullScreen = isFullscreen;

    private void OnResetProgress()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("Прогресс сброшен!");
        StartCoroutine(ShowResetNotification());
    }

    private IEnumerator ShowResetNotification()
    {
        if (panelManager.SettingsPanel == null) yield break;

        GameObject notify = new GameObject("ResetNotification");
        notify.transform.SetParent(panelManager.SettingsPanel.transform, false);

        RectTransform rt = notify.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = config.resetNotifySize;
        rt.anchoredPosition = new Vector2(0, config.resetNotifyYOffset);

        TextMeshProUGUI tmp = notify.AddComponent<TextMeshProUGUI>();
        tmp.text = config.resetNotifyText;
        tmp.fontSize = config.resetNotifyFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = config.resetNotifyColor;
        if (defaultFont != null) tmp.font = defaultFont;

        yield return new WaitForSeconds(config.resetNotifyDuration);
        Destroy(notify);
    }

    private void OnNewGame()
    {
        Debug.Log("Новая игра");
        GlobalControl.Instance?.StartNewGame();
    }

    private void OnContinue()
    {
        Debug.Log("Продолжить");
        GlobalControl.Instance?.ContinueGame();
    }

    private void OnQuit()
    {
        Debug.Log("Выход из игры");
        GlobalControl.Instance?.QuitGame();
    }

    #endregion
}