using UnityEngine;
using System.Collections;

public class PickupItem : MonoBehaviour
{
    public ItemType itemType = ItemType.None;

    [Header("Visual Feedback")]
    public Color highlightColor = Color.yellow;
    public float highlightScale = 1.15f;
    public float animationSpeed = 4f;

    [Header("Effects")]
    public string hoverEffectPath = "Particles/HoverEffect";
    public string pickupEffectPath = "Particles/PickupEffect";
    public AudioClip pickupSound;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector3 originalScale;
    private bool isHovered = false;
    private GameObject currentEffect;
    private AudioSource audioSource;
    private GameObject hoverEffectPrefab;
    private GameObject pickupEffectPrefab;
    private bool isBeingPickedUp = false;
    private ItemSO cachedItemData;

    private void Awake()
    {
        cachedItemData = ItemDatabase.Instance?.GetItem(itemType);
        if (cachedItemData != null)
        {
            ApplyItemData(cachedItemData);
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        originalScale = transform.localScale;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && pickupSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        LoadEffects();

        Logger.Log(LogModule.Inventory, $"Предмет {itemType} инициализирован в мире");
    }

    private void ApplyItemData(ItemSO data)
    {
        highlightColor = data.highlightColor;
        highlightScale = data.highlightScale;
        animationSpeed = data.animationSpeed;
        hoverEffectPath = data.hoverEffectPath;
        pickupEffectPath = data.pickupEffectPath;
        pickupSound = data.pickupSound;
    }

    private void LoadEffects()
    {
        if (!string.IsNullOrEmpty(hoverEffectPath))
        {
            hoverEffectPrefab = Resources.Load<GameObject>(hoverEffectPath);
            if (hoverEffectPrefab == null)
            {
                Logger.LogWarning(LogModule.Inventory, $"Эффект наведения не найден по пути: Resources/{hoverEffectPath}");
            }
        }

        if (!string.IsNullOrEmpty(pickupEffectPath))
        {
            pickupEffectPrefab = Resources.Load<GameObject>(pickupEffectPath);
            if (pickupEffectPrefab == null)
            {
                Logger.LogWarning(LogModule.Inventory, $"Эффект подбора не найден по пути: Resources/{pickupEffectPath}");
            }
        }
    }

    private void Update()
    {
        if (isBeingPickedUp) return;

        if (isHovered)
        {
            float pulse = 1f + Mathf.Sin(Time.time * animationSpeed) * 0.05f;
            transform.localScale = originalScale * highlightScale * pulse;

            if (spriteRenderer != null)
            {
                float colorLerp = Mathf.Sin(Time.time * animationSpeed) * 0.3f + 0.7f;
                spriteRenderer.color = Color.Lerp(originalColor, highlightColor, colorLerp);
            }
        }
    }

    private void OnMouseEnter()
    {
        if (isBeingPickedUp) return;

        isHovered = true;

        if (hoverEffectPrefab != null)
        {
            currentEffect = Instantiate(hoverEffectPrefab, transform.position, Quaternion.identity);
            currentEffect.transform.SetParent(transform);
            currentEffect.transform.localPosition = Vector3.zero;
            currentEffect.transform.localScale = Vector3.one;
        }

        Logger.Log(LogModule.Inventory, $"Наведение на предмет {itemType}");
    }

    private void OnMouseExit()
    {
        if (isBeingPickedUp) return;

        isHovered = false;

        if (currentEffect != null)
        {
            Destroy(currentEffect);
            currentEffect = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        transform.localScale = originalScale;

        Logger.Log(LogModule.Inventory, $"Убрано наведение с предмета {itemType}");
    }

    private void OnMouseDown()
    {
        if (isBeingPickedUp) return;
        PickUp();
    }

    public void PickUp()
    {
        if (isBeingPickedUp)
        {
            Logger.Log(LogModule.Inventory, $"Предмет {itemType} уже подбирается");
            return;
        }

        isBeingPickedUp = true;
        this.enabled = false;

        isHovered = false;
        if (currentEffect != null)
        {
            Destroy(currentEffect);
            currentEffect = null;
        }

        if (pickupEffectPrefab != null)
        {
            GameObject effect = Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }

        if (pickupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pickupSound);
        }

        InventoryUIManager inventory = FindFirstObjectByType<InventoryUIManager>();
        if (inventory != null)
        {
            inventory.AddItem(itemType);
            Logger.Log(LogModule.Inventory, $"Предмет {itemType} подобран и добавлен в инвентарь");

            EventManager.Instance?.TriggerEvent(EventTriggerType.PickupItem,
                new EventContext().WithItem(itemType));

            StartCoroutine(FadeAndDestroy(0.2f));
        }
        else
        {
            Logger.LogError(LogModule.Inventory, "InventoryUIManager не найден");
            isBeingPickedUp = false;
            this.enabled = true;
        }
    }

    private IEnumerator FadeAndDestroy(float duration)
    {
        float elapsed = 0f;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (spriteRenderer != null)
            {
                Color color = startColor;
                color.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = color;
            }

            float scaleMultiplier = 1f - t * 0.7f;
            transform.localScale = originalScale * scaleMultiplier;

            yield return null;
        }

        Logger.Log(LogModule.Inventory, $"Предмет {itemType} удалён из мира");
        Destroy(gameObject);
    }

    public void CancelPickup()
    {
        isBeingPickedUp = false;
        this.enabled = true;
        Logger.Log(LogModule.Inventory, $"Подбор предмета {itemType} отменён");
    }
}
