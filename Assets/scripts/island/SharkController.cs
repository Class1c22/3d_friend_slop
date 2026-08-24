using UnityEngine;
using System.Collections;

// Керує рухом і анімацією акули.
// Коли не кусає остров - плаває по колу навколо нього (патрулювання).
// Коли SharkBiteController викликає PerformBite() - підпливає до точки укусу,
// розвертається до острова, програє анімацію "Bite" і саме в потрібний момент
// анімації викликає callback, що фактично "відкушує" шматок острова.
[RequireComponent(typeof(Animator))]
public class SharkController : MonoBehaviour
{
    [Header("Патрулювання навколо острова")]
    public Transform island;
    public float patrolRadius = 25f;
    public float patrolHeight = 2f;
    public float patrolSpeed = 15f;        // градусів за секунду навколо острова
    public float patrolBobAmplitude = 0.5f; // легке гойдання вгору-вниз на плаву
    public float patrolBobSpeed = 1f;

    [Header("Атака (укус)")]
    public float approachSpeed = 20f;
    public float biteApproachDistance = 3f; // на якій відстані від берега зупиняється перед укусом
    public float rotateSpeed = 5f;

    [Header("Аніматор")]
    [Tooltip("Назва тригера в Animator Controller, що вмикає анімацію укусу")]
    public string biteTriggerName = "Bite";
    [Tooltip("Частка тривалості укусу (0..1), в яку відбувається фактичний 'вгризання' в острів")]
    [Range(0f, 1f)] public float biteImpactFraction = 0.4f;

    [Header("Поїдання (після укусу)")]
    [Tooltip("Назва тригера в Animator Controller, що вмикає анімацію поїдання")]
    public string eatTriggerName = "Eat";
    [Tooltip("Скільки секунд акула лишається вчепленою в острів і 'їсть', перш ніж повернутись до патрулювання")]
    public float eatHoldDuration = 4f;
    [Tooltip("Амплітуда легкого потрушування головою під час поїдання (щоб не виглядало як застигання)")]
    public float eatShakeAmplitude = 0.15f;
    public float eatShakeSpeed = 6f;

    private Animator animator;
    private float patrolAngle;
    private bool isBiting = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        patrolAngle = Random.Range(0f, 360f);
    }

    void Update()
    {
        if (!isBiting)
        {
            Patrol();
        }
    }

    void Patrol()
    {
        if (island == null) return;

        patrolAngle += patrolSpeed * Time.deltaTime;
        float rad = patrolAngle * Mathf.Deg2Rad;

        float bob = Mathf.Sin(Time.time * patrolBobSpeed) * patrolBobAmplitude;

        Vector3 targetPos = island.position + new Vector3(
            Mathf.Cos(rad) * patrolRadius,
            patrolHeight + bob,
            Mathf.Sin(rad) * patrolRadius
        );

        // Дотична до кола - напрямок руху по колу
        Vector3 tangent = new Vector3(-Mathf.Sin(rad), 0, Mathf.Cos(rad));

        transform.position = targetPos;

        if (tangent != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(tangent);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime);
        }
    }

    // Викликається ззовні (SharkBiteController). Акула підпливає до точки укусу,
    // програє анімацію Bite, і рівно посеред анімації викликає onBiteImpact
    // (там, де щелепи "змикаються" на острові).
    public void PerformBite(Vector3 bitePosition, System.Action onBiteImpact, float biteDuration)
    {
        StartCoroutine(BiteRoutine(bitePosition, onBiteImpact, biteDuration));
    }

    private IEnumerator BiteRoutine(Vector3 bitePosition, System.Action onBiteImpact, float biteDuration)
    {
        isBiting = true;

        // Підпливаємо з боку моря, трохи назовні від точки укусу
        Vector3 dirFromCenter = bitePosition - island.position;
        dirFromCenter.y = 0;
        dirFromCenter.Normalize();
        Vector3 approachPos = bitePosition + dirFromCenter * biteApproachDistance;
        approachPos.y = patrolHeight;

        while (Vector3.Distance(transform.position, approachPos) > 0.5f)
        {
            transform.position = Vector3.MoveTowards(transform.position, approachPos, approachSpeed * Time.deltaTime);

            Vector3 lookDir = bitePosition - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime);
            }

            yield return null;
        }

        // Точний розворот на точку укусу перед атакою
        Vector3 finalLookDir = bitePosition - transform.position;
        finalLookDir.y = 0;
        if (finalLookDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(finalLookDir);

        animator.SetTrigger(biteTriggerName);

        yield return new WaitForSeconds(biteDuration * biteImpactFraction);
        onBiteImpact?.Invoke();

        yield return new WaitForSeconds(biteDuration * (1f - biteImpactFraction));

        // Акула лишається вчепленою в берег і "доїдає" відкушений шматок,
        // замість того щоб одразу повертатись до патрулювання.
        animator.SetTrigger(eatTriggerName);

        Vector3 latchPos = transform.position;
        Quaternion latchRot = transform.rotation;
        float eatElapsed = 0f;

        while (eatElapsed < eatHoldDuration)
        {
            eatElapsed += Time.deltaTime;

            // легке "жувальне" потрушування головою, щоб пауза не виглядала застиглою
            float shakeX = Mathf.Sin(eatElapsed * eatShakeSpeed) * eatShakeAmplitude;
            float shakeY = Mathf.Sin(eatElapsed * eatShakeSpeed * 1.7f) * eatShakeAmplitude * 0.5f;
            transform.position = latchPos + new Vector3(shakeX, shakeY, 0f);
            transform.rotation = latchRot;

            yield return null;
        }

        transform.position = latchPos;
        transform.rotation = latchRot;

        // Плавно допливаємо назад до кола патрулювання, синхронізувавши patrolAngle
        // з поточною позицією - інакше Patrol() на наступному кадрі різко "телепортує"
        // акулу на коло.
        if (island != null)
        {
            Vector3 toShark = transform.position - island.position;
            toShark.y = 0f;
            patrolAngle = Mathf.Atan2(toShark.z, toShark.x) * Mathf.Rad2Deg;
            float rad = patrolAngle * Mathf.Deg2Rad;

            Vector3 returnTargetPos = island.position + new Vector3(
                Mathf.Cos(rad) * patrolRadius,
                patrolHeight,
                Mathf.Sin(rad) * patrolRadius
            );

            while (Vector3.Distance(transform.position, returnTargetPos) > 0.5f)
            {
                transform.position = Vector3.MoveTowards(transform.position, returnTargetPos, approachSpeed * Time.deltaTime);

                Vector3 lookDir = returnTargetPos - transform.position;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    Quaternion lookRot = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime);
                }

                yield return null;
            }
        }

        isBiting = false;
    }
}