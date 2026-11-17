using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSelectionPanel : MonoBehaviour
{
    private bool isNewGameMode;
    private bool isDeleteMode = false;
    private GameObject panel;
    private Transform listContainer;
    private MenuUIConfig config;
    private TMP_FontAsset font;
    private GameObject bigPanelPrefab;
    private GameObject buttonPrefab;
    private GameObject savePanelPrefab;
    private GameObject inputPrefab;
    private Button deleteModeBtn;
    private ScrollRect scrollRect;
    private readonly List<SaveEntryView> activeEntries = new List<SaveEntryView>();

    private const int ScreenshotWidth = 128;

    private void Awake()
    {
        config = Resources.Load<MenuUIConfig>("Configs/MenuUIConfig");
        if (config != null) font = Resources.Load<TMP_FontAsset>(config.defaultFontPath);
        bigPanelPrefab = Resources.Load<GameObject>("UI/Prefab/Panels/BigPanel");
        buttonPrefab = Resources.Load<GameObject>("UI/Prefab/Buttons/CancelButton");
        savePanelPrefab = Resources.Load<GameObject>("UI/Prefab/Panels/SavePanel");
        inputPrefab = Resources.Load<GameObject>("UI/Prefab/Input");

        string saveRoot = GamePaths.SaveRoot;
        if (!Directory.Exists(saveRoot))
            Directory.CreateDirectory(saveRoot);
    }

    public void Show(bool newGameMode, Transform canvasTransform)
    {
        isNewGameMode = newGameMode;
        isDeleteMode = false;
        if (panel != null) Destroy(panel);
        CreateUI(canvasTransform);
    }

    private void CreateUI(Transform canvasTransform)
    {
        panel = new GameObject("SaveSelectionPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(canvasTransform, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = panel.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);

        var cg = panel.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = true;

        GameObject mainPanel = Instantiate(bigPanelPrefab, panel.transform);
        mainPanel.name = "MainPanel";
        var mainRt = mainPanel.GetComponent<RectTransform>();
        mainRt.anchorMin = new Vector2(0.5f, 0.5f);
        mainRt.anchorMax = new Vector2(0.5f, 0.5f);
        mainRt.sizeDelta = new Vector2(1000, 700);
        mainRt.anchoredPosition = Vector2.zero;

        var mainImg = mainPanel.GetComponent<Image>();
        if (mainImg != null) mainImg.color = Color.white;

        CreateTitle(mainPanel.transform);
        CreateScrollView(mainPanel.transform);

        PopulateList();

        var backBtn = CreatePrefabButton(panel.transform, "Назад", new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(200, 60));
        backBtn.onClick.AddListener(Close);

        deleteModeBtn = CreatePrefabButton(mainPanel.transform, "Удалить профиль", new Vector2(0.2f, 0), new Vector2(0, 70), new Vector2(250, 50));
        deleteModeBtn.onClick.AddListener(ToggleDeleteMode);

        if (isNewGameMode)
        {
            var newBtn = CreatePrefabButton(mainPanel.transform, "Новый профиль", new Vector2(0.8f, 0), new Vector2(0, 70), new Vector2(250, 50));
            newBtn.onClick.AddListener(() => ShowNameInputDialog(profileName => OnProfileAction(profileName, false)));
        }
    }

    private void CreateTitle(Transform parent)
    {
        var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(parent, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -20);
        titleRt.sizeDelta = new Vector2(0, 60);

        var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = isNewGameMode ? "Новая игра" : "Загрузить";
        titleTmp.fontSize = 40;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.black;
        if (font != null) titleTmp.font = font;
    }

    private void CreateScrollView(Transform parent)
    {
        var scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(parent, false);
        var scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.05f, 0.25f);
        scrollRt.anchorMax = new Vector2(0.95f, 0.85f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        scrollRect = scrollObj.GetComponent<ScrollRect>();

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollObj.transform, false);
        var vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;
        var vpImg = viewport.GetComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0.05f);
        var mask = viewport.GetComponent<Mask>();
        mask.showMaskGraphic = true;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.sizeDelta = new Vector2(0, 0);

        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 15;
        layout.padding = new RectOffset(15, 15, 15, 15);
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.UpperCenter;

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = vpRt;
        scrollRect.content = contentRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.onValueChanged.AddListener(OnScrollChanged);

        listContainer = content.transform;
    }

    private void ToggleDeleteMode()
    {
        isDeleteMode = !isDeleteMode;

        var text = deleteModeBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = isDeleteMode ? "Отмена удаления" : "Удалить профиль";
            text.color = isDeleteMode ? new Color(0.8f, 0.2f, 0.2f) : Color.black;
        }

        PopulateList();
    }

    private void PopulateList()
    {
        foreach (Transform child in listContainer)
            Destroy(child.gameObject);

        activeEntries.Clear();

        var profiles = GamePaths.GetAllProfiles(onlyWithSave: false);
        if (profiles.Count == 0)
        {
            CreateEmptyLabel();
            return;
        }

        foreach (var profile in profiles)
            CreateProfileEntry(profile);

        StartCoroutine(LoadVisibleScreenshotsDelayed());
    }

    private void CreateEmptyLabel()
    {
        var emptyObj = new GameObject("Empty", typeof(RectTransform), typeof(TextMeshProUGUI));
        emptyObj.transform.SetParent(listContainer, false);
        var emptyRt = emptyObj.GetComponent<RectTransform>();
        emptyRt.sizeDelta = new Vector2(0, 100);
        var tmp = emptyObj.GetComponent<TextMeshProUGUI>();
        tmp.text = "Нет сохранений";
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.gray;
        if (font != null) tmp.font = font;
    }

    private void CreateProfileEntry(string profileName)
    {
        GameObject entry = Instantiate(savePanelPrefab, listContainer);
        entry.name = $"Entry_{profileName}";
        var entryRt = entry.GetComponent<RectTransform>();
        entryRt.sizeDelta = new Vector2(0, 150);

        var le = entry.GetComponent<LayoutElement>();
        if (le == null) le = entry.AddComponent<LayoutElement>();
        le.minHeight = 150;
        le.preferredHeight = 150;

        var entryImg = entry.GetComponent<Image>();
        if (entryImg != null) entryImg.color = new Color(1f, 1f, 1f, 1f);

        var view = entry.AddComponent<SaveEntryView>();
        view.Setup(profileName, CreateScreenshotPlaceholder(entry.transform));

        var nameObj = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(entry.transform, false);
        var nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.28f, 0.5f);
        nameRt.anchorMax = new Vector2(0.75f, 1);
        nameRt.offsetMin = new Vector2(15, 0);
        nameRt.offsetMax = Vector2.zero;
        var nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
        nameTmp.text = profileName;
        nameTmp.fontSize = 36;
        nameTmp.color = Color.black;
        nameTmp.alignment = TextAlignmentOptions.Left;
        if (font != null) nameTmp.font = font;

        var timeObj = new GameObject("Time", typeof(RectTransform), typeof(TextMeshProUGUI));
        timeObj.transform.SetParent(entry.transform, false);
        var timeRt = timeObj.GetComponent<RectTransform>();
        timeRt.anchorMin = new Vector2(0.28f, 0);
        timeRt.anchorMax = new Vector2(0.75f, 0.5f);
        timeRt.offsetMin = new Vector2(15, 0);
        timeRt.offsetMax = Vector2.zero;
        var timeTmp = timeObj.GetComponent<TextMeshProUGUI>();
        timeTmp.text = view.HasSave ? $"Сохранено: {view.SaveTime}" : "Пусто";
        timeTmp.fontSize = 22;
        timeTmp.color = new Color(0.3f, 0.3f, 0.3f);
        timeTmp.alignment = TextAlignmentOptions.Left;
        if (font != null) timeTmp.font = font;

        string buttonText = "Загрузить";
        if (isDeleteMode) buttonText = "Удалить";
        else if (isNewGameMode) buttonText = view.HasSave ? "Перезаписать" : "Начать";

        var btn = CreatePrefabButton(entry.transform, buttonText, new Vector2(0.88f, 0.5f), Vector2.zero, new Vector2(200, 60));

        if (isDeleteMode)
        {
            var btnTmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnTmp != null) btnTmp.color = new Color(0.8f, 0.2f, 0.2f);

            string capturedProfile = profileName;
            btn.onClick.AddListener(() =>
            {
                string path = Path.Combine(GamePaths.SaveRoot, capturedProfile);
                try
                {
                    if (Directory.Exists(path)) Directory.Delete(path, true);
                    PopulateList();
                    FindAnyObjectByType<MenuUIManager>()?.RefreshMenuButtons();
                }
                catch (System.Exception e) { Logger.LogError(LogModule.Save, $"Ошибка удаления профиля: {e.Message}"); }
            });
        }
        else
        {
            string capturedProfile = profileName;
            bool hasSave = view.HasSave;
            btn.onClick.AddListener(() => OnProfileAction(capturedProfile, hasSave));
        }

        activeEntries.Add(view);
    }

    private Image CreateScreenshotPlaceholder(Transform parent)
    {
        var imgObj = new GameObject("Screenshot", typeof(RectTransform), typeof(Image));
        imgObj.transform.SetParent(parent, false);
        var imgRt = imgObj.GetComponent<RectTransform>();
        imgRt.anchorMin = new Vector2(0.02f, 0.1f);
        imgRt.anchorMax = new Vector2(0.25f, 0.9f);
        imgRt.offsetMin = Vector2.zero;
        imgRt.offsetMax = Vector2.zero;

        var img = imgObj.GetComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.2f, 0.5f);
        img.preserveAspect = true;
        return img;
    }

    private void OnScrollChanged(Vector2 _)
    {
        RefreshVisibleScreenshots();
    }

    private IEnumerator LoadVisibleScreenshotsDelayed()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        RefreshVisibleScreenshots();
    }

    private void RefreshVisibleScreenshots()
    {
        if (scrollRect == null || scrollRect.viewport == null) return;

        Rect viewportRect = scrollRect.viewport.rect;
        Vector2 viewportCenter = scrollRect.viewport.TransformPoint(viewportRect.center);

        foreach (var view in activeEntries)
        {
            if (view == null || view.ScreenshotTarget == null) continue;
            if (view.ScreenshotLoaded) continue;
            if (!view.HasSave) continue;

            RectTransform rt = view.ScreenshotTarget.rectTransform;
            if (rt == null) continue;

            Vector3 worldCenter = rt.TransformPoint(rt.rect.center);
            RectTransform viewportT = scrollRect.viewport;
            Vector3 local = viewportT.InverseTransformPoint(worldCenter);
            Vector2 size = viewportT.rect.size;
            float margin = 200f;

            bool visible = Mathf.Abs(local.x) <= size.x * 0.5f + margin
                        && Mathf.Abs(local.y) <= size.y * 0.5f + margin;

            if (visible) view.LoadScreenshot(ScreenshotWidth);
        }
    }

    private void ShowNameInputDialog(System.Action<string> onConfirm)
    {
        var overlay = new GameObject("NameInputDialog", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        overlay.transform.SetParent(panel.transform, false);
        var overlayRt = overlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        var overlayImg = overlay.GetComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.7f);

        GameObject box = Instantiate(bigPanelPrefab, overlay.transform);
        box.name = "InputBox";
        var boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(600, 350);
        boxRt.anchoredPosition = Vector2.zero;

        var boxImg = box.GetComponent<Image>();
        if (boxImg != null) boxImg.color = Color.white;

        var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(box.transform, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -20);
        titleRt.sizeDelta = new Vector2(0, 40);
        var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "Название профиля";
        titleTmp.fontSize = 32;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.black;
        if (font != null) titleTmp.font = font;

        GameObject inputObj = Instantiate(inputPrefab, box.transform);
        var inputRt = inputObj.GetComponent<RectTransform>();
        inputRt.anchorMin = new Vector2(0.5f, 0.5f);
        inputRt.anchorMax = new Vector2(0.5f, 0.5f);
        inputRt.sizeDelta = new Vector2(500, 60);
        inputRt.anchoredPosition = new Vector2(0, 20);

        var input = inputObj.GetComponent<TMP_InputField>();
        if (input == null) input = inputObj.GetComponentInChildren<TMP_InputField>();

        if (input != null)
        {
            input.text = "";
            input.characterLimit = 20;
            input.onValidateInput = (text, charIndex, addedChar) =>
            {
                if ((addedChar >= 'a' && addedChar <= 'z') || (addedChar >= 'A' && addedChar <= 'Z') ||
                    (addedChar >= '0' && addedChar <= '9') || addedChar == '_')
                    return addedChar;
                return '\0';
            };

            var tmp = input.textComponent as TextMeshProUGUI;
            if (tmp != null && font != null) tmp.font = font;
        }

        var errorObj = new GameObject("Error", typeof(RectTransform), typeof(TextMeshProUGUI));
        errorObj.transform.SetParent(box.transform, false);
        var errorRt = errorObj.GetComponent<RectTransform>();
        errorRt.anchorMin = new Vector2(0.5f, 0.5f);
        errorRt.anchorMax = new Vector2(0.5f, 0.5f);
        errorRt.sizeDelta = new Vector2(500, 40);
        errorRt.anchoredPosition = new Vector2(0, -40);
        var errorTmp = errorObj.GetComponent<TextMeshProUGUI>();
        errorTmp.text = "";
        errorTmp.fontSize = 20;
        errorTmp.color = Color.red;
        errorTmp.alignment = TextAlignmentOptions.Center;
        if (font != null) errorTmp.font = font;

        var btnRow = new GameObject("Buttons", typeof(RectTransform));
        btnRow.transform.SetParent(box.transform, false);
        var btnRowRt = btnRow.GetComponent<RectTransform>();
        btnRowRt.anchorMin = new Vector2(0.5f, 0);
        btnRowRt.anchorMax = new Vector2(0.5f, 0);
        btnRowRt.sizeDelta = new Vector2(400, 60);
        btnRowRt.anchoredPosition = new Vector2(0, 70);

        var okBtn = CreatePrefabButton(btnRow.transform, "ОК", new Vector2(0.3f, 0.5f), Vector2.zero, new Vector2(150, 50));
        var cancelBtn = CreatePrefabButton(btnRow.transform, "Отмена", new Vector2(0.7f, 0.5f), Vector2.zero, new Vector2(150, 50));

        okBtn.onClick.AddListener(() =>
        {
            string name = input != null ? input.text.Trim() : "";
            if (string.IsNullOrEmpty(name)) name = GetDefaultProfileName();

            if (IsValidProfileName(name, out string error))
            {
                Destroy(overlay);
                onConfirm?.Invoke(name);
            }
            else errorTmp.text = error;
        });

        cancelBtn.onClick.AddListener(() => Destroy(overlay));
    }

    private string GetDefaultProfileName()
    {
        string baseName = "Finding_Oneself";
        if (!Directory.Exists(Path.Combine(GamePaths.SaveRoot, baseName))) return baseName;

        int i = 2;
        while (Directory.Exists(Path.Combine(GamePaths.SaveRoot, $"{baseName}_{i}"))) i++;
        return $"{baseName}_{i}";
    }

    private bool IsValidProfileName(string name, out string error)
    {
        if (string.IsNullOrEmpty(name)) { error = "Имя не может быть пустым"; return false; }
        if (name.Length > 20) { error = "Имя не может быть длиннее 20 символов"; return false; }

        foreach (char c in name)
        {
            if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_'))
            {
                error = "Разрешены только английские буквы, цифры и _";
                return false;
            }
        }

        if (Directory.Exists(Path.Combine(GamePaths.SaveRoot, name)))
        {
            error = "Профиль уже существует";
            return false;
        }

        error = null;
        return true;
    }

    private void OnProfileAction(string profileName, bool hasSave)
    {
        GamePaths.CurrentProfile = profileName;
        PlayerPrefs.SetString("LastProfile", profileName);
        PlayerPrefs.Save();

        if (isNewGameMode)
        {
            string profilePath = Path.Combine(GamePaths.SaveRoot, profileName);
            if (Directory.Exists(profilePath))
            {
                try { Directory.Delete(profilePath, true); }
                catch (System.Exception e) { Logger.LogError(LogModule.Save, $"Ошибка удаления профиля '{profileName}': {e.Message}"); }
            }
            Directory.CreateDirectory(profilePath);
            GamePaths.GetSaveFolder("", true);
        }

        Close();

        var menu = FindAnyObjectByType<MenuUIManager>();
        if (menu != null)
        {
            if (isNewGameMode) menu.StartNewGameFromMenu();
            else menu.LoadGameFromMenu();
        }
    }

    private Button CreatePrefabButton(Transform parent, string text, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        GameObject btnObj = Instantiate(buttonPrefab, parent);
        btnObj.name = text + "Button";

        var rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var img = btnObj.GetComponent<Image>();
        if (img != null) img.color = Color.white;

        var btn = btnObj.GetComponent<Button>();
        if (btn == null) btn = btnObj.AddComponent<Button>();

        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        btn.colors = colors;

        var tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;
            if (font != null) tmp.font = font;
        }

        return btn;
    }

    private void Close()
    {
        if (panel != null) Destroy(panel);
        panel = null;
        activeEntries.Clear();
        scrollRect = null;
        FindAnyObjectByType<MenuUIManager>()?.RefreshMenuButtons();
    }
}

