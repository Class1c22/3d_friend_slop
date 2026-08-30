using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Керує рядом з 4 слотів інвентаря: фоновий Image показує стан підсвітки
/// (нічого не вибрано / вибрано слот N - готові PNG з Figma), а окремі Image
/// поверх нього (slotIconImages) показують іконку предмета, що реально лежить у слоті.
///
/// Тут же зберігається, який саме Pickupable лежить у кожному слоті (AddItem/RemoveItem),
/// і генеруються події OnEquip/OnUnequip, на які підписується PlayerPickup, щоб фізично
/// показати/прибрати предмет з руки, коли змінюється обраний слот.
///
/// МЕРЕЖА: цей скрипт НЕ синхронізується по Photon - кожен гравець бачить
/// і керує лише своїм власним інвентарем. GameObject/Canvas з цим скриптом
/// вимикається на чужих копіях аватара через PlayerRig.cs (photonView.IsMine),
/// тому Update() тут ніколи не читає чужий scroll-input.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    [Header("Background image showing the whole row")]
    [SerializeField] private Image rowImage;

    [Header("Sprites: index 0 = normal (nothing selected), 1..4 = selected slot 1..4")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite[] selectedSprites = new Sprite[4]; // selectedSprites[0] = slot 1 selected, etc.

    [Header("Іконки предметів у слотах (звичайний розмір, коли слот НЕ вибраний)")]
    [Tooltip("Окремі UI Image поверх фону - по одному на слот. Показують item.icon, коли слот зайнятий і НЕ є поточним обраним.")]
    [SerializeField] private Image[] slotIconImages = new Image[4];

    [Header("Іконки предметів у слотах (збільшений розмір, коли слот ВИБРАНИЙ)")]
    [Tooltip("Окремі UI Image, розташовані/розтягнуті під збільшений кадр підсвітки. Показують item.icon тільки для того слоту, який зараз обрано.")]
    [SerializeField] private Image[] slotIconImagesSelected = new Image[4];

    [Header("Scroll wheel switching")]
    [Tooltip("Якщо увімкнено — прокрутка коліщатком завжди тримає один зі слотів вибраним (як зброя в шутерах). Якщо вимкнено — можна докрутити до стану 'нічого не вибрано'.")]
    [SerializeField] private bool alwaysKeepOneSelected = true;
    [Tooltip("Чутливість коліщатка — більше значення = треба сильніше крутнути для перемикання")]
    [SerializeField] private float scrollThreshold = 0.05f;

    private const int slotCount = 4;
    private int selectedIndex = -1; // -1 = нічого не вибрано
    private readonly Pickupable[] items = new Pickupable[slotCount];

    /// <summary>Стріляє, коли треба фізично взяти предмет в руку (обрано слот, у якому щось лежить).</summary>
    public event Action<Pickupable> OnEquip;
    /// <summary>Стріляє, коли треба прибрати предмет з руки (обрано пустий слот / знято виділення).</summary>
    public event Action OnUnequip;

    private void Start()
    {
        if (selectedIndex < 0 && alwaysKeepOneSelected)
            selectedIndex = 0;

        RefreshVisual();
        NotifyEquipState();
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
        NotifyEquipState();
    }

    public void SelectSlot(int index)
    {
        // повторний клік по вже обраному слоту знімає виділення
        selectedIndex = selectedIndex == index ? -1 : index;
        RefreshVisual();
        NotifyEquipState();
    }

    public void ClearSelection()
    {
        selectedIndex = -1;
        RefreshVisual();
        NotifyEquipState();
    }

    /// <summary>Чи є хоч один вільний слот у інвентарі.</summary>
    public bool HasFreeSlot()
    {
        for (int i = 0; i < slotCount; i++)
            if (items[i] == null) return true;

        return false;
    }

    /// <summary>
    /// Кладе предмет у перший вільний слот. Якщо в руці зараз нічого немає - одразу
    /// стає обраним (і викличе OnEquip, щоб PlayerPickup показав його в руці).
    /// Повертає індекс слота, або -1, якщо інвентар повний.
    /// </summary>
    public int AddItem(Pickupable item)
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (items[i] != null) continue;

            items[i] = item;
            RefreshSlotIcon(i);

            // Якщо руки зараз порожні - переходимо на щойно підібраний слот
            if (SelectedItem == null)
            {
                selectedIndex = i;
                RefreshVisual();
            }

            NotifyEquipState();
            return i;
        }

        Debug.LogWarning("[InventoryManager] Інвентар повний - немає вільного слота.");
        return -1;
    }

    /// <summary>
    /// Прибирає предмет з інвентаря (напр. при викиданні). Якщо він саме зараз обраний -
    /// знімає виділення (це викличе OnUnequip).
    /// </summary>
    public void RemoveItem(Pickupable item)
    {
        for (int i = 0; i < slotCount; i++)
        {
            if (items[i] != item) continue;

            items[i] = null;
            RefreshSlotIcon(i);

            if (selectedIndex == i)
            {
                selectedIndex = -1;
                RefreshVisual();
            }

            NotifyEquipState();
            return;
        }
    }

    public Pickupable GetItem(int index)
    {
        if (index < 0 || index >= slotCount) return null;
        return items[index];
    }

    public int SelectedIndex => selectedIndex;
    public Pickupable SelectedItem => selectedIndex >= 0 ? items[selectedIndex] : null;

    private void NotifyEquipState()
    {
        Pickupable current = SelectedItem;

        if (current != null)
            OnEquip?.Invoke(current);
        else
            OnUnequip?.Invoke();
    }

    private void RefreshSlotIcon(int index)
    {
        Sprite icon = items[index] != null ? items[index].icon : null;

        // задаємо спрайт в обидва Image (звичайний і збільшений) - показує/ховає RefreshVisual
        if (slotIconImages != null && index < slotIconImages.Length && slotIconImages[index] != null)
            slotIconImages[index].sprite = icon;

        if (slotIconImagesSelected != null && index < slotIconImagesSelected.Length && slotIconImagesSelected[index] != null)
            slotIconImagesSelected[index].sprite = icon;

        RefreshIconVisibility(index);
    }

    /// <summary>
    /// Вмикає/вимикає обидва Image іконки для одного слота залежно від того,
    /// чи є там предмет і чи саме цей слот зараз обраний (тоді показуємо збільшену версію).
    /// </summary>
    private void RefreshIconVisibility(int index)
    {
        bool hasItem = items[index] != null;
        bool isSelected = index == selectedIndex;

        if (slotIconImages != null && index < slotIconImages.Length && slotIconImages[index] != null)
            slotIconImages[index].enabled = hasItem && !isSelected;

        if (slotIconImagesSelected != null && index < slotIconImagesSelected.Length && slotIconImagesSelected[index] != null)
            slotIconImagesSelected[index].enabled = hasItem && isSelected;
    }

    private void RefreshVisual()
    {
        if (rowImage != null)
        {
            rowImage.sprite = selectedIndex < 0
                ? normalSprite
                : selectedSprites[selectedIndex];
        }

        // при кожній зміні виділення - перерахувати, яка з двох іконок (звичайна/збільшена)
        // повинна бути видима в кожному слоті
        for (int i = 0; i < slotCount; i++)
            RefreshIconVisibility(i);
    }
}