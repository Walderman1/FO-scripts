using UnityEngine;
using System.Collections.Generic;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance;

    [Header("Session Settings")]
    [SerializeField] private bool autoSaveOnQuit = true;
    [SerializeField] private string saveFileName = "game_save.dat";

    private string currentSessionID;
    private float sessionStartTime;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            StartNewSession();
        }
        else
        {
            Logger.Log(LogModule.Core, "Уничтожение дублирующего SessionManager");
            Destroy(gameObject);
        }
    }

    private void StartNewSession()
    {
        currentSessionID = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        sessionStartTime = Time.time;
        Logger.Log(LogModule.Core, $"Начата новая сессия: {currentSessionID}");
    }

    public string GetSessionID() => currentSessionID;

    public float GetSessionTime() => Time.time - sessionStartTime;

    public void SaveEventStates()
    {
        if (EventStateManager.Instance == null)
        {
            Logger.LogWarning(LogModule.Core, "EventStateManager не найден, сохранение состояний событий пропущено");
            return;
        }

        Logger.Log(LogModule.Core, $"Сохранены состояния событий для сессии: {currentSessionID}");
    }

    public void LoadEventStates()
    {
        if (EventStateManager.Instance == null)
        {
            Logger.LogWarning(LogModule.Core, "EventStateManager не найден, загрузка состояний событий пропущена");
            return;
        }

        Logger.Log(LogModule.Core, $"Загружены состояния событий для сессии: {currentSessionID}");
    }

    private void OnApplicationQuit()
    {
        if (autoSaveOnQuit)
        {
            SaveEventStates();
            Logger.Log(LogModule.Core, $"Сессия {currentSessionID} завершена, данные сохранены");
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && autoSaveOnQuit)
        {
            SaveEventStates();
            Logger.Log(LogModule.Core, $"Приложение приостановлено, данные сессии {currentSessionID} сохранены");
        }
    }
}
