using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ColorBlindFilter : MonoBehaviour
{
    private Volume volume;
    private VolumeProfile profile;
    private ChannelMixer channelMixer;

    public void Initialize()
    {
        var volumeGO = new GameObject("ColorBlindVolume");
        volumeGO.transform.SetParent(transform, false);

        volume = volumeGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1000f;

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.profile = profile;

        channelMixer = profile.Add<ChannelMixer>(true);
        ResetMixer();
    }

    public void SetMode(ColorBlindMode mode)
    {
        if (channelMixer == null) return;

        switch (mode)
        {
            case ColorBlindMode.None:
                ResetMixer();
                break;

            case ColorBlindMode.Deuteranopia:
                SetMatrix(70f, 30f, 0f, 0f, 70f, 30f, 0f, 30f, 70f);
                break;

            case ColorBlindMode.Protanopia:
                SetMatrix(56f, 44f, 0f, 0f, 56f, 44f, 0f, 24f, 76f);
                break;

            case ColorBlindMode.Tritanopia:
                SetMatrix(95f, 5f, 0f, 0f, 43f, 57f, 0f, 47f, 53f);
                break;
        }
    }

    private void ResetMixer()
    {
        SetMatrix(100f, 0f, 0f, 0f, 100f, 0f, 0f, 0f, 100f);
    }

    private void SetMatrix(float rr, float rg, float rb,
                            float gr, float gg, float gb,
                            float br, float bg, float bb)
    {
        if (channelMixer == null) return;

        channelMixer.redOutRedIn.value = rr;
        channelMixer.redOutGreenIn.value = rg;
        channelMixer.redOutBlueIn.value = rb;

        channelMixer.greenOutRedIn.value = gr;
        channelMixer.greenOutGreenIn.value = gg;
        channelMixer.greenOutBlueIn.value = gb;

        channelMixer.blueOutRedIn.value = br;
        channelMixer.blueOutGreenIn.value = bg;
        channelMixer.blueOutBlueIn.value = bb;
    }
}