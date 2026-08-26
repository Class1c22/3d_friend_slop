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

    [Tooltip("Посилання на інвентар. Якщо не задано - предмети підбираються напряму в руку по-старому, без слотів.")]
    public InventoryManager inventoryManager;

    [Tooltip("Назва Equip Anim Trigger, яка означає саме вудку (має співпадати з полем Equip Anim Trigger на об'єкті вудки)")]
    public string fishingRodTriggerName = "EquipRod";

    [Tooltip("Сила кидка вперед")]
    public float throwForwardForce = 6f;

    [Tooltip("Сила кидка вгору (щоб предмет летів дугою, а не по прямій)")]
    public float throwUpwardForce = 2f;

    private Pickupable currentlyHeld = null;

    void Awake()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnEquip += HandleEquip;
            inventoryManager.OnUnequip += HandleUnequip;
        }
    }

    void OnDestroy()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnEquip -= HandleEquip;
            inventoryManager.OnUnequip -= HandleUnequip;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            TryPickUpNearby();

        if (Input.GetKeyDown(KeyCode.R) && currentlyHeld != null)
            ThrowCurrent();
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

        if (closest == null) return;

        if (inventoryManager != null)
        {
            if (!inventoryManager.HasFreeSlot())
            {
                Debug.Log("[PlayerPickup] Інвентар повний - неможливо підняти предмет.");
                return;
            }

            // Ховаємо предмет за замовчуванням (він тепер "в інвентарі").
            // Якщо руки зараз порожні - AddItem одразу викличе OnEquip -> HandleEquip,
            // і той самий кадр покаже предмет у руці (перекриє щойно виконаний Store()).
            closest.Store();
            inventoryManager.AddItem(closest);
        }
        else
        {
            // Фолбек, якщо InventoryManager не призначений - стара поведінка "напряму в руку"
            EquipItem(closest);
        }
    }

    /// <summary>Викликається InventoryManager, коли треба показати конкретний предмет у руці.</summary>
    private void HandleEquip(Pickupable item)
    {
        if (currentlyHeld == item) return;

        // Попередній предмет лишається в інвентарі, просто ховаємо його з руки
        if (currentlyHeld != null)
            currentlyHeld.Store();

        EquipItem(item);
    }

    /// <summary>Викликається InventoryManager, коли обрано пустий слот / знято виділення.</summary>
    private void HandleUnequip()
    {
        if (currentlyHeld != null)
            currentlyHeld.Store();

        currentlyHeld = null;

        if (handAnimatorController != null)
        {
            handAnimatorController.SetHolding(false);
            handAnimatorController.SetFishingEquipped(false);
        }
    }

    private void EquipItem(Pickupable item)
    {
        Transform attachTarget = item.customAttachPoint != null
            ? item.customAttachPoint
            : handAttachPoint;

        item.PickUp(attachTarget);
        currentlyHeld = item;

        if (handAnimatorController != null)
        {
            handAnimatorController.SetHolding(true);

            // Якщо для цього предмета задана окрема анімація "взяти в руки" -
            // програємо саме її (напр. EquipRod для вудки).
            handAnimatorController.PlayEquipAnimation(item.equipAnimTrigger);

            // Якщо взяли саме вудку - тримаємо позу риболовлі, поки вудка в руках.
            bool isFishingRod = item.equipAnimTrigger == fishingRodTriggerName;
            handAnimatorController.SetFishingEquipped(isFishingRod);
        }
    }

    void ThrowCurrent()
    {
        if (currentlyHeld == null) return;

        Vector3 throwStartPos = transform.position - transform.forward * 1f + Vector3.up * 1f;
        Vector3 throwVelocity = -transform.forward * throwForwardForce + Vector3.up * throwUpwardForce;

        Pickupable thrown = currentlyHeld;

        if (inventoryManager != null)
            inventoryManager.RemoveItem(thrown); // прибирає зі слота і викличе HandleUnequip

        thrown.Drop(throwStartPos, throwVelocity);
        currentlyHeld = null;

        if (handAnimatorController != null)
        {
            handAnimatorController.SetHolding(false);
            handAnimatorController.SetFishingEquipped(false);
        }
    }
}