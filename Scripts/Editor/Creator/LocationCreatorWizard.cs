using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine.Rendering.Universal;

public class LocationCreatorWizard : EditorWindow
{
    private int currentStep = 0;
    private int _lastStep = -1;
    private const int TOTAL_STEPS = 8;

    private const float MOVE_STEP_X = 20f;
    private const float MOVE_STEP_Y = 12f;
    private const float PREVIEW_LIGHT_RADIUS = 25f;
    private const float FULL_WIDTH = 460f;
    private const float COMPACT_WIDTH = 240f;

    private const int LAYER_IGNORE_RAYCAST = 2;
    private const int LAYER_DEFAULT = 0;
    private const int LOCATION_ORDER_IN_LAYER = -100;
    private const string CHARACTER_PREVIEW_RESOURCES_PATH = "Characters/Трикси";
    private const string ITEMS_RESOURCES_PATH = "Items/World";
    private const string REFERENCE_TAG = "LocationReference";
    private const string NEIGHBORS_SCRIPT_NAME = "LocationNeighbors";

    private const float DEFAULT_ITEM_SCALE = 0.5f;

    private bool autoFocusOnMove = true;
    private bool showPreviewLight = true;
    private bool compactMode = false;
    private bool _compactChangeScheduled = false;

    private bool skipLocationReference = false;
    private bool skipLight = false;
    private bool skipItems = false;
    private bool showCharacterPreview = false;

    private bool _refInitialized = false;
    private bool _step3CompactApplied = false;
    private bool _showExistingLocations = false;

    private float _pendingHeight = -1f;
    private bool _resizeScheduled = false;
    private bool _focusSynced = false;

    private GameObject worldObject;
    private List<GameObject> availableRegions = new List<GameObject>();
    private string[] regionNames;
    private int selectedRegionIndex = 0;

    private List<GameObject> existingLocations = new List<GameObject>();
    private HashSet<string> existingLocationNames = new HashSet<string>();

    private Sprite locationSprite;
    private string locationName = "";
    private GameObject createdLocationObj;
    private GameObject _previewLightObj;
    private GameObject _refObj;
    private GameObject _previewCharacterObj;

    private string spriteSearchPath = "Assets/Picture/Background/Game0";
    private string spriteSearchPrefix = "bg_";
    private List<Sprite> foundSprites = new List<Sprite>();
    private Vector2 spriteScrollPos;
    private Vector2 folderScrollPos;
    private Vector2 existingLocScrollPos;
    private bool _didInitialSpriteSearch = false;

    private Vector3 locationRefLocalPos = Vector3.zero;

    private bool isPickingLocationRef = false;
    private bool isPickingLight = false;
    private bool isPickingItem = false;

    [System.Serializable]
    private class LightData
    {
        public Light2D.LightType type = Light2D.LightType.Point;
        public Color color = Color.white;
        public float intensity = 1f;
        public float radius = 5f;
        public Vector3 localPos = Vector3.zero;
    }

    private readonly List<LightData> _lights = new List<LightData>();
    private readonly List<GameObject> _lightPreviewObjects = new List<GameObject>();
    private bool _isEditingLight = false;
    private int _editingLightIndex = -1;
    private GameObject _tempRealLightObj;
    private bool _lightPreviewOn = true;

    private Color lightColor = Color.white;
    private float lightIntensity = 1f;
    private Light2D.LightType lightType = Light2D.LightType.Point;
    private float lightRadius = 5f;
    private Vector3 lightLocalPos = Vector3.zero;

    [System.Serializable]
    private class ItemData
    {
        public GameObject prefab;
        public Vector3 localPos = Vector3.zero;
        public Vector3 localScale = new Vector3(DEFAULT_ITEM_SCALE, DEFAULT_ITEM_SCALE, 1f);
    }

    private readonly List<ItemData> _items = new List<ItemData>();
    private readonly List<GameObject> _itemPreviewObjects = new List<GameObject>();
    private List<GameObject> _availableItemPrefabs = new List<GameObject>();
    private bool _didInitialItemSearch = false;
    private bool _isEditingItem = false;
    private bool _isPickingItemPrefab = false;
    private int _editingItemIndex = -1;
    private GameObject _pendingItemPrefab;
    private GameObject _itemEditPreviewObj;
    private Vector3 _itemLocalPos = Vector3.zero;
    private Vector3 _itemLocalScale = new Vector3(DEFAULT_ITEM_SCALE, DEFAULT_ITEM_SCALE, 1f);
    private Vector2 _itemPickerScrollPos;

    private GUIStyle _bannerBox, _bannerTitle, _bannerSub, _cardBox, _lightRowBox, _itemRowBox, _sectionTitle,
                     _richLabel, _richLabelSmall, _stepDot, _bigButton, _iconButton,
                     _arrowButton, _toggleButton, _miniArrow, _scaleBtn, _wideScaleBtn,
                     _compactNavBtn, _compactStepDot, _locationRowBtn;
    private Texture2D _bannerTex, _cardTex, _lightRowTex, _itemRowTex;

    private static readonly Color ColCard = new Color(0.20f, 0.22f, 0.26f);
    private static readonly Color ColRow = new Color(0.16f, 0.18f, 0.22f);
    private static readonly Color ColItemRow = new Color(0.18f, 0.20f, 0.24f);
    private static readonly Color ColOk = new Color(0.35f, 0.85f, 0.45f);
    private static readonly Color ColWarn = new Color(1.00f, 0.78f, 0.30f);
    private static readonly Color ColErr = new Color(1.00f, 0.42f, 0.42f);
    private static readonly Color ColAccent = new Color(0.45f, 0.70f, 1.00f);
    private static readonly Color ColDim = new Color(0.65f, 0.68f, 0.72f);
    private static readonly Color ColUsed = new Color(0.85f, 0.45f, 0.45f);
    private static readonly Color ColOff = new Color(0.35f, 0.35f, 0.38f);

    [MenuItem("Tools/Меню создания локаций")]
    public static void ShowWindow()
    {
        var win = GetWindow<LocationCreatorWizard>("Создание локации");
        win.minSize = new Vector2(COMPACT_WIDTH, 200);
        win.position = new Rect(win.position.x, win.position.y, FULL_WIDTH, 400);
        win.Focus();
    }

    private void OnEnable()
    {
        FindWorldAndRegions();
        FindExistingLocations();

        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.focusChanged += OnEditorFocusChanged;
        EditorApplication.update += OnEditorUpdate;

        wantsMouseMove = true;

        if (!_didInitialSpriteSearch)
        {
            _didInitialSpriteSearch = true;
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                if (AssetDatabase.IsValidFolder(spriteSearchPath)) SearchSprites();
                Repaint();
            };
        }

