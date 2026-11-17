using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class InventoryButtonPanel : MonoBehaviour
{
    [Header("Префабы кнопок")]
    [SerializeField] private GameObject questButtonPrefab;
    [SerializeField] private string questButtonPrefabPath = "UI/Prefab/Buttons/MiniPanelButtons/QuestButtonMini";

    [SerializeField] private GameObject craftButtonPrefab;
    [SerializeField] private string craftButtonPrefabPath = "UI/Prefab/Buttons/MiniPanelButtons/CraftButtonMini";

    [SerializeField] private GameObject magicSpellButtonPrefab;
    [SerializeField] private string magicSpellButtonPrefabPath = "UI/Prefab/Buttons/MiniPanelButtons/MagicSpellMini";

    private CanvasGroup canvasGroup;
    private GameObject questButtonInstance;
    private GameObject craftButtonInstance;
    private GameObject magicSpellButtonInstance;

    private void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        CreateQuestButton();
        CreateCraftButton();
        CreateMagicSpellButton();
    }

    private void CreateQuestButton()
    {
        if (questButtonPrefab == null)
            questButtonPrefab = Resources.Load<GameObject>(questButtonPrefabPath);
        if (questButtonPrefab == null)
        {
            Logger.LogWarning(LogModule.QuestSystem,
                $"QuestButtonMini не найден: Resources/{questButtonPrefabPath}");
            return;
        }

        questButtonInstance = Instantiate(questButtonPrefab, transform);
        questButtonInstance.name = "QuestButtonMini";

        var button = questButtonInstance.GetComponent<Button>();
        if (button == null) button = questButtonInstance.GetComponentInChildren<Button>();
        if (button != null) button.onClick.AddListener(OnQuestButtonClicked);
    }

    private void CreateCraftButton()
    {
        if (craftButtonPrefab == null)
            craftButtonPrefab = Resources.Load<GameObject>(craftButtonPrefabPath);
        if (craftButtonPrefab == null) return;

        craftButtonInstance = Instantiate(craftButtonPrefab, transform);
        craftButtonInstance.name = "CraftButtonMini";

        var button = craftButtonInstance.GetComponent<Button>();
        if (button == null) button = craftButtonInstance.GetComponentInChildren<Button>();
        if (button != null) button.onClick.AddListener(OnCraftButtonClicked);
    }

    private void CreateMagicSpellButton()
    {
        if (magicSpellButtonPrefab == null)
            magicSpellButtonPrefab = Resources.Load<GameObject>(magicSpellButtonPrefabPath);
        if (magicSpellButtonPrefab == null) return;

        magicSpellButtonInstance = Instantiate(magicSpellButtonPrefab, transform);
        magicSpellButtonInstance.name = "MagicSpellMini";

        var button = magicSpellButtonInstance.GetComponent<Button>();
        if (button == null) button = magicSpellButtonInstance.GetComponentInChildren<Button>();
        if (button != null) button.onClick.AddListener(OnMagicSpellButtonClicked);
    }

    private static T SceneResolve<T>() where T : class
    {
        var scope = SceneInjector.Instance?.CurrentSceneScope;
        if (scope == null) return null;
        try { return scope.Resolve<T>(); } catch { return null; }
    }

    private static T ResolveWithFallback<T>() where T : Component
    {
        var fromScope = SceneResolve<T>();
        if (fromScope != null) return fromScope;

        var refs = GameReferences.Instance;
        if (refs != null)
        {
            if (typeof(T) == typeof(QuestUI) && refs.QuestUI != null) return refs.QuestUI as T;
            if (typeof(T) == typeof(CraftingUI) && refs.CraftingUI != null) return refs.CraftingUI as T;
            if (typeof(T) == typeof(MagicSpellPanel) && refs.MagicSpellPanel != null) return refs.MagicSpellPanel as T;
            if (typeof(T) == typeof(InventoryUIManager) && refs.InventoryUIManager != null) return refs.InventoryUIManager as T;
        }

        return FindAnyObjectByType<T>(FindObjectsInactive.Include);
    }

    private void OnQuestButtonClicked()
    {
        var quests = ResolveWithFallback<QuestUI>();
        if (quests == null)
        {
            Logger.LogError(LogModule.QuestSystem,
                "OnQuestButtonClicked: QuestUI не найден ни через SceneInjector, ни через GameReferences, ни через FindAnyObjectByType");
            return;
        }

        if (quests.IsOpen) quests.CloseQuestPanel();
        else quests.OpenQuestPanel();
    }

    private void OnCraftButtonClicked()
    {
        var craft = ResolveWithFallback<CraftingUI>();
        if (craft == null)
        {
            Logger.LogError(LogModule.Inventory, "OnCraftButtonClicked: CraftingUI не найден");
            return;
        }

        if (craft.IsOpen)
        {
            craft.SetVisible(false);
            return;
        }

        ResolveWithFallback<MagicSpellPanel>()?.SetVisible(false);
        craft.SetVisible(true);
    }

    private void OnMagicSpellButtonClicked()
    {
        var spell = ResolveWithFallback<MagicSpellPanel>();
        if (spell == null)
        {
            Logger.LogError(LogModule.Abilities, "OnMagicSpellButtonClicked: MagicSpellPanel не найден");
            return;
        }

        if (spell.IsOpen)
        {
            spell.SetVisible(false);
            return;
        }

        ResolveWithFallback<CraftingUI>()?.SetVisible(false);
        spell.SetVisible(true);
    }

    public void SetVisible(bool visible)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }
}