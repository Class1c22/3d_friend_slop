using System.Collections;
using UnityEngine;

/// <summary>
/// Логіка риболовлі: гравець тримає вудку в руці (equipAnimTrigger == "EquipRod",
/// див. PlayerPickup.fishingRodTriggerName).
///
/// Флоу:
/// 1. ПКМ, поки вудка в руках і гравець вільний (Idle) - "закидає гачок":
///    Raycast від камери; якщо влучили у воду (waterLayer) - починається очікування клювання.
/// 2. Через випадковий час (biteWaitMin..biteWaitMax) риба "клює" - відкривається
///    коротке вікно (strikeWindow), протягом якого гравець має ЩЕ РАЗ клікнути ПКМ,
///    щоб підсікти рибу. Автоматично риба НЕ ловиться.
/// 3. Якщо гравець встиг клікнути вчасно - грається анімація підсічки (swing rod),
///    потім анімація витягування, і випадкова риба з fishPrefabs потрапляє в інвентар
///    (як Pickupable, "схований", через ту саму систему AddItem/Store, що й для звичайних предметів).
/// 4. Якщо гравець НЕ встиг клікнути протягом strikeWindow - риба зривається,
///    гачок повертається в стан Idle і можна закидати знов.
///
/// Повісити на того самого persona-об'єкта, де є PlayerPickup.
/// </summary>
public class FishingController : MonoBehaviour
{
    private enum FishingState
    {
        Idle,           // вудка вільна, можна закидати
        WaitingForBite, // гачок закинуто, чекаємо, поки риба клюне
        WaitingForStrike, // риба клюнула - гравцю треба встигнути клікнути (підсікти)
        Reeling         // йде анімація підсічки/витягування, після якої риба потрапить в інвентар
    }

    [Header("Посилання")]
    [Tooltip("Скрипт підбору/екіпірування - звідси береться, чи саме вудка зараз в руці")]
    public PlayerPickup playerPickup;

    [Tooltip("Куди складати впійману рибу")]
    public InventoryManager inventoryManager;

    [Tooltip("Камера гравця - звідси йде промінь закидання. Якщо не задано - Camera.main")]
    public Camera playerCamera;

    [Tooltip("Аніматор самої вудки (окремий Animator на об'єкті вудки/руки, що тримає її) - для анімацій підсічки та витягування риби")]
    public Animator rodAnimator;

    [Header("Закидання")]
    [Tooltip("Шар, на якому знаходиться вода (Plane з HeightmapIsland.seaLevel)")]
    public LayerMask waterLayer;
    [Tooltip("Максимальна дистанція закидання гачка")]
    public float castRange = 25f;

    [Header("Клювання")]
    [Tooltip("Мінімальний і максимальний час очікування клювання (секунди)")]
    public float biteWaitMin = 2f;
    public float biteWaitMax = 5f;

    [Header("Підсічка (гравець мусить клікнути вчасно)")]
    [Tooltip("Скільки секунд є в гравця, щоб клікнути ПКМ і підсікти рибу після того, як вона клюнула. Не встиг - риба зірветься.")]
    public float strikeWindow = 1.2f;

    [Tooltip("Назва Trigger-параметра в rodAnimator, що грає в момент підсічки (swing rod). Пусто = без анімації.")]
    public string strikeTriggerName = "CastRod";
    [Tooltip("Тривалість анімації підсічки (сек) перед тим, як почне гратись анімація витягування.")]
    public float strikeAnimDuration = 0.3f;

    [Header("Витягування (після вдалої підсічки)")]
    [Tooltip("Назва Trigger-параметра в rodAnimator, що грає під час витягування впійманої риби. Пусто = без анімації.")]
    public string reelInTriggerName = "CatchFish";
    [Tooltip("Затримка (сек) перед тим, як риба фактично потрапляє в інвентар - щоб встигла програтись анімація витягування.")]
    public float reelInAnimDuration = 0.8f;

    [Header("Риба (перетягни сюди 5 префабів Pickupable-риб)")]
    public Pickupable[] fishPrefabs;

