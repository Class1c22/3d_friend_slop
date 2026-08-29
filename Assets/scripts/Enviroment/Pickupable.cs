using UnityEngine;

// Повісити на будь-який об'єкт, який гравець зможе підняти (наприклад, куля, зброя, предмет).
// Об'єкт має мати Collider (не обов'язково Trigger - для виявлення гравцем достатньо звичайного).
[RequireComponent(typeof(Collider))]
public class Pickupable : MonoBehaviour
{
    [Tooltip("Чи зараз предмет належить гравцю (в руці АБО в інвентарі, не в світі)")]
    public bool isHeld = false;

    [Header("Іконка для слота інвентаря")]
    [Tooltip("Показується в InventoryManager, коли предмет лежить у слоті")]
    public Sprite icon;

    [Header("Індивідуальне кріплення в руці (необов'язково)")]
    [Tooltip("Якщо задано - предмет кріпиться саме сюди замість загальної точки руки гравця (напр. окреме місце для вудки). Для звичайних предметів лишити пустим.")]
    public Transform customAttachPoint;

    [Tooltip("Локалье зміщення позиції відносно точки кріплення (щоб предмет ліг у руку під потрібним кутом/зсувом)")]
    public Vector3 attachPositionOffset = Vector3.zero;

    [Tooltip("Локальне зміщення повороту відносно точки кріплення, в градусах")]
    public Vector3 attachRotationOffset = Vector3.zero;

    [Header("Анімація взяття в руки (необов'язково)")]
    [Tooltip("Назва Trigger-параметра в Animator рук, який зіграє при підборі цього предмета. Пусто = без окремої анімації (звичайний IsHolding, як раніше).")]
    public string equipAnimTrigger;

    // Зберігаємо оригінальні налаштування фізики, щоб повернути при викиданні
    private Rigidbody rb;
    private Collider col;

    // Природний поворот предмета "в світі" (яким його розставив дизайнер/спавнер),
    // ЗАФІКСОВАНИЙ ДО першого підбору. Потрібен, щоб при Drop() повернути предмет
    // саме в цю орієнтацію, а не в кут attachRotationOffset, під яким він лежав у руці -
    // інакше викинутий предмет виглядає "зігнутим"/перекошеним відносно землі.
    private Quaternion defaultWorldRotation;

    // Оригінальний localScale предмета, ЗАФІКСОВАНИЙ ДО першого підбору.
    // Потрібен, щоб явно відновлювати правильний масштаб при PickUp/Store/Drop,
    // а не покладатись на автоматичний перерахунок Unity в SetParent(x, true) -
    // саме він і "роздував"/спотворював предмет, якщо серед батьків руки
    // трапляється нерівномірний (не 1,1,1) масштаб.
    private Vector3 originalScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        defaultWorldRotation = transform.rotation;
        originalScale = transform.localScale;
    }

    /// <summary>
    /// Фізично прикріплює предмет до руки (те, що бачить гравець в руках).
    /// </summary>
    public void PickUp(Transform handAttachPoint)
    {
        gameObject.SetActive(true); // на випадок якщо предмет лежав "схований" в інвентарі (Store())
        isHeld = true;

        // Вимикаємо фізику, поки предмет в руці
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }

        // Вимикаємо колайдер, щоб не заважав рухатись і не тригерив повторний підбір
        if (col != null)
            col.enabled = false;

        // "Телепортуємо" в руку: робимо дочірнім об'єктом точки прикріплення.
        // false = НЕ намагатись зберегти world position/rotation/scale при зміні батька.
        // Саме дефолтне true викликало неправильний авто-перерахунок localScale,
        // якщо десь у батьках handAttachPoint є нерівномірний масштаб - звідси й "роздування".
        transform.SetParent(handAttachPoint, false);
        transform.localPosition = attachPositionOffset;
        transform.localRotation = Quaternion.Euler(attachRotationOffset);
        transform.localScale = originalScale; // явно, а не покладаємось на Unity
    }

    /// <summary>
    /// "Ховає" предмет, поки він лежить в інвентарі, але не в руці:
    /// вимикає рендер/колайдер і від'єднує від руки. Гравець досі ним володіє (isHeld = true),
    /// просто зараз тримає в руці щось інше (або нічого).
    /// </summary>
    public void Store()
    {
        isHeld = true;

        transform.SetParent(null, false);
        transform.localScale = originalScale;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }

        if (col != null)
            col.enabled = false;

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Повертає предмет у світ (викидання). Знімає isHeld повністю - предмет знову підбирабельний.
    /// </summary>
    public void Drop(Vector3 dropWorldPosition, Vector3 throwVelocity = default)
    {
        gameObject.SetActive(true); // якщо викидаємо прямо зі "схованого" стану інвентаря
        isHeld = false;

        transform.SetParent(null, false);
        transform.position = dropWorldPosition;
        transform.rotation = defaultWorldRotation; // повертаємо природну орієнтацію, а не кут, під яким предмет лежав у руці
        transform.localScale = originalScale; // підстраховка від накопиченої похибки масштабу

        if (col != null)
            col.enabled = true;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = throwVelocity;
        }
    }
}