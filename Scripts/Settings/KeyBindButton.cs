using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyBindButton : MonoBehaviour
{
    public static bool IsRebinding { get; private set; } = false;

    private TextMeshProUGUI keyText;
    private Button button;
    private Image buttonImage;

    private KeyCode currentKey;
    private Action<KeyCode> onKeyChanged;
    private bool isListening = false;
    private bool waitingForRelease = false;

    private Color normalColor = new Color(0.2f, 0.2f, 0.2f);
    private Color listeningColor = new Color(0.3f, 0.5f, 0.8f);

    public KeyCode CurrentKey => currentKey;

    public void Initialize(TextMeshProUGUI text, Button btn, Image img,
                           KeyCode initialKey, Action<KeyCode> callback)
    {
        keyText = text;
        button = btn;
        buttonImage = img;
        currentKey = initialKey;
        onKeyChanged = callback;

        if (buttonImage != null)
            normalColor = buttonImage.color;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(StartListening);
        }

        RefreshDisplay();
    }

    public void SetKeySilent(KeyCode key)
    {
        currentKey = key;
        RefreshDisplay();
    }

    private void StartListening()
    {
        if (isListening) return;
        isListening = true;
        IsRebinding = true;
        waitingForRelease = true;

        if (keyText != null) keyText.text = "Нажмите...";
        if (buttonImage != null) buttonImage.color = listeningColor;
    }

    private void StopListening()
    {
        isListening = false;
        IsRebinding = false;
        waitingForRelease = false;
        if (buttonImage != null) buttonImage.color = normalColor;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (keyText != null) keyText.text = currentKey.ToString();
    }

    private void Update()
    {
        if (!isListening) return;

        if (waitingForRelease)
        {
            if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
                waitingForRelease = false;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StopListening();
            return;
        }

        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            if (key == KeyCode.None) continue;
            if (key == KeyCode.Mouse0 || key == KeyCode.Mouse1 || key == KeyCode.Mouse2) continue;
            if (key == KeyCode.Escape) continue;

            if (Input.GetKeyDown(key))
            {
                currentKey = key;
                onKeyChanged?.Invoke(key);
                StopListening();
                return;
            }
        }
    }

    private void OnDisable()
    {
        if (isListening)
        {
            StopListening();
        }
    }
}