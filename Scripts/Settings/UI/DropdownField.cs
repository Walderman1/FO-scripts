using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DropdownField : MonoBehaviour
{
    private TMP_Text buttonText;
    private Button button;

    private System.Action<int> onValueChanged;
    private string[] options;
    private int currentIndex;
    private GameObject listPopup;

    private void Init()
    {
        if (button != null && buttonText != null) return;

        button = GetComponent<Button>() ?? GetComponentInChildren<Button>(true);

        if (button != null)
        {
            buttonText = button.GetComponentInChildren<TMP_Text>(true);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(TogglePopup);
        }

        if (buttonText == null)
        {
            Transform t = transform.Find("DropdownButton/ButtonText");
            if (t != null) buttonText = t.GetComponent<TMP_Text>();
        }

        if (buttonText == null)
        {
            buttonText = GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void Awake()
    {
        Init();
    }

    public void Setup(string label, string[] opts, int curr, System.Action<int> callback)
    {
        Init();

        options = opts;
        currentIndex = curr;
        onValueChanged = callback;

        if (buttonText != null && options != null && options.Length > 0)
        {
            buttonText.text = options[Mathf.Clamp(currentIndex, 0, options.Length - 1)];
        }
        else
        {
            Logger.LogError(LogModule.UI, $"DropdownField: Не удалось обновить текст.");
        }
    }

    private void TogglePopup()
    {
        if (listPopup != null)
        {
            Destroy(listPopup);
            return;
        }

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) return;

        listPopup = new GameObject("DropdownBlocker", typeof(RectTransform), typeof(Image), typeof(Button));
        listPopup.transform.SetParent(rootCanvas.transform, false);

        var rt = listPopup.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.SetAsLastSibling();

        var bg = listPopup.GetComponent<Image>();
        bg.color = new Color(0, 0, 0, 0);

        var btn = listPopup.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => {
            if (listPopup != null) Destroy(listPopup);
        });

        GameObject listContainer = new GameObject("ListContainer", typeof(RectTransform), typeof(Image));
        listContainer.transform.SetParent(listPopup.transform, false);

        var listRt = listContainer.GetComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0.5f, 0.5f);
        listRt.anchorMax = new Vector2(0.5f, 0.5f);
        listRt.pivot = new Vector2(0.5f, 1f);

        RectTransform buttonRt = (RectTransform)transform;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(rootCanvas.worldCamera, buttonRt.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rootCanvas.transform, screenPos, rootCanvas.worldCamera, out Vector2 localPos);

        listRt.anchoredPosition = new Vector2(localPos.x, localPos.y);
        listRt.sizeDelta = new Vector2(buttonRt.rect.width, 0);

        var listBg = listContainer.GetComponent<Image>();
        listBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        var vlg = listContainer.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.spacing = 2;

        var fitter = listContainer.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = 0; i < options.Length; i++)
        {
            int index = i;
            GameObject item = new GameObject("Item", typeof(RectTransform), typeof(Image), typeof(Button));
            item.transform.SetParent(listContainer.transform, false);

            var itemImg = item.GetComponent<Image>();
            itemImg.color = (index == currentIndex) ? new Color(0.3f, 0.5f, 0.8f) : new Color(0.2f, 0.2f, 0.2f);

            var itemBtn = item.GetComponent<Button>();
            itemBtn.targetGraphic = itemImg;
            itemBtn.onClick.AddListener(() => {
                SelectOption(index);
            });

            GameObject txtObj = new GameObject("Text", typeof(RectTransform));
            txtObj.transform.SetParent(item.transform, false);
            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = options[index];
            tmp.fontSize = 20;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(10, 5);
            txtRt.offsetMax = new Vector2(-10, -5);

            var le = item.AddComponent<LayoutElement>();
            le.minHeight = 30;
        }
    }

    private void SelectOption(int index)
    {
        currentIndex = index;
        if (buttonText != null) buttonText.text = options[index];
        onValueChanged?.Invoke(index);
        if (listPopup != null) Destroy(listPopup);
    }

    private void OnDisable()
    {
        if (listPopup != null)
        {
            Destroy(listPopup);
            listPopup = null;
        }
    }
}