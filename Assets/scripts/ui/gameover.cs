using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Вішається на кожну кнопку Game Over UI (об'єкт з Image/Button).
/// При наведенні миші програє Trigger "hover" на Animator'і; при виведенні
/// миші за межі кнопки - програє окремий Trigger "unhover" (реверс-анімація).
/// </summary>
public class ButtonHoverAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Animator, на якому програються анімації hover/unhover.")]
    [SerializeField] private Animator animator;

    [Tooltip("Trigger при наведенні миші, напр. \"HoverButton1\".")]
    [SerializeField] private string triggerName;

    [Tooltip("Trigger при виведенні миші (реверс-анімація), напр. \"UnhoverButton1\".")]
    [SerializeField] private string exitTriggerName;

    [Tooltip("Скільки секунд після появи кнопки ігнорувати перший ховер (захист від \"фантомного\" наведення через розблокування курсора рівно над кнопкою).")]
    [SerializeField] private float ignoreDuration = 0.15f;

    [Tooltip("Мінімальна відстань у пікселях, на яку курсор має реально зрушити відносно позиції на момент активації, щоб ховер зарахувався достроково (навіть до завершення ignoreDuration).")]
    [SerializeField] private float minMouseMoveToAccept = 3f;

    private float enabledAtTime;
    private Vector3 mousePosAtEnable;
    private bool baselineCaptured;
    private bool isHovering;

    void OnEnable()
    {
        enabledAtTime = Time.unscaledTime;
        mousePosAtEnable = Input.mousePosition;
        baselineCaptured = true;
        isHovering = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName)) return;

        bool cooldownPassed = Time.unscaledTime >= enabledAtTime + ignoreDuration;
        bool mouseActuallyMoved = baselineCaptured &&
            Vector3.Distance(Input.mousePosition, mousePosAtEnable) >= minMouseMoveToAccept;

        if (!cooldownPassed && !mouseActuallyMoved)
        {
            // "Фантомний" ховер від розблокування курсора рівно над кнопкою - ігноруємо.
            return;
        }

        isHovering = true;
        animator.SetTrigger(triggerName);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (animator == null || string.IsNullOrEmpty(exitTriggerName)) return;

        // Реверс-анімацію грати тільки якщо реально був "справжній" ховер
        // (щоб той самий фантомний OnPointerEnter при появі UI не спричиняв
        // одразу ж непотрібний OnPointerExit -> unhover).
        if (!isHovering) return;

        isHovering = false;
        animator.SetTrigger(exitTriggerName);
    }
}