public class SaveEntryView : MonoBehaviour
{
    public string ProfileName { get; private set; }
    public bool HasSave { get; private set; }
    public string SaveTime { get; private set; }
    public Image ScreenshotTarget { get; private set; }
    public bool ScreenshotLoaded { get; private set; }

    private string screenshotBase64;
    private bool base64Loaded;

    public void Setup(string profileName, Image screenshotTarget)
    {
        ProfileName = profileName;
        ScreenshotTarget = screenshotTarget;

        string savePath = Path.Combine(GamePaths.SaveRoot, profileName, GamePaths.GameSaveSubFolder, GamePaths.GameSaveFileName);
        if (!File.Exists(savePath)) return;

        try
        {
            string json = File.ReadAllText(savePath);
            var preview = JsonConvert.DeserializeObject<SavePreview>(json);
            if (preview != null)
            {
                HasSave = true;
                SaveTime = preview.saveTime ?? "";
                screenshotBase64 = preview.screenshotBase64;
            }
        }
        catch { }
    }

    public void LoadScreenshot(int targetWidth)
    {
        if (ScreenshotLoaded) return;
        ScreenshotLoaded = true;

        if (!base64Loaded)
        {
            base64Loaded = true;
            if (string.IsNullOrEmpty(screenshotBase64))
            {
                if (ScreenshotTarget != null) ScreenshotTarget.color = new Color(0.2f, 0.2f, 0.25f, 0.4f);
                return;
            }
        }

        if (string.IsNullOrEmpty(screenshotBase64)) return;

        try
        {
            byte[] imgBytes = System.Convert.FromBase64String(screenshotBase64);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imgBytes);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            if (ScreenshotTarget != null)
            {
                ScreenshotTarget.sprite = sprite;
                ScreenshotTarget.preserveAspect = true;
                ScreenshotTarget.color = Color.white;
            }
        }
        catch { }
    }

    [System.Serializable]
    private class SavePreview
    {
        public string saveTime;
        public string screenshotBase64;
    }
}