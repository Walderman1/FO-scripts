using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GlobalControl : MonoBehaviour
{
    public static GlobalControl Instance;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public Slider volumeSlider;
    public AudioClip detectionClip;
    private float _audioVolume;

    [Header("UI Elements")]
    public GameObject settingsPanel;
    public GameObject fullscreenToggleImage;
    public Button continueButton;
    public Dropdown cursorDropdown;
    public TextBeginner textBeginner;
    public RadialMenu radialMenu;

    [Header("Visual Effects")]
    public VolumeProfile postProcessProfile;
    public Animator transitionAnimator;
    private DepthOfField _depthOfField;

    [Header("Game Objects")]
    public GameObject currentObject;
    public GameObject nextObject;
    public GameObject[] inventoryItems;
    public List<GameObject> audioSources = new List<GameObject>();
    [SerializeField] private LayerMask targetLayer;

    [Header("Cursor Settings")]
    public Texture2D cursorTexture;

    [Header("Player State")]
    public bool isMenuOpen;
    public bool isMagicLightActive;
    private Vector3 _mousePosition;

    [Header("Abilities")]
    [SerializeField] private AbilityManager abilityManager;
    private MagicLightAbility magicLightAbility;

    [Header("Inventory")]
    [SerializeField] private string inventoryPanelTag = "InventoryPanel";
    private InventoryUIManager inventoryManager;

    [Header("Equipment")]
    [SerializeField] private string equipmentPanelTag = "EquipmentPanel";
    private CanvasGroup equipmentCanvasGroup;
    private bool isEquipmentOpen = false;
    private Coroutine equipmentFadeCoroutine = null;
    private float equipmentFadeDuration = 0.3f;

    [Header("Radial Menu")]
    [SerializeField] private string radialMenuTag = "RadialMenu";

    [Header("Input Keys")]
    [SerializeField] private KeyCode equipmentKey = KeyCode.E;
    [SerializeField] private KeyCode questKey = KeyCode.J;

    private const string CURSOR_PREF_KEY = "Cursor";
    private const string FULLSCREEN_PREF_KEY = "Fullscreen";
    private const string VOLUME_PREF_KEY = "Volume";
    private const string SAVE_PATH = "/saves/save.sv";
    private const string CURSOR_RESOURCE_PATH = "Hooves/";

    private bool isSceneInitialized = false;
    private int currentSceneIndex = -1;
    private List<Component> componentsToKeep = new List<Component>();

    #region Singleton

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Logger.Log(LogModule.Core, "GlobalControl инициализирован");

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Logger.Log(LogModule.Core, "Уничтожение дублирующего GlobalControl");
            Destroy(gameObject);
            return;
        }

        componentsToKeep.Clear();
        componentsToKeep.Add(this);
        componentsToKeep.Add(GetComponent<Transform>());

        if (textBeginner == null)
        {
            textBeginner = GetComponent<TextBeginner>();
            if (textBeginner == null)
            {
                textBeginner = FindObjectOfType<TextBeginner>();
            }
            Logger.Log(LogModule.Core, $"TextBeginner найден: {textBeginner != null}");
        }

        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        Logger.Log(LogModule.Core, $"Текущая сцена: {sceneIndex}");

        if (sceneIndex == 0)
        {
            Logger.Log(LogModule.Core, "Сцена меню - добавление MenuUIManager");
            EnsureMenuUIManager();
        }
    }

    private void EnsureMenuUIManager()
    {
        MenuUIManager oldManager = GetComponent<MenuUIManager>();
        if (oldManager != null)
        {
            Destroy(oldManager);
        }

        gameObject.AddComponent<MenuUIManager>();
        Logger.Log(LogModule.Core, "MenuUIManager добавлен в GlobalControl");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (magicLightAbility != null)
        {
            magicLightAbility.OnStateChanged -= OnMagicLightStateChanged;
        }
    }

    #endregion

    #region Scene Loaded Handler

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Logger.Log(LogModule.Core, $"Загружена сцена: {scene.name} (индекс: {scene.buildIndex})");

        isSceneInitialized = false;
        currentSceneIndex = scene.buildIndex;

        if (scene.buildIndex == 0)
        {
            Logger.Log(LogModule.Core, "Загружена сцена меню - добавление MenuUIManager");
            EnsureMenuUIManager();
            CleanupGameComponents();
        }
        else
        {
            Logger.Log(LogModule.Core, "Загружена игровая сцена - инициализация игровых компонентов");
            CleanupComponents();

            MenuUIManager menuManager = GetComponent<MenuUIManager>();
            if (menuManager != null)
            {
                Destroy(menuManager);
            }

            InitializeGameScene();
        }
    }

    private void CleanupComponents()
    {
        Logger.Log(LogModule.Core, "Очистка компонентов на объекте GlobalControl");

        Component[] allComponents = GetComponents<Component>();
        List<Component> toDestroy = new List<Component>();

        foreach (Component comp in allComponents)
        {
            if (comp is GlobalControl || comp is Transform)
                continue;

            bool shouldKeep = false;
            foreach (Component keep in componentsToKeep)
            {
                if (keep == comp)
                {
                    shouldKeep = true;
                    break;
                }
            }

            if (!shouldKeep)
            {
                toDestroy.Add(comp);
            }
        }

        foreach (Component comp in toDestroy)
        {
            Logger.Log(LogModule.Core, $"Уничтожение компонента: {comp.GetType().Name}");
            Destroy(comp);
        }

        audioSource = null;
        volumeSlider = null;
        detectionClip = null;
        settingsPanel = null;
        fullscreenToggleImage = null;
        continueButton = null;
        cursorDropdown = null;
        textBeginner = null;
        radialMenu = null;
        postProcessProfile = null;
        transitionAnimator = null;
        abilityManager = null;
        magicLightAbility = null;
        inventoryManager = null;
        equipmentCanvasGroup = null;
        audioSources.Clear();

        isSceneInitialized = false;
        isMenuOpen = false;
        isMagicLightActive = false;
        isEquipmentOpen = false;

        if (equipmentFadeCoroutine != null)
        {
            StopCoroutine(equipmentFadeCoroutine);
            equipmentFadeCoroutine = null;
        }

        Logger.Log(LogModule.Core, $"Уничтожено {toDestroy.Count} компонентов на объекте GlobalControl");
    }

    private void CleanupGameComponents()
    {
        inventoryManager = null;
        equipmentCanvasGroup = null;
        isEquipmentOpen = false;

        if (equipmentFadeCoroutine != null)
        {
            StopCoroutine(equipmentFadeCoroutine);
            equipmentFadeCoroutine = null;
        }
    }

    private void InitializeGameScene()
    {
        if (isSceneInitialized) return;

        FindInventoryManager();
        FindEquipmentPanel();
        FindRadialMenu();
        FindTextBeginner();
        FindAudioSources();
        FindAbilityManager();
        FindMagicLightAbility();
        FindSettingsPanel();
        FindVolumeSlider();
        FindContinueButton();
        FindCursorDropdown();
        FindPostProcessProfile();
        FindTransitionAnimator();

        if (radialMenu != null)
        {
            radialMenu.onButtonClick += OnRadialButtonClick;
            Logger.Log(LogModule.Core, "RadialMenu подписан после загрузки сцены");
        }

        CheckForSaveFile();

        isSceneInitialized = true;
        Logger.Log(LogModule.Core, "Инициализация игровой сцены завершена");
    }

    #endregion

    #region Find Methods for Scene Components

    private void FindInventoryManager()
    {
        GameObject inventoryPanel = GameObject.FindGameObjectWithTag(inventoryPanelTag);
        if (inventoryPanel != null)
        {
            inventoryManager = inventoryPanel.GetComponent<InventoryUIManager>();
            if (inventoryManager != null)
            {
                Logger.Log(LogModule.UI, "InventoryManager найден");
            }
            else
            {
                Logger.LogWarning(LogModule.UI, "InventoryUIManager не найден на InventoryPanel");
            }
        }
        else
        {
            Logger.LogWarning(LogModule.UI, $"InventoryPanel с тегом '{inventoryPanelTag}' не найден");
        }
    }

    private void FindEquipmentPanel()
    {
        GameObject equipmentPanel = GameObject.FindGameObjectWithTag(equipmentPanelTag);
        if (equipmentPanel != null)
        {
            equipmentCanvasGroup = equipmentPanel.GetComponent<CanvasGroup>();
            if (equipmentCanvasGroup != null)
            {
                equipmentCanvasGroup.alpha = 0f;
                equipmentCanvasGroup.blocksRaycasts = false;
                equipmentCanvasGroup.interactable = false;
                isEquipmentOpen = false;
                Logger.Log(LogModule.UI, "EquipmentPanel найден и скрыт");
            }
            else
            {
                Logger.LogWarning(LogModule.UI, "CanvasGroup не найден на EquipmentPanel");
            }
        }
        else
        {
            Logger.LogWarning(LogModule.UI, $"EquipmentPanel с тегом '{equipmentPanelTag}' не найден");
        }
    }

    private void FindRadialMenu()
    {
        if (radialMenu == null)
        {
            try
            {
                GameObject radialMenuObj = GameObject.FindGameObjectWithTag(radialMenuTag);
                if (radialMenuObj != null)
                {
                    radialMenu = radialMenuObj.GetComponent<RadialMenu>();
                    if (radialMenu != null)
                    {
                        Logger.Log(LogModule.UI, "RadialMenu найден по тегу");
                        return;
                    }
                }
            }
            catch (UnityException)
            {
                Logger.Log(LogModule.UI, $"Тег '{radialMenuTag}' не определен, поиск по имени...");
            }

            radialMenu = FindObjectOfType<RadialMenu>();
            if (radialMenu != null)
            {
                Logger.Log(LogModule.UI, "RadialMenu найден по типу");
            }
            else
            {
                Logger.LogWarning(LogModule.UI, "RadialMenu не найден");
            }
        }
    }

    private void FindTextBeginner()
    {
        if (textBeginner == null)
        {
            textBeginner = FindObjectOfType<TextBeginner>();
            Logger.Log(LogModule.Core, $"TextBeginner найден: {textBeginner != null}");
        }
    }

    private void FindAudioSources()
    {
        audioSources.Clear();

        try
        {
            GameObject[] foundSources = GameObject.FindGameObjectsWithTag("ASO");
            if (foundSources.Length > 0)
            {
                audioSources.AddRange(foundSources);
                Logger.Log(LogModule.Audio, $"Найдено {audioSources.Count} источников звука с тегом 'ASO'");
                return;
            }
        }
        catch (UnityException)
        {
            Logger.Log(LogModule.Audio, "Тег 'ASO' не определен");
        }

        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allAudioSources)
        {
            if (source.gameObject != gameObject)
            {
                audioSources.Add(source.gameObject);
            }
        }
        Logger.Log(LogModule.Audio, $"Найдено {audioSources.Count} источников звука по компоненту");
    }

    private void FindAbilityManager()
    {
        if (abilityManager == null)
        {
            abilityManager = FindObjectOfType<AbilityManager>();
            Logger.Log(LogModule.Core, $"AbilityManager найден: {abilityManager != null}");
        }
    }

    private void FindMagicLightAbility()
    {
        if (abilityManager == null)
        {
            abilityManager = FindObjectOfType<AbilityManager>();
        }

        if (abilityManager != null)
        {
            magicLightAbility = abilityManager.GetAbility<MagicLightAbility>();
            if (magicLightAbility != null)
            {
                Logger.Log(LogModule.Core, "MagicLightAbility найден");

                magicLightAbility.OnStateChanged -= OnMagicLightStateChanged;
                magicLightAbility.OnStateChanged += OnMagicLightStateChanged;

                isMagicLightActive = magicLightAbility.IsActive;
            }
            else
            {
                Logger.LogWarning(LogModule.Core, "MagicLightAbility не найден в AbilityManager");
            }
        }
        else
        {
            Logger.LogWarning(LogModule.Core, "AbilityManager не найден");
        }
    }

    private void OnMagicLightStateChanged(bool isActive)
    {
        isMagicLightActive = isActive;
        Logger.Log(LogModule.Core, $"Состояние магического света изменено: {isActive}");
    }

    private void FindSettingsPanel()
    {
        if (settingsPanel == null)
        {
            settingsPanel = GameObject.Find("SettingsPanel");

            if (settingsPanel == null)
            {
                try
                {
                    settingsPanel = GameObject.FindGameObjectWithTag("SettingsPanel");
                }
                catch (UnityException)
                {
                    Logger.Log(LogModule.UI, "Тег 'SettingsPanel' не определен, поиск по имени");
                }
            }

            if (settingsPanel == null)
            {
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("Settings") || obj.name.Contains("Menu"))
                    {
                        settingsPanel = obj;
                        break;
                    }
                }
            }

            Logger.Log(LogModule.UI, $"SettingsPanel найден: {settingsPanel != null}");
        }
    }

    private void FindVolumeSlider()
    {
        if (volumeSlider == null)
        {
            volumeSlider = FindObjectOfType<Slider>();
            Logger.Log(LogModule.Audio, $"VolumeSlider найден: {volumeSlider != null}");
        }
    }

    private void FindContinueButton()
    {
        if (continueButton == null)
        {
            continueButton = FindObjectOfType<Button>();
            Logger.Log(LogModule.UI, $"ContinueButton найден: {continueButton != null}");
        }
    }

    private void FindCursorDropdown()
    {
        if (cursorDropdown == null)
        {
            cursorDropdown = FindObjectOfType<Dropdown>();
            Logger.Log(LogModule.UI, $"CursorDropdown найден: {cursorDropdown != null}");
        }
    }

    private void FindPostProcessProfile()
    {
        if (postProcessProfile == null)
        {
            Volume volume = FindObjectOfType<Volume>();
            if (volume != null)
            {
                postProcessProfile = volume.profile;
            }
            Logger.Log(LogModule.UI, $"PostProcessProfile найден: {postProcessProfile != null}");
        }
    }

    private void FindTransitionAnimator()
    {
        if (transitionAnimator == null)
        {
            transitionAnimator = FindObjectOfType<Animator>();
            Logger.Log(LogModule.Animation, $"TransitionAnimator найден: {transitionAnimator != null}");
        }
    }

    #endregion

    #region Equipment Panel Control

    public void ToggleEquipment()
    {
        if (equipmentCanvasGroup == null)
        {
            FindEquipmentPanel();
            if (equipmentCanvasGroup == null) return;
        }

        if (isEquipmentOpen)
        {
            CloseEquipment();
        }
        else
        {
            OpenEquipment();
        }
    }

    public void OpenEquipment()
    {
        if (equipmentCanvasGroup == null) return;

        if (equipmentFadeCoroutine != null) StopCoroutine(equipmentFadeCoroutine);
        equipmentFadeCoroutine = StartCoroutine(FadeEquipmentCanvasGroup(1f, equipmentFadeDuration));
    }

    public void CloseEquipment()
    {
        if (equipmentCanvasGroup == null) return;

        if (equipmentFadeCoroutine != null) StopCoroutine(equipmentFadeCoroutine);
        equipmentFadeCoroutine = StartCoroutine(FadeEquipmentCanvasGroup(0f, equipmentFadeDuration));
    }

    private IEnumerator FadeEquipmentCanvasGroup(float targetAlpha, float duration)
    {
        float startAlpha = equipmentCanvasGroup.alpha;
        float elapsed = 0f;

        if (targetAlpha >= 1f)
        {
            equipmentCanvasGroup.blocksRaycasts = true;
            equipmentCanvasGroup.interactable = true;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            equipmentCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        equipmentCanvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            equipmentCanvasGroup.blocksRaycasts = false;
            equipmentCanvasGroup.interactable = false;
        }

        isEquipmentOpen = (targetAlpha >= 1f);
        equipmentFadeCoroutine = null;
    }

    public bool IsEquipmentOpen()
    {
        return isEquipmentOpen;
    }

    #endregion

    #region Quest UI

    public void ToggleQuestUI()
    {
        if (textBeginner != null && (textBeginner.IsInDialogue || textBeginner.IsChoosing))
            return;

        if (isMenuOpen || isEquipmentOpen)
            return;

        if (QuestUI.Instance != null)
        {
            QuestUI.Instance.ToggleQuestPanel();
        }
        else
        {
            Logger.LogWarning(LogModule.UI, "QuestUI.Instance не найден");
        }
    }

    #endregion

    #region Radial Menu Callback

    private void OnRadialButtonClick(RadialMenu.RadialAction action, string buttonName, int buttonIndex)
    {
        Logger.Log(LogModule.UI, $"Нажата кнопка: {buttonName}, действие: {action}");

        switch (action)
        {
            case RadialMenu.RadialAction.Talk:
                if (textBeginner != null && !textBeginner.IsInDialogue)
                {
                    Logger.Log(LogModule.UI, "Начинаем диалог");
                }
                break;

            case RadialMenu.RadialAction.Take:
                Logger.Log(LogModule.UI, "Взять предмет");
                break;

            case RadialMenu.RadialAction.Use:
                Logger.Log(LogModule.UI, "Использовать предмет");
                break;

            case RadialMenu.RadialAction.Examine:
                Logger.Log(LogModule.UI, "Осмотреть");
                break;

            case RadialMenu.RadialAction.UseMagic:
                Logger.Log(LogModule.UI, "Использовать магию");
                UseMagicLight();
                break;

            case RadialMenu.RadialAction.OpenInventory:
                Logger.Log(LogModule.UI, "Открыть инвентарь");
                if (inventoryManager == null)
                {
                    FindInventoryManager();
                }
                if (inventoryManager != null)
                {
                    inventoryManager.ToggleInventory();
                    Logger.Log(LogModule.UI, "Инвентарь переключен");
                }
                else
                {
                    Logger.LogError(LogModule.UI, "InventoryManager все еще null");
                }
                break;

            case RadialMenu.RadialAction.OpenEquipment:
                Logger.Log(LogModule.UI, "Открыть экипировку");
                ToggleEquipment();
                break;

            case RadialMenu.RadialAction.Cancel:
                Logger.Log(LogModule.UI, "Выход в главное меню");
                LoadScene(0);
                break;

            case RadialMenu.RadialAction.Settings:
                Logger.Log(LogModule.UI, "Открыть настройки");
                if (settingsPanel != null)
                {
                    settingsPanel.SetActive(true);
                    isMenuOpen = true;
                }
                break;

            case RadialMenu.RadialAction.Menu:
                Logger.Log(LogModule.UI, "Открыть меню");
                break;

            case RadialMenu.RadialAction.Exit:
                Logger.Log(LogModule.UI, "Выход из игры");
                QuitGame();
                break;

            default:
                Logger.LogWarning(LogModule.UI, $"Неизвестное действие: {action}");
                break;
        }
    }

    #endregion

    #region Magic Light Control

    public void UseMagicLight()
    {
        if (magicLightAbility == null)
        {
            FindMagicLightAbility();
            if (magicLightAbility == null)
            {
                Logger.LogWarning(LogModule.Core, "MagicLightAbility недоступен");
                return;
            }
        }

        magicLightAbility.Activate();
    }

    #endregion

    #region Abilities

    public void UseAbility(string abilityName)
    {
        switch (abilityName)
        {
            case "MagicLight":
                UseMagicLight();
                break;
        }
    }

    #endregion

    #region Cursor Management

    public void ApplyCursor()
    {
        if (cursorDropdown == null)
        {
            FindCursorDropdown();
            if (cursorDropdown == null) return;
        }

        if (cursorDropdown.value == 0)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else
        {
            string cursorName = cursorDropdown.captionText.text;
            cursorTexture = Resources.Load<Texture2D>(CURSOR_RESOURCE_PATH + cursorName);
            Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
        }

        PlayerPrefs.SetInt(CURSOR_PREF_KEY, cursorDropdown.value);
        PlayerPrefs.Save();
        Logger.Log(LogModule.Core, $"Курсор изменен на: {PlayerPrefs.GetInt(CURSOR_PREF_KEY)}");
    }

    #endregion

    #region Display Settings

    public void ToggleFullscreen()
    {
        bool newFullscreenState = !Screen.fullScreen;
        SetFullscreenState(newFullscreenState);
        PlayerPrefs.SetInt(FULLSCREEN_PREF_KEY, newFullscreenState ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void SetFullscreenState(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        if (fullscreenToggleImage != null)
        {
            fullscreenToggleImage.SetActive(isFullscreen);
        }
    }

    #endregion

    #region Audio Settings

    public void UpdateGlobalVolume()
    {
        if (volumeSlider == null)
        {
            FindVolumeSlider();
            if (volumeSlider == null) return;
        }

        _audioVolume = volumeSlider.value / 10f;
        AudioListener.volume = _audioVolume;
        PlayerPrefs.SetFloat(VOLUME_PREF_KEY, _audioVolume);
        PlayerPrefs.Save();
    }

    public void RandomizePitch()
    {
        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(1f, 3f);
        }
    }

    #endregion

    #region Scene Management

    public void StartNewGame()
    {
        StartCoroutine(StartNewGameWithFade());
    }

    private IEnumerator StartNewGameWithFade()
    {
        if (SceneTransition.Instance != null)
        {
            yield return StartCoroutine(SceneTransition.Instance.FadeOut(0.5f));
        }

        LoadScene(1);

        yield return new WaitForSeconds(0.3f);

        if (SceneTransition.Instance != null)
        {
            yield return StartCoroutine(SceneTransition.Instance.FadeIn(0.5f));
        }
    }

    public void ContinueGame()
    {
        PlayerPrefs.SetInt("Continue", 1);
        PlayerPrefs.Save();
        LoadScene(1);
    }

    public void ReturnToMenu()
    {
        LoadScene(0);
    }

    public void LoadScene(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    #endregion

    #region Initialization Helpers

    private void InitializeCursor()
    {
        if (cursorDropdown == null)
        {
            FindCursorDropdown();
            if (cursorDropdown == null) return;
        }

        if (!PlayerPrefs.HasKey(CURSOR_PREF_KEY))
        {
            PlayerPrefs.SetInt(CURSOR_PREF_KEY, 0);
            PlayerPrefs.Save();
            return;
        }

        cursorDropdown.value = PlayerPrefs.GetInt(CURSOR_PREF_KEY);
        ApplyCursor();
    }

    private void InitializeFullscreen()
    {
        if (Application.isEditor) return;

        if (!PlayerPrefs.HasKey(FULLSCREEN_PREF_KEY))
        {
            PlayerPrefs.SetInt(FULLSCREEN_PREF_KEY, 0);
            PlayerPrefs.Save();
            return;
        }

        bool isFullscreen = PlayerPrefs.GetInt(FULLSCREEN_PREF_KEY) == 1;
        SetFullscreenState(isFullscreen);
    }

    private void InitializeVolume()
    {
        if (!PlayerPrefs.HasKey(VOLUME_PREF_KEY))
        {
            PlayerPrefs.SetFloat(VOLUME_PREF_KEY, 0.5f);
            PlayerPrefs.Save();
        }

        AudioListener.volume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY);
        _audioVolume = AudioListener.volume;

        if (volumeSlider == null)
        {
            FindVolumeSlider();
        }

        if (volumeSlider != null)
        {
            volumeSlider.value = _audioVolume * 10;
        }
    }

    private void CheckForSaveFile()
    {
        if (SceneManager.GetActiveScene().buildIndex != 0) return;

        string savePath = Application.dataPath + SAVE_PATH;
        if (continueButton != null)
        {
            continueButton.interactable = File.Exists(savePath);
        }
    }

    #endregion

    #region Object Interaction

    public void SetNextObject(GameObject targetObject)
    {
        nextObject = targetObject;
    }

    public void InteractWithObject(GameObject targetObject)
    {
        currentObject = targetObject;
        Invoke(nameof(CompleteInteraction), 0.15f);
    }

    private void CompleteInteraction()
    {
        if (nextObject != null)
        {
            nextObject.SetActive(true);
        }
        if (currentObject != null)
        {
            currentObject.SetActive(false);
        }
    }

    public void DetectObject(GameObject targetObject)
    {
        if (!isMagicLightActive) return;

        var spriteRenderer = targetObject.GetComponent<SpriteRenderer>();
        var collider = targetObject.GetComponent<BoxCollider2D>();

        if (spriteRenderer != null && !spriteRenderer.enabled)
        {
            if (audioSource != null && detectionClip != null)
            {
                audioSource.PlayOneShot(detectionClip);
            }
            PlayTransitionAnimation();
            spriteRenderer.enabled = true;
            if (collider != null) collider.enabled = true;
        }
    }

    private void PlayTransitionAnimation()
    {
        if (transitionAnimator != null)
        {
            transitionAnimator.Play("Path");
        }
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        if (SceneManager.GetActiveScene().buildIndex == 0) return;

        if (!isSceneInitialized)
        {
            InitializeGameScene();
        }

        if (Input.GetKeyDown(equipmentKey))
        {
            ToggleEquipment();
        }

        if (Input.GetKeyDown(questKey))
        {
            ToggleQuestUI();
        }

        if (ShouldHandleGameplayInput() && Input.GetMouseButtonDown(0))
        {
            if (Camera.main == null) return;

            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, targetLayer);

            if (hit.collider != null)
            {
                Logger.Log(LogModule.Core, $"Клик по: {hit.collider.name}");

                DialogueTrigger trigger = hit.collider.GetComponent<DialogueTrigger>();
                Logger.Log(LogModule.Core, $"DialogueTrigger найден: {trigger != null}");

                if (trigger != null && textBeginner != null)
                {
                    Logger.Log(LogModule.Core, $"Запуск диалога с файлом {trigger.dialogueFileIndex}");
                    textBeginner.StartDialogueWithFile(trigger.dialogueFileIndex);
                }
                else
                {
                    if (trigger == null) Logger.LogWarning(LogModule.Core, "DialogueTrigger отсутствует");
                    if (textBeginner == null) Logger.LogWarning(LogModule.Core, "TextBeginner отсутствует");
                }
            }
        }

        HandleInventoryInput();
        HandleMagicLightInput();
        HandleEscapeKey();
    }

    private bool ShouldHandleGameplayInput()
    {
        bool isInGameScene = SceneManager.GetActiveScene().buildIndex != 0;
        bool notInDialogue = textBeginner == null || (!textBeginner.IsInDialogue && !textBeginner.IsChoosing);
        bool menuClosed = !isMenuOpen;
        bool inventoryClosed = inventoryManager == null || !inventoryManager.IsInventoryOpen;
        bool equipmentClosed = !isEquipmentOpen;
        bool questClosed = QuestUI.Instance == null || !QuestUI.Instance.IsOpen;

        return isInGameScene && notInDialogue && menuClosed && inventoryClosed && equipmentClosed && questClosed;
    }

    #endregion

    #region Input Handlers

    private void HandleInventoryInput()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (inventoryManager == null)
            {
                FindInventoryManager();
            }

            if (inventoryManager != null)
            {
                inventoryManager.ToggleInventory();
            }
        }
    }

    private void HandleMagicLightInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && isMagicLightActive)
        {
            if (magicLightAbility != null && magicLightAbility.IsActive)
            {
                magicLightAbility.Activate();
            }
        }
    }

    #endregion

    #region Menu System

    private void HandleEscapeKey()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (QuestUI.Instance != null && QuestUI.Instance.IsOpen)
        {
            QuestUI.Instance.CloseQuestPanel();
            return;
        }

        if (isEquipmentOpen)
        {
            CloseEquipment();
            return;
        }

        if (inventoryManager != null && inventoryManager.IsInventoryOpen)
        {
            MenuManager.Instance?.CloseInventoryAndMenus();
            return;
        }

        if (isMagicLightActive)
        {
            if (magicLightAbility != null && magicLightAbility.IsActive)
            {
                magicLightAbility.Activate();
            }
            return;
        }

        MenuManager.Instance?.CloseAllMenus();
        ToggleMenu();
    }

    private void ToggleMenu()
    {
        if (settingsPanel == null)
        {
            FindSettingsPanel();
            if (settingsPanel == null) return;
        }

        settingsPanel.SetActive(!settingsPanel.activeSelf);
        UpdateDepthOfField();
    }

    public void CloseMenu()
    {
        UpdateDepthOfFieldSettings(10f);
        isMenuOpen = false;
    }

    private void UpdateDepthOfField()
    {
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            UpdateDepthOfFieldSettings(0.1f);
            isMenuOpen = true;
        }
        else
        {
            UpdateDepthOfFieldSettings(10f);
            isMenuOpen = false;
        }
    }

    private void UpdateDepthOfFieldSettings(float focusDistance)
    {
        if (postProcessProfile == null)
        {
            FindPostProcessProfile();
        }

        if (postProcessProfile != null && postProcessProfile.TryGet(out _depthOfField))
        {
            _depthOfField.focusDistance.value = focusDistance;
        }
    }

    #endregion

    #region Utility Methods

    public void OpenWebsite()
    {
        Application.OpenURL("https://vk.com/walderman");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void SetDialogueCharacter(string characterName)
    {
        if (textBeginner != null)
        {
            textBeginner.SetCurrentCharacter(characterName);
        }
    }

    #endregion
}
