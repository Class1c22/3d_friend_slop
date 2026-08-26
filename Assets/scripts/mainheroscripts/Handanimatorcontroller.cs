using UnityEngine;

public class HandAnimatorController : MonoBehaviour
{
    [Tooltip("Animator компонент на об'єкті руки (або перетягніть сюди вручну)")]
    public Animator handAnimator;

    [Tooltip("Швидкість, вище якої вважаємо що персонаж рухається")]
    public float moveThreshold = 0.1f;

    [Tooltip("Швидкість, вище якої вмикається пришвидшений біг (runfaster)")]
    public float sprintThreshold = 5f;

    [Tooltip("Множник швидкості при утриманні клавіші бігу (Left Shift)")]
    public float sprintMultiplier = 2f;

    [Tooltip("Клавіша для стрибка")]
    public KeyCode jumpKey = KeyCode.Space;

    // Чи тримає персонаж зараз предмет у руках
    private bool isHolding = false;

    // Чи тримає персонаж зараз саме вудку (утримує позу риболовлі)
    private bool isFishingEquipped = false;

    // Чи триває зараз стрибок - не дає повторно тригерити анімацію в повітрі
    private bool isJumping = false;

    // Чи стоїть персонаж на землі - оновлюється ззовні з PlayerController.SetGrounded()
    private bool isGrounded = true;

    void Awake()
    {
        // Якщо не призначено вручну в інспекторі - спробувати знайти автоматично
        if (handAnimator == null)
            handAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        // --- Приклад визначення руху через WASD ---
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        float speed = new Vector2(horizontal, vertical).magnitude;

        // Якщо затиснутий Shift - вважаємо це спринтом і збільшуємо швидкість
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && speed > moveThreshold;
        if (isSprinting)
            speed *= sprintMultiplier;

        bool isMoving = speed > moveThreshold;
        handAnimator.SetBool("IsMoving", isMoving);

        // Параметр speed використовується в контролері для переходів
        // між Armature|run та Armature_runfaster (за умовою Greater/Less)
        handAnimator.SetFloat("speed", speed);

        // --- Стрибок ---
        // ВАЖЛИВО: назва параметра в Animator Controller - "jump" (з малої літери),
        // тому рядок нижче має точно співпадати з назвою в контролері!
        // Стрибок дозволений лише стоячи на землі - так само, як в PlayerController.
        if (Input.GetKeyDown(jumpKey) && !isJumping && isGrounded)
        {
            isJumping = true;
            handAnimator.SetBool("isJumping", isJumping);
            handAnimator.SetTrigger("jump");
        }

        // Керування IsHolding відбувається ЛИШЕ через SetHolding(),
        // який викликає PlayerPickup при фактичному підборі/викиданні предмета.
        // Тут більше немає власної обробки клавіші E - інакше стан аніматора
        // розсинхронізовується з тим, чи предмет справді в руках.
    }

    // Викликайте це з іншого скрипта (напр. системи підбору предметів),
    // якщо потрібно керувати станом ззовні, а не через клавішу E.
    public void SetHolding(bool holding)
    {
        isHolding = holding;
        handAnimator.SetBool("IsHolding", isHolding);
    }

    // Викликайте це з PlayerController кожен кадр (або хоча б при зміні стану),
    // щоб руки знали, стоїть персонаж на землі чи ні - так само, як isGrounded
    // в PlayerController. Це і дозволяє стрибку скидатись і повторюватись.
    public void SetGrounded(bool grounded)
    {
        bool wasGrounded = isGrounded;
        isGrounded = grounded;
        handAnimator.SetBool("isGrounded", isGrounded);

        // Щойно приземлились - скидаємо прапорець стрибка, щоб можна було стрибнути знову
        if (isGrounded && !wasGrounded)
            SetJumpFinished();
    }

    // Скидає прапорець стрибка вручну, якщо потрібно окремо від SetGrounded
    public void SetJumpFinished()
    {
        isJumping = false;
        handAnimator.SetBool("isJumping", isJumping);
    }

    // Викликається ззовні (PlayerPickup) одразу після підбору предмета,
    // якщо для цього предмета задана окрема анімація "взяти в руки"
    // (наприклад EquipRod для вудки). Якщо triggerName пустий - нічого не робить,
    // і тоді предмет просто прикріплюється без додаткової анімації.
    public void PlayEquipAnimation(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName)) return;
        handAnimator.SetTrigger(triggerName);
    }

    // Тримає (або знімає) стан "вудка в руках" - Animator лишається
    // в позі риболовлі (Armature|fishing), поки цей Bool == true,
    // і виходить з неї лише коли стає false (при викиданні вудки).
    public void SetFishingEquipped(bool equipped)
    {
        isFishingEquipped = equipped;
        handAnimator.SetBool("IsFishingEquipped", isFishingEquipped);
    }
}