using UnityEngine;

/// <summary>
/// Вішається на акулу. Коли акула торкається гравця (Trigger-колайдер, зазвичай
/// невеликий на "пащі" або весь корпус акули) - грає анімацію поїдання
/// і запускає ланцюжок: гравець зникає -> камера смерті -> через delaySeconds Game Over.
///
/// Потрібен Collider з Is Trigger = true на акулі (або на дочірньому об'єкті "паща").
/// </summary>
public class SharkKillPlayer : MonoBehaviour
{
    [Tooltip("Аніматор акули")]
    public Animator sharkAnimator;

    [Tooltip("Назва Trigger-параметра анімації поїдання")]
    public string eatTriggerName = "Eat";

    [Tooltip("Скільки секунд триває анімація поїдання, перш ніж показати Game Over")]
    public float eatAnimDuration = 2.5f;

    [Tooltip("Щоб акула не могла з'їсти вже мертвого гравця ще раз і не спамила тригером")]
    private bool isEating = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isEating) return;

        // GetComponentInParent - на випадок, якщо колайдер гравця висить на дочірньому
        // об'єкті (моделі), а PlayerDeathHandler - на корені persona-об'єкта.
        PlayerDeathHandler player = other.GetComponentInParent<PlayerDeathHandler>();
        if (player == null || player.IsDead) return;

        isEating = true;

        if (sharkAnimator != null && !string.IsNullOrEmpty(eatTriggerName))
            sharkAnimator.SetTrigger(eatTriggerName);

        player.Die();
        player.ShowGameOverDelayed(eatAnimDuration);
    }
}