    private FishingState state = FishingState.Idle;
    private Coroutine biteRoutine;
    private Coroutine strikeTimeoutRoutine;

    void Update()
    {
        // Керувати вудкою можна лише поки вона в руках
        if (playerPickup == null || !playerPickup.IsHoldingFishingRod) return;

        if (!Input.GetMouseButtonDown(1)) return;

        switch (state)
        {
            case FishingState.Idle:
                TryCast();
                break;

            case FishingState.WaitingForStrike:
                Strike();
                break;

            // WaitingForBite і Reeling - клік ігнорується, треба чекати
            default:
                break;
        }
    }

    private void TryCast()
    {
        Camera cam = playerCamera != null ? playerCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[FishingController] Не задано камеру - неможливо визначити напрямок закидання.");
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, castRange, waterLayer))
        {
            state = FishingState.WaitingForBite;
            biteRoutine = StartCoroutine(WaitForBiteRoutine());
        }
        else
        {
            Debug.Log("[FishingController] Гачок не влучив у воду - спробуй прицілитись точніше.");
        }
    }

    private IEnumerator WaitForBiteRoutine()
    {
        float waitTime = Random.Range(biteWaitMin, biteWaitMax);
        yield return new WaitForSeconds(waitTime);

        // Риба клюнула - відкриваємо вікно на підсічку
        state = FishingState.WaitingForStrike;
        Debug.Log("[FishingController] Клює! Клікни ще раз (ПКМ), щоб підсікти!");

        strikeTimeoutRoutine = StartCoroutine(StrikeTimeoutRoutine());
    }

    private IEnumerator StrikeTimeoutRoutine()
    {
        yield return new WaitForSeconds(strikeWindow);

        // Якщо гравець так і не клікнув - риба зривається
        if (state == FishingState.WaitingForStrike)
        {
            Debug.Log("[FishingController] Не встиг підсікти - риба зірвалась.");
            state = FishingState.Idle;
        }
    }

    private void Strike()
    {
        if (strikeTimeoutRoutine != null)
        {
            StopCoroutine(strikeTimeoutRoutine);
            strikeTimeoutRoutine = null;
        }

        state = FishingState.Reeling;
        StartCoroutine(ReelInRoutine());
    }

    private IEnumerator ReelInRoutine()
    {
        // Анімація підсічки
        if (rodAnimator != null && !string.IsNullOrEmpty(strikeTriggerName))
        {
            rodAnimator.SetTrigger(strikeTriggerName);
            yield return new WaitForSeconds(strikeAnimDuration);
        }

        // Анімація витягування
        if (rodAnimator != null && !string.IsNullOrEmpty(reelInTriggerName))
        {
            rodAnimator.SetTrigger(reelInTriggerName);
            yield return new WaitForSeconds(reelInAnimDuration);
        }

        CatchRandomFish();

        state = FishingState.Idle;
    }

    private void CatchRandomFish()
    {
        if (fishPrefabs == null || fishPrefabs.Length == 0)
        {
            Debug.LogWarning("[FishingController] Список fishPrefabs порожній - нема з чого ловити.");
            return;
        }

        if (inventoryManager == null)
        {
            Debug.LogWarning("[FishingController] InventoryManager не призначено.");
            return;
        }

        if (!inventoryManager.HasFreeSlot())
        {
            Debug.Log("[FishingController] Спіймали рибу, але інвентар повний - вона зірвалась.");
            return;
        }

        Pickupable prefab = fishPrefabs[Random.Range(0, fishPrefabs.Length)];

        // Створюємо саме інстанс (а не сам ассет), одразу ховаємо його як "предмет у кишені"
        // і кладемо в перший вільний слот - так само, як звичайний підбір через PlayerPickup.
        Pickupable fishInstance = Instantiate(prefab);
        fishInstance.Store();
        inventoryManager.AddItem(fishInstance);

        Debug.Log($"[FishingController] Спіймано рибу: {prefab.name}");
    }
}