using UnityEngine;

// Повісити на будь-який об'єкт, який гравець зможе підняти (наприклад, куля, зброя, предмет).
// Об'єкт має мати Collider (не обов'язково Trigger - для виявлення гравцем достатньо звичайного).
[RequireComponent(typeof(Collider))]
public class Pickupable : MonoBehaviour
{
    [Tooltip("Чи зараз предмет у руках гравця")]
    public bool isHeld = false;

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

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void PickUp(Transform handAttachPoint)
    {
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
        // Якщо для предмета задані власні offset'и (напр. вудка має лежати не рівно
        // по нулях, а під кутом) - використовуємо їх, інакше поведінка як і раніше (нулі).
        transform.SetParent(handAttachPoint);
        transform.localPosition = attachPositionOffset;
        transform.localRotation = Quaternion.Euler(attachRotationOffset);
    }

    public void Drop(Vector3 dropWorldPosition, Vector3 throwVelocity = default)
    {
        isHeld = false;

        transform.SetParent(null);
        transform.position = dropWorldPosition;

        if (col != null)
            col.enabled = true;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = throwVelocity;
        }
    }
}