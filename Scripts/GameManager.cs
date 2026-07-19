using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Reference")]
    private Dictionary<string, AudioClip> soundLibrary = new Dictionary<string, AudioClip>();
    [SerializeField] private AudioSource soundSource;

    public enum Sounds
    {
        Click, Magic
    }

    private void Awake()
    {
        // ✅ Если экземпляр уже существует - уничтожаем себя
        if (Instance != null && Instance != this)
        {
            Debug.Log("GameManager: Уничтожаем дубликат");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("GameManager: Создан синглтон");

        LoadSound();
    }

    public void PlaySound(string name)
    {
        if (soundLibrary.ContainsKey(name))
        {
            soundSource.clip = soundLibrary[name];
            soundSource.Play();
        }
        else
        {
            Debug.LogWarning($"Звук '{name}' не найден!");
        }
    }

    public void PlaySound(Sounds sound)
    {
        PlaySound(sound.ToString());
    }

    public void LoadSound()
    {
        soundLibrary.Clear();
        AudioClip[] allSounds = Resources.LoadAll<AudioClip>("Sounds");
        foreach (AudioClip clip in allSounds)
        {
            if (!soundLibrary.ContainsKey(clip.name))
            {
                soundLibrary.Add(clip.name, clip);
                Debug.Log($"Загружен звук: {clip.name}");
            }
        }
    }
}