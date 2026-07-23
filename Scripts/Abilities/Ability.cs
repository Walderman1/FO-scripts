using UnityEngine;

public abstract class Ability : MonoBehaviour
{
    [Header("Ability Settings")]
    public string abilityName = "New Ability";
    public KeyCode activationKey = KeyCode.None;
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

        if (Input.GetKeyDown(activationKey) && !isOnCooldown)
        {
            if (CanActivate())
            {
                Activate();
            }
        }
    }

    protected virtual bool CanActivate()
    {
        return true;
    }

    public abstract void Activate();

    protected virtual void OnCooldownEnd() { }

    protected void StartCooldown()
    {
        if (cooldown > 0f)
        {
            isOnCooldown = true;
            currentCooldown = cooldown;
            Logger.Log(LogModule.Abilities, $"Способность {abilityName} перешла в режим перезарядки на {cooldown} секунд");
        }
    }
}
