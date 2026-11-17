using UnityEngine;

public abstract class Ability : MonoBehaviour
{
    [Header("Ability Settings")]
    public string abilityName = "New Ability";
    public float cooldown = 0f;
    public float energyCost = 0f;

    protected bool isOnCooldown = false;
    protected float currentCooldown = 0f;

    protected virtual void Update()
    {
        if (currentCooldown > 0f)
        {
            currentCooldown -= Time.deltaTime;
            if (currentCooldown <= 0f)
            {
                isOnCooldown = false;
                OnCooldownEnd();
            }
        }
    }

    protected virtual bool CanActivate()
    {
        return true;
    }

    public virtual void TryActivate()
    {
        if (isOnCooldown) return;
        if (!CanActivate()) return;
        Activate();
    }

    public abstract void Activate();

    protected virtual void OnCooldownEnd() { }

    protected void StartCooldown()
    {
        if (cooldown > 0f)
        {
            isOnCooldown = true;
            currentCooldown = cooldown;
            Logger.LogVerbose(LogModule.Abilities, $"Способность {abilityName} перешла в режим перезарядки на {cooldown} секунд");
        }
    }

    public bool IsOnCooldown => isOnCooldown;
}