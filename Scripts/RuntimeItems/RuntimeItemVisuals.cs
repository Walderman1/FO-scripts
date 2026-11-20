using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Создаёт во время игры аналоги префабов (UI и World) для предметов, у которых нет
/// ни одного ассета в проекте. Полученные объекты используются как обычные префабы:
/// ItemSO.uiPrefab / ItemSO.worldPrefab, а игра их просто Instantiate'ит.
///
/// UI-шаблон повторяет структуру обычных предметов (NullUIItem.prefab):
///   корень: RectTransform 100x100, localScale 0.2, InventoryItemMarker
///   дочерний Image «Icon»: sizeDelta = размер картинки в пикселях, localScale 1, preserveAspect
/// Поэтому при одинаковой картинке иконка получается ровно такого же размера,
/// как у предметов, созданных через редактор.
///
/// Шаблоны живут на выключенном объекте-контейнере, поэтому в сцене их не видно,
/// но при Instantiate копия получается активной (как обычный префаб).
/// </summary>
public static class RuntimeItemVisuals
{
    /// <summary>Масштаб корня UI-предмета — как у NullUIItem.prefab и всех обычных предметов.</summary>
    public const float UIRootScale = 0.2f;

    /// <summary>Размер корня UI-предмета — как у NullUIItem.prefab.</summary>
    private const float UIRootSize = 100f;

    /// <summary>
    /// Масштаб корня мирового предмета — как у NullWorldItem.prefab и всех обычных world-префабов.
    /// Итоговый размер в мире = размер картинки / 100 (ppu) * 0.2.
    /// </summary>
    public const float WorldRootScale = 0.2f;

    private static Transform _container;
    private static bool _instantiateSemanticsChecked;

    /// <summary>Запомненный uiSize каждого предмета (для обновления уже выданных иконок).</summary>
    private static readonly Dictionary<ItemSO, float> _uiSizes = new Dictionary<ItemSO, float>();

    public static Transform Container
    {
        get
        {
            EnsureContainer();
            return _container;
        }
    }

    private static void EnsureContainer()
    {
        if (_container != null) return;

        var go = new GameObject("[RuntimeItemTemplates]");
        Object.DontDestroyOnLoad(go);
        go.SetActive(false);
        _container = go.transform;
    }

    /// <summary>Создаёт UI- и World-шаблоны для предмета.</summary>
    public static void Build(ItemSO so, Sprite icon, float uiSize, Sprite worldSprite, float worldSize)
    {
        if (so == null) return;

        DestroyTemplates(so);

        _uiSizes[so] = uiSize;

        so.uiPrefab = CreateUITemplate(so, icon, uiSize);
        so.worldPrefab = CreateWorldTemplate(so, worldSprite, worldSize);

        VerifyInstantiateSemantics(so.uiPrefab);
    }

    /// <summary>
    /// Страховка: шаблоны лежат на выключенном контейнере, и мы рассчитываем, что Instantiate
    /// создаёт активную копию (activeSelf у шаблона = true). Если поведение когда-нибудь изменится —
    /// в логе появится ошибка, а не «невидимые предметы» без объяснения.
    /// </summary>
    private static void VerifyInstantiateSemantics(GameObject template)
    {
        if (_instantiateSemanticsChecked || template == null) return;
        _instantiateSemanticsChecked = true;

        var probe = Object.Instantiate(template);
        bool active = probe != null && probe.activeSelf;
        if (probe != null) Object.Destroy(probe);

        if (!active)
        {
            Logger.LogError(LogModule.Inventory,
                "RuntimeItemVisuals: копия шаблона предмета получилась выключенной — " +
                "иконки предметов из папки могут не отображаться. Сообщите программисту.");
        }
    }

    /// <summary>Удаляет ранее созданные шаблоны (перед пересборкой или при выгрузке).</summary>
    public static void DestroyTemplates(ItemSO so)
    {
        if (so == null) return;

        _uiSizes.Remove(so);

        if (so.uiPrefab != null)
        {
            Object.Destroy(so.uiPrefab);
            so.uiPrefab = null;
        }

        if (so.worldPrefab != null)
        {
            Object.Destroy(so.worldPrefab);
            so.worldPrefab = null;
        }
    }