        if (!_didInitialItemSearch)
        {
            _didInitialItemSearch = true;
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                SearchItems();
                Repaint();
            };
        }
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorApplication.focusChanged -= OnEditorFocusChanged;
        EditorApplication.update -= OnEditorUpdate;

        isPickingLocationRef = false;
        isPickingLight = false;
        isPickingItem = false;
        HideCharacterPreview();
        DestroyAllLightPreviews();
        DestroyAllItemPreviews();
        DestroyItemEditPreview();
        DestroyPreviewLight();

        if (_bannerTex != null) DestroyImmediate(_bannerTex);
        if (_cardTex != null) DestroyImmediate(_cardTex);
        if (_lightRowTex != null) DestroyImmediate(_lightRowTex);
        if (_itemRowTex != null) DestroyImmediate(_itemRowTex);
        if (_cachedInstance == this) _cachedInstance = null;
    }

    private void OnEditorFocusChanged(bool hasFocus)
    {
        if (hasFocus)
        {
            EditorApplication.delayCall += ResetStuckInputState;
            EditorApplication.delayCall += ResetStuckInputState;
        }
        Repaint();
    }

    private void OnEditorUpdate()
    {
        if (EditorWindow.focusedWindow == this && !_focusSynced)
        {
            _focusSynced = true;
            ResetStuckInputState();
        }
        else if (EditorWindow.focusedWindow != this)
        {
            _focusSynced = false;
        }
    }

    private void ResetStuckInputState()
    {
        GUIUtility.hotControl = 0;
        GUIUtility.keyboardControl = 0;
        EditorGUIUtility.editingTextField = false;

        Repaint();
        SceneView.RepaintAll();
        EditorApplication.QueuePlayerLoopUpdate();

        try { UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); }
        catch { }

        if (EditorWindow.focusedWindow == this) Focus();
    }

    private void OnFocus() { Repaint(); }
    private void OnLostFocus() { Repaint(); }

    private void RequestFocusBack()
    {
        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            try
            {
                Focus();
                _focusSynced = false;
                ResetStuckInputState();
            }
            catch { }
        };
    }

    private static LocationCreatorWizard _cachedInstance;

    private static LocationCreatorWizard Instance
    {
        get
        {
            if (_cachedInstance != null) return _cachedInstance;
            var arr = Resources.FindObjectsOfTypeAll<LocationCreatorWizard>();
            if (arr != null && arr.Length > 0) _cachedInstance = arr[0];
            return _cachedInstance;
        }
    }

    private static bool IsFocusedOnWizardOrScene()
    {
        var fw = EditorWindow.focusedWindow;
        if (fw == null) return true;
        if (fw is LocationCreatorWizard) return true;
        if (fw is SceneView) return true;

        var w = Instance;
        if (w != null && w.createdLocationObj != null &&
            Selection.activeGameObject == w.createdLocationObj)
            return true;

        return false;
    }

    private static void SC_Next()
    {
        var w = Instance;
        if (w == null) return;
        if (!IsFocusedOnWizardOrScene()) return;
        if (w.isPickingLocationRef || w.isPickingLight || w.isPickingItem) return;
        if (w.currentStep < 0 || w.currentStep > 7) return;

        if (w.TryAdvance())
        {
            w.Repaint();
            SceneView.RepaintAll();
        }
    }

    [Shortcut("Location Wizard/Next Step (Numpad)", KeyCode.KeypadEnter, ShortcutModifiers.None)]
    private static void SC_NextNumpad() { SC_Next(); }

    [Shortcut("Location Wizard/Arrow Up", KeyCode.UpArrow, ShortcutModifiers.None)]
    private static void SC_Up() { RunArrow(0, 1); }

    [Shortcut("Location Wizard/Arrow Down", KeyCode.DownArrow, ShortcutModifiers.None)]
    private static void SC_Down() { RunArrow(0, -1); }

    [Shortcut("Location Wizard/Arrow Left", KeyCode.LeftArrow, ShortcutModifiers.None)]
    private static void SC_Left() { RunArrow(-1, 0); }

    [Shortcut("Location Wizard/Arrow Right", KeyCode.RightArrow, ShortcutModifiers.None)]
    private static void SC_Right() { RunArrow(1, 0); }

    [Shortcut("Location Wizard/Shift Arrow Up", KeyCode.UpArrow, ShortcutModifiers.Shift)]
    private static void SC_ShiftUp() { RunArrowShift(0, 1); }

    [Shortcut("Location Wizard/Shift Arrow Down", KeyCode.DownArrow, ShortcutModifiers.Shift)]
    private static void SC_ShiftDown() { RunArrowShift(0, -1); }

    [Shortcut("Location Wizard/Shift Arrow Left", KeyCode.LeftArrow, ShortcutModifiers.Shift)]
    private static void SC_ShiftLeft() { RunArrowShift(-1, 0); }

    [Shortcut("Location Wizard/Shift Arrow Right", KeyCode.RightArrow, ShortcutModifiers.Shift)]
    private static void SC_ShiftRight() { RunArrowShift(1, 0); }

    private static void RunArrow(float dirX, float dirY) { RunArrowInternal(dirX, dirY, 1f); }
    private static void RunArrowShift(float dirX, float dirY) { RunArrowInternal(dirX, dirY, 5f); }

    private static void RunArrowInternal(float dirX, float dirY, float mult)
    {
        var w = Instance;
        if (w == null) return;
        if (!IsFocusedOnWizardOrScene()) return;

        if (w.currentStep == 3 && w.createdLocationObj != null)
        {
            float dx = dirX * MOVE_STEP_X * mult;
            float dy = dirY * MOVE_STEP_Y * mult;
            w.MoveLocation(dx, dy);
            w.Repaint();
            SceneView.RepaintAll();
            return;
        }

        if (w.currentStep == 4 && w.showCharacterPreview && w._refObj != null)
        {
            if (dirX < 0) { w.AdjustRefScale(-0.1f * mult); w.Repaint(); SceneView.RepaintAll(); return; }
            if (dirX > 0) { w.AdjustRefScale(0.1f * mult); w.Repaint(); SceneView.RepaintAll(); return; }
            if (dirY > 0) { w.FlipReferenceScaleX(); w.Repaint(); SceneView.RepaintAll(); return; }
        }

        if (w.currentStep == 5 && w.isPickingLight && w._isEditingLight)
        {
            float dx = dirX * MOVE_STEP_X * mult;
            float dy = dirY * MOVE_STEP_Y * mult;
            w.lightLocalPos += new Vector3(dx, dy, 0);
            w.SyncEditPreview();
            w.Repaint();
            SceneView.RepaintAll();
            return;
        }

        if (w.currentStep == 6 && w._isEditingItem && w._itemEditPreviewObj != null)
        {
            if (dirX < 0) { w.AdjustItemScale(-0.1f * mult); w.Repaint(); SceneView.RepaintAll(); return; }
            if (dirX > 0) { w.AdjustItemScale(0.1f * mult); w.Repaint(); SceneView.RepaintAll(); return; }
            if (dirY > 0) { w.FlipItemScaleX(); w.Repaint(); SceneView.RepaintAll(); return; }
        }
    }

    private void UpdateLightVisibility()
    {
        for (int i = 0; i < _lightPreviewObjects.Count; i++)
        {
            var go = _lightPreviewObjects[i];
            if (go == null) continue;

            if (_isEditingLight && i == _editingLightIndex)
            {
                go.SetActive(false);
                continue;
            }
            go.SetActive(_lightPreviewOn);
        }

        if (_tempRealLightObj != null)
            _tempRealLightObj.SetActive(_lightPreviewOn || _isEditingLight);

        if (_previewLightObj != null)
        {
            bool onStep5 = currentStep == 5;
            bool auxOn;
            if (onStep5)
                auxOn = isPickingLight || !_lightPreviewOn;
            else
                auxOn = showPreviewLight;

            _previewLightObj.SetActive(auxOn);
        }
    }

    private void RefreshLightPreviews()
    {
        foreach (var go in _lightPreviewObjects) if (go != null) DestroyImmediate(go);
        _lightPreviewObjects.Clear();

        if (createdLocationObj == null) return;

        foreach (var ld in _lights)
        {
            var go = new GameObject("__PreviewLightItem");
            go.layer = LAYER_IGNORE_RAYCAST;
            go.transform.SetParent(createdLocationObj.transform);
            go.transform.localPosition = SnapXY(ld.localPos);
            go.hideFlags = HideFlags.DontSave;

            var l = go.AddComponent<Light2D>();
            l.lightType = ld.type;
            l.color = ld.color;
            l.intensity = ld.intensity;
            if (ld.type == Light2D.LightType.Point)
            {
                l.pointLightInnerRadius = ld.radius * 0.5f;
                l.pointLightOuterRadius = ld.radius;
            }
            _lightPreviewObjects.Add(go);
        }
        UpdateLightVisibility();
    }

    private void DestroyAllLightPreviews()
    {
        foreach (var go in _lightPreviewObjects) if (go != null) DestroyImmediate(go);
        _lightPreviewObjects.Clear();

        if (_tempRealLightObj != null) { DestroyImmediate(_tempRealLightObj); _tempRealLightObj = null; }

        if (createdLocationObj != null)
        {
            var t = createdLocationObj.transform.Find("__PreviewRealLight");
            if (t != null) DestroyImmediate(t.gameObject);
        }
        UpdateLightVisibility();
    }

    private void UpdateItemVisibility()
    {
        for (int i = 0; i < _itemPreviewObjects.Count; i++)
        {
            var go = _itemPreviewObjects[i];
            if (go == null) continue;
            if (_isEditingItem && i == _editingItemIndex)
            {
                go.SetActive(false);
                continue;
            }
            go.SetActive(true);
        }
    }

    private void RefreshItemPreviews()
    {
        foreach (var go in _itemPreviewObjects) if (go != null) DestroyImmediate(go);
        _itemPreviewObjects.Clear();

        if (createdLocationObj == null) return;

        foreach (var item in _items)
        {
            if (item.prefab == null) continue;
            var go = Instantiate(item.prefab);
            go.name = "__PreviewItem_" + item.prefab.name;
            go.transform.SetParent(createdLocationObj.transform);
            go.transform.localPosition = SnapXY(item.localPos);
            go.transform.localScale = item.localScale;
            go.hideFlags = HideFlags.DontSave;
            SetLayerRecursively(go, LAYER_DEFAULT);
            _itemPreviewObjects.Add(go);
        }
        UpdateItemVisibility();
    }

    private void DestroyAllItemPreviews()
    {
        foreach (var go in _itemPreviewObjects) if (go != null) DestroyImmediate(go);
        _itemPreviewObjects.Clear();

        if (createdLocationObj != null)
        {
            var all = new List<Transform>();
            foreach (Transform c in createdLocationObj.transform)
                if (c.name.StartsWith("__PreviewItem_")) all.Add(c);
            foreach (var t in all) if (t != null) DestroyImmediate(t.gameObject);
        }
        UpdateItemVisibility();
    }

    private void DestroyItemEditPreview()
    {
        if (_itemEditPreviewObj != null) { DestroyImmediate(_itemEditPreviewObj); _itemEditPreviewObj = null; }
        if (createdLocationObj != null)
        {
            var t = createdLocationObj.transform.Find("__PreviewItemEdit");
            if (t != null) DestroyImmediate(t.gameObject);
        }
    }

    private bool HandleEnterKeyDirect()
    {
        var e = Event.current;
        if (e == null || e.type != EventType.KeyDown) return false;
        if (e.keyCode != KeyCode.Return && e.keyCode != KeyCode.KeypadEnter) return false;
        if (isPickingLocationRef || isPickingLight || isPickingItem) return false;

        if (TryAdvance())
        {
            e.Use();
            Repaint();
            SceneView.RepaintAll();
            return true;
        }
        return false;
    }

    private void InitStyles()
    {
        if (_bannerBox != null) return;
        if (Event.current == null) return;

        _bannerTex = MakeTex(2, 2, new Color(0.16f, 0.28f, 0.48f));
        _cardTex = MakeTex(2, 2, ColCard);
        _lightRowTex = MakeTex(2, 2, ColRow);
        _itemRowTex = MakeTex(2, 2, ColItemRow);

        _bannerBox = new GUIStyle(EditorStyles.helpBox)
        {
            normal = { background = _bannerTex },
            padding = new RectOffset(12, 12, 10, 10),
            margin = new RectOffset(6, 6, 6, 6)
        };
        _bannerTitle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 17,
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            normal = { textColor = Color.white }
        };
        _bannerSub = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            normal = { textColor = new Color(0.85f, 0.90f, 1f) }
        };
        _cardBox = new GUIStyle(EditorStyles.helpBox)
        {
            normal = { background = _cardTex },
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(6, 6, 4, 4)
        };

        _lightRowBox = new GUIStyle(EditorStyles.helpBox)
        {
            normal = { background = _lightRowTex },
            padding = new RectOffset(8, 8, 6, 6),
            margin = new RectOffset(0, 0, 2, 2),
            border = new RectOffset(1, 1, 1, 1)
        };

        _itemRowBox = new GUIStyle(EditorStyles.helpBox)
        {
            normal = { background = _itemRowTex },
            padding = new RectOffset(8, 8, 6, 6),
            margin = new RectOffset(0, 0, 2, 2),
            border = new RectOffset(1, 1, 1, 1)
        };

        _sectionTitle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            richText = true,
            normal = { textColor = Color.white },
            margin = new RectOffset(0, 0, 2, 4)
        };
        _richLabel = new GUIStyle(EditorStyles.label) { richText = true, fontSize = 12 };
        _richLabelSmall = new GUIStyle(EditorStyles.miniLabel) { richText = true };
        _stepDot = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
        _bigButton = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold, fixedHeight = 34, richText = true };
        _iconButton = new GUIStyle(GUI.skin.button) { fontSize = 12, fixedHeight = 24, richText = true };
        _arrowButton = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold, fixedWidth = 55, fixedHeight = 38, richText = true };
        _toggleButton = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold, richText = true, fixedHeight = 28 };
        _miniArrow = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, fixedWidth = 32, fixedHeight = 24 };

        _scaleBtn = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            richText = true,
            fixedHeight = 30,
            fixedWidth = 110
        };

        _wideScaleBtn = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            richText = true,
            fixedHeight = 30
        };

        _compactNavBtn = new GUIStyle(GUI.skin.button)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            richText = true,
            fixedHeight = 26
        };
        _compactStepDot = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11
        };

        _locationRowBtn = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            richText = true,
            fixedHeight = 24,
            padding = new RectOffset(8, 8, 2, 2)
        };
    }

    private Texture2D MakeTex(int w, int h, Color col)
    {
        var pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        var t = new Texture2D(w, h); t.SetPixels(pix); t.Apply();
        return t;
    }

    private string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
    private string Colored(string t, Color c) => $"<color={Hex(c)}>{t}</color>";
    private string Bold(string t) => $"<b>{t}</b>";

    private static float Round1(float v) => Mathf.Round(v * 10f) / 10f;
    private static Vector3 SnapXY(Vector3 v) => new Vector3(Round1(v.x), Round1(v.y), 0f);

    private bool DrawToggleButton(string label, bool value, Color onColor)
    {
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = value ? onColor : ColOff;
        bool clicked = GUILayout.Button(label, _toggleButton);
        GUI.backgroundColor = prev;
        return clicked ? !value : value;
    }

    private void ScheduleHeightFit()
    {
        if (Event.current == null) return;
        if (Event.current.type != EventType.Repaint) return;

        var last = GUILayoutUtility.GetLastRect();
        float targetH = Mathf.Max(200f, last.yMax + 10f);
        float maxH = Screen.currentResolution.height * 0.9f;
        targetH = Mathf.Min(targetH, maxH);

        if (Mathf.Abs(position.height - targetH) < 3f) return;
        if (Mathf.Abs(_pendingHeight - targetH) < 1f) return;

        _pendingHeight = targetH;
        if (_resizeScheduled) return;
        _resizeScheduled = true;

        EditorApplication.delayCall += () =>
        {
            _resizeScheduled = false;
            if (_pendingHeight <= 0) return;
            if (Mathf.Abs(position.height - _pendingHeight) < 2f) return;
            float w = compactMode ? COMPACT_WIDTH : FULL_WIDTH;
            position = new Rect(position.x, position.y, w, _pendingHeight);
            Repaint();
        };
    }

    private void RequestCompactMode(bool on)
    {
        if (_compactChangeScheduled) return;
        if (on == compactMode) return;
        _compactChangeScheduled = true;
        EditorApplication.delayCall += () =>
        {
            _compactChangeScheduled = false;
            compactMode = on;
            float w = on ? COMPACT_WIDTH : FULL_WIDTH;
            position = new Rect(position.x, position.y, w, position.height);
            Repaint();
        };
    }

    private void OnGUI()
    {
        HandleEnterKeyDirect();
        InitStyles();

        if (_lastStep != currentStep)
        {
            OnStepEntered(currentStep);
            _lastStep = currentStep;
        }

        if (compactMode && currentStep != 3)
        {
            RequestCompactMode(false);
            compactMode = false;
        }

        if (compactMode)
        {
            DrawCompactUI();
            ScheduleHeightFit();
            return;
        }

        DrawBanner();
        DrawStepProgress();
        GUILayout.Space(2);

        switch (currentStep)
        {
            case 0: DrawStep_Init(); break;
            case 1: DrawStep_SelectRegion(); break;
            case 2: DrawStep_SelectSpriteAndName(); break;
            case 3: DrawStep_PositionLocation(); break;
            case 4: DrawStep_PickReference(); break;
            case 5: DrawStep_ConfigureLight(); break;
            case 6: DrawStep_Items(); break;
            case 7: DrawStep_Finalize(); break;
        }

        ScheduleHeightFit();
    }

    private bool TryAdvance()
    {
        if (isPickingLocationRef || isPickingLight || isPickingItem) return false;
        if (_isEditingLight && currentStep == 5) return false;
        if (_isEditingItem && currentStep == 6) return false;

        switch (currentStep)
        {
            case 0:
                if (worldObject == null || availableRegions.Count == 0) return false;
                currentStep++;
                return true;
            case 1:
                if (createdLocationObj != null) CleanupEverything();
                currentStep++;
                return true;
            case 2:
                if (locationSprite == null || string.IsNullOrEmpty(locationName)) return false;
                CreateOrUpdateBaseLocation();
                currentStep++;
                return true;
            case 3:
                if (compactMode) { compactMode = false; RequestCompactMode(false); }
                currentStep++;
                return true;
            case 4:
                isPickingLocationRef = false;
                HideCharacterPreview();
                showCharacterPreview = false;
                currentStep++;
                return true;
            case 5:
                if (_isEditingLight) return false;
                isPickingLight = false;
                DestroyAllLightPreviews();
                currentStep++;
                return true;
            case 6:
                if (_isEditingItem) return false;
                isPickingItem = false;
                _isPickingItemPrefab = false;
                DestroyItemEditPreview();
                DestroyAllItemPreviews();
                currentStep++;
                return true;
            case 7:
                FinalizeCreation();
                currentStep = 0;
                return true;
        }
        return false;
    }

    private bool TryGoBack()
    {
        if (isPickingLocationRef || isPickingLight || isPickingItem) return false;
        if (_isEditingLight || _isEditingItem) return false;
        if (currentStep <= 0) return false;

        switch (currentStep)
        {
            case 1:
                if (createdLocationObj != null) CleanupEverything();
                currentStep--;
                return true;
            case 2:
                currentStep--;
                return true;
            case 3:
                if (compactMode) { compactMode = false; RequestCompactMode(false); }
                currentStep--;
                return true;
            case 4:
                isPickingLocationRef = false;
                HideCharacterPreview();
                showCharacterPreview = false;
                currentStep--;
                return true;
            case 5:
                isPickingLight = false;
                DestroyAllLightPreviews();
                currentStep--;
                return true;
            case 6:
                isPickingItem = false;
                _isPickingItemPrefab = false;
                DestroyItemEditPreview();
                DestroyAllItemPreviews();
                currentStep--;
                return true;
            case 7:
                currentStep--;
                return true;
        }
        return false;
    }

    private void OnStepEntered(int step)
    {
        if (step == 3 && !_step3CompactApplied)
        {
            _step3CompactApplied = true;
            RequestCompactMode(true);
        }
        if (step != 3) _step3CompactApplied = false;

        if (step == 4 && !skipLocationReference && createdLocationObj != null)
        {
            EnsureReferenceObject();
            if (_refObj != null && !_refInitialized)
            {
                _refInitialized = true;
                showCharacterPreview = true;
                ShowCharacterPreview();
            }
        }

        if (step != 4 && showCharacterPreview)
        {
            showCharacterPreview = false;
            HideCharacterPreview();
        }

        if (step == 5)
        {
            _isEditingLight = false;
            _editingLightIndex = -1;
            _lightPreviewOn = true;
            showPreviewLight = true;

            EnsurePreviewLight();
            RefreshLightPreviews();
            UpdateLightVisibility();

            EditorApplication.delayCall += () =>
            {
                if (currentStep != 5) return;
                EnsurePreviewLight();
                RefreshLightPreviews();
                UpdateLightVisibility();
                SceneView.RepaintAll();
                Repaint();
            };
        }
        else
        {
            isPickingLight = false;
            _isEditingLight = false;
            DestroyAllLightPreviews();
        }

        if (step == 6)
        {
            _isEditingItem = false;
            _editingItemIndex = -1;
            _isPickingItemPrefab = false;
            _pendingItemPrefab = null;

            RefreshItemPreviews();
            UpdateItemVisibility();

            EditorApplication.delayCall += () =>
            {
                if (currentStep != 6) return;
                RefreshItemPreviews();
                UpdateItemVisibility();
                SceneView.RepaintAll();
                Repaint();
            };
        }
        else
        {
            isPickingItem = false;
            _isEditingItem = false;
            _isPickingItemPrefab = false;
            DestroyItemEditPreview();
            DestroyAllItemPreviews();
        }

        EnsurePreviewLight();
        UpdateLightVisibility();
    }

    private void DrawCompactUI()
    {
        EditorGUILayout.BeginVertical(_cardBox);
        DrawCompactStepDots();
        EditorGUILayout.LabelField("🎯  Позиция локации", _sectionTitle);
        EditorGUILayout.EndVertical();

        if (createdLocationObj == null)
        {
            EditorGUILayout.HelpBox("Объект локации не создан.", MessageType.Error);
        }
        else
        {
            EditorGUILayout.BeginVertical(_cardBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = ColAccent;
            if (GUILayout.Button("↑", _arrowButton)) MoveLocation(0, MOVE_STEP_Y);
            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = ColAccent;
            if (GUILayout.Button("←", _arrowButton)) MoveLocation(-MOVE_STEP_X, 0);
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("⟳", _arrowButton, GUILayout.Width(50)))
            {
                Undo.RecordObject(createdLocationObj.transform, "Reset Position");
                createdLocationObj.transform.localPosition = new Vector3(0, 0, 0);
                Selection.activeGameObject = createdLocationObj;
                if (autoFocusOnMove) SmoothFocusOnLocation();
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = ColAccent;
            if (GUILayout.Button("→", _arrowButton)) MoveLocation(MOVE_STEP_X, 0);
            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = ColAccent;
            if (GUILayout.Button("↓", _arrowButton)) MoveLocation(0, -MOVE_STEP_Y);
            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                Colored("World: ", ColDim) + Colored(createdLocationObj.transform.position.ToString("F1"), ColAccent),
                _richLabelSmall);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(_cardBox);
            autoFocusOnMove = DrawToggleButton(autoFocusOnMove ? "🎥 Фокус: ВКЛ" : "🎥 Фокус: ВЫКЛ", autoFocusOnMove, ColOk);
            bool nl = DrawToggleButton(showPreviewLight ? "💡 Свет: ВКЛ" : "💡 Свет: ВЫКЛ", showPreviewLight, ColWarn);
            if (nl != showPreviewLight)
            {
                showPreviewLight = nl;
                if (showPreviewLight) EnsurePreviewLight(); else DestroyPreviewLight();
                UpdateLightVisibility();
            }
            EditorGUILayout.EndVertical();
        }

        GUILayout.Space(2);
        GUILayout.BeginHorizontal();
        GUI.enabled = !isPickingLocationRef && !isPickingLight && !isPickingItem;
        GUI.backgroundColor = ColDim;
        if (GUILayout.Button("←  Назад", _compactNavBtn)) TryGoBack();
        GUI.backgroundColor = Color.white;
        GUI.enabled = !isPickingLocationRef && !isPickingLight && !isPickingItem;
        GUI.backgroundColor = ColOk;
        if (GUILayout.Button("Далее  ➜", _compactNavBtn)) TryAdvance();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.Space(2);
        if (GUILayout.Button("📐  Развернуть", _iconButton))
            RequestCompactMode(false);
    }

    private void DrawCompactStepDots()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        for (int i = 0; i < TOTAL_STEPS; i++)
        {
            string symbol = i < currentStep ? "●" : (i == currentStep ? "◉" : "○");
            Color c = i < currentStep ? ColOk : (i == currentStep ? ColWarn : ColDim);
            _compactStepDot.normal.textColor = c;
            GUILayout.Label(symbol, _compactStepDot, GUILayout.Width(12));
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawBanner()
    {
        EditorGUILayout.BeginVertical(_bannerBox);
        EditorGUILayout.LabelField("🎨  Мастер создания локаций", _bannerTitle);
        EditorGUILayout.LabelField("Пошаговое создание локации", _bannerSub);
        EditorGUILayout.EndVertical();
    }

    private void DrawStepProgress()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        for (int i = 0; i < TOTAL_STEPS; i++)
        {
            string symbol = i < currentStep ? "●" : (i == currentStep ? "◉" : "○");
            Color c = i < currentStep ? ColOk : (i == currentStep ? ColWarn : ColDim);
            _stepDot.normal.textColor = c;
            GUILayout.Label(symbol, _stepDot, GUILayout.Width(18));
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        var stepLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { richText = true };
        EditorGUILayout.LabelField($"Шаг {currentStep + 1} из {TOTAL_STEPS}", stepLabelStyle);
    }

    private void DrawStep_Init()
    {
        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("🔍  Поиск объектов", _sectionTitle);
        EditorGUILayout.LabelField(
            Colored("Ищем ", ColDim) + Colored("World", ColAccent) +
            Colored(", ", ColDim) + Colored("Region", ColAccent) +
            Colored(" и ", ColDim) + Colored("Location", ColAccent) +
            Colored(" (включая неактивные).", ColDim), _richLabel);
        EditorGUILayout.EndVertical();

        if (GUILayout.Button("🔄  Обновить поиск", _iconButton))
        {
            FindWorldAndRegions();
            FindExistingLocations();
        }

        EditorGUILayout.BeginVertical(_cardBox);
        if (worldObject == null)
        {
            EditorGUILayout.LabelField(Colored("✗", ColErr) + "  " + Bold("World не найден"), _richLabel);
            EditorGUILayout.LabelField(Colored("Объект должен быть на сцене с тегом 'World' или именем 'World'.", ColDim), _richLabelSmall);
        }
        else
        {
            EditorGUILayout.LabelField(Colored("✓", ColOk) + "  " + Bold("World найден"), _richLabel);
            EditorGUILayout.LabelField(Colored("   имя: ", ColDim) + Colored(worldObject.name, ColAccent), _richLabelSmall);
        }

        if (availableRegions.Count == 0)
            EditorGUILayout.LabelField(Colored("⚠", ColWarn) + "  " + Bold("Регионы не найдены"), _richLabel);
        else
        {
            EditorGUILayout.LabelField(Colored("✓", ColOk) + "  " + Bold("Регионы найдены"), _richLabel);
            EditorGUILayout.LabelField(Colored("   количество: ", ColDim) + Colored(availableRegions.Count.ToString(), ColAccent), _richLabelSmall);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(_cardBox);
        string foldoutTitle = existingLocations.Count > 0
            ? $"📁  Существующие локации: {existingLocations.Count}"
            : "📁  Существующие локации: 0";
        _showExistingLocations = EditorGUILayout.Foldout(_showExistingLocations, foldoutTitle, true);

        if (_showExistingLocations)
        {
            if (existingLocations.Count == 0)
            {
                EditorGUILayout.LabelField(Colored("Список пуст.", ColDim), _richLabelSmall);
            }
            else
            {
                existingLocScrollPos = EditorGUILayout.BeginScrollView(existingLocScrollPos, GUILayout.Height(140));
                foreach (var loc in existingLocations)
                {
                    if (loc == null) continue;

                    string label =
                        $"<color={Hex(ColOk)}>👁  {loc.name}</color>" +
                        $"  <color={Hex(ColDim)}>(в {loc.transform.parent?.name ?? "—"})</color>";

                    if (GUILayout.Button(label, _locationRowBtn))
                        FocusOnExistingLocation(loc);
                }
                EditorGUILayout.EndScrollView();
            }
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.enabled = worldObject != null && availableRegions.Count > 0;
        if (GUILayout.Button("Далее  ➜", _bigButton, GUILayout.Width(140))) currentStep++;
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    private void FocusOnExistingLocation(GameObject loc)
    {
        if (loc == null) return;

        Selection.activeGameObject = loc;
        EditorGUIUtility.PingObject(loc);

        var sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.LookAt(loc.transform.position, sv.rotation, sv.size, sv.orthographic);
            sv.Repaint();
        }

        RequestFocusBack();
    }

    private void DrawStep_SelectRegion()
    {
        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("🌍  Выбор региона", _sectionTitle);
        EditorGUILayout.LabelField(Colored("В каком регионе будет создана локация?", ColDim), _richLabel);
        selectedRegionIndex = EditorGUILayout.Popup("Регион:", selectedRegionIndex, regionNames);
        EditorGUILayout.EndVertical();

        if (availableRegions.Count > 0 && !availableRegions[selectedRegionIndex].activeInHierarchy)
            EditorGUILayout.HelpBox("⚠  Выбранный регион неактивен. Локация тоже будет неактивна.", MessageType.Warning);

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("⬅  Назад", _bigButton, GUILayout.Width(140)))
        {
            if (createdLocationObj != null) { CleanupEverything(); currentStep--; }
            else currentStep--;
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Далее  ➜", _bigButton, GUILayout.Width(140)))
        {
            if (createdLocationObj != null) CleanupEverything();
            currentStep++;
        }
        GUILayout.EndHorizontal();
    }

    private void DrawStep_SelectSpriteAndName()
    {
        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("📁  Папка со спрайтами", _sectionTitle);
        EditorGUILayout.LabelField(Colored(spriteSearchPath, ColAccent), _richLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("📂  Обзор…", _iconButton))
        {
            string abs = EditorUtility.OpenFolderPanel("Выберите папку", spriteSearchPath, "");
            if (!string.IsNullOrEmpty(abs))
            {
                string rel = AbsoluteToAssetPath(abs);
                if (rel != null) spriteSearchPath = rel;
            }
        }
        if (GUILayout.Button("⬆  Вверх", _iconButton))
        {
            string parent = System.IO.Path.GetDirectoryName(spriteSearchPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && parent.StartsWith("Assets"))
                spriteSearchPath = parent;
        }
        EditorGUILayout.EndHorizontal();

        string[] subFolders = AssetDatabase.GetSubFolders(spriteSearchPath);
        if (subFolders != null && subFolders.Length > 0)
        {
            folderScrollPos = EditorGUILayout.BeginScrollView(folderScrollPos, GUILayout.Height(50));
            foreach (var sub in subFolders)
            {
                string name = System.IO.Path.GetFileName(sub);
                if (GUILayout.Button("📁  " + name, EditorStyles.miniButton))
                    spriteSearchPath = sub;
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("🖼️  Поиск спрайтов", _sectionTitle);
        spriteSearchPrefix = EditorGUILayout.TextField("Префикс:", spriteSearchPrefix);
        if (GUILayout.Button("🔎  Найти спрайты", _iconButton)) SearchSprites();
        EditorGUILayout.EndVertical();

        if (foundSprites.Count > 0)
        {
            EditorGUILayout.BeginVertical(_cardBox);
            EditorGUILayout.LabelField(
                Colored("Найдено: ", ColDim) + Colored(foundSprites.Count.ToString(), ColAccent) +
                Colored("  (⚠ красный = уже используется)", ColDim), _richLabelSmall);

            spriteScrollPos = EditorGUILayout.BeginScrollView(spriteScrollPos, GUILayout.Height(110));
            foreach (Sprite spr in foundSprites)
            {
                string cleaned = Regex.Replace(spr.name, @"_\d+$", "");
                bool alreadyUsed = existingLocationNames.Contains(cleaned);
                bool isSelected = locationSprite == spr;

                string label = (alreadyUsed ? "⚠  " : "   ") + spr.name;
                Color bg = isSelected ? ColAccent : (alreadyUsed ? ColUsed : Color.white);
                GUI.backgroundColor = bg;

                if (GUILayout.Button(label, EditorStyles.miniButton))
                {
                    locationSprite = spr;
                    locationName = cleaned;
                    if (createdLocationObj != null)
                    {
                        var sr = createdLocationObj.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.sprite = locationSprite;
                        createdLocationObj.name = locationName;
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("✏️  Имя локации", _sectionTitle);
        locationSprite = (Sprite)EditorGUILayout.ObjectField("Спрайт:", locationSprite, typeof(Sprite), false);
        if (locationSprite != null && string.IsNullOrEmpty(locationName))
            locationName = Regex.Replace(locationSprite.name, @"_\d+$", "");
        locationName = EditorGUILayout.TextField("Имя:", locationName);

        bool conflict = !string.IsNullOrEmpty(locationName)
                        && existingLocationNames.Contains(locationName)
                        && (createdLocationObj == null || createdLocationObj.name != locationName);
        if (conflict)
        {
            EditorGUILayout.HelpBox($"⚠ Имя '{locationName}' уже используется!", MessageType.Warning);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Добавить _copy", _iconButton)) locationName += "_copy";
            if (GUILayout.Button("Изменить вручную", _iconButton)) GUI.FocusControl(null);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("⬅  Назад", _bigButton, GUILayout.Width(140))) currentStep--;
        GUILayout.FlexibleSpace();
        GUI.enabled = locationSprite != null && !string.IsNullOrEmpty(locationName);
        if (GUILayout.Button("Далее  ➜", _bigButton, GUILayout.Width(140)))
        {
            CreateOrUpdateBaseLocation();
            currentStep++;
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    private void DrawStep_PositionLocation()
    {
        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("🎯  Позиционирование", _sectionTitle);
        EditorGUILayout.LabelField(
            Colored($"Шаг: ", ColDim) + Colored($"{MOVE_STEP_X:0} по X", ColAccent) +
            Colored("  ·  ", ColDim) + Colored($"{MOVE_STEP_Y:0} по Y", ColAccent), _richLabel);
        EditorGUILayout.EndVertical();

        if (createdLocationObj == null)
        {
            EditorGUILayout.HelpBox("⚠ Объект локации не создан. Вернитесь назад.", MessageType.Error);
            if (GUILayout.Button("⬅  Назад", _bigButton)) currentStep--;
            return;
        }

        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("⚙️  Поведение", _sectionTitle);
        autoFocusOnMove = DrawToggleButton(autoFocusOnMove ? "🎥  Авто-фокус: ВКЛ" : "🎥  Авто-фокус: ВЫКЛ", autoFocusOnMove, ColOk);
        bool newLight = DrawToggleButton(showPreviewLight ? "💡  Свет предпросмотра: ВКЛ" : "💡  Свет предпросмотра: ВЫКЛ", showPreviewLight, ColWarn);
        if (newLight != showPreviewLight)
        {
            showPreviewLight = newLight;
            if (showPreviewLight) EnsurePreviewLight();
            else DestroyPreviewLight();
            UpdateLightVisibility();
        }
        if (GUILayout.Button("📐  Компактный режим", _iconButton))
            RequestCompactMode(true);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("Перемещение:", _sectionTitle);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.backgroundColor = ColAccent;
        if (GUILayout.Button("↑", _arrowButton)) MoveLocation(0, MOVE_STEP_Y);
        GUI.backgroundColor = Color.white;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.backgroundColor = ColAccent;
        if (GUILayout.Button("←", _arrowButton)) MoveLocation(-MOVE_STEP_X, 0);
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("⟳", _arrowButton, GUILayout.Width(50)))
        {
            Undo.RecordObject(createdLocationObj.transform, "Reset Position");
            createdLocationObj.transform.localPosition = new Vector3(0, 0, 0);
            Selection.activeGameObject = createdLocationObj;
            if (autoFocusOnMove) SmoothFocusOnLocation();
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = ColAccent;
        if (GUILayout.Button("→", _arrowButton)) MoveLocation(MOVE_STEP_X, 0);
        GUI.backgroundColor = Color.white;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.backgroundColor = ColAccent;
        if (GUILayout.Button("↓", _arrowButton)) MoveLocation(0, -MOVE_STEP_Y);
        GUI.backgroundColor = Color.white;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(Colored("World: ", ColDim) + Colored(createdLocationObj.transform.position.ToString("F1"), ColAccent), _richLabelSmall);
        EditorGUILayout.LabelField(Colored("Local: ", ColDim) + Colored(createdLocationObj.transform.localPosition.ToString("F1"), ColDim), _richLabelSmall);

        if (GUILayout.Button("🔍  Сфокусироваться в Scene View", _iconButton)) FocusOnLocation();
        EditorGUILayout.EndVertical();

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("⬅  Назад", _bigButton, GUILayout.Width(140))) currentStep--;
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Далее  ➜", _bigButton, GUILayout.Width(140))) currentStep++;
        GUILayout.EndHorizontal();
    }

    private void MoveLocation(float dx, float dy)
    {
        if (createdLocationObj == null) return;
        Undo.RecordObject(createdLocationObj.transform, "Move Location");
        createdLocationObj.transform.position += new Vector3(dx, dy, 0);
        Selection.activeGameObject = createdLocationObj;
        if (autoFocusOnMove) SmoothFocusOnLocation();
        SceneView.RepaintAll();
        Repaint();
    }

    private void SmoothFocusOnLocation()
    {
        if (createdLocationObj == null) return;
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) return;
        sv.LookAt(createdLocationObj.transform.position, sv.rotation, sv.size, sv.orthographic);
        sv.Repaint();
    }

    private void FocusOnLocation()
    {
        if (createdLocationObj == null) return;
        Selection.activeGameObject = createdLocationObj;
        EditorGUIUtility.PingObject(createdLocationObj);
        if (createdLocationObj.activeInHierarchy)
        {
            SceneView.lastActiveSceneView?.FrameSelected();
            SceneView.RepaintAll();
        }
    }

    private void EnsurePreviewLight()
    {
        if (createdLocationObj == null) return;
        if (!showPreviewLight) { DestroyPreviewLight(); return; }

        var existing = createdLocationObj.transform.Find("__PreviewLight");
        if (existing != null)
        {
            _previewLightObj = existing.gameObject;
        }
        else
        {
            _previewLightObj = new GameObject("__PreviewLight");
            _previewLightObj.layer = LAYER_IGNORE_RAYCAST;
            _previewLightObj.transform.SetParent(createdLocationObj.transform);
            _previewLightObj.transform.localPosition = Vector3.zero;

            var l = _previewLightObj.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = Color.white;
            l.intensity = 1f;
            l.pointLightInnerRadius = PREVIEW_LIGHT_RADIUS * 0.9f;
            l.pointLightOuterRadius = PREVIEW_LIGHT_RADIUS;

            _previewLightObj.hideFlags = HideFlags.DontSave;
        }
        UpdateLightVisibility();
    }

    private void DestroyPreviewLight()
    {
        if (_previewLightObj != null) { DestroyImmediate(_previewLightObj); _previewLightObj = null; }
        if (createdLocationObj != null)
        {
            var t = createdLocationObj.transform.Find("__PreviewLight");
            if (t != null) DestroyImmediate(t.gameObject);
        }
    }

    private void DrawStep_PickReference()
    {
        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("📍  Точка появления персонажа", _sectionTitle);
        EditorGUILayout.LabelField(
            Colored("Координаты ", ColDim) + Colored("локальные", ColWarn) +
            Colored(". Кликните в Scene View.", ColDim), _richLabel);
        EditorGUILayout.EndVertical();

        if (createdLocationObj == null)
        {
            EditorGUILayout.HelpBox("⚠ Объект локации не создан.", MessageType.Error);
            if (GUILayout.Button("⬅  Назад", _bigButton)) currentStep--;
            return;
        }

        bool prevSkip = skipLocationReference;
        skipLocationReference = DrawToggleButton(
            skipLocationReference
                ? "🚫  LocationReference: не создаётся"
                : "📍  LocationReference: создаётся",
            skipLocationReference,
            skipLocationReference ? ColWarn : ColOk);

        if (skipLocationReference != prevSkip)
        {
            if (skipLocationReference) DestroyReferenceObject();
            else
            {
                EnsureReferenceObject();
                if (_refObj != null && !_refInitialized)
                {
                    _refInitialized = true;
                    showCharacterPreview = true;
                    ShowCharacterPreview();
                }
            }
        }

        if (!skipLocationReference)
        {
            EnsureReferenceObject();

            EditorGUILayout.BeginVertical(_cardBox);
            EditorGUILayout.LabelField("🎯  Выбор позиции", _sectionTitle);
            DrawVector3Editor("Локальные координаты:", ref locationRefLocalPos);

            GUI.backgroundColor = isPickingLocationRef ? ColWarn : ColAccent;
            if (GUILayout.Button(isPickingLocationRef ? "❌  Отменить выбор" : "🎯  Выбрать точку в сцене", _bigButton))
            {
                isPickingLocationRef = !isPickingLocationRef;
                isPickingLight = false;
                isPickingItem = false;
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;

            if (isPickingLocationRef)
                EditorGUILayout.HelpBox(
                    "Кликните ЛКМ, чтобы поставить референс.\n" +
                    "← → — размер Трикси, ↑ — поворот, Shift — ×5.",
                    MessageType.Info);
            EditorGUILayout.EndVertical();

            if (_refObj != null)
            {
                EditorGUILayout.BeginVertical(_cardBox);
                EditorGUILayout.LabelField("🧍  Персонаж", _sectionTitle);

                bool newPrev = DrawToggleButton(
                    showCharacterPreview ? "👤  Трикси: показана" : "👤  Трикси: не показана",
                    showCharacterPreview, ColAccent);
                if (newPrev != showCharacterPreview)
                {
                    showCharacterPreview = newPrev;
                    if (showCharacterPreview) ShowCharacterPreview();
                    else HideCharacterPreview();
                }
                if (showCharacterPreview)
                {
                    EditorGUILayout.LabelField(Colored($"Resources/{CHARACTER_PREVIEW_RESOURCES_PATH}.prefab", ColDim), _richLabelSmall);
                    EditorGUILayout.LabelField(
                        Colored("Клавиши: ", ColDim) + Colored("←", ColAccent) + " / " +
                        Colored("→", ColAccent) + Colored(" — размер, ", ColDim) +
                        Colored("↑", ColAccent) + Colored(" — поворот", ColDim), _richLabelSmall);
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(_cardBox);
                EditorGUILayout.LabelField("📏  Масштаб", _sectionTitle);

                Vector3 ls = _refObj.transform.localScale;
                EditorGUILayout.LabelField(
                    Colored("X / Y: ", ColDim) +
                    Colored(Round1(ls.x).ToString("F1"), ColAccent) + " / " +
                    Colored(Round1(ls.y).ToString("F1"), ColAccent),
                    _richLabel);

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = ColWarn;
                if (GUILayout.Button("−  0.1", _scaleBtn)) AdjustRefScale(-0.1f);
                GUI.backgroundColor = ColOk;
                if (GUILayout.Button("+  0.1", _scaleBtn)) AdjustRefScale(0.1f);
                GUI.backgroundColor = Color.white;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("↺  Сбросить масштаб (1, 1)", _iconButton))
                    SetRefScale(1f, 1f);
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(_cardBox);
                EditorGUILayout.LabelField("🔄  Поворот", _sectionTitle);
                EditorGUILayout.LabelField(
                    Colored($"Scale.X: ", ColDim) + Colored(Round1(_refObj.transform.localScale.x).ToString("F1"), ColAccent),
                    _richLabel);
                if (GUILayout.Button("↔  Повернуть персонажа", _wideScaleBtn))
                    FlipReferenceScaleX();
                EditorGUILayout.EndVertical();
            }
        }

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("⬅  Назад", _bigButton, GUILayout.Width(140)))
        {
            isPickingLocationRef = false;
            HideCharacterPreview();
            showCharacterPreview = false;
            currentStep--;
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Далее  ➜", _bigButton, GUILayout.Width(140)))
        {
            isPickingLocationRef = false;
            HideCharacterPreview();
            showCharacterPreview = false;
            currentStep++;
        }
        GUILayout.EndHorizontal();
    }

    private void EnsureReferenceObject()
    {
        if (createdLocationObj == null) return;
        if (_refObj != null) return;
        if (skipLocationReference) return;

        EnsureTagExists(REFERENCE_TAG);

        _refObj = new GameObject("LocationReference");
        _refObj.tag = REFERENCE_TAG;
        _refObj.layer = LAYER_IGNORE_RAYCAST;
        _refObj.transform.SetParent(createdLocationObj.transform);
        _refObj.transform.localPosition = SnapXY(locationRefLocalPos);
        _refObj.transform.localRotation = Quaternion.identity;
        _refObj.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        _refInitialized = false;
    }

    private void DestroyReferenceObject()
    {
        HideCharacterPreview();
        showCharacterPreview = false;
        if (_refObj != null) { DestroyImmediate(_refObj); _refObj = null; }
        if (createdLocationObj != null)
        {
            var t = createdLocationObj.transform.Find("LocationReference");
            if (t != null) DestroyImmediate(t.gameObject);
        }
    }

    private void UpdateReferencePosition()
    {
        if (_refObj == null) return;
        _refObj.transform.localPosition = SnapXY(locationRefLocalPos);
    }

    private void SetRefScale(float x, float y)
    {
        if (_refObj == null) return;
        Undo.RecordObject(_refObj.transform, "Set Ref Scale");
        x = Round1(x);
        y = Round1(y);
        _refObj.transform.localScale = new Vector3(x, y, 1f);
        SceneView.RepaintAll();
    }

    private void AdjustRefScale(float delta)
    {
        if (_refObj == null) return;

        Vector3 s = _refObj.transform.localScale;
        float signX = s.x < 0f ? -1f : 1f;
        float signY = s.y < 0f ? -1f : 1f;

        float magX = Mathf.Abs(s.x) + delta;
        float magY = Mathf.Abs(s.y) + delta;

        if (magX < 0.1f) magX = 0.1f;
        if (magY < 0.1f) magY = 0.1f;

        magX = Round1(magX);
        magY = Round1(magY);

        Undo.RecordObject(_refObj.transform, "Adjust Ref Scale");
        _refObj.transform.localScale = new Vector3(magX * signX, magY * signY, 1f);
        SceneView.RepaintAll();
    }

    private void FlipReferenceScaleX()
    {
        if (_refObj == null) return;
        Undo.RecordObject(_refObj.transform, "Flip Ref Scale X");
        Vector3 s = _refObj.transform.localScale;
        s.x = -Round1(s.x);
        _refObj.transform.localScale = s;
        SceneView.RepaintAll();
    }

    private void ShowCharacterPreview()
    {
        HideCharacterPreview();
        if (_refObj == null) return;

        var prefab = Resources.Load<GameObject>(CHARACTER_PREVIEW_RESOURCES_PATH);
        if (prefab == null)
        {
            Logger.LogWarning(LogModule.Editor, $"[LocationWizard] Префаб не найден: Resources/{CHARACTER_PREVIEW_RESOURCES_PATH}.prefab"); 
            return;
        }

        _previewCharacterObj = Instantiate(prefab);
        _previewCharacterObj.name = "__PreviewCharacter";
        _previewCharacterObj.transform.SetParent(_refObj.transform);
        _previewCharacterObj.transform.localPosition = Vector3.zero;
        _previewCharacterObj.transform.localRotation = Quaternion.identity;
        _previewCharacterObj.transform.localScale = Vector3.one;
        _previewCharacterObj.hideFlags = HideFlags.DontSave;
        SetLayerRecursively(_previewCharacterObj, LAYER_IGNORE_RAYCAST);
    }

    private void HideCharacterPreview()
    {
        if (_previewCharacterObj != null)
        {
            DestroyImmediate(_previewCharacterObj);
            _previewCharacterObj = null;
        }
        if (_refObj != null)
        {
            var t = _refObj.transform.Find("__PreviewCharacter");
            if (t != null) DestroyImmediate(t.gameObject);
        }
    }

    private void DrawStep_ConfigureLight()
    {
        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("💡  Источники света", _sectionTitle);
        EditorGUILayout.LabelField(
            Colored("Можно добавить несколько источников света. ", ColDim) +
            Colored("Позиция — локальная.", ColDim), _richLabel);
        EditorGUILayout.EndVertical();

        if (createdLocationObj == null)
        {
            EditorGUILayout.HelpBox("⚠ Объект локации не создан.", MessageType.Error);
            if (GUILayout.Button("⬅  Назад", _bigButton)) currentStep--;
            return;
        }

        bool prevSkip = skipLight;
        skipLight = DrawToggleButton(
            skipLight
                ? "🚫  Источники света: не создаются"
                : "💡  Источники света: создаются",
            skipLight,
            skipLight ? ColWarn : ColOk);

        if (skipLight != prevSkip)
        {
            if (skipLight)
            {
                isPickingLight = false;
                _isEditingLight = false;
                DestroyAllLightPreviews();
            }
            else
            {
                showPreviewLight = true;
                EnsurePreviewLight();
                RefreshLightPreviews();
                UpdateLightVisibility();
            }
        }

        if (!skipLight)
        {
            if (_isEditingLight)
                DrawLightEditor();
            else
                DrawLightList();
        }

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("⬅  Назад", _bigButton, GUILayout.Width(140)))
        {
            isPickingLight = false;
            _isEditingLight = false;
            DestroyAllLightPreviews();
            currentStep--;
        }
        GUILayout.FlexibleSpace();
        GUI.enabled = !_isEditingLight;
        if (GUILayout.Button("Далее  ➜", _bigButton, GUILayout.Width(140)))
        {
            isPickingLight = false;
            DestroyAllLightPreviews();
            currentStep++;
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    private void DrawLightList()
    {
        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField($"📋  Список источников ({_lights.Count})", _sectionTitle);

        if (_lights.Count == 0)
        {
            EditorGUILayout.LabelField(Colored("Источников пока нет.", ColDim), _richLabel);
        }
        else
        {
            for (int i = 0; i < _lights.Count; i++)
            {
                var ld = _lights[i];
                string colorHex = Hex(ld.color);

                EditorGUILayout.BeginVertical(_lightRowBox);
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(
                    Bold($"#{i + 1}") + "   " +
                    Colored("pos ", ColDim) + Colored(ld.localPos.ToString("F1"), ColAccent) + "   " +
                    Colored(colorHex, ld.color) + "   " +
                    Colored($"I={ld.intensity:F1}", ColDim),
                    _richLabel);

                GUILayout.FlexibleSpace();

                GUI.backgroundColor = ColAccent;
                if (GUILayout.Button("✎", _miniArrow)) StartEditingLight(i);
                GUI.backgroundColor = ColWarn;
                if (GUILayout.Button("✕", _miniArrow)) DeleteLight(i);
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        GUILayout.Space(6);
        GUI.backgroundColor = ColAccent;
        if (GUILayout.Button("➕  Создать источник света", _bigButton))
            StartCreatingLight();
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(_cardBox);
        bool newPrev = DrawToggleButton(
            _lightPreviewOn
                ? "👁  Реальные источники: ВКЛ"
                : "💡  Вспомогательный свет: ВКЛ",
            _lightPreviewOn,
            _lightPreviewOn ? ColOk : ColWarn);
        if (newPrev != _lightPreviewOn)
        {
            _lightPreviewOn = newPrev;
            UpdateLightVisibility();
        }
        EditorGUILayout.LabelField(
            Colored("Вспомогательный свет — белый preview, чтобы видеть сцену.", ColDim),
            _richLabelSmall);
        EditorGUILayout.EndVertical();
    }

    private void DrawLightEditor()
    {
        EditorGUILayout.BeginVertical(_cardBox);
        string header = _editingLightIndex < 0
            ? "🆕  Новый источник"
            : $"✎  Редактирование #{_editingLightIndex + 1}";
        EditorGUILayout.LabelField(header, _sectionTitle);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(Colored("Позиция:", ColDim), _richLabel, GUILayout.Width(70));
        lightLocalPos.x = EditorGUILayout.FloatField(lightLocalPos.x);
        lightLocalPos.y = EditorGUILayout.FloatField(lightLocalPos.y);
        EditorGUILayout.EndHorizontal();

        GUI.backgroundColor = isPickingLight ? ColWarn : ColAccent;
        if (GUILayout.Button(isPickingLight ? "❌  Отменить выбор" : "🎯  Выбрать позицию в сцене", _bigButton))
        {
            isPickingLight = !isPickingLight;
            isPickingLocationRef = false;
            isPickingItem = false;
            if (isPickingLight) EnsureEditPreview();
            UpdateLightVisibility();
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        if (isPickingLight)
            EditorGUILayout.HelpBox("Кликните ЛКМ или используйте ← → ↑ ↓. Вспомогательный свет включён на время выбора.", MessageType.Info);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("🎨  Настройки", _sectionTitle);

        lightType = (Light2D.LightType)EditorGUILayout.EnumPopup("Тип:", lightType);
        lightColor = EditorGUILayout.ColorField("Цвет:", lightColor);
        lightIntensity = EditorGUILayout.Slider("Яркость:", lightIntensity, 0f, 10f);
        if (lightType == Light2D.LightType.Point)
            lightRadius = EditorGUILayout.Slider("Радиус:", lightRadius, 0.1f, 30f);

        SyncEditPreview();
        EditorGUILayout.EndVertical();

        GUILayout.Space(2);
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = ColWarn;
        if (GUILayout.Button("⬅  Отмена", _bigButton, GUILayout.Width(140))) CancelEdit();
        GUI.backgroundColor = ColOk;
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("✅  Сохранить", _bigButton, GUILayout.Width(180))) SaveEdit();
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
    }

    private void StartCreatingLight()
    {
        _isEditingLight = true;
        _editingLightIndex = -1;
        lightType = Light2D.LightType.Point;
        lightColor = Color.white;
        lightIntensity = 1f;
        lightRadius = 5f;
        lightLocalPos = Vector3.zero;

        isPickingLight = false;
        EnsureEditPreview();
        UpdateLightVisibility();
    }

    private void StartEditingLight(int index)
    {
        if (index < 0 || index >= _lights.Count) return;
        var ld = _lights[index];
        _isEditingLight = true;
        _editingLightIndex = index;
        lightType = ld.type;
        lightColor = ld.color;
        lightIntensity = ld.intensity;
        lightRadius = ld.radius;
        lightLocalPos = ld.localPos;

        isPickingLight = false;
        EnsureEditPreview();
        UpdateLightVisibility();
    }

    private void SaveEdit()
    {
        var ld = new LightData
        {
            type = lightType,
            color = lightColor,
            intensity = lightIntensity,
            radius = lightRadius,
            localPos = SnapXY(lightLocalPos)
        };

        if (_editingLightIndex < 0) _lights.Add(ld);
        else _lights[_editingLightIndex] = ld;

        _isEditingLight = false;
        _editingLightIndex = -1;
        isPickingLight = false;

        if (_tempRealLightObj != null) { DestroyImmediate(_tempRealLightObj); _tempRealLightObj = null; }
        RefreshLightPreviews();
        UpdateLightVisibility();
        SceneView.RepaintAll();
    }

    private void CancelEdit()
    {
        _isEditingLight = false;
        _editingLightIndex = -1;
        isPickingLight = false;

        if (_tempRealLightObj != null) { DestroyImmediate(_tempRealLightObj); _tempRealLightObj = null; }
        UpdateLightVisibility();
        SceneView.RepaintAll();
    }

    private void DeleteLight(int index)
    {
        if (index < 0 || index >= _lights.Count) return;
        _lights.RemoveAt(index);
        RefreshLightPreviews();
        SceneView.RepaintAll();
    }

    private void EnsureEditPreview()
    {
        if (createdLocationObj == null) return;
        if (_tempRealLightObj != null) return;

        _tempRealLightObj = new GameObject("__PreviewRealLight");
        _tempRealLightObj.layer = LAYER_IGNORE_RAYCAST;
        _tempRealLightObj.transform.SetParent(createdLocationObj.transform);
        _tempRealLightObj.transform.localPosition = SnapXY(lightLocalPos);
        _tempRealLightObj.hideFlags = HideFlags.DontSave;

        _tempRealLightObj.AddComponent<Light2D>();
        SyncEditPreview();
    }

    private void SyncEditPreview()
    {
        if (_tempRealLightObj == null) return;
        var l = _tempRealLightObj.GetComponent<Light2D>();
        if (l == null) return;

        _tempRealLightObj.transform.localPosition = SnapXY(lightLocalPos);
        l.lightType = lightType;
        l.color = lightColor;
        l.intensity = lightIntensity;
        if (lightType == Light2D.LightType.Point)
        {
            l.pointLightInnerRadius = lightRadius * 0.5f;
            l.pointLightOuterRadius = lightRadius;
        }
        UpdateLightVisibility();
        SceneView.RepaintAll();
    }

    private void DrawStep_Items()
    {
        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("🧩  Предметы", _sectionTitle);
        EditorGUILayout.LabelField(
            Colored("Можно добавить несколько предметов из ", ColDim) +
            Colored($"Resources/{ITEMS_RESOURCES_PATH}", ColAccent) + Colored(".", ColDim),
            _richLabel);
        EditorGUILayout.EndVertical();

        if (createdLocationObj == null)
        {
            EditorGUILayout.HelpBox("⚠ Объект локации не создан.", MessageType.Error);
            if (GUILayout.Button("⬅  Назад", _bigButton)) currentStep--;
            return;
        }

        bool prevSkip = skipItems;
        skipItems = DrawToggleButton(
            skipItems
                ? "🚫  Предметы: не создаются"
                : "🧩  Предметы: создаются",
            skipItems,
            skipItems ? ColWarn : ColOk);

        if (skipItems != prevSkip)
        {
            if (skipItems)
            {
                isPickingItem = false;
                _isEditingItem = false;
                _isPickingItemPrefab = false;
                DestroyItemEditPreview();
                DestroyAllItemPreviews();
            }
            else
            {
                RefreshItemPreviews();
                UpdateItemVisibility();
            }
        }

        if (!skipItems)
        {
            if (_isEditingItem)
                DrawItemEditor();
            else if (_isPickingItemPrefab)
                DrawItemPicker();
            else
                DrawItemList();
        }

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("⬅  Назад", _bigButton, GUILayout.Width(140)))
        {
            isPickingItem = false;
            _isEditingItem = false;
            _isPickingItemPrefab = false;
            DestroyItemEditPreview();
            DestroyAllItemPreviews();
            currentStep--;
        }
        GUILayout.FlexibleSpace();
        GUI.enabled = !_isEditingItem && !_isPickingItemPrefab;
        if (GUILayout.Button("Далее  ➜", _bigButton, GUILayout.Width(140)))
        {
            isPickingItem = false;
            DestroyItemEditPreview();
            DestroyAllItemPreviews();
            currentStep++;
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    private void DrawItemList()
    {
        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField($"📋  Список предметов ({_items.Count})", _sectionTitle);

        if (_items.Count == 0)
        {
            EditorGUILayout.LabelField(Colored("Предметов пока нет.", ColDim), _richLabel);
        }
        else
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var it = _items[i];
                if (it.prefab == null) continue;

                EditorGUILayout.BeginVertical(_itemRowBox);
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(
                    Bold($"#{i + 1} ") + Colored(it.prefab.name, ColAccent) + "   " +
                    Colored("pos ", ColDim) + Colored(it.localPos.ToString("F1"), ColDim) + "   " +
                    Colored("scl ", ColDim) + Colored($"{Round1(it.localScale.x):F1}/{Round1(it.localScale.y):F1}", ColDim),
                    _richLabel);

                GUILayout.FlexibleSpace();

                GUI.backgroundColor = ColAccent;
                if (GUILayout.Button("✎", _miniArrow)) StartEditingItem(i);
                GUI.backgroundColor = ColWarn;
                if (GUILayout.Button("✕", _miniArrow)) DeleteItem(i);
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }

        GUILayout.Space(6);
        GUI.backgroundColor = ColAccent;
        if (GUILayout.Button("➕  Добавить предмет", _bigButton))
            StartPickingItemPrefab();
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();
    }

    private void DrawItemPicker()
    {
        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("🎒  Выбор предмета", _sectionTitle);
        EditorGUILayout.LabelField(
            Colored($"Найдено: ", ColDim) + Colored(_availableItemPrefabs.Count.ToString(), ColAccent),
            _richLabelSmall);

        if (_availableItemPrefabs.Count == 0)
        {
            EditorGUILayout.HelpBox($"В Resources/{ITEMS_RESOURCES_PATH} ничего не найдено.", MessageType.Warning);
        }
        else
        {
            _itemPickerScrollPos = EditorGUILayout.BeginScrollView(_itemPickerScrollPos, GUILayout.Height(200));
            foreach (var prefab in _availableItemPrefabs)
            {
                if (prefab == null) continue;
                if (GUILayout.Button("  " + prefab.name, EditorStyles.miniButton))
                {
                    SelectItemPrefab(prefab);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(4);
        GUI.backgroundColor = ColWarn;
        if (GUILayout.Button("⬅  Отмена", _bigButton))
        {
            _isPickingItemPrefab = false;
            _pendingItemPrefab = null;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();
    }

    private void DrawItemEditor()
    {
        EditorGUILayout.BeginVertical(_cardBox);
        string header = _editingItemIndex < 0
            ? $"🆕  Новый предмет: {(_pendingItemPrefab != null ? _pendingItemPrefab.name : "—")}"
            : $"✎  Редактирование #{_editingItemIndex + 1} ({(_pendingItemPrefab != null ? _pendingItemPrefab.name : "—")})";
        EditorGUILayout.LabelField(header, _sectionTitle);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(Colored("Позиция:", ColDim), _richLabel, GUILayout.Width(70));
        _itemLocalPos.x = EditorGUILayout.FloatField(_itemLocalPos.x);
        _itemLocalPos.y = EditorGUILayout.FloatField(_itemLocalPos.y);
        EditorGUILayout.EndHorizontal();

        GUI.backgroundColor = isPickingItem ? ColWarn : ColAccent;
        if (GUILayout.Button(isPickingItem ? "❌  Отменить выбор" : "🎯  Выбрать позицию в сцене", _bigButton))
        {
            isPickingItem = !isPickingItem;
            isPickingLocationRef = false;
            isPickingLight = false;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;

        if (isPickingItem)
            EditorGUILayout.HelpBox(
                "Кликните ЛКМ, чтобы поставить предмет.\n" +
                "← → — размер, ↑ — поворот, Shift — ×5.",
                MessageType.Info);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("📏  Масштаб", _sectionTitle);
        EditorGUILayout.LabelField(
            Colored("X / Y: ", ColDim) +
            Colored(Round1(_itemLocalScale.x).ToString("F1"), ColAccent) + " / " +
            Colored(Round1(_itemLocalScale.y).ToString("F1"), ColAccent),
            _richLabel);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = ColWarn;
        if (GUILayout.Button("−  0.1", _scaleBtn)) AdjustItemScale(-0.1f);
        GUI.backgroundColor = ColOk;
        if (GUILayout.Button("+  0.1", _scaleBtn)) AdjustItemScale(0.1f);
        GUI.backgroundColor = Color.white;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("↺  Сбросить масштаб (0.5, 0.5)", _iconButton))
        {
            _itemLocalScale = new Vector3(DEFAULT_ITEM_SCALE, DEFAULT_ITEM_SCALE, 1f);
            SyncItemEditPreview();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("🔄  Поворот", _sectionTitle);
        EditorGUILayout.LabelField(
            Colored($"Scale.X: ", ColDim) + Colored(Round1(_itemLocalScale.x).ToString("F1"), ColAccent),
            _richLabel);
        if (GUILayout.Button("↔  Повернуть предмет", _wideScaleBtn))
            FlipItemScaleX();
        EditorGUILayout.EndVertical();

        GUILayout.Space(2);
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = ColWarn;
        if (GUILayout.Button("⬅  Отмена", _bigButton, GUILayout.Width(140))) CancelItemEdit();
        GUI.backgroundColor = ColOk;
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("✅  Сохранить", _bigButton, GUILayout.Width(180))) SaveItemEdit();
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
    }

    private void StartPickingItemPrefab()
    {
        _isPickingItemPrefab = true;
        _isEditingItem = false;
        _editingItemIndex = -1;
        _pendingItemPrefab = null;
        if (_availableItemPrefabs.Count == 0) SearchItems();
    }

    private void SelectItemPrefab(GameObject prefab)
    {
        if (prefab == null) return;
        _pendingItemPrefab = prefab;
        _isPickingItemPrefab = false;

        _isEditingItem = true;
        _editingItemIndex = -1;
        _itemLocalPos = Vector3.zero;
        _itemLocalScale = new Vector3(DEFAULT_ITEM_SCALE, DEFAULT_ITEM_SCALE, 1f);

        isPickingItem = false;
        DestroyItemEditPreview();
        EnsureItemEditPreview();
    }

    private void StartEditingItem(int index)
    {
        if (index < 0 || index >= _items.Count) return;
        var it = _items[index];
        if (it.prefab == null) return;

        _pendingItemPrefab = it.prefab;
        _isEditingItem = true;
        _editingItemIndex = index;
        _itemLocalPos = it.localPos;
        _itemLocalScale = it.localScale;

        isPickingItem = false;
        DestroyItemEditPreview();
        EnsureItemEditPreview();
        UpdateItemVisibility();
    }

    private void SaveItemEdit()
    {
        if (_pendingItemPrefab == null) { CancelItemEdit(); return; }

        var it = new ItemData
        {
            prefab = _pendingItemPrefab,
            localPos = SnapXY(_itemLocalPos),
            localScale = _itemLocalScale
        };

        if (_editingItemIndex < 0) _items.Add(it);
        else _items[_editingItemIndex] = it;

        _isEditingItem = false;
        _editingItemIndex = -1;
        isPickingItem = false;
        _pendingItemPrefab = null;

        DestroyItemEditPreview();
        RefreshItemPreviews();
        UpdateItemVisibility();
        SceneView.RepaintAll();
    }

    private void CancelItemEdit()
    {
        _isEditingItem = false;
        _editingItemIndex = -1;
        isPickingItem = false;
        _pendingItemPrefab = null;

        DestroyItemEditPreview();
        UpdateItemVisibility();
        SceneView.RepaintAll();
    }

    private void DeleteItem(int index)
    {
        if (index < 0 || index >= _items.Count) return;
        _items.RemoveAt(index);
        RefreshItemPreviews();
        SceneView.RepaintAll();
    }

    private void EnsureItemEditPreview()
    {
        if (createdLocationObj == null) return;
        if (_itemEditPreviewObj != null) return;
        if (_pendingItemPrefab == null) return;

        _itemEditPreviewObj = Instantiate(_pendingItemPrefab);
        _itemEditPreviewObj.name = "__PreviewItemEdit";
        _itemEditPreviewObj.transform.SetParent(createdLocationObj.transform);
        _itemEditPreviewObj.transform.localPosition = SnapXY(_itemLocalPos);
        _itemEditPreviewObj.transform.localScale = _itemLocalScale;
        _itemEditPreviewObj.hideFlags = HideFlags.DontSave;
        SetLayerRecursively(_itemEditPreviewObj, LAYER_DEFAULT);
        SyncItemEditPreview();
    }

    private void SyncItemEditPreview()
    {
        if (_itemEditPreviewObj == null) return;
        _itemEditPreviewObj.transform.localPosition = SnapXY(_itemLocalPos);
        _itemEditPreviewObj.transform.localScale = _itemLocalScale;
        UpdateItemVisibility();
        SceneView.RepaintAll();
    }

    private void AdjustItemScale(float delta)
    {
        if (_itemEditPreviewObj == null) return;

        float signX = _itemLocalScale.x < 0f ? -1f : 1f;
        float signY = _itemLocalScale.y < 0f ? -1f : 1f;

        float magX = Mathf.Abs(_itemLocalScale.x) + delta;
        float magY = Mathf.Abs(_itemLocalScale.y) + delta;

        if (magX < 0.1f) magX = 0.1f;
        if (magY < 0.1f) magY = 0.1f;

        magX = Round1(magX);
        magY = Round1(magY);

        _itemLocalScale = new Vector3(magX * signX, magY * signY, 1f);
        SyncItemEditPreview();
    }

    private void FlipItemScaleX()
    {
        if (_itemEditPreviewObj == null) return;
        _itemLocalScale.x = -Round1(_itemLocalScale.x);
        SyncItemEditPreview();
    }

    private void SearchItems()
    {
        _availableItemPrefabs.Clear();
        try
        {
            var all = Resources.LoadAll<GameObject>(ITEMS_RESOURCES_PATH);
            if (all != null)
                foreach (var go in all)
                    if (go != null) _availableItemPrefabs.Add(go);
        }
        catch (System.Exception e)
        {
            Logger.LogWarning(LogModule.Editor, $"[LocationWizard] Не удалось загрузить предметы из Resources/{ITEMS_RESOURCES_PATH}: {e.Message}");
        }
        Logger.Log(LogModule.Editor, $"[LocationWizard] Найдено предметов: {_availableItemPrefabs.Count}");
        Repaint();
    }

    private void DrawStep_Finalize()
    {
        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("📋  Итог", _sectionTitle);
        DrawSummaryRow("Регион", regionNames != null && regionNames.Length > 0 ? regionNames[selectedRegionIndex] : "—");
        DrawSummaryRow("Имя", locationName);
        DrawSummaryRow("Спрайт", locationSprite != null ? locationSprite.name : "—");
        DrawSummaryRow("Позиция", createdLocationObj != null ? createdLocationObj.transform.position.ToString("F1") : "—");
        DrawSummaryRow("Reference", skipLocationReference ? "не создаётся" : locationRefLocalPos.ToString("F2"));
        DrawSummaryRow("Источники света", skipLight ? "не создаются" : $"{_lights.Count}");
        DrawSummaryRow("Предметы", skipItems ? "не создаются" : $"{_items.Count}");
        EditorGUILayout.EndVertical();

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("⬅  Назад", _bigButton, GUILayout.Width(140))) currentStep--;
        GUILayout.FlexibleSpace();
        GUI.backgroundColor = ColOk;
        if (GUILayout.Button("✅  СОЗДАТЬ", _bigButton, GUILayout.Width(180)))
        {
            FinalizeCreation();
            currentStep = 0;
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
    }

    private void DrawSummaryRow(string key, string value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(Colored(key + ":", ColDim), _richLabel, GUILayout.Width(140));
        EditorGUILayout.LabelField(Colored(value, ColAccent), _richLabel);
        EditorGUILayout.EndHorizontal();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        var e = Event.current;

        if (isPickingLocationRef || isPickingLight || isPickingItem)
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlID);

            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                UpdatePickFromMouse(e.mousePosition);
            }

            if (e.type == EventType.Repaint)
            {
                Vector3 previewWorld = isPickingLocationRef ? locationRefLocalPos
                    : (isPickingLight ? lightLocalPos : _itemLocalPos);
                if (createdLocationObj != null)
                    previewWorld = createdLocationObj.transform.TransformPoint(previewWorld);
                previewWorld.z = 0;

                Color previewColor = isPickingLocationRef ? ColOk : (isPickingLight ? ColWarn : ColAccent);
                Handles.color = previewColor;
                Handles.DrawWireDisc(previewWorld, Vector3.forward, 0.5f);
                Handles.DrawWireDisc(previewWorld, Vector3.forward, 0.15f);
                Handles.DrawLine(previewWorld + Vector3.left * 0.7f, previewWorld + Vector3.right * 0.7f);
                Handles.DrawLine(previewWorld + Vector3.up * 0.7f, previewWorld + Vector3.down * 0.7f);

                var labelStyle = new GUIStyle
                {
                    normal = { textColor = previewColor },
                    fontStyle = FontStyle.Bold,
                    richText = true
                };
                string label = isPickingLocationRef ? "→ LocationReference"
                    : (isPickingLight ? "→ Light 2D" : "→ Item");
                Handles.Label(previewWorld + new Vector3(0.8f, 0.8f, 0), label, labelStyle);
            }

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && !e.control && !e.shift)
            {
                UpdatePickFromMouse(e.mousePosition);

                bool wasPickingLight = isPickingLight;
                bool wasPickingItem = isPickingItem;
                isPickingLocationRef = false;
                isPickingLight = false;
                isPickingItem = false;

                if (wasPickingLight) SyncEditPreview();
                if (wasPickingItem) SyncItemEditPreview();
                UpdateLightVisibility();
                UpdateItemVisibility();

                e.Use();
                Repaint();
                GUIUtility.ExitGUI();
                return;
            }

            Repaint();
            sceneView.Repaint();
            return;
        }

        if (e != null && e.type == EventType.KeyDown &&
            (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
        {
            if (HandleEnterKeyDirect())
            {
                e.Use();
                sceneView.Repaint();
                return;
            }
        }
    }

    private void UpdatePickFromMouse(Vector2 mousePos)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        Vector3 worldPos = Vector3.zero;
        if (plane.Raycast(ray, out float dist)) worldPos = ray.GetPoint(dist);
        worldPos.z = 0;

        Vector3 localPos = worldPos;
        if (createdLocationObj != null)
        {
            localPos = createdLocationObj.transform.InverseTransformPoint(worldPos);
            localPos = SnapXY(localPos);
        }

        if (isPickingLocationRef)
        {
            locationRefLocalPos = localPos;
            UpdateReferencePosition();
        }
        else if (isPickingLight)
        {
            lightLocalPos = localPos;
            SyncEditPreview();
        }
        else if (isPickingItem)
        {
            _itemLocalPos = localPos;
            SyncItemEditPreview();
        }
    }

    private void DrawVector3Editor(string label, ref Vector3 value)
    {
        EditorGUILayout.LabelField(label, _sectionTitle);
        EditorGUILayout.BeginHorizontal();
        value.x = EditorGUILayout.FloatField("X:", value.x);
        value.y = EditorGUILayout.FloatField("Y:", value.y);
        value.z = 0f;
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("↺  Сбросить в (0, 0, 0)", _iconButton))
            value = Vector3.zero;
    }

    private void FindWorldAndRegions()
    {
        worldObject = null;
        availableRegions.Clear();
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in all)
        {
            if (!obj.scene.IsValid() || EditorUtility.IsPersistent(obj)) continue;
            if (worldObject == null && (obj.tag == "World" || obj.name == "World")) worldObject = obj;
            if (obj.tag == "Region") availableRegions.Add(obj);
        }
        availableRegions.Sort((a, b) => a.name.CompareTo(b.name));
        regionNames = new string[availableRegions.Count];
        for (int i = 0; i < availableRegions.Count; i++) regionNames[i] = availableRegions[i].name;
        Repaint();
    }

    private void FindExistingLocations()
    {
        existingLocations.Clear();
        existingLocationNames.Clear();
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in all)
        {
            if (!obj.scene.IsValid() || EditorUtility.IsPersistent(obj)) continue;
            if (obj.tag == "Location")
            {
                existingLocations.Add(obj);
                existingLocationNames.Add(obj.name);
            }
        }
        Repaint();
    }

    private void SearchSprites()
    {
        foundSprites.Clear();
        if (!AssetDatabase.IsValidFolder(spriteSearchPath)) { Logger.LogError(LogModule.Editor, $"Папка '{spriteSearchPath}' не найдена!"); return; }
        string[] guids = AssetDatabase.FindAssets($"{spriteSearchPrefix} t:Sprite", new[] { spriteSearchPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) foundSprites.Add(sprite);
        }
        Logger.Log(LogModule.Editor, $"Найдено спрайтов: {foundSprites.Count}");
        Repaint();
    }

    private string AbsoluteToAssetPath(string abs)
    {
        string dataPath = Application.dataPath;
        string projectPath = System.IO.Directory.GetParent(dataPath).FullName;
        if (!abs.StartsWith(projectPath)) return null;
        string rel = abs.Substring(projectPath.Length);
        if (rel.StartsWith("/") || rel.StartsWith("\\")) rel = rel.Substring(1);
        rel = rel.Replace('\\', '/');
        return rel.StartsWith("Assets") ? rel : null;
    }

    private void CreateOrUpdateBaseLocation()
    {
        EnsureTagExists("Location");
        GameObject region = availableRegions[selectedRegionIndex];

        if (createdLocationObj != null)
        {
            if (createdLocationObj.transform.parent != region.transform)
                createdLocationObj.transform.SetParent(region.transform, false);
            if (createdLocationObj.name != locationName) createdLocationObj.name = locationName;
            ApplyLocationObjectSettings(createdLocationObj, locationSprite);
            EnsurePreviewLight();
            if (autoFocusOnMove) SmoothFocusOnLocation();
            return;
        }

        createdLocationObj = new GameObject(locationName);
        createdLocationObj.tag = "Location";
        createdLocationObj.transform.SetParent(region.transform);
        createdLocationObj.transform.localPosition = new Vector3(0, 0, 0);

        ApplyLocationObjectSettings(createdLocationObj, locationSprite);
        Undo.RegisterCreatedObjectUndo(createdLocationObj, "Create Location");

        _refInitialized = false;
        _lights.Clear();
        _items.Clear();

        EnsurePreviewLight();
        if (autoFocusOnMove) SmoothFocusOnLocation();
    }

    private void ApplyLocationObjectSettings(GameObject loc, Sprite sprite)
    {
        loc.layer = LAYER_IGNORE_RAYCAST;

        var sr = loc.GetComponent<SpriteRenderer>();
        if (sr == null) sr = loc.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = LOCATION_ORDER_IN_LAYER;
        try { sr.renderingLayerMask = (RenderingLayerMask)0xFFFFFFFFu; }
        catch { }

        var neighborsType = FindTypeByName(NEIGHBORS_SCRIPT_NAME);
        if (neighborsType != null && loc.GetComponent(neighborsType) == null)
        {
            loc.AddComponent(neighborsType);
        }
    }

    private void ApplyLocationStaticFlag(GameObject loc)
    {
        if (loc == null) return;
        GameObjectUtility.SetStaticEditorFlags(loc, StaticEditorFlags.BatchingStatic);
    }

    private void FinalizeCreation()
    {
        if (createdLocationObj == null) { Logger.LogError(LogModule.Editor, "Объект локации не создан."); return; }

        isPickingLight = false;
        isPickingItem = false;
        isPickingLocationRef = false;
        _isEditingLight = false;
        _isEditingItem = false;
        _isPickingItemPrefab = false;
        HideCharacterPreview();
        showCharacterPreview = false;

        DestroyAllLightPreviews();
        DestroyPreviewLight();
        DestroyAllItemPreviews();
        DestroyItemEditPreview();

        if (!skipLocationReference)
        {
            EnsureReferenceObject();
            if (_refObj != null)
            {
                _refObj.transform.localPosition = SnapXY(locationRefLocalPos);
                _refObj.tag = REFERENCE_TAG;
                _refObj.layer = LAYER_IGNORE_RAYCAST;
            }
        }

        if (!skipLight && _lights.Count > 0)
        {
            var oldLight = createdLocationObj.transform.Find("Spot Light 2D");
            if (oldLight != null) DestroyImmediate(oldLight.gameObject);

            foreach (var ld in _lights)
            {
                var lightObj = new GameObject("Spot Light 2D");
                lightObj.layer = LAYER_IGNORE_RAYCAST;
                lightObj.transform.SetParent(createdLocationObj.transform);
                lightObj.transform.localPosition = SnapXY(ld.localPos);

                var light2D = lightObj.AddComponent<Light2D>();
                light2D.lightType = ld.type;
                light2D.color = ld.color;
                light2D.intensity = ld.intensity;
                if (ld.type == Light2D.LightType.Point)
                {
                    light2D.pointLightInnerRadius = ld.radius * 0.5f;
                    light2D.pointLightOuterRadius = ld.radius;
                }
            }
        }

        SetLayerRecursively(createdLocationObj, LAYER_IGNORE_RAYCAST);
        ApplyLocationObjectSettings(createdLocationObj, locationSprite);
        ApplyLocationStaticFlag(createdLocationObj);

        if (!skipItems && _items.Count > 0)
        {
            foreach (var it in _items)
            {
                if (it.prefab == null) continue;
                var go = Instantiate(it.prefab);
                go.name = it.prefab.name;
                go.transform.SetParent(createdLocationObj.transform);
                go.transform.localPosition = SnapXY(it.localPos);
                go.transform.localScale = it.localScale;
                SetLayerRecursively(go, LAYER_DEFAULT);
            }
        }

        Logger.Log(LogModule.Editor, $"✅ Локация '{locationName}' создана! Источников света: {_lights.Count}, предметов: {_items.Count}");
        Selection.activeGameObject = createdLocationObj;
        EditorGUIUtility.PingObject(createdLocationObj);

        RequestFocusBack();

        createdLocationObj = null;
        _previewLightObj = null;
        _refObj = null;
        _tempRealLightObj = null;
        _previewCharacterObj = null;
        _itemEditPreviewObj = null;
        locationSprite = null;
        locationName = "";
        locationRefLocalPos = Vector3.zero;
        lightLocalPos = Vector3.zero;
        _itemLocalPos = Vector3.zero;
        _itemLocalScale = new Vector3(DEFAULT_ITEM_SCALE, DEFAULT_ITEM_SCALE, 1f);
        foundSprites.Clear();
        skipLocationReference = false;
        skipLight = false;
        skipItems = false;
        _lights.Clear();
        _lightPreviewObjects.Clear();
        _items.Clear();
        _itemPreviewObjects.Clear();
        _isEditingLight = false;
        _editingLightIndex = -1;
        _isEditingItem = false;
        _editingItemIndex = -1;
        _isPickingItemPrefab = false;
        _pendingItemPrefab = null;
        _refInitialized = false;
        _lastStep = -1;
        _step3CompactApplied = false;
        _focusSynced = false;

        FindWorldAndRegions();
        FindExistingLocations();

        if (compactMode) RequestCompactMode(false);
    }

    private void CleanupEverything()
    {
        HideCharacterPreview();
        DestroyAllLightPreviews();
        DestroyReferenceObject();
        DestroyPreviewLight();
        DestroyAllItemPreviews();
        DestroyItemEditPreview();
        if (createdLocationObj != null)
        {
            DestroyImmediate(createdLocationObj);
            createdLocationObj = null;
        }
        _refObj = null;
        _tempRealLightObj = null;
        _previewCharacterObj = null;
        _previewLightObj = null;
        _itemEditPreviewObj = null;
        _lights.Clear();
        _lightPreviewObjects.Clear();
        _items.Clear();
        _itemPreviewObjects.Clear();
        _isEditingLight = false;
        _editingLightIndex = -1;
        _isEditingItem = false;
        _editingItemIndex = -1;
        _isPickingItemPrefab = false;
        _pendingItemPrefab = null;
        _refInitialized = false;
    }

    private void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private System.Type FindTypeByName(string name)
    {
        foreach (var t in UnityEditor.TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
        {
            if (t != null && t.Name == name)
                return t;
        }
        return null;
    }

    private void EnsureTagExists(string tag)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;

        var tagManager = new SerializedObject(assets[0]);
        var tagsProp = tagManager.FindProperty("tags");
        for (int i = 0; i < tagsProp.arraySize; i++)
            if (tagsProp.GetArrayElementAtIndex(i).stringValue.Equals(tag)) return;
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }
}