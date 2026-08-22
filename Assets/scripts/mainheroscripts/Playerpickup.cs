using UnityEngine;

// Повісити на персонажа гравця (mainhero_animated).
public class PlayerPickup : MonoBehaviour
{
    [Tooltip("Точка в руці, куди буде 'телепортуватись' предмет (порожній GameObject у долоні)")]
    public Transform handAttachPoint;

    [Tooltip("Радіус, у якому можна підняти предмет")]
    public float pickupRadius = 2f;

    [Tooltip("Шар, на якому знаходяться предмети для підбору (щоб не чіплялись зайві об'єкти)")]
    public LayerMask pickupableLayer;

    [Tooltip("Посилання на контролер анімацій рук - щоб вмикати/вимикати IsHolding")]
    public HandAnimatorController handAnimatorController;

    [Tooltip("Сила кидка вперед")]
    public float throwForwardForce = 6f;

    [Tooltip("Сила кидка вгору (щоб предмет летів дугою, а не по прямій)")]
    public float throwUpwardForce = 2f;

    private Pickupable currentlyHeld = null;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && currentlyHeld == null)
        {
            TryPickUpNearby();
        }

        if (Input.GetKeyDown(KeyCode.R) && currentlyHeld != null)
        {
            ThrowCurrent();
        }
    }

    void TryPickUpNearby()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius, pickupableLayer);

        Pickupable closest = null;
        float closestDist = float.MaxValue;

        foreach (Collider hit in hits)
        {
            Pickupable p = hit.GetComponent<Pickupable>();
            if (p != null && !p.isHeld)
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = p;
                }
            }
        }

        if (closest != null)
        {
            closest.PickUp(handAttachPoint);
            currentlyHeld = closest;

            if (handAnimatorController != null)
                handAnimatorController.SetHolding(true);
        }
    }

    void ThrowCurrent()
    {
        if (currentlyHeld == null) return;

        // Стартова точка кидка - трохи попереду гравця, щоб предмет не застряг у тілі
        Vector3 throwStartPos = transform.position - transform.forward * 1f + Vector3.up * 1f;
        Vector3 throwVelocity = -transform.forward * throwForwardForce + Vector3.up * throwUpwardForce;

        currentlyHeld.Drop(throwStartPos, throwVelocity);
        currentlyHeld = null;

        if (handAnimatorController != null)
            handAnimatorController.SetHolding(false);
    }
}