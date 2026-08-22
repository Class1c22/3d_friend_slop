using UnityEngine;

public class HandAnimatorController : MonoBehaviour
{
    [Tooltip("Animator компонент на об'єкті руки (або перетягніть сюди вручну)")]
    public Animator handAnimator;

    [Tooltip("Швидкість, вище якої вважаємо що персонаж рухається")]
    public float moveThreshold = 0.1f;

    // Чи тримає персонаж зараз предмет у руках
    private bool isHolding = false;

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

        bool isMoving = speed > moveThreshold;
        handAnimator.SetBool("IsMoving", isMoving);

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
}