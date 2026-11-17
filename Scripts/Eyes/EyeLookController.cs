using UnityEngine;

public class EyeLookController : MonoBehaviour
{
    [Header("=== Движение ===")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float saccadeSpeedMultiplier = 6f;
    [SerializeField] private float minIdleTime = 0.7f;
    [SerializeField] private float maxIdleTime = 3.5f;
    [SerializeField] private float noiseSpeed = 0.4f;
    [SerializeField] private float noiseAmplitude = 0.3f;
    [SerializeField] private bool invertLookY = false;
    [SerializeField] private bool invertLookX = true;
    [SerializeField] private bool lookAtTarget = false;
    [SerializeField] private Transform lookTarget;
    [SerializeField] private bool useNoise = true;
    [SerializeField] private bool useSaccades = true;

    [Header("=== Следование за мышью ===")]
    [SerializeField] private bool followMouse = false;
    [SerializeField] private Camera mouseCamera;
    [Range(0.5f, 20f)][SerializeField] private float mouseFollowSpeed = 6f;
    [SerializeField] private float mouseFollowMaxDistance = 0f;
    [SerializeField] private bool returnToIdleWhenMouseFar = true;
    [SerializeField] private bool useMouseWorldZ = false;
    [Range(0.5f, 10f)][SerializeField] private float lookRespondRadiusMultiplier = 3f;

    [Header("=== Перспективное сжатие ===")]
    [SerializeField] private bool usePerspectiveForeshortening = false;
    [Range(0.1f, 1f)][SerializeField] private float minIrisYForeshortening = 0.35f;
    [Range(0.1f, 1f)][SerializeField] private float minIrisXForeshortening = 0.85f;
    [Range(0.1f, 1f)][SerializeField] private float maxLookMagnitudeForeshortening = 1f;
    [Range(0f, 0.9f)][SerializeField] private float foreshorteningDeadzone = 0.15f;
    [Range(0.3f, 2f)][SerializeField] private float foreshorteningCurve = 1.2f;
    [SerializeField] private bool rotateIrisWithOrbit = false;
    [SerializeField] private bool disableForeshorteningWhenSquinting = true;

    [Header("=== Множители прищура ===")]
    [Range(0f, 1f)][SerializeField] private float squintingIdleAmplitude = 0.2f;
    [Range(0f, 1f)][SerializeField] private float squintingLookAmplitude = 0.3f;
    [Range(0f, 1f)][SerializeField] private float squintingNoiseAmplitude = 0f;

    private EyeController master;
    private Transform eyeIris;
    private Vector3 baseIrisScaleVector = Vector3.one;

    private Vector3 targetOffset;
    private Vector3 currentOffset;
    private float noiseSeedX;
    private float noiseSeedY;
    private float nextLookChangeTime = 0f;
    private bool isInitialized = false;
    private bool debugLogPosition = false;

    private Transform mouseVirtualTarget;
    private Vector3 mouseWorldPosition;

    public bool FollowMouse { get => followMouse; set => followMouse = value; }
    public Camera MouseCamera { get => mouseCamera; set => mouseCamera = value; }
    public Vector3 MouseWorldPosition => mouseWorldPosition;
    public bool InvertLookY { get => invertLookY; set => invertLookY = value; }
    public bool InvertLookX { get => invertLookX; set => invertLookX = value; }
    public Transform LookTarget { get => lookTarget; set => lookTarget = value; }
    public bool LookAtTarget { get => lookAtTarget; set => lookAtTarget = value; }
    public Vector3 CurrentOffset => currentOffset;

    public void Init(EyeController masterRef, bool logPos)
    {
        master = masterRef;
        debugLogPosition = logPos;
        eyeIris = master.EyeIris;
        if (eyeIris == null) return;

        baseIrisScaleVector = eyeIris.localScale;

        noiseSeedX = Random.Range(0f, 100f);
        noiseSeedY = Random.Range(0f, 100f);

        targetOffset = Vector3.zero;
        currentOffset = Vector3.zero;
        nextLookChangeTime = Time.time + Random.Range(minIdleTime, maxIdleTime);

        CreateMouseVirtualTarget();
        isInitialized = true;
    }

    public void Subscribe()
    {
        if (mouseVirtualTarget == null) CreateMouseVirtualTarget();
        nextLookChangeTime = Time.time + Random.Range(minIdleTime, maxIdleTime);
    }

    public void Unsubscribe() { }

    private void OnDestroy()
    {
        if (mouseVirtualTarget != null)
        {
            if (Application.isPlaying) Destroy(mouseVirtualTarget.gameObject);
            else DestroyImmediate(mouseVirtualTarget.gameObject);
            mouseVirtualTarget = null;
        }
    }

    public void UpdateLook()
    {
        if (!isInitialized || eyeIris == null || master == null) return;

        UpdateMouseFollow();

        EyeController.EyeState effective = master.GetEffectiveState();

        if (!lookAtTarget && Time.time >= nextLookChangeTime)
        {
            PickNewRandomTarget();
            nextLookChangeTime = Time.time + Random.Range(minIdleTime, maxIdleTime);
        }

        Vector3 desired;
        if (lookAtTarget && lookTarget != null) desired = CalculateLookAtTargetOffset();
        else if (useNoise) desired = targetOffset + CalculateNoiseOffset();
        else desired = targetOffset;

        float speed = moveSpeed;
        if (useSaccades && !lookAtTarget)
        {
            if (Time.time - (nextLookChangeTime - Random.Range(minIdleTime, maxIdleTime)) < 0.15f)
                speed = moveSpeed * saccadeSpeedMultiplier;
        }
        else if (lookAtTarget) speed = moveSpeed * 2f;

        currentOffset = Vector3.Lerp(currentOffset, desired, Time.deltaTime * speed);

        if (master.FreezeYWhenSquinting && effective == EyeController.EyeState.Squinting)
            currentOffset.y = Mathf.Lerp(currentOffset.y, 0f, Time.deltaTime * 10f);

        ApplyOffset(currentOffset);
        UpdateIrisForeshortening(effective);
    }

    private void UpdateMouseFollow()
    {
        if (!followMouse || eyeIris == null) return;
        if (mouseVirtualTarget == null) CreateMouseVirtualTarget();

        Camera cam = mouseCamera;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 ec = master.GetEyeCenterPosition();
        Vector3 sp = Input.mousePosition;
        float depth = Mathf.Abs(cam.transform.position.z - ec.z);
        if (depth <= 0.001f) depth = 10f;

        Vector3 w = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, depth));
        if (!useMouseWorldZ) w.z = ec.z;
        mouseWorldPosition = w;

