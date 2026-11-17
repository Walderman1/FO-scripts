using UnityEngine;
using VContainer;

[DefaultExecutionOrder(-100)]
public class PickupVisualsManager : MonoBehaviour
{
    [Header("Beam")]
    [SerializeField] private Color beamColor = new Color(0f, 1f, 1f, 1f);
    [Range(0.02f, 0.5f)][SerializeField] private float beamCoreWidth = 0.08f;
    [Range(1f, 6f)][SerializeField] private float beamGlowWidthMultiplier = 3f;
    [Range(0f, 1f)][SerializeField] private float beamGlowAlpha = 0.15f;
    [Range(0f, 1f)][SerializeField] private float beamStartAlpha = 0.3f;
    [Range(0f, 1f)][SerializeField] private float beamEndAlpha = 0.9f;
    [SerializeField] private int beamCoreSortingOrder = 1000;
    [SerializeField] private int beamGlowSortingOrder = 999;

    [Header("Drop Indicator")]
    [SerializeField] private Color indicatorColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Vector3 indicatorScale = new Vector3(1.2f, 0.5f, 1f);
    [SerializeField] private int indicatorSortingOrder = 49;

    private LineRenderer beamCore;
    private LineRenderer beamGlow;
    private SpriteRenderer dropIndicator;
    private Transform visualsRoot;
    private int beamRequesters = 0;

    private void Awake()
    {
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        CreateVisuals();
    }

    private void CreateVisuals()
    {
        visualsRoot = new GameObject("PickupVisualsRoot").transform;
        visualsRoot.SetParent(transform, false);

        beamGlow = CreateBeamLayer("BeamGlow", beamGlowWidthMultiplier, beamGlowAlpha, beamGlowSortingOrder);
        beamCore = CreateBeamLayer("BeamCore", 1f, 1f, beamCoreSortingOrder);
        dropIndicator = CreateDropIndicator();

        HideBeam();
        HideDropIndicator();
    }

    private LineRenderer CreateBeamLayer(string layerName, float widthMultiplier, float alphaMultiplier, int sortingOrder)
    {
        GameObject lineObj = new GameObject(layerName);
        lineObj.transform.SetParent(visualsRoot);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = beamCoreWidth * widthMultiplier;
        lr.endWidth = beamCoreWidth * widthMultiplier * 0.7f;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 4;
        lr.sortingOrder = sortingOrder;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        float baseAlpha = alphaMultiplier < 1f ? beamGlowAlpha : 1f;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(beamColor, 0f),
                new GradientColorKey(beamColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(beamStartAlpha * baseAlpha, 0f),
                new GradientAlphaKey(beamColor.a * baseAlpha, 0.7f),
                new GradientAlphaKey(beamEndAlpha * baseAlpha, 1f)
            }
        );
        lr.colorGradient = gradient;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) lr.material = new Material(shader);
        else lr.material = new Material(Shader.Find("Unlit/Transparent"));

        lr.enabled = false;
        return lr;
    }

    private SpriteRenderer CreateDropIndicator()
    {
        GameObject go = new GameObject("DropIndicator");
        go.transform.SetParent(visualsRoot);
        go.transform.localScale = indicatorScale;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GenerateCircleSprite();
        sr.color = indicatorColor;
        sr.sortingOrder = indicatorSortingOrder;

        go.SetActive(false);
        return sr;
    }

    private Sprite GenerateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius && dist >= radius - 4f) colors[y * size + x] = Color.white;
                else colors[y * size + x] = Color.clear;
            }
        }
        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    public void ShowBeam(Vector3 start, Vector3 end)
    {
        beamRequesters++;
        if (beamCore != null) beamCore.enabled = true;
        if (beamGlow != null) beamGlow.enabled = true;
        UpdateBeam(start, end);
    }

    public void UpdateBeam(Vector3 start, Vector3 end)
    {
        if (beamCore == null || !beamCore.enabled) return;
        beamCore.SetPosition(0, start);
        beamCore.SetPosition(1, end);
        if (beamGlow != null)
        {
            beamGlow.SetPosition(0, start);
            beamGlow.SetPosition(1, end);
        }
    }

    public void HideBeam()
    {
        beamRequesters = Mathf.Max(0, beamRequesters - 1);
        if (beamRequesters > 0) return;
        if (beamCore != null) beamCore.enabled = false;
        if (beamGlow != null) beamGlow.enabled = false;
    }

    public void ForceHideBeam()
    {
        beamRequesters = 0;
        if (beamCore != null) beamCore.enabled = false;
        if (beamGlow != null) beamGlow.enabled = false;
    }

    public void ShowDropIndicator(Vector3 worldPos)
    {
        if (dropIndicator == null) return;
        dropIndicator.gameObject.SetActive(true);
        dropIndicator.transform.position = worldPos;
    }

    public void UpdateDropIndicator(Vector3 worldPos)
    {
        if (dropIndicator == null || !dropIndicator.gameObject.activeSelf) return;
        dropIndicator.transform.position = worldPos;
    }

    public void HideDropIndicator()
    {
        if (dropIndicator == null) return;
        dropIndicator.gameObject.SetActive(false);
    }
}