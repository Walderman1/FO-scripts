using UnityEngine;

[ExecuteAlways]
public class FlipAllSprites : MonoBehaviour
{
    public bool flipOnStart = true;

    void Start()
    {
        if (flipOnStart)
        {
            ApplyFlip();
        }
    }

    void OnValidate()
    {
        if (flipOnStart)
        {
            ApplyFlip();
        }
        else
        {
            RevertFlip();
        }
        ResetChildrenScale();
    }

    void ApplyFlip()
    {
        Vector3 scale = transform.localScale;
        if (scale.x > 0)
        {
            scale.x *= -1;
        }
        transform.localScale = scale;
    }

    void RevertFlip()
    {
        Vector3 scale = transform.localScale;
        if (scale.x < 0)
        {
            scale.x *= -1;
        }
        transform.localScale = scale;
    }

    void ResetChildrenScale()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child == transform) continue;
            child.localScale = Vector3.one;
        }
    }
}