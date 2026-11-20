using DialogueSystem;
using System.Collections;
using UnityEngine;
using VContainer;

public class PickupItem : MonoBehaviour
{
    [Header("Save System")]
    [SerializeField] private string uniqueSaveID;
    public string UniqueSaveID => uniqueSaveID;

    public ItemSO itemSO;

    [System.Obsolete("Используйте itemSO")]
    public ItemType itemType = ItemType.None;

    [Header("Stack Count")]
    [SerializeField] private int count = 1;

    [Header("Visual Feedback")]
    public Color highlightColor = Color.yellow;
    public float highlightScale = 1.15f;
    public float animationSpeed = 4f;
    [SerializeField] private float dragLiftHeight = 1.5f;

    [Header("Effects")]
    public string hoverEffectPath = "Particles/HoverEffect";
    public string pickupEffectPath = "Particles/PickupEffect";
    public AudioClip pickupSound;

    [Header("Beam")]
    [SerializeField] private bool enableBeam = true;

    [Inject] private ItemDatabase _itemDatabase;
    [Inject] private InventoryUIManager _inventoryUIManager;
    [Inject] private EventManager _eventManager;
    [Inject] private AbilityManager _abilityManager;
    [Inject] private TextBeginner _textBeginner;
    [Inject] private DialogueCharacterManager _dialogueCharacterManager;
    [Inject] private TooltipManager _tooltipManager;
    [Inject] private WorldItemTracker _worldItemTracker;
    [Inject] private PickupVisualsManager _pickupVisuals;
    [Inject] private RadialMenuController _radialMenuController;

    private PickupVisualsManager Visuals => _pickupVisuals;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector3 originalScale;
    private bool isHovered = false;
    private GameObject currentEffect;
    private AudioSource audioSource;
    private GameObject hoverEffectPrefab;
    private GameObject pickupEffectPrefab;
    private bool isBeingPickedUp = false;

    private bool isDragging = false;
    private bool isClicking = false;
    private Vector3 dragOffset;
    private Vector3 targetDragPos;
    private float currentDragLift = 0f;

    private float spriteBottomOffset;
    private Transform characterParticles;
    private Camera cachedCamera;

    public int Count => count;

    public static System.Action<PickupItem> OnPickedUp;

    public ItemSO ItemSO
    {
        get
        {
            if (itemSO != null) return itemSO;

#pragma warning disable CS0618
            if (itemType != ItemType.None)
                itemSO = _itemDatabase?.GetItemByLegacyType(itemType);
#pragma warning restore CS0618

            return itemSO;
        }
    }

    public void SetCount(int newCount) => count = Mathf.Max(1, newCount);

