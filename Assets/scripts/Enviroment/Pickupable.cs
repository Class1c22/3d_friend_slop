using UnityEngine;

// Повісити на будь-який об'єкт, який гравець зможе підняти (наприклад, куля, зброя, предмет).
// Об'єкт має мати Collider (не обов'язково Trigger - для виявлення гравцем достатньо звичайного).
[RequireComponent(typeof(Collider))]
public class Pickupable : MonoBehaviour
{
    [Tooltip("Чи зараз предмет у руках гравця")]
    public bool isHeld = false;

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

        // "Телепортуємо" в руку: робимо дочірнім об'єктом точки прикріплення
        transform.SetParent(handAttachPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
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