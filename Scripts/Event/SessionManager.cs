using UnityEngine;
using VContainer;

[DefaultExecutionOrder(-9300)]
public class SessionManager : MonoBehaviour
{
    [Header("Session Settings")]
    [SerializeField] private bool autoSaveOnQuit = true;

    [Inject] private SaveSystem _saveSystem;

    private string currentSessionID;
    private float sessionStartTime;

    private void Awake()
    {
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        currentSessionID = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        sessionStartTime = Time.time;
    }

    public string GetSessionID() => currentSessionID;
    public float GetSessionTime() => Time.time - sessionStartTime;

    private void OnApplicationQuit()
    {
        if (!autoSaveOnQuit) return;

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 0) return;

        _saveSystem?.SaveGame(false);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus || !autoSaveOnQuit) return;

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 0) return;

        _saveSystem?.SaveGame(false);
    }
}