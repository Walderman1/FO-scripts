using UnityEngine;

public class EyeBlinkController : MonoBehaviour
{
    [Header("=== Моргание ===")]
    [SerializeField] private bool enableBlinking = true;
    [Range(0.5f, 20f)][SerializeField] private float minBlinkInterval = 2f;
    [Range(0.5f, 30f)][SerializeField] private float maxBlinkInterval = 6f;
    [Range(0.02f, 1f)][SerializeField] private float minBlinkDuration = 0.08f;
    [Range(0.02f, 1f)][SerializeField] private float maxBlinkDuration = 0.18f;
    [Range(0f, 1f)][SerializeField] private float doubleBlinkChance = 0.25f;
    [Range(0.02f, 0.5f)][SerializeField] private float doubleBlinkGap = 0.1f;
    [Range(0f, 5f)][SerializeField] private float initialBlinkDelay = 1f;

    private EyeController master;
    private float nextBlinkTime;
    private float blinkEndTime;
    private bool isBlinking;
    private bool pendingDoubleBlink;
    private float secondBlinkTime;

    public bool IsBlinking => isBlinking;

    public bool EnableBlinking
    {
        get => enableBlinking;
        set
        {
            enableBlinking = value;
            if (!value && isBlinking)
            {
                isBlinking = false;
                pendingDoubleBlink = false;
                secondBlinkTime = 0f;
                master?.ApplyEyeState();
            }
        }
    }

    public void Init(EyeController masterRef)
    {
        master = masterRef;
        nextBlinkTime = Time.time + initialBlinkDelay + Random.Range(0f, 1f);
        isBlinking = false;
        pendingDoubleBlink = false;
        secondBlinkTime = 0f;
    }

    public void Subscribe() { }
    public void Unsubscribe() { }

    public void UpdateBlink()
    {
        if (!enableBlinking || master == null) return;

        bool hasEyelid = master.EyelidUpper != null;
        bool hasHalfClosed = master.HalfClosedEyesGroup != null;
        if (!hasEyelid && !hasHalfClosed) return;

        if (isBlinking)
        {
            if (Time.time >= blinkEndTime)
            {
                isBlinking = false;
                master.ApplyEyeState();

                if (pendingDoubleBlink)
                {
                    pendingDoubleBlink = false;
                    secondBlinkTime = Time.time + doubleBlinkGap;
                }
                else ScheduleNextBlink();
            }
            return;
        }

        if (secondBlinkTime > 0f && Time.time >= secondBlinkTime)
        {
            secondBlinkTime = 0f;
            StartBlink();
            return;
        }

        if (Time.time >= nextBlinkTime) StartBlink();
    }

    public void BlinkOnce()
    {
        if (isBlinking) return;
        StartBlink();
    }

    public void BlinkDouble()
    {
        if (isBlinking) return;
        StartBlink();
        pendingDoubleBlink = true;
    }

    private void StartBlink()
    {
        isBlinking = true;
        blinkEndTime = Time.time + Random.Range(minBlinkDuration, maxBlinkDuration);
        master.ApplyEyeState();
        pendingDoubleBlink = Random.value < doubleBlinkChance;
    }

    private void ScheduleNextBlink()
    {
        nextBlinkTime = Time.time + Random.Range(minBlinkInterval, maxBlinkInterval);
    }
}