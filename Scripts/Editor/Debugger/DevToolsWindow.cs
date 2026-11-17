#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public class DevToolsWindow : EditorWindow, ICallbacks
{
    private enum Tab { Logs, World, Progress, Tests }

    private Tab currentTab = Tab.Logs;
    private Vector2 scrollPosWorld;
    private Vector2 scrollPosProgress;

    private string newFlagName = "";
    private int selectedItemIndex = 0;
    private int itemCountToAdd = 1;

    private TestRunnerApi testRunnerApi;
    private bool testRunInProgress = false;
    private int testsPassed = 0;
    private int testsFailed = 0;
    private int testsTotal = 0;

    private struct TestResultData
    {
        public TestStatus Status;
        public double Duration;
        public string Message;
    }

    private ITestAdaptor loadedTestTree;
    private Vector2 testTreeScroll;
    private Dictionary<string, TestResultData> testResults = new Dictionary<string, TestResultData>();
    private bool isLoadingTestList = false;
    private string testSearchFilter = "";
    private HashSet<string> expandedCategories = new HashSet<string>();
    private HashSet<string> expandedTestInfos = new HashSet<string>();

    private Texture2D _bannerTex;
    private Texture2D _hoverTex;
    private Texture2D _activeTex;

    private GUIStyle _bannerTitle;
    private GUIStyle _cardBox;
    private GUIStyle _categoryDescStyle;
    private GUIStyle _testRowBox;
    private GUIStyle _wrapLabelStyle;
    private GUIStyle _categoryRowBtnStyle;
    private GUIStyle _testRowBtnStyle;

    [MenuItem("Tools/DevTools")]
    public static void ShowWindow()
    {
        var w = GetWindow<DevToolsWindow>("DevTools");
        w.minSize = new Vector2(360, 400);
    }

    private void OnEnable()
    {
        DebuggerCore.Register(this);
        testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
        testRunnerApi.RegisterCallbacks(this);
    }

    private void OnDisable()
    {
        DebuggerCore.Unregister(this);
        if (testRunnerApi != null) testRunnerApi.UnregisterCallbacks(this);

        if (_bannerTex != null) DestroyImmediate(_bannerTex);
        if (_hoverTex != null) DestroyImmediate(_hoverTex);
        if (_activeTex != null) DestroyImmediate(_activeTex);
    }

    private void OnFocus() => Repaint();

    private void OnGUI()
    {
        EnsureStyles();
        DrawTabs();

        if (!Application.isPlaying && currentTab != Tab.Tests)
        {
            EditorGUILayout.HelpBox("Инструменты геймплея доступны только в Play Mode.", MessageType.Info);
        }

        var refs = GameReferences.Instance;
        if (refs == null && Application.isPlaying && currentTab != Tab.Tests)
        {
            EditorGUILayout.HelpBox("GameReferences не найден. Возможно, сцена ещё не загрузилась.", MessageType.Warning);
        }

        switch (currentTab)
        {
            case Tab.Logs: DebuggerCore.Draw(position.width); break;
            case Tab.World: DrawWorldTab(refs); break;
            case Tab.Progress: DrawProgressTab(refs); break;
            case Tab.Tests: DrawTestsTab(position.width); break;
        }
    }

    private void EnsureStyles()
    {
        if (_bannerTex != null) return;
        if (Event.current == null) return;

        // Текстуры для фонов
        _bannerTex = MakeTex(2, 2, new Color(0.16f, 0.28f, 0.48f));
        _hoverTex = MakeTex(2, 2, new Color(0.3f, 0.3f, 0.3f, 0.4f)); // Полупрозрачный серый
        _activeTex = MakeTex(2, 2, new Color(0.2f, 0.4f, 0.6f, 0.5f)); // Полупрозрачный синий

        _bannerTitle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };
        _cardBox = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(10, 10, 8, 8), margin = new RectOffset(0, 0, 4, 4) };

        _categoryDescStyle = new GUIStyle(EditorStyles.label)
        {
            wordWrap = true,
            fontSize = 11,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
            margin = new RectOffset(20, 10, 2, 8)
        };

        _testRowBox = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(8, 8, 6, 6),
            margin = new RectOffset(10, 4, 2, 2)
        };

        _wrapLabelStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };

        // Стиль для кликабельной строки категории
        _categoryRowBtnStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            richText = true,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(4, 4, 4, 4)
        };
        _categoryRowBtnStyle.normal.textColor = Color.white;
        _categoryRowBtnStyle.hover.background = _hoverTex;
        _categoryRowBtnStyle.hover.textColor = Color.white;
        _categoryRowBtnStyle.active.background = _activeTex;
        _categoryRowBtnStyle.active.textColor = Color.white;

        // Стиль для кликабельного имени теста
        _testRowBtnStyle = new GUIStyle(EditorStyles.label)
        {
            richText = true,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(2, 2, 2, 2)
        };
        _testRowBtnStyle.normal.textColor = Color.white;
        _testRowBtnStyle.hover.background = _hoverTex;
        _testRowBtnStyle.hover.textColor = Color.white;
        _testRowBtnStyle.active.background = _activeTex;
        _testRowBtnStyle.active.textColor = Color.white;
    }

    private Texture2D MakeTex(int w, int h, Color col)
    {
        var pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        var t = new Texture2D(w, h); t.SetPixels(pix); t.Apply();
        return t;
    }

    private void DrawTabs()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        DrawTabButton("Логи", Tab.Logs);
        DrawTabButton("Мир", Tab.World);
        DrawTabButton("Прогресс", Tab.Progress);
        DrawTabButton("Тесты", Tab.Tests);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("🔄", EditorStyles.toolbarButton, GUILayout.Width(30)))
            Repaint();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTabButton(string label, Tab tab)
    {
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = currentTab == tab ? new Color(0.55f, 0.75f, 1f) : Color.white;
        if (GUILayout.Button(label, EditorStyles.toolbarButton, GUILayout.ExpandWidth(true))) currentTab = tab;
        GUI.backgroundColor = prev;
    }

    private void DrawWorldTab(GameReferences refs)
    {
        scrollPosWorld = EditorGUILayout.BeginScrollView(scrollPosWorld);

        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("📍 Телепорт по локациям", _bannerTitle);

        if (Application.isPlaying && refs != null)
        {
            var sst = refs.SceneSlideTransition;
            if (sst != null)
            {
                var scenes = sst.GetAllScenes();
                if (scenes != null && scenes.Count > 0)
                {
                    for (int i = 0; i < scenes.Count; i++)
                    {
                        if (scenes[i] != null && GUILayout.Button($"→ {scenes[i].name}"))
                            sst.SwitchToScene(scenes[i].name);
                    }
                }
                else EditorGUILayout.LabelField("Список сцен пуст.");
            }
            else EditorGUILayout.HelpBox("SceneSlideTransition не найден на сцене.", MessageType.Warning);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("🎒 Спавн предметов", _bannerTitle);

        if (Application.isPlaying && refs != null)
        {
            var db = refs.ItemDatabase;
            if (db != null)
            {
                var items = db.GetAllItems()?.Where(x => x != null && !string.IsNullOrEmpty(x.itemName)).ToList();
                if (items != null && items.Count > 0)
                {
                    string[] itemNames = items.Select(x => x.itemName).ToArray();
                    selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, itemNames.Length - 1);

                    EditorGUILayout.BeginHorizontal();
                    selectedItemIndex = EditorGUILayout.Popup(selectedItemIndex, itemNames);
                    itemCountToAdd = EditorGUILayout.IntField(itemCountToAdd, GUILayout.Width(50));
                    if (itemCountToAdd < 1) itemCountToAdd = 1;

                    if (GUILayout.Button("Выдать", GUILayout.Width(80)))
                    {
                        var inv = refs.InventoryUIManager;
                        if (inv != null)
                        {
                            int added = inv.AddItem(items[selectedItemIndex], itemCountToAdd);
                            Logger.Log(LogModule.UI, $"DevTools: Выдано {added}x {items[selectedItemIndex].itemName}");
                        }
                        else Logger.LogWarning(LogModule.UI, "DevTools: InventoryUIManager не найден!");
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else EditorGUILayout.HelpBox("ItemDatabase не найден.", MessageType.Warning);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
    }

    private void DrawProgressTab(GameReferences refs)
    {
        scrollPosProgress = EditorGUILayout.BeginScrollView(scrollPosProgress);

        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("📜 Активные квесты", _bannerTitle);

        if (Application.isPlaying && refs != null)
        {
            var qm = refs.QuestManager;
            if (qm != null)
            {
                var activeQuests = qm.GetActiveQuests();
                if (activeQuests != null && activeQuests.Count > 0)
                {
                    foreach (var quest in activeQuests.ToList())
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"{quest.QuestName} ({quest.GetProgressText()})", _wrapLabelStyle);

                        if (GUILayout.Button("Завершить", GUILayout.Width(90)))
                        {
                            qm.CompleteQuest(quest.QuestID);
                            GUIUtility.ExitGUI();
                            return;
                        }
                        if (GUILayout.Button("Отменить", GUILayout.Width(80)))
                        {
                            qm.CancelQuest(quest.QuestID);
                            GUIUtility.ExitGUI();
                            return;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else EditorGUILayout.LabelField("Нет активных квестов.");
            }
            else EditorGUILayout.HelpBox("QuestManager не найден.", MessageType.Warning);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(_cardBox);
        EditorGUILayout.LabelField("🚩 Флаги", _bannerTitle);

        if (Application.isPlaying && refs != null)
        {
            var fm = refs.FlagManager;
            if (fm != null)
            {
                var flags = fm.GetAllFlags();
                if (flags != null && flags.Count > 0)
                {
                    foreach (var kvp in flags.ToList())
                    {
                        bool newVal = EditorGUILayout.Toggle(kvp.Key, kvp.Value);
                        if (newVal != kvp.Value)
                        {
                            fm.SetFlag(kvp.Key, newVal);
                            GUIUtility.ExitGUI();
                            return;
                        }
                    }
                }
                else EditorGUILayout.LabelField("Нет активных флагов.");

                EditorGUILayout.BeginHorizontal();
                newFlagName = EditorGUILayout.TextField(newFlagName);
                if (GUILayout.Button("Добавить") && !string.IsNullOrEmpty(newFlagName))
                {
                    fm.SetFlag(newFlagName, true);
                    newFlagName = "";
                    GUIUtility.ExitGUI();
                    return;
                }
                EditorGUILayout.EndHorizontal();
            }
            else EditorGUILayout.HelpBox("FlagManager не найден.", MessageType.Warning);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
    }

    #region Tests Tab

    private void DrawTestsTab(float width)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUI.enabled = !testRunInProgress && !EditorApplication.isPlaying;

        if (GUILayout.Button("▶ EditMode", EditorStyles.toolbarButton)) StartTests(TestMode.EditMode);
        if (GUILayout.Button("▶ PlayMode", EditorStyles.toolbarButton)) StartTests(TestMode.PlayMode);

        if (GUILayout.Button("Очистить", EditorStyles.toolbarButton)) ClearResults();
        if (GUILayout.Button("Test Runner", EditorStyles.toolbarButton)) EditorApplication.ExecuteMenuItem("Window/General/Test Runner");

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Всего: {testsTotal}", EditorStyles.boldLabel, GUILayout.Width(80));
        EditorGUILayout.LabelField($"<color=#4CAF50>Прошло: {testsPassed}</color>", new GUIStyle(EditorStyles.boldLabel) { richText = true }, GUILayout.Width(100));
        EditorGUILayout.LabelField($"<color=#F44336>Провалено: {testsFailed}</color>", new GUIStyle(EditorStyles.boldLabel) { richText = true }, GUILayout.Width(110));
        EditorGUILayout.LabelField(testRunInProgress ? "Выполняется..." : "Готово", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        if (EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Тесты нельзя запускать в Play Mode.", MessageType.Warning);
            return;
        }

        if (loadedTestTree == null)
        {
            if (!isLoadingTestList)
            {
                EditorGUILayout.Space(10);
                if (GUILayout.Button("Загрузить список тестов", GUILayout.Height(30)))
                    LoadTestList(TestMode.EditMode);
            }
            else
            {
                EditorGUILayout.HelpBox("Загрузка списка...", MessageType.Info);
            }
            return;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Поиск:", GUILayout.Width(50));
        testSearchFilter = EditorGUILayout.TextField(testSearchFilter);
        if (GUILayout.Button("X", GUILayout.Width(24))) { testSearchFilter = ""; GUI.FocusControl(null); }
        if (GUILayout.Button("Развернуть", GUILayout.Width(90))) ExpandAll(loadedTestTree, true);
        if (GUILayout.Button("Свернуть", GUILayout.Width(80))) expandedCategories.Clear();
        EditorGUILayout.EndHorizontal();

        testTreeScroll = EditorGUILayout.BeginScrollView(testTreeScroll);

        foreach (var child in loadedTestTree.Children)
            DrawTestCategory(child, 0);

        EditorGUILayout.EndScrollView();
    }

    private void DrawTestCategory(ITestAdaptor node, int depth)
    {
        if (node == null) return;
        if (!string.IsNullOrEmpty(testSearchFilter) && !AnyChildMatches(node, testSearchFilter)) return;

        if (!node.HasChildren)
        {
            DrawSingleTestRow(node, depth);
            return;
        }

        if (depth == 0) EditorGUILayout.Space(10);

        string key = node.FullName ?? node.Name;
        bool expanded = expandedCategories.Contains(key) || !string.IsNullOrEmpty(testSearchFilter);

        var catInfo = FindCategoryInfo(node);
        string displayName = catInfo != null ? catInfo.DisplayName : node.Name;
        string description = catInfo != null ? catInfo.Description : "";

        int total = CountTests(node);
        int passed = CountStatus(node, TestStatus.Passed);
        int failed = CountStatus(node, TestStatus.Failed);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(depth * 12);

        string statusIcon = "○";
        string statusColor = "white";
        if (failed > 0) { statusIcon = "✗"; statusColor = "#F44336"; }
        else if (passed > 0 && passed == total) { statusIcon = "✓"; statusColor = "#4CAF50"; }

        string arrow = expanded ? "▼" : "▶";
        string label = $"<color={statusColor}>{statusIcon}</color> {arrow} {displayName}";

        // Кликабельная строка категории с эффектом наведения
        if (GUILayout.Button(label, _categoryRowBtnStyle, GUILayout.ExpandWidth(true), GUILayout.Height(22)))
        {
            if (expandedCategories.Contains(key)) expandedCategories.Remove(key);
            else expandedCategories.Add(key);
        }

        if (failed > 0) EditorGUILayout.LabelField($"<color=#F44336>{failed} fail</color>", new GUIStyle(EditorStyles.label) { richText = true }, GUILayout.Width(50));
        else if (passed > 0) EditorGUILayout.LabelField($"<color=#4CAF50>{passed}/{total}</color>", new GUIStyle(EditorStyles.label) { richText = true }, GUILayout.Width(50));
        else GUILayout.Space(50);

        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(description))
        {
            EditorGUILayout.LabelField(description, _categoryDescStyle, GUILayout.ExpandWidth(true));
        }

        if (expanded)
        {
            foreach (var child in node.Children)
                DrawTestCategory(child, depth + 1);
        }
        else
        {
            GUILayout.Space(5);
        }
    }

    private void DrawSingleTestRow(ITestAdaptor node, int depth)
    {
        if (!string.IsNullOrEmpty(testSearchFilter))
        {
            string searchIn = GetSearchText(node);
            if (!searchIn.ToLowerInvariant().Contains(testSearchFilter.ToLowerInvariant())) return;
        }

        var testInfo = FindTestInfo(node);
        string displayName = testInfo != null ? testInfo.DisplayName : node.Name;
        string description = testInfo != null ? testInfo.Description : "";
        string fullName = node.FullName ?? node.Name;

        TestResultData? result = testResults.TryGetValue(fullName, out var r) ? r : (TestResultData?)null;

        string icon = "•";
        string color = "white";
        if (result.HasValue)
        {
            switch (result.Value.Status)
            {
                case TestStatus.Passed: icon = "✓"; color = "#4CAF50"; break;
                case TestStatus.Failed: icon = "✗"; color = "#F44336"; break;
                case TestStatus.Skipped: icon = "○"; color = "#FFC107"; break;
                default: icon = "-"; break;
            }
        }

        EditorGUILayout.BeginVertical(_testRowBox);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("▶", EditorStyles.label, GUILayout.Width(20))) RunSingleTest(node);

        var iconStyle = new GUIStyle(EditorStyles.boldLabel) { richText = true, normal = { textColor = Color.white } };
        EditorGUILayout.LabelField($"<color={color}>{icon}</color>", iconStyle, GUILayout.Width(20));

        string infoKey = fullName;
        bool isInfoExpanded = expandedTestInfos.Contains(infoKey);

        // Если есть описание, название теста становится кликабельным
        if (!string.IsNullOrEmpty(description))
        {
            if (GUILayout.Button(displayName, _testRowBtnStyle, GUILayout.ExpandWidth(true)))
            {
                if (isInfoExpanded) expandedTestInfos.Remove(infoKey);
                else expandedTestInfos.Add(infoKey);
            }
        }
        else
        {
            EditorGUILayout.LabelField(displayName, _wrapLabelStyle, GUILayout.ExpandWidth(true));
        }

        if (result.HasValue)
        {
            EditorGUILayout.LabelField($"{result.Value.Duration:F2} c", EditorStyles.miniLabel, GUILayout.Width(45));
        }
        else
        {
            GUILayout.Space(45);
        }

        if (!string.IsNullOrEmpty(description))
        {
            if (GUILayout.Button("i", EditorStyles.miniButton, GUILayout.Width(18)))
            {
                if (isInfoExpanded) expandedTestInfos.Remove(infoKey);
                else expandedTestInfos.Add(infoKey);
            }
        }
        else
        {
            GUILayout.Space(18);
        }

        EditorGUILayout.EndHorizontal();

        if (isInfoExpanded && !string.IsNullOrEmpty(description))
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("<b>Что проверяет:</b>", new GUIStyle(EditorStyles.boldLabel) { richText = true });
            EditorGUILayout.LabelField(description, _wrapLabelStyle);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"<color=grey>Техническое имя: {fullName}</color>", _wrapLabelStyle);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();
    }

    private void ClearResults()
    {
        testsPassed = 0;
        testsFailed = 0;
        testResults.Clear();
        expandedTestInfos.Clear();
        Repaint();
    }

    private TestLocalization.TestInfo FindTestInfo(ITestAdaptor node)
    {
        if (node == null || TestLocalization.Tests == null) return null;
        string fullName = node.FullName ?? "";
        string name = node.Name ?? "";
        string className = ExtractClassName(fullName);
        string methodName = ExtractMethodName(name);

        string[] candidates = { fullName, name, $"{className}.{methodName}", $"{className}.{name}", methodName, name };
        foreach (var c in candidates)
        {
            if (string.IsNullOrEmpty(c)) continue;
            if (TestLocalization.Tests.TryGetValue(c, out var info)) return info;
        }

        foreach (var kvp in TestLocalization.Tests)
        {
            if (!string.IsNullOrEmpty(kvp.Key) && kvp.Key.EndsWith("." + methodName, System.StringComparison.Ordinal))
                return kvp.Value;
        }
        return null;
    }

    private TestLocalization.CategoryInfo FindCategoryInfo(ITestAdaptor node)
    {
        if (node == null || TestLocalization.Categories == null) return null;
        string fullName = node.FullName ?? "";
        string name = node.Name ?? "";

        string[] candidates = { name, ExtractClassName(fullName), fullName };
        foreach (var c in candidates)
        {
            if (string.IsNullOrEmpty(c)) continue;
            if (TestLocalization.Categories.TryGetValue(c, out var info)) return info;
        }
        foreach (var kvp in TestLocalization.Categories)
        {
            if (!string.IsNullOrEmpty(kvp.Key) && fullName.Contains(kvp.Key)) return kvp.Value;
        }
        return null;
    }

    private int CountStatus(ITestAdaptor node, TestStatus status)
    {
        if (node == null) return 0;
        if (!node.HasChildren)
        {
            if (testResults.TryGetValue(node.FullName, out var r) && r.Status == status) return 1;
            return 0;
        }
        int count = 0;
        foreach (var child in node.Children) count += CountStatus(child, status);
        return count;
    }

    private void ExpandAll(ITestAdaptor node, bool expand)
    {
        if (node == null || !node.HasChildren) return;
        string key = node.FullName ?? node.Name;
        if (expand) expandedCategories.Add(key);
        else expandedCategories.Remove(key);
        foreach (var child in node.Children) ExpandAll(child, expand);
    }

    private bool AnyChildMatches(ITestAdaptor node, string filter)
    {
        if (node.HasChildren)
        {
            foreach (var child in node.Children) if (AnyChildMatches(child, filter)) return true;
            return false;
        }
        string s = GetSearchText(node);
        return s.ToLowerInvariant().Contains(filter.ToLowerInvariant());
    }

    private int CountTests(ITestAdaptor node)
    {
        if (node == null) return 0;
        if (!node.HasChildren) return 1;
        int total = 0;
        foreach (var child in node.Children) total += CountTests(child);
        return total;
    }

    private string GetSearchText(ITestAdaptor node)
    {
        string name = node.Name ?? "";
        string fullName = node.FullName ?? "";
        var ti = FindTestInfo(node);
        if (ti != null) return $"{ti.DisplayName} {name} {fullName}";
        var ci = FindCategoryInfo(node);
        if (ci != null) return $"{ci.DisplayName} {name} {fullName}";
        return $"{name} {fullName}";
    }

    private string ExtractClassName(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return "";
        int lastDot = fullName.LastIndexOf('.');
        if (lastDot < 0) return fullName;
        string beforeLast = fullName.Substring(0, lastDot);
        int prevDot = beforeLast.LastIndexOf('.');
        if (prevDot < 0) return beforeLast;
        return beforeLast.Substring(prevDot + 1);
    }

    private string ExtractMethodName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        int paren = name.IndexOf('(');
        if (paren > 0) name = name.Substring(0, paren);
        int lastDot = name.LastIndexOf('.');
        if (lastDot >= 0) name = name.Substring(lastDot + 1);
        return name.Trim();
    }

    private void LoadTestList(TestMode mode)
    {
        if (testRunnerApi == null)
        {
            testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            testRunnerApi.RegisterCallbacks(this);
        }
        isLoadingTestList = true;
        testRunnerApi.RetrieveTestList(mode, OnTestListLoaded);
    }

    private void OnTestListLoaded(ITestAdaptor tests)
    {
        loadedTestTree = tests;
        testsTotal = CountTests(loadedTestTree);
        isLoadingTestList = false;
        Repaint();
    }

    private void RunSingleTest(ITestAdaptor test)
    {
        if (EditorApplication.isPlaying || test == null || testRunInProgress) return;
        if (testRunnerApi == null) testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();

        var filter = new Filter { testMode = TestMode.EditMode, testNames = new[] { test.FullName } };
        testRunnerApi.Execute(new ExecutionSettings(filter));
    }

    private void StartTests(TestMode mode)
    {
        if (EditorApplication.isPlaying || testRunnerApi == null) return;

        ClearResults();
        testRunInProgress = true;

        var filter = new Filter { testMode = mode };
        testRunnerApi.Execute(new ExecutionSettings(filter));
    }

    public void RunStarted(ITestAdaptor testsToRun) { Repaint(); }
    public void RunFinished(ITestResultAdaptor result) { testRunInProgress = false; Repaint(); }
    public void TestStarted(ITestAdaptor test) { }

    public void TestFinished(ITestResultAdaptor result)
    {
        if (!result.HasChildren)
        {
            testResults[result.Test.FullName] = new TestResultData
            {
                Status = result.TestStatus,
                Duration = result.Duration,
                Message = result.Message
            };

            if (result.TestStatus == TestStatus.Passed) testsPassed++;
            else if (result.TestStatus == TestStatus.Failed) testsFailed++;
        }
        Repaint();
    }

    #endregion
}
#endif