        bool use = true;
        if (mouseFollowMaxDistance > 0f && Vector3.Distance(ec, w) > mouseFollowMaxDistance)
            use = false;

        if (use)
        {
            mouseVirtualTarget.position = Vector3.Lerp(mouseVirtualTarget.position, w, Time.deltaTime * mouseFollowSpeed);
            lookTarget = mouseVirtualTarget;
            lookAtTarget = true;
            useNoise = false;
        }
        else if (returnToIdleWhenMouseFar)
        {
            if (lookAtTarget && lookTarget == mouseVirtualTarget)
            {
                lookAtTarget = false;
                useNoise = true;
            }
        }
    }

    private void ApplyOffset(Vector3 offset)
    {
        if (eyeIris == null || master == null) return;

        Vector2 squintOff = master.SquintIrisOffset;
        Vector3 squintOffVec = new Vector3(squintOff.x, squintOff.y, 0f);

        Transform parent = eyeIris.parent;
        Vector3 ps = parent != null ? parent.lossyScale : Vector3.one;
        float psx = Mathf.Max(Mathf.Abs(ps.x), 0.0001f);
        float psy = Mathf.Max(Mathf.Abs(ps.y), 0.0001f);

        float wx = (offset.x + squintOffVec.x) * psx;
        float wy = (offset.y + squintOffVec.y) * psy;

        float rX, rY, tiltDeg;
        Vector2 stateCenter;
        master.GetCurrentRadii(out rX, out rY, out tiltDeg, out stateCenter);

        if (rX <= 0.0001f || rY <= 0.0001f)
        {
            eyeIris.localPosition = master.BaseIrisPosition;
            return;
        }

        float tiltRad = tiltDeg * Mathf.Deg2Rad;
        float cosT = Mathf.Cos(tiltRad);
        float sinT = Mathf.Sin(tiltRad);

        float lx = cosT * wx + sinT * wy;
        float ly = -sinT * wx + cosT * wy;

        float nx = lx / rX;
        float ny = ly / rY;
        float magSq = nx * nx + ny * ny;
        if (magSq > 1f)
        {
            float mag = Mathf.Sqrt(magSq);
            lx /= mag;
            ly /= mag;
        }

        float fwx = cosT * lx - sinT * ly;
        float fwy = sinT * lx + cosT * ly;

        float lox = fwx / psx;
        float loy = fwy / psy;

        Vector3 finalLocal = master.BaseIrisPosition + new Vector3(lox, loy, 0f);

        if (stateCenter.sqrMagnitude > 0.0000001f)
        {
            finalLocal.x += stateCenter.x / psx;
            finalLocal.y += stateCenter.y / psy;
        }

        eyeIris.localPosition = finalLocal;

        if (debugLogPosition)
        {
            float chkX = wx / rX;
            float chkY = wy / rY;
            Logger.LogVerbose(LogModule.UI, $"[EyeClamp] want=({wx:F3},{wy:F3}) r=({rX:F3},{rY:F3}) wantInside={(chkX * chkX + chkY * chkY):F3}");
        }
    }

    private void UpdateIrisForeshortening(EyeController.EyeState effective)
    {
        if (eyeIris == null || master == null) return;

        if (!usePerspectiveForeshortening)
        {
            eyeIris.localScale = baseIrisScaleVector;
            return;
        }

        if (disableForeshorteningWhenSquinting && effective == EyeController.EyeState.Squinting)
        {
            eyeIris.localScale = baseIrisScaleVector;
            return;
        }

        float rX, rY, tiltDeg;
        Vector2 stateCenter;
        master.GetCurrentRadii(out rX, out rY, out tiltDeg, out stateCenter);
        if (rX <= 0.0001f || rY <= 0.0001f) return;

        Transform parent = eyeIris.parent;
        Vector3 ps = parent != null ? parent.lossyScale : Vector3.one;
        Vector2 off = new Vector2(currentOffset.x * Mathf.Abs(ps.x), currentOffset.y * Mathf.Abs(ps.y));

        float nx = Mathf.Clamp(off.x / rX, -1f, 1f);
        float ny = Mathf.Clamp(off.y / rY, -1f, 1f);

        float lookX = Mathf.Abs(nx);
        float lookY = Mathf.Abs(ny);

        if (foreshorteningDeadzone > 0f)
        {
            lookX = Mathf.Max(0f, (lookX - foreshorteningDeadzone) / (1f - foreshorteningDeadzone));
            lookY = Mathf.Max(0f, (lookY - foreshorteningDeadzone) / (1f - foreshorteningDeadzone));
        }

        lookX = Mathf.Pow(lookX, foreshorteningCurve);
        lookY = Mathf.Pow(lookY, foreshorteningCurve);

        float sy = Mathf.Lerp(1f, minIrisYForeshortening, lookY);
        float sx = Mathf.Lerp(1f, minIrisXForeshortening, lookX);

        if (maxLookMagnitudeForeshortening < 1f)
        {
            float m = Mathf.Clamp01(Mathf.Sqrt(nx * nx + ny * ny));
            float g = Mathf.Lerp(1f, maxLookMagnitudeForeshortening, m);
            sx *= g;
            sy *= g;
        }

        eyeIris.localScale = new Vector3(
            baseIrisScaleVector.x * sx,
            baseIrisScaleVector.y * sy,
            baseIrisScaleVector.z);

        if (rotateIrisWithOrbit)
            eyeIris.localRotation = Quaternion.Euler(0f, 0f, tiltDeg);
    }

    private void PickNewRandomTarget()
    {
        if (master == null) return;

        float rX, rY, td;
        Vector2 sc;
        master.GetCurrentRadii(out rX, out rY, out td, out sc);

        float amp = 1f;
        if (master.GetEffectiveState() == EyeController.EyeState.Squinting) amp = squintingIdleAmplitude;

        Transform parent = eyeIris.parent;
        Vector3 ps = parent != null ? parent.lossyScale : Vector3.one;
        float psx = Mathf.Max(Mathf.Abs(ps.x), 0.0001f);
        float psy = Mathf.Max(Mathf.Abs(ps.y), 0.0001f);

        Vector2 c = Random.insideUnitCircle;
        targetOffset = new Vector3(
            c.x * rX * 0.8f * amp / psx,
            c.y * rY * 0.8f * amp / psy,
            0f);
    }

    private Vector3 CalculateLookAtTargetOffset()
    {
        if (lookTarget == null || master == null) return Vector3.zero;

        float rX, rY, td;
        Vector2 sc;
        master.GetCurrentRadii(out rX, out rY, out td, out sc);

        float amp = 1f;
        if (master.GetEffectiveState() == EyeController.EyeState.Squinting) amp = squintingLookAmplitude;

        Vector3 delta = lookTarget.position - master.GetEyeCenterPosition();
        delta.z = 0;
        if (invertLookX) delta.x = -delta.x;
        if (invertLookY) delta.y = -delta.y;

        float respond = lookRespondRadiusMultiplier;
        float kx = Mathf.Clamp(delta.x / Mathf.Max(rX * respond, 0.0001f), -1f, 1f);
        float ky = Mathf.Clamp(delta.y / Mathf.Max(rY * respond, 0.0001f), -1f, 1f);

        Transform parent = eyeIris.parent;
        Vector3 ps = parent != null ? parent.lossyScale : Vector3.one;
        float psx = Mathf.Max(Mathf.Abs(ps.x), 0.0001f);
        float psy = Mathf.Max(Mathf.Abs(ps.y), 0.0001f);

        return new Vector3(
            kx * rX * 0.9f * amp / psx,
            ky * rY * 0.9f * amp / psy,
            0f);
    }

    private Vector3 CalculateNoiseOffset()
    {
        if (master == null) return Vector3.zero;

        float rX, rY, td;
        Vector2 sc;
        master.GetCurrentRadii(out rX, out rY, out td, out sc);

        float amp = noiseAmplitude;
        if (master.GetEffectiveState() == EyeController.EyeState.Squinting) amp *= squintingNoiseAmplitude;

        float t = Time.time * noiseSpeed;
        float nX = (Mathf.PerlinNoise(noiseSeedX, t) - 0.5f) * 2f;
        float nY = (Mathf.PerlinNoise(noiseSeedY, t) - 0.5f) * 2f;

        Transform parent = eyeIris.parent;
        Vector3 ps = parent != null ? parent.lossyScale : Vector3.one;
        float psx = Mathf.Max(Mathf.Abs(ps.x), 0.0001f);
        float psy = Mathf.Max(Mathf.Abs(ps.y), 0.0001f);

        return new Vector3(
            nX * rX * amp / psx,
            nY * rY * amp / psy,
            0f);
    }

    private void CreateMouseVirtualTarget()
    {
        if (mouseVirtualTarget != null) return;
        GameObject go = new GameObject("~MouseVirtualTarget");
        go.hideFlags = HideFlags.HideAndDontSave;
        mouseVirtualTarget = go.transform;
        mouseVirtualTarget.SetParent(null);
    }

    public void LookAt(Vector3 worldPos)
    {
        if (!isInitialized || master == null) return;

        float rX, rY, td;
        Vector2 sc;
        master.GetCurrentRadii(out rX, out rY, out td, out sc);

        float amp = 1f;
        if (master.GetEffectiveState() == EyeController.EyeState.Squinting) amp = squintingLookAmplitude;

        Vector3 delta = worldPos - master.GetEyeCenterPosition();
        delta.z = 0;
        if (invertLookX) delta.x = -delta.x;
        if (invertLookY) delta.y = -delta.y;

        float respond = lookRespondRadiusMultiplier;
        float kx = Mathf.Clamp(delta.x / Mathf.Max(rX * respond, 0.0001f), -1f, 1f);
        float ky = Mathf.Clamp(delta.y / Mathf.Max(rY * respond, 0.0001f), -1f, 1f);

        Transform parent = eyeIris.parent;
        Vector3 ps = parent != null ? parent.lossyScale : Vector3.one;
        float psx = Mathf.Max(Mathf.Abs(ps.x), 0.0001f);
        float psy = Mathf.Max(Mathf.Abs(ps.y), 0.0001f);

        targetOffset = new Vector3(
            kx * rX * 0.9f * amp / psx,
            ky * rY * 0.9f * amp / psy,
            0f);

        useNoise = false;
        lookAtTarget = false;
    }

    public void ReturnToIdle()
    {
        useNoise = true;
        lookAtTarget = false;
        targetOffset = Vector3.zero;
        nextLookChangeTime = Time.time + Random.Range(minIdleTime, maxIdleTime);
    }

    public void StartLookingAt(Transform t)
    {
        lookTarget = t;
        lookAtTarget = true;
        useNoise = false;
    }

    public void StartLookingAtWorld(Vector3 p)
    {
        if (mouseVirtualTarget == null) CreateMouseVirtualTarget();
        mouseVirtualTarget.position = p;
        lookTarget = mouseVirtualTarget;
        lookAtTarget = true;
        useNoise = false;
    }
}