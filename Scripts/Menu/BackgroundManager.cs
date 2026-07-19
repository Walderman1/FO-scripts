using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BackgroundManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MenuUIConfig config;
    [SerializeField] private Transform parentTransform;

    private GameObject currentBackground;
    private GameObject mainBackground;
    private GameObject settingsBackground;
    private GameObject memoriesBackground;

    private bool isBackgroundAnimating = false;
    private bool isInitialized = false;

    public System.Action<GameObject> OnBackgroundChanged;

    public void Initialize(MenuUIConfig config, Transform parent)
    {
        this.config = config;
        this.parentTransform = parent;
        isInitialized = true;
    }

    public void CreateBackgrounds()
    {
        if (!isInitialized)
        {
            Debug.LogError("BackgroundManager not initialized!");
            return;
        }

        GameObject[] allBg = Resources.LoadAll<GameObject>(config.backgroundsFolder);
        List<GameObject> menuBg = allBg.Where(b => b.name.StartsWith("MenuBackground")).ToList();

        GameObject randomBg = menuBg.Count > 0 ? menuBg[Random.Range(0, menuBg.Count)] : null;
        GameObject settingsBg = allBg.FirstOrDefault(b => b.name == "SettingBackground");

        GameObject memoriesBgPrefab = Resources.Load<GameObject>(config.memoriesBackgroundPath);

        mainBackground = CreateBackground("MainBackground", randomBg, Vector2.zero);
        currentBackground = mainBackground;

        settingsBackground = CreateBackground("SettingsBackground", settingsBg ?? randomBg, new Vector2(config.backgroundOffset, 0));
        settingsBackground.SetActive(false);

        memoriesBackground = CreateBackground("MemoriesBackground", memoriesBgPrefab, new Vector2(-config.backgroundOffset, 0));
        memoriesBackground.SetActive(false);
    }

    private GameObject CreateBackground(string name, GameObject prefab, Vector2 startPos)
    {
        GameObject bg = prefab != null ? Instantiate(prefab, parentTransform) : CreateFallbackBackground(name);
        bg.name = name;
        bg.transform.position = new Vector3(startPos.x / 100f, startPos.y / 100f, 10f);

        SpriteRenderer sr = bg.GetComponent<SpriteRenderer>();
        if (sr != null) FitSpriteToScreen(sr);

        return bg;
    }

    private GameObject CreateFallbackBackground(string name)
    {
        GameObject bg = new GameObject(name);
        bg.transform.SetParent(parentTransform);

        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.15f));
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        sr.sortingOrder = -100;

        return bg;
    }

    private void FitSpriteToScreen(SpriteRenderer sr)
    {
        if (sr.sprite == null || Camera.main == null) return;

        float height = Camera.main.orthographicSize * 2f;
        float width = height * Camera.main.aspect;
        float scale = Mathf.Max(width / sr.sprite.bounds.size.x, height / sr.sprite.bounds.size.y);

        sr.transform.localScale = new Vector3(scale, scale, 1f);
        sr.transform.position = new Vector3(0, 0, 10f);
    }

    public void ShowBackground(GameObject bg)
    {
        if (bg == null) return;
        HideAllBackgrounds();
        bg.SetActive(true);
        bg.transform.position = Vector3.zero;
        currentBackground = bg;
        OnBackgroundChanged?.Invoke(bg);
    }

    public void HideAllBackgrounds()
    {
        if (mainBackground != null) mainBackground.SetActive(false);
        if (settingsBackground != null) settingsBackground.SetActive(false);
        if (memoriesBackground != null) memoriesBackground.SetActive(false);
    }

    public IEnumerator AnimateBackgrounds(GameObject oldBg, GameObject newBg, Vector2 direction, Vector2 newStart)
    {
        if (isBackgroundAnimating || oldBg == null) yield break;
        isBackgroundAnimating = true;

        if (!oldBg.activeSelf) oldBg.SetActive(true);

        if (newBg != null)
        {
            newBg.SetActive(true);
            newBg.transform.position = new Vector3(newStart.x / 100f, newStart.y / 100f, 10f);
        }

        Vector3 oldStart = oldBg.transform.position;
        Vector3 oldEnd = oldStart + new Vector3(direction.x * config.backgroundOffset / 100f, direction.y * config.backgroundOffset / 100f, 0);
        Vector3 newStartPos = newBg != null ? newBg.transform.position : Vector3.zero;

        float elapsed = 0f;
        while (elapsed < config.backgroundTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / config.backgroundTransitionDuration;
            float smooth = t * t * (3f - 2f * t);

            oldBg.transform.position = Vector3.Lerp(oldStart, oldEnd, smooth);
            if (newBg != null) newBg.transform.position = Vector3.Lerp(newStartPos, Vector3.zero, smooth);

            yield return null;
        }

        oldBg.transform.position = oldEnd;
        if (newBg != null) newBg.transform.position = Vector3.zero;

        oldBg.SetActive(false);
        if (newBg != null)
        {
            currentBackground = newBg;
            OnBackgroundChanged?.Invoke(newBg);
        }

        isBackgroundAnimating = false;
    }

    public GameObject GetMainBackground() => mainBackground;
    public GameObject GetSettingsBackground() => settingsBackground;
    public GameObject GetMemoriesBackground() => memoriesBackground;
    public GameObject GetCurrentBackground() => currentBackground;
    public bool IsAnimating() => isBackgroundAnimating;

    public void Cleanup()
    {
        if (mainBackground != null) Destroy(mainBackground);
        if (settingsBackground != null) Destroy(settingsBackground);
        if (memoriesBackground != null) Destroy(memoriesBackground);
    }
}