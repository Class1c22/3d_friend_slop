using UnityEngine;

// Повісити на персонажа гравця (mainhero_animated).
public class PlayerPickup : MonoBehaviour
{
    [Tooltip("Точка в руці, куди буде 'телепортуватись' предмет за замовчуванням (порожній GameObject у долоні). Використовується, якщо у предмета не задано власну customAttachPoint.")]
    public Transform handAttachPoint;

    [Tooltip("Радіус, у якому можна підняти предмет")]
    public float pickupRadius = 2f;

    [Tooltip("Шар, на якому знаходяться предмети для підбору (щоб не чіплялись зайві об'єкти)")]
    public LayerMask pickupableLayer;

    [Tooltip("Посилання на контролер анімацій рук - щоб вмикати/вимикати IsHolding, грати анімацію взяття в руки та тримати позу риболовлі")]
    public HandAnimatorController handAnimatorController;

    [Tooltip("Назва Equip Anim Trigger, яка означає саме вудку (має співпадати з полем Equip Anim Trigger на об'єкті вудки)")]
    public string fishingRodTriggerName = "EquipRod";

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
            // Якщо у предмета задана власна точка кріплення (напр. окреме місце
            // для вудки) - використовуємо саме її, інакше - загальна точка руки.
            Transform attachTarget = closest.customAttachPoint != null
                ? closest.customAttachPoint
                : handAttachPoint;

            closest.PickUp(attachTarget);
            currentlyHeld = closest;

            if (handAnimatorController != null)
            {
                handAnimatorController.SetHolding(true);

                // Якщо для цього предмета задана окрема анімація "взяти в руки" -
                // програємо саме її (напр. EquipRod для вудки).
                handAnimatorController.PlayEquipAnimation(closest.equipAnimTrigger);

                // Якщо взяли саме вудку - тримаємо позу риболовлі, поки вудка в руках.
                bool isFishingRod = closest.equipAnimTrigger == fishingRodTriggerName;
                handAnimatorController.SetFishingEquipped(isFishingRod);
            }
        }
    }

    void ThrowCurrent()
    {
        if (currentlyHeld == null) return;

        // Стартова точка кидка - трохи попереду гравця, щоб предмет не застряг у тілі
        Vector3 throwStartPos = transform.position - transform.forward * 1f + Vector3.up * 1f;
        Vector3 throwVelocity = -transform.forward * throwForwardForce + Vector3.up * throwUpwardForce;

        currentlyHeld.Drop(throwStartPos, throwVelocity);

        if (handAnimatorController != null)
        {
            handAnimatorController.SetHolding(false);

            // Виходимо з пози риболовлі незалежно від того, що саме кидали -
            // якщо це була не вудка, прапорець і так вже був false.
            handAnimatorController.SetFishingEquipped(false);
        }

        currentlyHeld = null;
    }
}