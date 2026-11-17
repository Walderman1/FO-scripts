using System.Collections;
using UnityEngine;
using VContainer;

public class ClairvoyanceAbility : Ability
{
    [Header("Clairvoyance Settings")]
    [SerializeField] private float particleDuration = 2f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem castParticles;
    [SerializeField] private AudioClip castSound;
    [SerializeField] private AudioClip deactivateSound;

    [Inject] private GameReferences _refs;
    [Inject] private AbilityManager _abilityManager;

    private GameReferences Refs => _refs ?? GameReferences.Instance;
    private AbilityManager Abilities => _abilityManager ?? Refs?.AbilityManager;

    public System.Action<bool> OnStateChanged;

    private AudioSource audioSource;
    private bool isActive = false;
    private Coroutine particlesCoroutine;

    public bool IsActive => isActive;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (castSound != null || deactivateSound != null))
            audioSource = gameObject.AddComponent<AudioSource>();

        abilityName = "Clairvoyance";
        cooldown = 0.5f;

        Logger.LogVerbose(LogModule.Abilities, "ClairvoyanceAbility инициализирован");
    }

    public override void Activate()
    {
        if (isActive) Deactivate();
        else ActivateClairvoyance();
        StartCooldown();
    }

    private void ActivateClairvoyance()
    {
        isActive = true;
        if (castParticles != null) castParticles.Play();
        if (castSound != null && audioSource != null) audioSource.PlayOneShot(castSound);
        TriggerParticlesBurst();
        Logger.Log(LogModule.Abilities, $"Ясновидение активировано");
        OnStateChanged?.Invoke(true);
    }

    private void Deactivate()
    {
        if (!isActive) return;
        isActive = false;
        if (castParticles != null) castParticles.Stop();
        if (deactivateSound != null && audioSource != null) audioSource.PlayOneShot(deactivateSound);
        TriggerParticlesBurst();
        Logger.Log(LogModule.Abilities, "Ясновидение деактивировано");
        OnStateChanged?.Invoke(false);
    }

    private void TriggerParticlesBurst()
    {
        var abilities = Abilities;
        if (abilities == null) return;
        abilities.SetCharacterParticlesActive(this, true);

        if (particlesCoroutine != null) StopCoroutine(particlesCoroutine);
        particlesCoroutine = StartCoroutine(ParticlesTimer(particleDuration));
    }

    private IEnumerator ParticlesTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
        Abilities?.SetCharacterParticlesActive(this, false);
        particlesCoroutine = null;
    }

    private void OnDestroy()
    {
        if (particlesCoroutine != null)
        {
            StopCoroutine(particlesCoroutine);
            particlesCoroutine = null;
        }
        Abilities?.SetCharacterParticlesActive(this, false);
    }
}