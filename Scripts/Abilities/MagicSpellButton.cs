using UnityEngine;
using UnityEngine.UI;

public class MagicSpellButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    private Ability ability;
    private System.Action onClick;

    public Ability Ability => ability;

    private void Awake()
    {
        if (iconImage == null)
            iconImage = FindIconImage();
    }

    private Image FindIconImage()
    {
        Transform t = transform.Find("Image");
        if (t != null)
        {
            Image img = t.GetComponent<Image>();
            if (img != null) return img;
        }
        return GetComponentInChildren<Image>();
    }

    public void Initialize(Ability newAbility, System.Action callback)
    {
        ability = newAbility;
        onClick = callback;

        if (iconImage == null)
            iconImage = FindIconImage();

        ApplyIcon();

        Button btn = GetComponent<Button>();
        if (btn == null) btn = gameObject.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(HandleClick);
    }

    private void ApplyIcon()
    {
        if (iconImage == null || ability == null) return;

        Sprite icon = Resources.Load<Sprite>($"UI/Sprite/{ability.abilityName}");
        if (icon != null)
        {
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
        }
        else
        {
            Logger.LogWarning(LogModule.Abilities,
                $"Иконка не найдена: Resources/UI/Sprite/{ability.abilityName}");
        }
    }

    private void HandleClick()
    {
        onClick?.Invoke();
    }
}