using UnityEngine;
using System.Collections;

// Керує рухом і анімацією акули.
// Патрулювання: акула рухається по ідеальному колу навколо orbitCenter -
// patrolAngle безперервно зростає (1°, 2°, 3° ... 360°, 361° ...), Mathf.Cos/Sin
// самі коректно "загортають" кут, тому коло ніколи не рветься само по собі.
//
// Укус: SharkBiteController задає бажаний градус на колі (RequestBite). Акула
// продовжує пливти звичайним патрулюванням, доки не ДОСЯГНЕ саме цього градуса -
// в цей момент вона зупиняється, грає анімацію Bite, в потрібну мить викликає
// callback (яким HeightmapIsland "відкушує" шматок острова саме в цьому напрямку),
// трохи "їсть" на місці, а потім продовжує патрулювання далі по колу.
//
// Поїдання риби: WaterFishZone задає позицію риби, яка впала в море (RequestEatFish).
// Акула перериває патрулювання, телепортується до риби, повертається обличчям
// до острова (а не до самої риби) і їсть, потім повертається на те саме місце
// на колі і продовжує звичайне патрулювання.
//
// Пріоритет: якщо в момент падіння риби акула ще тільки ЗАПЛАНУВАЛА укус
// (пливе до потрібного градуса, але ще не почала кусати) - риба скасовує
// цей план, і акула йде їсти рибу. Якщо акула вже ФАКТИЧНО кусає/жує острів
// (isBiting = true) - запит на рибу ігнорується, риба почекає наступного разу.
[RequireComponent(typeof(Animator))]
public class SharkController : MonoBehaviour
{
    [Header("Патрулювання (коло навколо порожнього об'єкта)")]
    [Tooltip("Порожній GameObject, поставлений в центр острова. Акула завжди рухається строго по колу навколо нього.")]
    public Transform orbitCenter;
    [Tooltip("Радіус кола патрулювання - постав з запасом, щоб акула не залазила на острів")]
    public float patrolRadius = 25f;
    public float patrolHeight = 2f;
    public float patrolSpeed = 15f;        // градусів за секунду навколо orbitCenter
    public float patrolBobAmplitude = 0.5f; // легке гойдання вгору-вниз на плаву
    public float patrolBobSpeed = 1f;
    public float rotateSpeed = 5f;

    [Header("Укус (зупинка на потрібному градусі)")]
    [Tooltip("Назва тригера в Animator Controller, що вмикає анімацію укусу")]
    public string biteTriggerName = "Bite";
    [Tooltip("Частка тривалості укусу (0..1), в яку відбувається фактичне 'вгризання' в острів")]
    [Range(0f, 1f)] public float biteImpactFraction = 0.4f;

    [Header("Поїдання (після укусу, акула стоїть на місці)")]
    [Tooltip("Назва тригера в Animator Controller, що вмикає анімацію поїдання")]
    public string eatTriggerName = "Eat";
    [Tooltip("Скільки секунд акула лишається на місці і 'жує', перш ніж продовжити патрулювання")]
    public float eatHoldDuration = 4f;
    [Tooltip("Амплітуда легкого потрушування головою під час поїдання (щоб не виглядало як застигання)")]
    public float eatShakeAmplitude = 0.15f;
    public float eatShakeSpeed = 6f;

    [Header("Поїдання риби в морі")]
    [Tooltip("Швидкість, з якою акула пливе напряму до впалої риби (не використовується при телепорті, лишено про запас)")]
    public float eatFishSwimSpeed = 8f;
    [Tooltip("На якій відстані до риби акула вважає, що доплила і може її з'їсти (не використовується при телепорті)")]
    public float eatFishStopDistance = 1.2f;
    [Tooltip("Скільки секунд триває плавне повернення на коло після поїдання риби (не використовується при телепорті)")]
    public float swimBackDuration = 1f;

    private Animator animator;
    private float patrolAngle;
    private bool isBiting = false;
    private bool isEating = false;

    // Кут (у тій самій "необгорнутій" шкалі, що й patrolAngle), на якому треба зупинитись і вкусити.
    private float? pendingTargetAngle = null;
    private System.Action pendingOnImpact;
    private float pendingBiteDuration;

    void Start()
    {
        animator = GetComponent<Animator>();
        patrolAngle = Random.Range(0f, 360f);

        if (orbitCenter == null)
            Debug.LogWarning("[SharkController] Orbit Center не задано - акула не буде патрулювати. Створи порожній GameObject в центрі острова і перетягни його сюди.");
    }

    void Update()
    {
        if (isBiting || isEating) return; // під час укусу/поїдання рухом керує окрема корутина

        Patrol();

        // Якщо є "запланований" укус і ми щойно досягли (чи проскочили б) потрібного градуса -
        // зупиняємось рівно на ньому і починаємо укус.
        if (pendingTargetAngle.HasValue && patrolAngle >= pendingTargetAngle.Value)
        {
            patrolAngle = pendingTargetAngle.Value;
            pendingTargetAngle = null;
            ApplyPatrolTransform(patrolAngle, 0f);

            var onImpact = pendingOnImpact;
            var duration = pendingBiteDuration;
            pendingOnImpact = null;
            StartCoroutine(BiteRoutine(onImpact, duration));
        }
    }

    void Patrol()
    {
        if (orbitCenter == null) return;

        patrolAngle += patrolSpeed * Time.deltaTime;
        float bob = Mathf.Sin(Time.time * patrolBobSpeed) * patrolBobAmplitude;
        ApplyPatrolTransform(patrolAngle, bob);
    }

