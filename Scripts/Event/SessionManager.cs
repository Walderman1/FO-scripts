// SessionManager.cs
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
            Destroy(gameObject);
        }
    }

    private void StartNewSession()
    {
        currentSessionID = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        sessionStartTime = Time.time;
        Debug.Log($"🆕 New session started: {currentSessionID}");
    }

    public string GetSessionID() => currentSessionID;

    public float GetSessionTime() => Time.time - sessionStartTime;

    // Сохранение состояния событий
    public void SaveEventStates()
    {
        if (EventStateManager.Instance == null) return;

        // Сохраняем состояния всех событий
        // Здесь можно реализовать сохранение в файл
        Debug.Log($"💾 Event states saved for session: {currentSessionID}");
    }

    // Загрузка состояния событий
    public void LoadEventStates()
    {
        if (EventStateManager.Instance == null) return;

        // Загружаем состояния событий
        Debug.Log($"📂 Event states loaded for session: {currentSessionID}");
    }

    private void OnApplicationQuit()
    {
        if (autoSaveOnQuit)
        {
            SaveEventStates();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && autoSaveOnQuit)
        {
            SaveEventStates();
        }
    }
}