    public void SetUniqueId(string id)
    {
        if (!string.IsNullOrEmpty(id)) uniqueSaveID = id;
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueSaveID))
            uniqueSaveID = System.Guid.NewGuid().ToString();
    }

    [ContextMenu("Сгенерировать новый ID")]
    public void GenerateNewID()
    {
        uniqueSaveID = System.Guid.NewGuid().ToString();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        originalScale = transform.localScale;
        audioSource = GetComponent<AudioSource>();
        cachedCamera = Camera.main;

        if (spriteRenderer != null && spriteRenderer.sprite != null)
            spriteBottomOffset = spriteRenderer.sprite.bounds.min.y * originalScale.y;
    }

    private static bool IsValidSceneObject(GameObject go)
    {
        if (go == null) return false;
        if (!go.scene.IsValid()) return false;
        if (!go.scene.isLoaded) return false;
#if UNITY_EDITOR
        if (UnityEditor.EditorUtility.IsPersistent(go)) return false;
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(go)) return false;
#endif
        return true;
    }

    private Transform FindParticlesReliably()
    {
        if (characterParticles != null && IsValidSceneObject(characterParticles.gameObject))
            return characterParticles;

        characterParticles = null;

        if (_abilityManager != null)
        {
            var go = _abilityManager.CharacterParticlesObject;
            if (go != null && IsValidSceneObject(go))
            {
                characterParticles = go.transform;
                return characterParticles;
            }
        }

        try
        {
            var go = GameObject.FindWithTag("CharacterParticles");
            if (go != null && IsValidSceneObject(go))
            {
                characterParticles = go.transform;
                return characterParticles;
            }
        }
        catch (UnityException) { }

        var current = _dialogueCharacterManager?.CurrentCharacter;
        if (current != null)
        {
            var all = current.GetComponentsInChildren<Transform>(true);
            int len = all.Length;
            for (int i = 0; i < len; i++)
            {
                var t = all[i];
                if (t == null) continue;
                if (t.name.IndexOf("Particle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    characterParticles = t;
                    return characterParticles;
                }

                try
                {
                    if (t.CompareTag("CharacterParticles"))
                    {
                        characterParticles = t;
                        return characterParticles;
                    }
                }
                catch (UnityException) { }
            }
        }

        return null;
    }

    private void Start()
    {
        var so = ItemSO;
        if (so != null)
        {
            highlightColor = so.highlightColor;
            highlightScale = so.highlightScale;
            animationSpeed = so.animationSpeed;
            hoverEffectPath = so.hoverEffectPath;
            pickupEffectPath = so.pickupEffectPath;
            pickupSound = so.pickupSound;
        }

        if (audioSource == null && pickupSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (!string.IsNullOrEmpty(hoverEffectPath))
            hoverEffectPrefab = Resources.Load<GameObject>(hoverEffectPath);

        if (!string.IsNullOrEmpty(pickupEffectPath))
            pickupEffectPrefab = Resources.Load<GameObject>(pickupEffectPath);
    }

    private void SetCharacterParticlesActive(bool active)
    {
        _abilityManager?.SetCharacterParticlesActiveExternal(this, active);
    }

    private void UpdateBeamPosition()
    {
        if (!enableBeam) return;
        var manager = Visuals;
        if (manager == null) return;

        Vector3 startPos = characterParticles != null ? characterParticles.position : transform.position;
        manager.UpdateBeam(startPos, transform.position);
    }

    private void Update()
    {
        if (isBeingPickedUp) return;

        if (isDragging)
        {
            currentDragLift = Mathf.Lerp(currentDragLift, dragLiftHeight, Time.deltaTime * 6f);

            Vector3 mousePos = GetMouseWorldPos();
            targetDragPos = new Vector3(mousePos.x + dragOffset.x, mousePos.y + currentDragLift + dragOffset.y, mousePos.z);

            transform.position = Vector3.Lerp(transform.position, targetDragPos, Time.deltaTime * 20f);
            UpdateBeamPosition();

            Visuals?.UpdateDropIndicator(new Vector3(mousePos.x + dragOffset.x, mousePos.y + dragOffset.y, mousePos.z));
            return;
        }

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
        if (isBeingPickedUp || isDragging) return;
        isHovered = true;

        if (hoverEffectPrefab != null)
        {
            currentEffect = Instantiate(hoverEffectPrefab, transform.position, Quaternion.identity);
            currentEffect.transform.SetParent(transform);
            currentEffect.transform.localPosition = Vector3.zero;
        }
    }

    private void OnMouseExit()
    {
        if (isBeingPickedUp || isDragging) return;
        isHovered = false;

        if (currentEffect != null) { Destroy(currentEffect); currentEffect = null; }
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        transform.localScale = originalScale;
    }

    private void OnMouseDown()
    {
        if (isBeingPickedUp) return;
        if (IsBlockedByUI()) return;

        isClicking = true;
        isDragging = false;
        dragOffset = transform.position - GetMouseWorldPos();
    }

    private void OnMouseDrag()
    {
        if (!isClicking || isBeingPickedUp) return;

        if (!isDragging)
        {
            if (Vector3.Distance(transform.position, GetMouseWorldPos() + dragOffset) > 0.1f)
            {
                isDragging = true;
                isHovered = false;
                currentDragLift = 0f;
                if (currentEffect != null) Destroy(currentEffect);
                if (spriteRenderer != null) spriteRenderer.color = originalColor;

                transform.localScale = originalScale * 1.3f;

                var particles = FindParticlesReliably();
                Vector3 beamStart = particles != null ? particles.position : transform.position;

                var manager = Visuals;
                if (enableBeam && manager != null)
                    manager.ShowBeam(beamStart, transform.position);

                if (manager != null)
                {
                    Vector3 mousePos = GetMouseWorldPos();
                    manager.ShowDropIndicator(new Vector3(mousePos.x + dragOffset.x, mousePos.y + dragOffset.y, mousePos.z));
                }

                SetCharacterParticlesActive(true);
                StartEyeTracking();
            }
        }
    }

    private void OnMouseUp()
    {
        if (!isClicking || isBeingPickedUp) return;
        isClicking = false;

        Visuals?.HideDropIndicator();

        if (isDragging)
        {
            isDragging = false;
            Vector3 targetPos = GetMouseWorldPos() + dragOffset;
            StartCoroutine(PlaceDownRoutine(targetPos));
        }
        else
        {
            SetCharacterParticlesActive(false);
            PickUp();
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        if (cachedCamera == null) cachedCamera = Camera.main;
        if (cachedCamera == null) return Vector3.zero;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;
        Vector3 worldPos = cachedCamera.ScreenToWorldPoint(mousePos);
        worldPos.z = 0f;
        return worldPos;
    }

    private IEnumerator PlaceDownRoutine(Vector3 targetPos)
    {
        Vector3 finalPos = new Vector3(targetPos.x, targetPos.y - spriteBottomOffset, targetPos.z);
        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        float duration = 0.15f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smooth = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(startPos, finalPos, smooth);
            transform.localScale = Vector3.Lerp(startScale, originalScale, smooth);

            UpdateBeamPosition();
            yield return null;
        }

        transform.position = finalPos;
        transform.localScale = originalScale;

        if (enableBeam) Visuals?.HideBeam();
        SetCharacterParticlesActive(false);
        StopEyeTracking();

        var currentLocation = _textBeginner?.CurrentLocation;
        string locationName = currentLocation != null ? currentLocation.name : "";
        Vector3 localPos = currentLocation != null
            ? currentLocation.transform.InverseTransformPoint(transform.position)
            : transform.position;

        _worldItemTracker?.RegisterMovedItem(UniqueSaveID, locationName, localPos);
    }

    public void AnimateDrop(Vector3 targetPos, System.Action onLanded)
    {
        StartCoroutine(DropRoutine(targetPos, onLanded));
    }

    private IEnumerator DropRoutine(Vector3 targetPos, System.Action onLanded)
    {
        Vector3 startPos = targetPos + new Vector3(0f, 1f, 0f);
        transform.position = startPos;

        Vector3 startScale = originalScale * 1.2f;
        transform.localScale = startScale;

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smooth = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(startPos, targetPos, smooth);
            transform.localScale = Vector3.Lerp(startScale, originalScale, smooth);

            UpdateBeamPosition();
            yield return null;
        }

        transform.position = targetPos;
        transform.localScale = originalScale;

        if (enableBeam) Visuals?.HideBeam();

        onLanded?.Invoke();
    }

    private bool IsBlockedByUI()
    {
        if (InGameDevTools.BlocksGameplayInput) return true;
        if (_textBeginner != null && _textBeginner.IsDialogueActive) return true;
        if (_radialMenuController != null && _radialMenuController.IsAnyMenuOpen()) return true;
        return false;
    }

    public void PickUp()
    {
        if (isBeingPickedUp) return;
        if (_inventoryUIManager == null) return;

        var so = ItemSO;
        if (so == null) return;

        int freeSpace = _inventoryUIManager.GetFreeSpaceFor(so, count);
        if (freeSpace < 1)
        {
            _tooltipManager?.ShowTooltip("Инвентарь полон", Input.mousePosition);
            return;
        }

        int toTake = Mathf.Min(count, freeSpace);
        int added = _inventoryUIManager.AddItem(so, toTake);

        if (added < 1) return;

        if (added < count)
        {
            count -= added;
            return;
        }

        isBeingPickedUp = true;
        this.enabled = false;
        isHovered = false;
        if (currentEffect != null) { Destroy(currentEffect); currentEffect = null; }

        if (enableBeam) Visuals?.HideBeam();
        SetCharacterParticlesActive(false);

        if (pickupEffectPrefab != null)
        {
            GameObject effect = Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }

        if (pickupSound != null && audioSource != null)
            audioSource.PlayOneShot(pickupSound);

        _eventManager?.TriggerEvent(EventTriggerType.PickupItem, new EventContext().WithItemId(so.id));

        OnPickedUp?.Invoke(this);

        StartCoroutine(FadeAndDeactivate(0.2f));
    }

    private IEnumerator FadeAndDeactivate(float duration)
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

            transform.localScale = originalScale * (1f - t * 0.7f);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    public void ResetState()
    {
        isBeingPickedUp = false;
        this.enabled = true;
        isHovered = false;
        isDragging = false;
        isClicking = false;
        currentDragLift = 0f;

        if (currentEffect != null) { Destroy(currentEffect); currentEffect = null; }
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        transform.localScale = originalScale;

        Visuals?.HideDropIndicator();
        if (enableBeam) Visuals?.HideBeam();
        SetCharacterParticlesActive(false);
        StopEyeTracking();
    }

    public void CancelPickup()
    {
        isBeingPickedUp = false;
        this.enabled = true;
    }

    private void OnDestroy()
    {
        StopEyeTracking();
        Visuals?.HideDropIndicator();
        if (enableBeam) Visuals?.HideBeam();
        SetCharacterParticlesActive(false);
        cachedCamera = null;
        characterParticles = null;
    }

    private EyeController cachedEyeController;

    [Header("Eye Tracking")]
    [SerializeField] private bool eyeTrackingDuringDrag = true;

    private EyeController FindEyeController()
    {
        if (cachedEyeController != null) return cachedEyeController;

        var current = _dialogueCharacterManager?.CurrentCharacter;
        if (current != null)
            cachedEyeController = current.GetComponentInChildren<EyeController>();

        if (cachedEyeController == null)
            cachedEyeController = FindAnyObjectByType<EyeController>();

        return cachedEyeController;
    }

    private void StartEyeTracking()
    {
        if (!eyeTrackingDuringDrag) return;
        var eye = FindEyeController();
        if (eye != null) eye.StartLookingAt(transform);
    }

    private void StopEyeTracking()
    {
        if (!eyeTrackingDuringDrag) return;
        if (cachedEyeController != null) cachedEyeController.ReturnToIdle();
    }
}