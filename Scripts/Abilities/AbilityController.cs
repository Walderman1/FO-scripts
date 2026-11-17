using UnityEngine;
using VContainer;

public class AbilityController : MonoBehaviour
{
    [Inject] private GameReferences _refs;

    private GameReferences Refs => _refs ?? GameReferences.Instance;

    private MagicLightAbility magicLight;
    private bool isMagicLightActive = false;

    public bool IsMagicLightActive => isMagicLightActive;

    public void Initialize()
    {
        Logger.LogVerbose(LogModule.Abilities, "AbilityController инициализирован");
    }

    public void UseAbility(string abilityName)
    {
        Refs?.AbilityManager?.TryActivate(abilityName);
    }

    public void DeactivateMagicLight()
    {
        if (magicLight != null && magicLight.IsActive)
            magicLight.Deactivate();
    }

    public void ResetState()
    {
        magicLight = null;
        isMagicLightActive = false;
    }

    private void Update()
    {
        if (magicLight == null)
            magicLight = Refs?.MagicLightAbility;

        isMagicLightActive = magicLight != null && magicLight.IsActive;
    }
}