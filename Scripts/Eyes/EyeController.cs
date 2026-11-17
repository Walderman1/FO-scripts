using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EyeController : MonoBehaviour
{
    public enum EyeState { Normal, Squinting, FullOpen, HalfClosedEyes }

    [System.Serializable]
    public class EyeBoundarySettings
    {
        public float radiusX = 0.30f;
        public float radiusY = 0.20f;
        public float tilt = 0f;
        public Vector2 centerOffset = Vector2.zero;
    }

    [Header("=== Поиск частей ===")]
    [SerializeField] private string eyeIrisName = "Eyelris";
    [SerializeField] private string eyeHighlightName = "EyeHighlight";
    [SerializeField] private string eyePupilName = "EyePupil";
    [SerializeField] private string gestureGroupName = "Gesture";
    [SerializeField] private string normalGroupName = "Normal";
    [SerializeField] private string squintingGroupName = "Squinting";
    [SerializeField] private string fullOpenGroupName = "FullOpen";
    [SerializeField] private string halfClosedEyesGroupName = "HalfClosedEyes";
    [SerializeField] private string eyelidUpperName = "EyelidUpper";

    [Header("=== Найденные ссылки ===")]
    [SerializeField] private Transform eyeIris;
    [SerializeField] private Transform eyeHighlight;
    [SerializeField] private Transform eyePupil;
    [SerializeField] private Transform gestureGroup;
    [SerializeField] private Transform normalGroup;
    [SerializeField] private Transform squintingGroup;
    [SerializeField] private Transform fullOpenGroup;
    [SerializeField] private Transform halfClosedEyesGroup;
    [SerializeField] private Transform eyelidUpper;

    [Header("=== Состояние ===")]
    [SerializeField] private EyeState baseState = EyeState.FullOpen;

    [Header("=== Границы по эмоциям ===")]
    [SerializeField] private EyeBoundarySettings normalBounds = new EyeBoundarySettings();
    [SerializeField] private EyeBoundarySettings squintingBounds = new EyeBoundarySettings();
    [SerializeField] private EyeBoundarySettings fullOpenBounds = new EyeBoundarySettings();
    [SerializeField] private EyeBoundarySettings halfClosedBounds = new EyeBoundarySettings();
    [Range(0f, 20f)][SerializeField] private float boundaryTransitionSpeed = 8f;

    [Header("=== Прищур от света ===")]
    [SerializeField] private bool squintOnStrongLight = true;
    [Range(0f, 1f)][SerializeField] private float squintLightThreshold = 0.7f;
    [Range(0f, 1f)][SerializeField] private float squintLightExitThreshold = 0.3f;
    [Range(0f, 2f)][SerializeField] private float squintLightEnterDelay = 0.15f;
    [Range(0f, 5f)][SerializeField] private float squintLightExitDelay = 1.0f;
    [Range(0f, 5f)][SerializeField] private float squintMinDuration = 1.0f;
    [Range(0.1f, 3f)][SerializeField] private float squintingRadiusXMultiplier = 0.3f;
    [Range(0.1f, 3f)][SerializeField] private float squintingRadiusYMultiplier = 1.1f;
    [SerializeField] private bool freezeYWhenSquinting = true;
    [SerializeField] private Vector2 squintingIrisPositionOffset = Vector2.zero;
    [Range(0.1f, 20f)][SerializeField] private float squintingIrisOffsetSpeed = 6f;

    [Header("=== Моргание в прищуре ===")]
    [SerializeField] private bool useHalfClosedForSquintBlink = true;

    [Header("=== Отладка ===")]
    [SerializeField] private bool debugDrawGizmos = true;
    [SerializeField] private bool debugLogHierarchy = false;
    [SerializeField] private bool debugLogPosition = false;

    private float currentRadiusX;
    private float currentRadiusY;
    private float currentTilt;
    private Vector2 currentCenterOffset;

    private bool isSquintingFromLight = false;
    private float squintLightOnTimer = 0f;
    private float squintLightOffTimer = 0f;
    private float squintStartTime = 0f;
    private Vector2 currentSquintIrisOffset = Vector2.zero;

    private Vector3 baseIrisPosition;
    private bool isInitialized = false;

    private EyeLookController look;
    private EyeBlinkController blink;
    private EyeLightController light;

    public bool IsInitialized => isInitialized;
    public bool IsBlinking => blink != null && blink.IsBlinking;
    public bool IsSquintingFromLight => isSquintingFromLight;
    public int LightSourceCount => light != null ? light.LightSourceCount : 0;

    public EyeState BaseState
    {
        get => baseState;
        set { if (baseState == value) return; baseState = value; ApplyEyeState(); }
    }

    public EyeState EffectiveState => GetEffectiveState();

    public Transform EyeIris => eyeIris;
    public Transform EyelidUpper => eyelidUpper;
    public Transform HalfClosedEyesGroup => halfClosedEyesGroup;
    public Transform SquintingGroup => squintingGroup;
    public Transform FullOpenGroup => fullOpenGroup;
    public Transform NormalGroup => normalGroup;
    public Transform GestureGroup => gestureGroup;

    public Vector3 BaseIrisPosition => baseIrisPosition;
    public Vector2 SquintIrisOffset => currentSquintIrisOffset;

    public void SetEyeState(EyeState s) => BaseState = s;
    public void SetEyeStateNormal() => BaseState = EyeState.Normal;
    public void SetEyeStateSquinting() => BaseState = EyeState.Squinting;
    public void SetEyeStateFullOpen() => BaseState = EyeState.FullOpen;
    public void SetEyeStateHalfClosed() => BaseState = EyeState.HalfClosedEyes;

    public bool FollowMouse
    {
        get => look != null && look.FollowMouse;
        set { if (look != null) look.FollowMouse = value; }
    }

    public Camera MouseCamera
    {
        get => look != null ? look.MouseCamera : null;
        set { if (look != null) look.MouseCamera = value; }
    }

    public Vector3 MouseWorldPosition => look != null ? look.MouseWorldPosition : Vector3.zero;

    public bool InvertLookY
    {
        get => look != null && look.InvertLookY;
        set { if (look != null) look.InvertLookY = value; }
    }

    public bool InvertLookX
    {
        get => look != null && look.InvertLookX;
        set { if (look != null) look.InvertLookX = value; }
    }

    public Transform LookTarget
    {
        get => look != null ? look.LookTarget : null;
        set { if (look != null) look.LookTarget = value; }
    }

    public bool LookAtTarget
    {
        get => look != null && look.LookAtTarget;
        set { if (look != null) look.LookAtTarget = value; }
    }

    public bool EnableBlinking
    {
        get => blink != null && blink.EnableBlinking;
        set { if (blink != null) blink.EnableBlinking = value; }
    }

    public bool UseHalfClosedForSquintBlink
    {
        get => useHalfClosedForSquintBlink;
        set => useHalfClosedForSquintBlink = value;
    }

    public bool FreezeYWhenSquinting
    {
        get => freezeYWhenSquinting;
        set => freezeYWhenSquinting = value;
    }

    public void BlinkOnce() => blink?.BlinkOnce();
    public void BlinkDouble() => blink?.BlinkDouble();

    private void Awake()
    {
        FindEyeParts();
        EnsureSubsystems();
        Initialize();
    }

    private void OnEnable()
    {
        if (!isInitialized)
        {
            FindEyeParts();
            EnsureSubsystems();
            Initialize();
        }

        look?.Subscribe();
        blink?.Subscribe();
        light?.Subscribe();
    }

    private void OnDisable()
    {
        look?.Unsubscribe();
        blink?.Unsubscribe();
        light?.Unsubscribe();
    }

    private void EnsureSubsystems()
    {
        look = GetComponent<EyeLookController>();
        if (look == null) look = gameObject.AddComponent<EyeLookController>();

        blink = GetComponent<EyeBlinkController>();
        if (blink == null) blink = gameObject.AddComponent<EyeBlinkController>();

        light = GetComponent<EyeLightController>();
        if (light == null) light = gameObject.AddComponent<EyeLightController>();
    }

    private void Initialize()
    {
        if (isInitialized) return;
        if (eyeIris == null)
        {
            Logger.LogError(LogModule.UI, $"[EyeController] Не найдена радужка (Eyelris) на {gameObject.name}!");
            return;
        }

        baseIrisPosition = eyeIris.localPosition;

        look.Init(this, debugLogPosition);
        blink.Init(this);
        light.Init(this, eyePupil);

        isSquintingFromLight = false;
        squintLightOnTimer = 0f;
        squintLightOffTimer = 0f;
        squintStartTime = 0f;
        currentSquintIrisOffset = Vector2.zero;

        var b = GetBoundsForState(GetEffectiveState());
        float mX = 1f, mY = 1f;
        if (GetEffectiveState() == EyeState.Squinting)
        {
            mX = squintingRadiusXMultiplier;
            mY = squintingRadiusYMultiplier;
        }
        currentRadiusX = b.radiusX * mX;
        currentRadiusY = b.radiusY * mY;
        currentTilt = b.tilt;
        currentCenterOffset = b.centerOffset;

        ApplyEyeState();
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;

        UpdateSquintIrisOffset();
        UpdateCurrentBoundary();
        look.UpdateLook();
        light.UpdatePupil();
        UpdateLightSquint();
        blink.UpdateBlink();
    }

    private void UpdateSquintIrisOffset()
    {
        Vector2 target = GetEffectiveState() == EyeState.Squinting ? squintingIrisPositionOffset : Vector2.zero;
        currentSquintIrisOffset = Vector2.Lerp(currentSquintIrisOffset, target, Time.deltaTime * squintingIrisOffsetSpeed);
    }

    private void UpdateCurrentBoundary()
    {
        EyeBoundarySettings target = GetBoundsForState(GetEffectiveState());

        float mulX = 1f, mulY = 1f;
        if (GetEffectiveState() == EyeState.Squinting)
        {
            mulX = squintingRadiusXMultiplier;
            mulY = squintingRadiusYMultiplier;
        }

        float tX = target.radiusX * mulX;
        float tY = target.radiusY * mulY;
        float tTilt = target.tilt;
        Vector2 tCenter = target.centerOffset;

        if (boundaryTransitionSpeed > 0.01f)
        {
            float k = Time.deltaTime * boundaryTransitionSpeed;
            currentRadiusX = Mathf.Lerp(currentRadiusX, tX, k);
            currentRadiusY = Mathf.Lerp(currentRadiusY, tY, k);
            currentTilt = Mathf.Lerp(currentTilt, tTilt, k);
            currentCenterOffset = Vector2.Lerp(currentCenterOffset, tCenter, k);
        }
        else
        {
            currentRadiusX = tX;
            currentRadiusY = tY;
            currentTilt = tTilt;
            currentCenterOffset = tCenter;
        }
    }

    private void UpdateLightSquint()
    {
        if (!squintOnStrongLight)
        {
            if (isSquintingFromLight)
            {
                isSquintingFromLight = false;
                squintLightOnTimer = 0f;
                squintLightOffTimer = 0f;
                ApplyEyeState();
            }
            return;
        }

        float l = light != null ? light.GetLightLevel() : 0f;

        if (!isSquintingFromLight)
        {
            if (l >= squintLightThreshold)
            {
                squintLightOnTimer += Time.deltaTime;
                if (squintLightOnTimer >= squintLightEnterDelay)
                {
                    isSquintingFromLight = true;
                    squintStartTime = Time.time;
                    squintLightOnTimer = 0f;
                    squintLightOffTimer = 0f;
                    ApplyEyeState();
                }
            }
            else squintLightOnTimer = 0f;
        }
        else
        {
            if (Time.time - squintStartTime < squintMinDuration) return;

            if (l <= squintLightExitThreshold)
            {
                squintLightOffTimer += Time.deltaTime;
                if (squintLightOffTimer >= squintLightExitDelay)
                {
                    isSquintingFromLight = false;
                    squintLightOffTimer = 0f;
                    squintLightOnTimer = 0f;
                    ApplyEyeState();
                }
            }
            else squintLightOffTimer = 0f;
        }
    }

    public EyeState GetEffectiveState() => isSquintingFromLight ? EyeState.Squinting : baseState;

    private EyeBoundarySettings GetBoundsForState(EyeState state)
    {
        switch (state)
        {
            case EyeState.Squinting: return squintingBounds;
            case EyeState.FullOpen: return fullOpenBounds;
            case EyeState.HalfClosedEyes: return halfClosedBounds;
            default: return normalBounds;
        }
    }

    public void GetCurrentRadii(out float rX, out float rY, out float tiltDeg, out Vector2 stateCenterOffset)
    {
        rX = currentRadiusX;
        rY = currentRadiusY;
        tiltDeg = currentTilt;
        stateCenterOffset = currentCenterOffset;
    }

    public Vector3 GetEyeCenterPosition()
    {
        if (eyeIris == null) return transform.position;
        Transform parent = eyeIris.parent;
        Vector3 baseLocal = isInitialized ? baseIrisPosition : eyeIris.localPosition;
        return parent != null ? parent.TransformPoint(baseLocal) : baseLocal;
    }

    public void ApplyEyeState()
    {
        SetActiveSafe(gestureGroup, false);
        SetActiveSafe(normalGroup, false);
        SetActiveSafe(squintingGroup, false);
        SetActiveSafe(fullOpenGroup, false);
        SetActiveSafe(halfClosedEyesGroup, false);
        SetActiveSafe(eyelidUpper, false);

        if (blink != null && blink.IsBlinking)
        {
            if (useHalfClosedForSquintBlink && halfClosedEyesGroup != null && GetEffectiveState() == EyeState.Squinting)
                SetActiveSafe(halfClosedEyesGroup, true);
            else
                SetActiveSafe(eyelidUpper, true);
            return;
        }

        switch (GetEffectiveState())
        {
            case EyeState.Normal:
                SetActiveSafe(gestureGroup, true);
                SetActiveSafe(normalGroup, true);
                break;
            case EyeState.Squinting:
                SetActiveSafe(gestureGroup, true);
                SetActiveSafe(squintingGroup, true);
                break;
            case EyeState.FullOpen:
                SetActiveSafe(gestureGroup, true);
                SetActiveSafe(fullOpenGroup, true);
                break;
            case EyeState.HalfClosedEyes:
                SetActiveSafe(halfClosedEyesGroup, true);
                break;
        }
    }

    private void SetActiveSafe(Transform t, bool a)
    {
        if (t == null) return;
        if (t.gameObject.activeSelf != a) t.gameObject.SetActive(a);
    }

    public void LookAt(Vector3 worldPos) => look?.LookAt(worldPos);
    public void ReturnToIdle() => look?.ReturnToIdle();
    public void StartLookingAt(Transform t) => look?.StartLookingAt(t);
    public void StartLookingAtWorld(Vector3 p) => look?.StartLookingAtWorld(p);

    public void SetLightSource(Transform l) => light?.SetLightSource(l);
    public void AddLightSource(Transform l) => light?.AddLightSource(l);
    public void RemoveLightSource(Transform l) => light?.RemoveLightSource(l);

    [ContextMenu("Найти MagicLightAbility")]
    public void FindMagicLightAbility() => light?.FindMagicLightAbility();

    [ContextMenu("Найти все Light2D")]
    public void FindAllLights() => light?.FindAllLights();

    [ContextMenu("Обновить ссылки")]
    public void RefreshReferences()
    {
        eyeIris = null;
        eyeHighlight = null;
        eyePupil = null;
        gestureGroup = null;
        normalGroup = null;
        squintingGroup = null;
        fullOpenGroup = null;
        halfClosedEyesGroup = null;
        eyelidUpper = null;

        FindEyeParts();
        Initialize();
        if (isInitialized) ApplyEyeState();
    }

    [ContextMenu("Пересчитать Light2D")]
    public void RefreshLights() => light?.FindAllLights();

    [ContextMenu("Моргнуть один раз")]
    public void EditorBlinkOnce() => BlinkOnce();

    [ContextMenu("Моргнуть дважды")]
    public void EditorBlinkDouble() => BlinkDouble();

    private void FindEyeParts()
    {
        if (debugLogHierarchy) PrintHierarchy(transform, 0);

        if (eyeIris == null)
        {
            string[] n = { eyeIrisName, "Eyelris", "EyeIris", "Iris" };
            foreach (var s in n)
            {
                if (string.IsNullOrEmpty(s)) continue;
                eyeIris = FindTransformByName(transform, s);
                if (eyeIris != null) break;
            }
        }

        if (eyePupil == null)
        {
            string[] n = { eyePupilName, "EyePupil", "Pupil" };
            foreach (var s in n)
            {
                if (string.IsNullOrEmpty(s)) continue;
                if (eyeIris != null) eyePupil = FindTransformByName(eyeIris, s);
                if (eyePupil == null) eyePupil = FindTransformByName(transform, s);
                if (eyePupil != null) break;
            }
        }

        if (eyeHighlight == null)
        {
            string[] n = { eyeHighlightName, "EyeHighlight", "Highlight", "Glint" };
            foreach (var s in n)
            {
                if (string.IsNullOrEmpty(s)) continue;
                if (eyeIris != null) eyeHighlight = FindTransformByName(eyeIris, s);
                if (eyeHighlight == null) eyeHighlight = FindTransformByName(transform, s);
                if (eyeHighlight != null) break;
            }
        }

        if (gestureGroup == null) gestureGroup = FindTransformByName(transform, gestureGroupName);
        if (gestureGroup != null)
        {
            if (normalGroup == null) normalGroup = FindTransformByName(gestureGroup, normalGroupName);
            if (squintingGroup == null) squintingGroup = FindTransformByName(gestureGroup, squintingGroupName);
            if (fullOpenGroup == null) fullOpenGroup = FindTransformByName(gestureGroup, fullOpenGroupName);
        }
        if (normalGroup == null) normalGroup = FindTransformByName(transform, normalGroupName);
        if (squintingGroup == null) squintingGroup = FindTransformByName(transform, squintingGroupName);
        if (fullOpenGroup == null) fullOpenGroup = FindTransformByName(transform, fullOpenGroupName);
        if (halfClosedEyesGroup == null) halfClosedEyesGroup = FindTransformByName(transform, halfClosedEyesGroupName);
        if (eyelidUpper == null) eyelidUpper = FindTransformByName(transform, eyelidUpperName);
    }

    private Transform FindTransformByName(Transform p, string n)
    {
        if (p == null || string.IsNullOrEmpty(n)) return null;
        if (p.name.Equals(n, System.StringComparison.OrdinalIgnoreCase)) return p;
        if (p.name.IndexOf(n, System.StringComparison.OrdinalIgnoreCase) >= 0) return p;
        foreach (Transform c in p)
        {
            var f = FindTransformByName(c, n);
            if (f != null) return f;
        }
        return null;
    }

    private void PrintHierarchy(Transform t, int d)
    {
        if (t == null) return;
        Logger.LogVerbose(LogModule.UI, $"{new string(' ', d * 2)}- {t.name}");
        foreach (Transform c in t) PrintHierarchy(c, d + 1);
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugDrawGizmos) return;

        if (eyeIris == null)
        {
            var f = FindTransformByName(transform, eyeIrisName);
            if (f == null) f = FindTransformByName(transform, "Eyelris");
            if (f != null) eyeIris = f;
        }
        if (eyeIris == null) return;

        Vector3 centerWorld = Application.isPlaying && isInitialized
            ? GetEyeCenterPosition()
            : (eyeIris.parent != null ? eyeIris.parent.TransformPoint(eyeIris.localPosition) : eyeIris.position);

        float rX, rY, tiltDeg;
        Vector2 centerOff;

        if (Application.isPlaying && isInitialized)
        {
            GetCurrentRadii(out rX, out rY, out tiltDeg, out centerOff);
        }
        else
        {
            var b = GetBoundsForState(baseState);
            rX = b.radiusX;
            rY = b.radiusY;
            tiltDeg = b.tilt;
            centerOff = b.centerOffset;
        }

        Vector3 ellipseCenter = centerWorld + new Vector3(centerOff.x, centerOff.y, 0f);

        DrawEllipseGizmo(ellipseCenter, rX, rY, tiltDeg, Color.green, 64);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(ellipseCenter, 0.02f);
        float cs = 0.03f;
        Gizmos.DrawLine(ellipseCenter + Vector3.left * cs, ellipseCenter + Vector3.right * cs);
        Gizmos.DrawLine(ellipseCenter + Vector3.down * cs, ellipseCenter + Vector3.up * cs);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centerWorld, 0.01f);

        if (eyeIris != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(eyeIris.position, 0.012f);
        }
    }

    private void DrawEllipseGizmo(Vector3 c, float rX, float rY, float tiltDeg, Color col, int segs)
    {
        Gizmos.color = col;
        float tr = tiltDeg * Mathf.Deg2Rad;
        float ct = Mathf.Cos(tr);
        float st = Mathf.Sin(tr);

        Vector3 prev = c + Rot(new Vector3(rX, 0f, 0f), ct, st);
        for (int i = 1; i <= segs; i++)
        {
            float a = (float)i / segs * Mathf.PI * 2f;
            Vector3 np = c + Rot(new Vector3(Mathf.Cos(a) * rX, Mathf.Sin(a) * rY, 0f), ct, st);
            Gizmos.DrawLine(prev, np);
            prev = np;
        }
    }

    private Vector3 Rot(Vector3 p, float c, float s)
        => new Vector3(p.x * c - p.y * s, p.x * s + p.y * c, p.z);
}