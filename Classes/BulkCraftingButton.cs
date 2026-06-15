using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace BulkCrafting.Classes;

public class BulkCraftingButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly Color UnhoveredColor = new(34f / 255f, 34f / 255f, 34f / 255f);
    private static readonly Color HoveredColor = new(86f / 255f, 86f / 255f, 86f / 255f);
    private static readonly Color DisabledColor = new(44f / 255f, 19f / 255f, 19f / 255f);
    private static readonly Color BorderColor = new(31f / 255f, 28f / 255f, 25f / 255f);
    private Image _image;
    private TextMeshProUGUI _text;
    private bool _isHovered;

    public static BulkCraftingButton Create(Transform parent, string label, string name = "BulkCraftingButton",
        TMP_FontAsset font = null)
    {
        var borderGo = new GameObject(name + "_Border");
        borderGo.transform.SetParent(parent, false);

        var borderImage = borderGo.AddComponent<Image>();
        borderImage.color = BorderColor;
        borderImage.raycastTarget = false;

        var borderRect = borderGo.GetComponent<RectTransform>();

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.color = UnhoveredColor;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);

        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 22f;
        text.color = Color.white;
        if (font != null)
            text.font = font;

        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var handler = go.AddComponent<BulkCraftingButton>();
        handler.Button = button;
        handler._image = image;
        handler._text = text;
        handler.BorderRect = borderRect;
        handler.BorderGo = borderGo;

        return handler;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        if (!Button.interactable)
            return;

        _image.color = HoveredColor;
        AudioController.instance.PlayUI(AudioController.UIFXID.Hover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        if (Button.interactable)
            _image.color = UnhoveredColor;
    }

    private void Update()
    {
        if (!Button.interactable)
        {
            _image.color = DisabledColor;
            _text.color = Color.grey;
        }
        else
        {
            _text.color = Color.white;
            _image.color = _isHovered ? HoveredColor : UnhoveredColor;
        }
    }

    public Button Button { get; private set; }
    public RectTransform BorderRect { get; private set; }
    public GameObject BorderGo { get; private set; }
}
