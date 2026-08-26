using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Керує одним фоновим Image, що показує весь ряд слотів як цілісний спрайт.
/// Кожен стан (нічого не вибрано / вибрано слот N) — окремий готовий PNG з Figma.
/// Клікабельні зони (InventoryClickZone) викликають SelectSlot(index).
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [Header("Background image showing the whole row")]
    [SerializeField] private Image rowImage;

    [Header("Sprites: index 0 = normal (nothing selected), 1..4 = selected slot 1..4")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite[] selectedSprites = new Sprite[4]; // selectedSprites[0] = slot 1 selected, etc.

    [Header("Scroll wheel switching")]
    [Tooltip("Якщо увімкнено — прокрутка коліщатком завжди тримає один зі слотів вибраним (як зброя в шутерах). Якщо вимкнено — можна докрутити до стану 'нічого не вибрано'.")]
    [SerializeField] private bool alwaysKeepOneSelected = true;
    [Tooltip("Чутливість коліщатка — більше значення = треба сильніше крутнути для перемикання")]
    [SerializeField] private float scrollThreshold = 0.05f;

    private int selectedIndex = -1; // -1 = нічого не вибрано
    private int slotCount = 4;

    private void Start()
    {
        if (selectedIndex < 0 && alwaysKeepOneSelected)
            selectedIndex = 0;

        RefreshVisual();
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > scrollThreshold)
            CycleSelection(1);
        else if (scroll < -scrollThreshold)
            CycleSelection(-1);
    }

    /// <summary>
    /// Перемикає вибір на один слот вперед/назад.
    /// direction: +1 — вперед (наступний слот), -1 — назад (попередній).
    /// </summary>
    public void CycleSelection(int direction)
    {
        if (alwaysKeepOneSelected)
        {
            if (selectedIndex < 0) selectedIndex = 0;
            selectedIndex = (selectedIndex + direction + slotCount) % slotCount;
        }
        else
        {
            // -1 (нічого не вибрано) теж є "позицією" в циклі: -1, 0, 1, 2, 3, знову -1...
            selectedIndex += direction;

            if (selectedIndex >= slotCount) selectedIndex = -1;
            else if (selectedIndex < -1) selectedIndex = slotCount - 1;
        }

        RefreshVisual();
    }

    public void SelectSlot(int index)
    {
        // повторний клік по вже обраному слоту знімає виділення
        selectedIndex = selectedIndex == index ? -1 : index;
        RefreshVisual();
    }

    public void ClearSelection()
    {
        selectedIndex = -1;
        RefreshVisual();
    }

    public int SelectedIndex => selectedIndex;

    private void RefreshVisual()
    {
        if (rowImage == null) return;

        rowImage.sprite = selectedIndex < 0
            ? normalSprite
            : selectedSprites[selectedIndex];
    }
}