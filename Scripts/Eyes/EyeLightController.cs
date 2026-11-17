using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class EyeLightController : MonoBehaviour
{
    [Header("=== Свет ===")]
    [SerializeField] private float lightLevelSmoothing = 12f;
    [SerializeField] private bool reactToLight = true;
    [SerializeField] private bool autoFindLights = true;
    [SerializeField] private string lightTag = "";
    [SerializeField] private LayerMask lightLayer = -1;
    [SerializeField] private bool refreshLightsOnSceneLoad = true;
    [SerializeField] private List<Transform> lightSources = new List<Transform>();
    [Range(0.3f, 2f)][SerializeField] private float basePupilScale = 1f;
    [Range(0.01f, 1f)][SerializeField] private float minPupilScale = 0.5f;
    [Range(1f, 3f)][SerializeField] private float maxPupilScale = 1f;
    [SerializeField] private float pupilReactSpeed = 5f;
    [SerializeField] private float pupilReactDelay = 0.1f;
    [SerializeField] private float lightInfluenceRadius = 3f;
    [SerializeField] private float maxLightIntensity = 1f;

    [Header("=== Магический свет ===")]
    [SerializeField] private bool autoFindMagicLight = true;
    [SerializeField] private MagicLightAbility magicLightAbility;
    [SerializeField] private bool useMagicLightAsSource = true;

    private EyeController master;
    private Transform eyePupil;
    private Vector3 basePupilScaleVector = Vector3.one;

    private float currentPupilScale = 1f;
    private float targetPupilScale = 1f;
    private float lastLightChangeTime = 0f;
    private float pendingLightValue = 0f;
    private bool hasPendingLightChange = false;

    private float cachedLightLevelValue = 0.5f;
    private int cachedLightLevelFrame = -1;
    private float smoothedLightLevel = 0.5f;

    private List<Transform> activeLightSources = new List<Transform>();

    public int LightSourceCount => activeLightSources.Count;

    public void Init(EyeController masterRef, Transform pupil)
    {
        master = masterRef;
        eyePupil = pupil;
        if (eyePupil != null) basePupilScaleVector = eyePupil.localScale;

        if (lightLayer == -1)
        {
            int layer = LayerMask.NameToLayer("Light");
            if (layer != -1) lightLayer = 1 << layer;
        }

        currentPupilScale = basePupilScale;
        targetPupilScale = basePupilScale;
        cachedLightLevelFrame = -1;
        smoothedLightLevel = 0.5f;

        FindMagicLightAbility();
        FindAllLights();
    }

    public void Subscribe()
    {
        if (refreshLightsOnSceneLoad)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    public void Unsubscribe()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (refreshLightsOnSceneLoad && autoFindLights) FindAllLights();
        if (autoFindMagicLight) FindMagicLightAbility();
    }

    public void UpdatePupil()
    {
        if (!reactToLight || eyePupil == null) return;

        if (hasPendingLightChange)
        {
            if (Time.time - lastLightChangeTime >= pupilReactDelay)
            {
                targetPupilScale = Mathf.Lerp(minPupilScale, maxPupilScale, pendingLightValue);
                hasPendingLightChange = false;
            }
        }
        else
        {
            float l = GetLightLevel();
            float nt = Mathf.Lerp(minPupilScale, maxPupilScale, 1f - l);
            if (Mathf.Abs(nt - targetPupilScale) > 0.05f)
            {
                pendingLightValue = 1f - l;
                lastLightChangeTime = Time.time;
                hasPendingLightChange = true;
            }
        }

        float sp = currentPupilScale > targetPupilScale ? pupilReactSpeed * 2f : pupilReactSpeed * 0.5f;
        currentPupilScale = Mathf.Lerp(currentPupilScale, targetPupilScale, Time.deltaTime * sp);

        eyePupil.localScale = new Vector3(
            basePupilScaleVector.x * currentPupilScale,
            basePupilScaleVector.y * currentPupilScale,
            basePupilScaleVector.z * currentPupilScale);
    }

    public float GetLightLevel()
    {
        if (cachedLightLevelFrame != Time.frameCount)
        {
            cachedLightLevelValue = CalculateLightLevel();
            cachedLightLevelFrame = Time.frameCount;
        }
        if (lightLevelSmoothing > 0.01f)
        {
            smoothedLightLevel = Mathf.Lerp(smoothedLightLevel, cachedLightLevelValue, Time.deltaTime * lightLevelSmoothing);
            return smoothedLightLevel;
        }
        smoothedLightLevel = cachedLightLevelValue;
        return cachedLightLevelValue;
    }

    private float CalculateLightLevel()
    {
        float maxL = 0f;
        Vector3 c = master != null ? master.GetEyeCenterPosition() : transform.position;

        foreach (var lt in activeLightSources)
        {
            if (lt == null || !lt.gameObject.activeInHierarchy) continue;
            Light2D l2 = lt.GetComponent<Light2D>();
            if (l2 == null) continue;
            float d = Vector3.Distance(c, lt.position);
            float dF = Mathf.Clamp01(1f - d / lightInfluenceRadius);
            float iF = Mathf.Clamp01(l2.intensity / maxLightIntensity);
            maxL = Mathf.Max(maxL, dF * iF);
        }

        if (useMagicLightAsSource) maxL = Mathf.Max(maxL, CalculateMagicLightLevel());
        return Mathf.Clamp01(maxL);
    }

    private float CalculateMagicLightLevel()
    {
        if (magicLightAbility == null || !magicLightAbility.IsActive) return 0f;
        if (magicLightAbility.currentLightBeam == null) return 0f;

        Light2D ml = magicLightAbility.currentLightBeam.GetComponent<Light2D>();
        if (ml == null) ml = magicLightAbility.currentLightBeam.GetComponentInChildren<Light2D>();
        if (ml == null) return 0f;

        Vector3 c = master != null ? master.GetEyeCenterPosition() : transform.position;
        float d = Vector3.Distance(c, ml.transform.position);
        float dF = Mathf.Clamp01(1f - d / lightInfluenceRadius);
        float iF = Mathf.Clamp01(ml.intensity / maxLightIntensity);
        return dF * iF;
    }

    public void SetLightSource(Transform l)
    {
        activeLightSources.Clear();
        if (l != null && l.GetComponent<Light2D>() != null) activeLightSources.Add(l);
    }

    public void AddLightSource(Transform l)
    {
        if (l == null || l.GetComponent<Light2D>() == null) return;
        if (!activeLightSources.Contains(l)) activeLightSources.Add(l);
    }

    public void RemoveLightSource(Transform l)
    {
        if (l != null) activeLightSources.Remove(l);
    }

    [ContextMenu("Найти MagicLightAbility")]
    public void FindMagicLightAbility()
    {
        if (!autoFindMagicLight || magicLightAbility != null) return;

        magicLightAbility = GameReferences.Instance?.MagicLightAbility;
        if (magicLightAbility == null) magicLightAbility = FindAnyObjectByType<MagicLightAbility>();
    }

    [ContextMenu("Найти все Light2D")]
    public void FindAllLights()
    {
        if (!autoFindLights) return;
        activeLightSources.Clear();

        foreach (var l in FindObjectsByType<Light2D>())
        {
            if (l == null || !l.enabled || !l.gameObject.activeInHierarchy) continue;
            if (!string.IsNullOrEmpty(lightTag) && !l.CompareTag(lightTag)) continue;
            if (lightLayer.value != -1 && ((1 << l.gameObject.layer) & lightLayer.value) == 0) continue;
            activeLightSources.Add(l.transform);
        }

        foreach (var l in lightSources)
        {
            if (l == null || !l.gameObject.activeInHierarchy) continue;
            if (activeLightSources.Contains(l)) continue;
            if (l.GetComponent<Light2D>() == null) continue;
            activeLightSources.Add(l);
        }
    }
}