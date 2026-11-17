using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

public class ObjectInteractionController : MonoBehaviour
{
    [Inject] private GameReferences _refs;

    private GameReferences Refs => _refs ?? GameReferences.Instance;

    private LayerMask targetLayer;
    private GameObject currentObject;
    private GameObject nextObject;
    private Animator transitionAnimator;
    private AudioSource audioSource;
    private AudioClip detectionClip;

    public void Initialize(Animator transitionAnimator, AudioSource audioSource, AudioClip detectionClip, LayerMask targetLayer)
    {
        this.transitionAnimator = transitionAnimator;
        this.audioSource = audioSource;
        this.detectionClip = detectionClip;
        this.targetLayer = targetLayer;

        Logger.LogVerbose(LogModule.Core, "ObjectInteractionController инициализирован");
    }

    public void SetNextObject(GameObject targetObject) => nextObject = targetObject;

    public void Interact(GameObject targetObject)
    {
        currentObject = targetObject;
        Invoke(nameof(CompleteInteraction), 0.15f);
    }

    public void Detect(GameObject targetObject, bool isMagicLightActive)
    {
        if (!isMagicLightActive) return;

        var spriteRenderer = targetObject.GetComponent<SpriteRenderer>();
        var collider = targetObject.GetComponent<BoxCollider2D>();

        if (spriteRenderer != null && !spriteRenderer.enabled)
        {
            if (audioSource != null && detectionClip != null)
                audioSource.PlayOneShot(detectionClip);

            PlayTransitionAnimation();

            spriteRenderer.enabled = true;
            if (collider != null) collider.enabled = true;
        }
    }

    public void HandleObjectClick()
    {
        if (IsPointerOverUI()) return;

        var cam = Refs?.MainCamera;
        if (cam == null) return;

        Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, targetLayer);

        if (hit.collider == null) return;

        Logger.Log(LogModule.Core, $"ObjectInteractionController: клик по {hit.collider.name}");

        DialogueTrigger trigger = hit.collider.GetComponent<DialogueTrigger>();
        if (trigger != null) { trigger.TriggerDialogue(); return; }

        IInteractable interactable = hit.collider.GetComponent<IInteractable>();
        if (interactable != null) { interactable.OnInteract(); return; }

        interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable != null) { interactable.OnInteract(); return; }

        Logger.Log(LogModule.Core, $"ObjectInteractionController: объект {hit.collider.name} не имеет интерактивных компонентов");
    }

    public void CheckClickAtPosition(Vector2 position)
    {
        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.zero, Mathf.Infinity, targetLayer);
        if (hit.collider == null) return;

        DialogueTrigger trigger = hit.collider.GetComponent<DialogueTrigger>();
        if (trigger != null) trigger.TriggerDialogue();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void CompleteInteraction()
    {
        if (nextObject != null) nextObject.SetActive(true);
        if (currentObject != null) currentObject.SetActive(false);
    }

    private void PlayTransitionAnimation()
    {
        if (transitionAnimator != null) transitionAnimator.Play("Path");
    }
}