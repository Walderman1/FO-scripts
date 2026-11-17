using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class MagicSpellPanel : MonoBehaviour
{
    [Header("Префаб кнопки")]
    [SerializeField] private string spellButtonPrefabPath = "UI/Prefab/Buttons/MagicSpellButton";

    [Header("Высота панели")]
    [SerializeField] private float firstBlockHeight = 100f;
    [SerializeField] private float heightPerBlock = 75f;
    [SerializeField] private int itemsPerBlock = 6;

    [Header("Контейнер")]
    [SerializeField] private Transform container;

    [Header("Поведение")]
    [SerializeField] private bool refreshOnStart = true;

    [Inject] private AbilityManager _abilityManager;

    private CanvasGroup canvasGroup;
    private GameObject spellButtonPrefab;
    private bool isOpen = false;
    private readonly List<GameObject> spawnedButtons = new();
    private readonly Stack<GameObject> buttonPool = new();

    public bool IsOpen => isOpen;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (container == null) container = transform;

        spellButtonPrefab = Resources.Load<GameObject>(spellButtonPrefabPath);
        if (spellButtonPrefab == null)
            Logger.LogError(LogModule.Abilities, $"Не найден префаб: Resources/{spellButtonPrefabPath}");

        SetVisible(false);
    }

    private void Start()
    {
        if (refreshOnStart) StartCoroutine(InitialRefresh());
    }

    private IEnumerator InitialRefresh()
    {
        yield return null;
        Refresh();
    }

    public void Toggle() => SetVisible(!isOpen);

    public void SetVisible(bool visible)
    {
        isOpen = visible;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = visible;
        }

        if (visible)
        {
            UICoordinator.BeforeOpen(UIKind.Spells);
            Refresh();
        }
    }

    public void Refresh()
    {
        ClearButtons();

        if (_abilityManager == null)
        {
            Logger.LogWarning(LogModule.Abilities, "AbilityManager не найден, панель магии пуста");
            ResizePanel(0);
            return;
        }

        if (spellButtonPrefab == null) { ResizePanel(0); return; }

        foreach (var ability in _abilityManager.GetRegisteredAbilities())
        {
            if (ability == null) continue;
            SpawnButton(ability);
        }

        ResizePanel(spawnedButtons.Count);
    }

    public bool IsPointerOverPanel(Vector2 screenPos)
    {
        if (!isOpen) return false;
        RectTransform rt = transform as RectTransform;
        if (rt == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, null);
    }

    private GameObject GetPooledButton()
    {
        if (buttonPool.Count > 0)
        {
            var pooled = buttonPool.Pop();
            if (pooled != null) { pooled.SetActive(true); return pooled; }
        }
        if (spellButtonPrefab == null) return null;
        return Instantiate(spellButtonPrefab, container);
    }

    private void ReturnButtonToPool(GameObject button)
    {
        if (button == null) return;
        button.SetActive(false);
        buttonPool.Push(button);
    }

    private void SpawnButton(Ability ability)
    {
        GameObject go = GetPooledButton();
        if (go == null) return;

        go.name = $"SpellButton_{ability.abilityName}";
        go.transform.SetParent(container, false);
        go.SetActive(true);

        MagicSpellButton btn = go.GetComponent<MagicSpellButton>();
        if (btn == null) btn = go.AddComponent<MagicSpellButton>();

        Ability captured = ability;
        btn.Initialize(captured, () => captured.TryActivate());

        spawnedButtons.Add(go);
    }

    private void ClearButtons()
    {
        foreach (var go in spawnedButtons)
            if (go != null) ReturnButtonToPool(go);
        spawnedButtons.Clear();
    }

    private void ResizePanel(int itemCount)
    {
        RectTransform rt = transform as RectTransform;
        if (rt == null) return;

        int perBlock = Mathf.Max(1, itemsPerBlock);
        int blocks = itemCount <= 0 ? 1 : Mathf.CeilToInt((float)itemCount / perBlock);
        if (blocks < 1) blocks = 1;

        float newHeight = firstBlockHeight + (blocks - 1) * heightPerBlock;

        Vector2 size = rt.sizeDelta;
        size.y = newHeight;
        rt.sizeDelta = size;
    }
}