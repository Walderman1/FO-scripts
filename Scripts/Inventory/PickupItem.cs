// PickupItem.cs (ПОЛНАЯ ВЕРСИЯ)
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
        // Загружаем данные из ItemSO
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
                Debug.LogWarning($"Hover effect not found at Resources/{hoverEffectPath}");
            }
        }

        if (!string.IsNullOrEmpty(pickupEffectPath))
        {
            pickupEffectPrefab = Resources.Load<GameObject>(pickupEffectPath);
            if (pickupEffectPrefab == null)
            {
                Debug.LogWarning($"Pickup effect not found at Resources/{pickupEffectPath}");
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
    }

    private void OnMouseDown()
    {
        if (isBeingPickedUp) return;
        PickUp();
    }

    public void PickUp()
    {
        if (isBeingPickedUp) return;
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

            // Отправляем событие в EventManager
            EventManager.Instance?.TriggerEvent(EventTriggerType.PickupItem,
                new EventContext().WithItem(itemType));

            StartCoroutine(FadeAndDestroy(0.2f));
        }
        else
        {
            Debug.LogError("InventoryUIManager not found!");
            isBeingPickedUp = false;
            this.enabled = true;
        }
    }

    // ✅ ===== ВОТ ЭТОТ МЕТОД БЫЛ ПРОПУЩЕН =====
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

        Destroy(gameObject);
    }
    // ========================================

    public void CancelPickup()
    {
        isBeingPickedUp = false;
        this.enabled = true;
    }
}