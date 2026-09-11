using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverImageSwap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Картинки для обміну")]
    [SerializeField] private Image normalImage;   // видима за замовчуванням
    [SerializeField] private Image hoverImage;     // видима при наведенні

    private void Start()
    {
        // Картинки-діти не повинні самі ловити raycast,
        // інакше наведення реагуватиме лише на їхню власну зону,
        // а не на всю область HoverZone.
        if (normalImage != null) normalImage.raycastTarget = false;
        if (hoverImage != null) hoverImage.raycastTarget = false;

        SetAlpha(normalImage, 1f);
        SetAlpha(hoverImage, 0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetAlpha(normalImage, 0f);
        SetAlpha(hoverImage, 1f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetAlpha(normalImage, 1f);
        SetAlpha(hoverImage, 0f);
    }

    private void SetAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}