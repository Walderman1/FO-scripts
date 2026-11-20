using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Точка входа системы предметов из картинок.
///
/// Создаётся автоматически при запуске игры (RuntimeInitializeOnLoadMethod), поэтому
/// в сценах и префабах ничего настраивать не нужно — работает и в редакторе, и в билде.
///
/// Что делает:
///  1. Сканирует папку ItemsInbox и превращает картинки в ItemSO.
///  2. Регистрирует их в ItemDatabase (появляются в инвентаре, крафте, сохранениях).
///  3. Раз в 1.5 секунды проверяет папку — новый PNG подхватывается без перезапуска.
///  4. Запускает внутриигровое окно DevTools (F1).
/// </summary>
[DefaultExecutionOrder(-9500)]
public class RuntimeItemLoader : MonoBehaviour
{
    public static RuntimeItemLoader Instance { get; private set; }

    /// <summary>Как часто проверять папку на изменения.</summary>
    private const float PollInterval = 1.5f;

    private float _nextPoll;
    private ItemDatabase _database;
    private readonly List<string> _registeredIds = new List<string>();

    public ItemDatabase Database { get { return _database; } }
    public int RegisteredCount { get { return _registeredIds.Count; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        var go = new GameObject("[RuntimeItems]");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<RuntimeItemLoader>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        RuntimeLogBuffer.EnsureHooked();
        InGameDevTools.EnsureCreated();

        RuntimeItemFolder.Scan(true);
        BindDatabase();
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextPoll) return;
        _nextPoll = Time.unscaledTime + PollInterval;

        if (RuntimeItemFolder.ScanIfChanged()) BindDatabase(true);
        else if (_database == null) BindDatabase(false);
    }

    /// <summary>Принудительно перечитать папку (вызывается из окна DevTools).</summary>
    public void Rescan()
    {
        RuntimeItemFolder.Scan(true);
        BindDatabase(true);
    }

    /// <summary>Статический вызов на случай, если экземпляр ещё не создан.</summary>
    public static void RescanNow()
    {
        if (Instance != null)
        {
            Instance.Rescan();
            return;
        }

        RuntimeItemFolder.Scan(true);
    }

    public void BindDatabase()
    {
        BindDatabase(false);
    }

    private void BindDatabase(bool verbose)
    {
        if (_database == null)
            _database = FindAnyObjectByType<ItemDatabase>(FindObjectsInactive.Include);

        if (_database == null) return;

        var items = RuntimeItemFolder.Items;
        int added = 0;
        int conflicts = 0;

        for (int i = 0; i < items.Count; i++)
        {
            var so = items[i];
            if (so == null || string.IsNullOrEmpty(so.id)) continue;

            bool isNew = !_registeredIds.Contains(so.id);

            if (_database.RegisterRuntimeItem(so))
            {
                if (isNew)
                {
                    _registeredIds.Add(so.id);
                    added++;
                }
            }
            else
            {
                conflicts++;
            }
        }

        if (added > 0 || verbose)
        {
            Logger.Log(LogModule.Inventory,
                $"Предметы из папки: +{added}, всего в базе {_database.GetAllItems().Count}" +
                (conflicts > 0 ? $", конфликтов id: {conflicts}" : ""));
        }
    }
}