    private static GameObject CreateUITemplate(ItemSO so, Sprite icon, float uiSize)
    {
        EnsureContainer();

        // Корень — сам предмет: RectTransform 100x100 со scale 0.2, как в NullUIItem.prefab.
        var root = new GameObject(so.id + "_UI", typeof(RectTransform));
        var rootRect = (RectTransform)root.transform;
        rootRect.SetParent(_container, false);
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(UIRootSize, UIRootSize);
        rootRect.localScale = new Vector3(UIRootScale, UIRootScale, UIRootScale);

        var marker = root.AddComponent<InventoryItemMarker>();
        marker.itemSO = so;
        marker.count = 1;

        // Иконка: sizeDelta = размер картинки в пикселях (как m_SizeDelta у Image
        // в обычных префабах), а корень сжимает её своим scale 0.2.
        Vector2 iconSize = GetIconSize(icon, uiSize);

        var iconGo = new GameObject("Icon", typeof(RectTransform));
        var iconRect = (RectTransform)iconGo.transform;
        iconRect.SetParent(rootRect, false);
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = iconSize;
        iconRect.localScale = Vector3.one;

        var image = iconGo.AddComponent<Image>();
        image.sprite = icon;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = true;

        return root;
    }

    /// <summary>
    /// Размер иконки в локальных координатах корня.
    /// uiSize &lt;= 0 — размер картинки как есть (обычное поведение предметов).
    /// uiSize &gt; 0 — принудительно вписать в квадрат uiSize пикселей экрана.
    /// </summary>
    private static Vector2 GetIconSize(Sprite icon, float uiSize)
    {
        float width = icon != null ? icon.rect.width : UIRootSize;
        float height = icon != null ? icon.rect.height : UIRootSize;

        if (uiSize <= 0.01f) return new Vector2(width, height);

        float target = uiSize / UIRootScale;
        float maxSide = Mathf.Max(width, height);
        if (maxSide <= 0.01f) return new Vector2(target, target);

        float scale = target / maxSide;
        return new Vector2(width * scale, height * scale);
    }

    private static GameObject CreateWorldTemplate(ItemSO so, Sprite sprite, float worldSize)
    {
        EnsureContainer();

        var root = new GameObject(so.id + "_World");
        var tr = root.transform;
        tr.SetParent(_container, false);
        tr.localScale = new Vector3(WorldRootScale, WorldRootScale, WorldRootScale);
        tr.localRotation = Quaternion.identity;

        var spriteRenderer = root.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Color.white;

        var box = root.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        if (sprite != null)
        {
            // Локальный размер = размер картинки / 100, как m_Size у обычных world-префабов;
            // корень сжимает его своим scale 0.2.
            box.size = (Vector2)sprite.bounds.size;
            box.offset = Vector2.zero;
        }
        else
        {
            float side = worldSize > 0.01f ? worldSize / WorldRootScale : 4f;
            box.size = new Vector2(side, side);
        }

        var pickup = root.AddComponent<PickupItem>();
        pickup.itemSO = so;

        return root;
    }

    /// <summary>
    /// Обновляет уже созданные иконки (например, если художник перерисовал PNG и нажал «Пересканировать»).
    /// </summary>
    public static void RefreshLiveInstances(ItemSO so)
    {
        if (so == null || so.icon == null) return;

        float uiSize = 0f;
        _uiSizes.TryGetValue(so, out uiSize);

        Vector2 iconSize = GetIconSize(so.icon, uiSize);

        var markers = Object.FindObjectsByType<InventoryItemMarker>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < markers.Length; i++)
        {
            var marker = markers[i];
            if (marker == null || marker.ItemSO != so) continue;

            var images = marker.GetComponentsInChildren<Image>(true);
            for (int j = 0; j < images.Length; j++)
            {
                var image = images[j];
                if (image == null) continue;

                image.sprite = so.icon;

                // Перерисовали картинку другого размера — обновляем и размер иконки.
                if (image.name == "Icon" && image.rectTransform != null)
                    image.rectTransform.sizeDelta = iconSize;
            }
        }
    }

    /// <summary>
    /// Пересоздаёт шаблоны после обновления картинки.
    /// </summary>
    public static void Rebuild(ItemSO so, Sprite icon, float uiSize, Sprite worldSprite, float worldSize)
    {
        Build(so, icon, uiSize, worldSprite, worldSize);
        RefreshLiveInstances(so);
    }

    /// <summary>Список шаблонов, которые нужно удалить при выгрузке системы.</summary>
    public static List<GameObject> GetTemplateObjects()
    {
        var result = new List<GameObject>();
        if (_container == null) return result;

        for (int i = 0; i < _container.childCount; i++)
        {
            var child = _container.GetChild(i);
            if (child != null) result.Add(child.gameObject);
        }

        return result;
    }
}
