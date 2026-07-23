using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MagicLightAbility : Ability
{
    [Header("Magic Light Settings")]
    [SerializeField] private GameObject lightBeamPrefab;
    [SerializeField] private float lightRadius = 2f;
    [SerializeField] private float lightIntensity = 1f;
    [SerializeField] private Color lightColor = Color.cyan;
    [SerializeField] private LayerMask detectLayers;

    [Header("Effects")]
    [SerializeField] private ParticleSystem lightParticles;
    [SerializeField] private AudioClip lightSound;
    [SerializeField] private bool enablePulse = false;
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 3f;
    [SerializeField] private float pulseInterval = 0.5f;
    [SerializeField] private GameObject detectedObjectIndicator;

    [Header("Trail")]
    [SerializeField] private ParticleSystem trailParticles;
    [SerializeField] private float trailInterval = 0.1f;

    [Header("Character Effects")]
    [SerializeField] private GameObject characterParticlesObject;

    public System.Action<bool> OnStateChanged;

    private GameObject currentLightBeam;
    private List<GameObject> detectedObjects = new List<GameObject>();
    private GameObject currentDetectedObject;
    private float lastPulseTime;
    private float lastTrailTime;
    private AudioSource audioSource;
    private Light2D pointLight2D;
    private bool isActive = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && lightSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        abilityName = "Magic Light";
        activationKey = KeyCode.L;
        cooldown = 0.5f;

        Logger.Log(LogModule.Abilities, "MagicLightAbility инициализирован");
    }

    private void Start()
    {
        if (characterParticlesObject == null)
        {
            characterParticlesObject = GameObject.FindGameObjectWithTag("CharacterParticles");
        }

        if (characterParticlesObject != null)
        {
            characterParticlesObject.SetActive(false);
            Logger.Log(LogModule.Abilities, "CharacterParticles найден и скрыт");
        }

        InvokeRepeating(nameof(SearchForParticles), 0f, 0.5f);
    }

    private void SearchForParticles()
    {
        if (characterParticlesObject != null) return;

        GameObject character = GameObject.FindGameObjectWithTag("Character");
        if (character != null)
        {
            characterParticlesObject = FindParticlesRecursive(character.transform);
            if (characterParticlesObject != null)
            {
                characterParticlesObject.SetActive(false);
                CancelInvoke(nameof(SearchForParticles));
                Logger.Log(LogModule.Abilities, "CharacterParticles найден и скрыт");
            }
        }
    }

    private GameObject FindParticlesRecursive(Transform parent)
    {
        if (parent.CompareTag("CharacterParticles"))
        {
            return parent.gameObject;
        }

        foreach (Transform child in parent)
        {
            GameObject result = FindParticlesRecursive(child);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public override void Activate()
    {
        if (isActive)
        {
            DeactivateLight();
        }
        else
        {
            ActivateLight();
        }

        StartCooldown();
    }

    private void ActivateLight()
    {
        isActive = true;

        if (lightBeamPrefab != null)
        {
            currentLightBeam = Instantiate(lightBeamPrefab, transform);
            currentLightBeam.transform.localPosition = Vector3.zero;

            pointLight2D = currentLightBeam.GetComponent<Light2D>();
            if (pointLight2D == null)
            {
                pointLight2D = currentLightBeam.GetComponentInChildren<Light2D>();
            }

            if (pointLight2D != null)
            {
                pointLight2D.intensity = lightIntensity;
                pointLight2D.color = lightColor;
                pointLight2D.pointLightOuterRadius = lightRadius;
            }
        }

        if (characterParticlesObject != null)
        {
            characterParticlesObject.SetActive(true);
        }

        if (lightParticles != null)
        {
            lightParticles.Play();
        }

        if (lightSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(lightSound);
        }

        Logger.Log(LogModule.Abilities, $"Магический свет активирован");

        OnStateChanged?.Invoke(true);
    }

    private void DeactivateLight()
    {
        isActive = false;

        if (currentLightBeam != null)
        {
            Destroy(currentLightBeam);
            currentLightBeam = null;
            pointLight2D = null;
        }

        if (characterParticlesObject != null)
        {
            characterParticlesObject.SetActive(false);
        }

        if (lightParticles != null)
        {
            lightParticles.Stop();
        }

        ClearDetectedObjects();

        Logger.Log(LogModule.Abilities, $"Магический свет деактивирован");

        OnStateChanged?.Invoke(false);
    }

    private void Update()
    {
        base.Update();

        if (!isActive) return;

        UpdateLightPosition();

        if (pointLight2D != null)
        {
            pointLight2D.intensity = lightIntensity;
            pointLight2D.color = lightColor;
            pointLight2D.pointLightOuterRadius = lightRadius;
        }

        if (enablePulse && pointLight2D != null)
        {
            float pulse = 0.7f + Mathf.Sin(Time.time * pulseSpeed) * 0.3f;
            pointLight2D.intensity = lightIntensity * pulse;
        }

        if (Time.time - lastTrailTime >= trailInterval)
        {
            lastTrailTime = Time.time;
            CreateTrail();
        }

        if (Time.time - lastPulseTime >= pulseInterval)
        {
            lastPulseTime = Time.time;
            ScanForObjects();
        }
    }

    private void UpdateLightPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
        worldPosition.z = 0;

        if (currentLightBeam != null)
        {
            currentLightBeam.transform.position = worldPosition;
        }
    }

    private void CreateTrail()
    {
        if (trailParticles != null)
        {
            Vector3 trailPosition = currentLightBeam != null ? currentLightBeam.transform.position : transform.position;

            ParticleSystem trail = Instantiate(trailParticles, trailPosition, Quaternion.identity);
            trail.Play();
            Destroy(trail.gameObject, 1f);
        }
    }

    private void ScanForObjects()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, detectLayers);

        ClearDetectedObjects();

        foreach (Collider2D hit in hits)
        {
            detectedObjects.Add(hit.gameObject);

            if (detectedObjectIndicator != null)
            {
                GameObject indicator = Instantiate(detectedObjectIndicator, hit.transform);
                indicator.transform.localPosition = Vector3.zero;
                Destroy(indicator, pulseInterval);
            }

            Logger.Log(LogModule.Abilities, $"Обнаружен объект: {hit.gameObject.name}");
            OnObjectDetected(hit.gameObject);
        }

        if (detectedObjects.Count > 0)
        {
            currentDetectedObject = detectedObjects[0];
        }
        else
        {
            currentDetectedObject = null;
        }
    }

    private void ClearDetectedObjects()
    {
        detectedObjects.Clear();
        currentDetectedObject = null;
    }

    protected virtual void OnObjectDetected(GameObject detected)
    {
        // Для наследников или ивентов
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, lightRadius);
    }

    public bool IsObjectDetected(GameObject target)
    {
        return detectedObjects.Contains(target);
    }

    public GameObject GetCurrentDetectedObject()
    {
        return currentDetectedObject;
    }

    public bool IsActive => isActive;
}