    // Виставляє позицію/поворот акули для заданого (необгорнутого) кута патрулювання.
    void ApplyPatrolTransform(float angleDeg, float bob)
    {
        if (orbitCenter == null) return;

        float rad = angleDeg * Mathf.Deg2Rad;

        Vector3 targetPos = orbitCenter.position + new Vector3(
            Mathf.Cos(rad) * patrolRadius,
            patrolHeight + bob,
            Mathf.Sin(rad) * patrolRadius
        );
        transform.position = targetPos;

        Vector3 tangent = new Vector3(-Mathf.Sin(rad), 0, Mathf.Cos(rad));
        if (tangent != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(tangent);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotateSpeed * Time.deltaTime);
        }
    }

    // true, якщо акула ФАКТИЧНО зайнята (кусає/їсть острів, чи зараз їсть рибу).
    // Запланований (ще не розпочатий) укус острова сюди НЕ входить -
    // його риба має право перебити (див. RequestEatFish).
    public bool IsBusyWithBite => isBiting || isEating;

    // Викликається ззовні (SharkBiteController). Акула НЕ телепортується і НЕ звертає -
    // вона просто продовжує звичайне патрулювання, аж доки природним чином не дійде
    // до desiredAngleDeg на своєму колі, і лише тоді зупиняється й кусає.
    public void RequestBite(float desiredAngleDeg, System.Action onBiteImpact, float biteDuration)
    {
        if (IsBusyWithBite) return;

        // Найближчий "наступний" момент проходження цього градуса вперед по ходу руху.
        float delta = Mathf.Repeat(desiredAngleDeg - patrolAngle, 360f);
        if (delta < 1f) delta += 360f; // щоб завжди проплила хоч трохи, а не кусала миттєво на місці

        pendingTargetAngle = patrolAngle + delta;
        pendingOnImpact = onBiteImpact;
        pendingBiteDuration = biteDuration;
    }

    // Викликається ззовні (WaterFishZone), коли риба впала в море.
    // Якщо акула вже ФАКТИЧНО кусає/жує острів - ігноруємо (риба почекає наступного разу).
    // Якщо акула лише ЗАПЛАНУВАЛА укус (ще пливе туди) - скасовуємо план і йдемо їсти рибу.
    // Акула телепортується до риби, з'їдає її, повертається на те саме місце на колі.
    public void RequestEatFish(Transform fish, FishProgressBar progressBar)
    {
        Debug.Log("[EatFish] Викликано. isBiting=" + isBiting + ", isEating=" + isEating + ", pendingTargetAngle=" + pendingTargetAngle);

        if (isBiting || isEating) return;
        if (fish == null) return;

        // Скасовуємо запланований (ще не розпочатий) укус острова - риба важливіша
        pendingTargetAngle = null;
        pendingOnImpact = null;

        StartCoroutine(EatFishRoutine(fish, progressBar));
    }

    private IEnumerator EatFishRoutine(Transform fish, FishProgressBar progressBar)
    {
        Debug.Log("EatFishRoutine ЗАПУЩЕНО, риба: " + (fish != null ? fish.name : "null"));
        isEating = true;

        // Запам'ятовуємо позицію на колі, де акула була до телепорту - щоб повернутись сюди ж
        Vector3 circleReturnPos = transform.position;
        Quaternion circleReturnRot = transform.rotation;
        float returnAngle = patrolAngle;

        // Телепортуємось прямо до риби
        if (fish != null)
        {
            Vector3 eatPos = fish.position;
            eatPos.y = patrolHeight; // тримаємось на висоті плавання, а не на висоті самої риби
            transform.position = eatPos;
        }

        // Повертаємось обличчям до острова (а не до риби), незалежно від того,
        // з якого боку впала риба - акула завжди "дивиться" на orbitCenter
        if (orbitCenter != null)
        {
            Vector3 dirToIsland = orbitCenter.position - transform.position;
            dirToIsland.y = 0f;
            if (dirToIsland != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dirToIsland.normalized);
        }

        // З'їдаємо рибу
        animator.SetTrigger(eatTriggerName);

        if (fish != null)
            Destroy(fish.gameObject);

        if (progressBar != null)
            progressBar.AddFish(1);

        yield return new WaitForSeconds(eatHoldDuration);

        // Телепортуємось назад на коло, туди, де акула була до цього
        transform.position = circleReturnPos;
        transform.rotation = circleReturnRot;
        patrolAngle = returnAngle;

        isEating = false;
    }

    private IEnumerator BiteRoutine(System.Action onBiteImpact, float biteDuration)
    {
        isBiting = true;

        animator.SetTrigger(biteTriggerName);

        yield return new WaitForSeconds(biteDuration * biteImpactFraction);
        onBiteImpact?.Invoke();

        yield return new WaitForSeconds(biteDuration * (1f - biteImpactFraction));

        // Акула лишається на місці і "доїдає" відкушений шматок.
        animator.SetTrigger(eatTriggerName);

        Vector3 latchPos = transform.position;
        Quaternion latchRot = transform.rotation;
        float eatElapsed = 0f;

        while (eatElapsed < eatHoldDuration)
        {
            eatElapsed += Time.deltaTime;

            float shakeX = Mathf.Sin(eatElapsed * eatShakeSpeed) * eatShakeAmplitude;
            float shakeY = Mathf.Sin(eatElapsed * eatShakeSpeed * 1.7f) * eatShakeAmplitude * 0.5f;
            transform.position = latchPos + new Vector3(shakeX, shakeY, 0f);
            transform.rotation = latchRot;

            yield return null;
        }

        transform.position = latchPos;
        transform.rotation = latchRot;

        isBiting = false; // з наступного кадру Update() знову звичайне патрулювання від поточного кута
    }
}