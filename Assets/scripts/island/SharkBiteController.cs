using Photon.Pun;
using UnityEngine;

// ВАЖЛИВО про мультиплеєр: Random.Range тут (кут укусу) НЕ синхронний
// між клієнтами - у кожного своя незалежна послідовність випадкових чисел.
// Якби Update() тут виконувався на КОЖНОМУ клієнті, кожен кусав би острів
// у своєму власному випадковому місці - острови розійшлися б з першого ж укусу.
//
// Рішення: логіка "коли і де кусати" виконується ЛИШЕ на MasterClient.
// MasterClient один раз рахує Random-параметри і викликає island.BiteAt(...),
// який сам розсилає вже ГОТОВІ (не випадкові) числа всім через RPC - так усі
// клієнти деформують меш однаково, хоча RNG виконався лише в одному місці.
//
// ЄДИНЕ, що треба налаштувати вручну: скільки укусів (totalBites) і розмір
// світу (HeightmapIsland.worldSize / islandRadius). Все інше - радіус
// окремого укусу, його тривалість, глибина, тайминг між укусами - тепер
// рахується автоматично, щоб гарантовано зїсти острів рівно за totalBites
// укусів незалежно від розміру світу.
public class SharkBiteController : MonoBehaviour
{
    public HeightmapIsland island;
    public SharkController shark;

    [Header("Кількість укусів (єдине, що треба задати тут)")]
    [Tooltip("За стільки укусів акула повністю зїсть острів. Розмір самого острова задається на HeightmapIsland (worldSize / islandRadius) - усе інше рахується автоматично.")]
    public int totalBites = 25;

    // Внутрішні константи реалізації укусу - НЕ призначені для налаштування
    // ззовні: користувач керує результатом лише через totalBites і розмір
    // острова, а не через ці "деталі".
    private const float BiteRadiusSafetyMargin = 1.15f; // невеликий запас, щоб не лишались "острівці" на межі сусідніх укусів
    private const float BiteDuration = 2.5f;             // тривалість анімації одного укусу
    private const float BiteDepthBelowSea = -30f;        // гарантовано нижче за baseDepth (-20f) у HeightmapIsland при будь-якому worldSize

    private int bitesDone = 0;

    void Update()
    {
        // Лише MasterClient вирішує, коли і де відбувається наступний укус.
        // На інших клієнтах цей скрипт не робить нічого - результат укусу
        // вони отримають готовим через RPC від HeightmapIsland.
        if (!PhotonNetwork.IsMasterClient) return;

        if (bitesDone >= totalBites) return;

        // Наступний укус запускається одразу, щойно акула повністю вільна -
        // окремого таймера/інтервалу більше не потрібно: паузу між укусами
        // й так дає тривалість анімацій Bite/Eat на акулі.
        //
        // ВАЖЛИВО: тут навмисно HasPendingOrActiveBite, а НЕ IsBusyWithBite.
        // IsBusyWithBite (isBiting || isEating) стає true лише коли акула
        // ФІЗИЧНО досягла потрібного кута і почала анімацію Bite. Поки вона
        // ще пливе туди (pendingTargetAngle вже встановлено, але isBiting
        // ще false), IsBusyWithBite == false - і без цієї перевірки Update()
        // викликав би DoBite() щокадру під час цього підпливання, щоразу
        // тихо ПЕРЕЗАПИСУЮЧИ pendingTargetAngle новим випадковим кутом
        // (бо RequestBite() усередині теж дивиться на IsBusyWithBite).
        // Це миттєво доганяло bitesDone до totalBites за кілька кадрів і
        // одразу спрацьовувала гілка "останній укус" (радіус на весь острів) -
        // саме тому острів з'їдався за один видимий укус.
        if (shark != null && shark.HasPendingOrActiveBite) return;

        bitesDone++;
        DoBite();
    }

    // Кожен ЗВИЧАЙНИЙ укус (i < totalBites) - це помірне "кусання" країв:
    // радіус підібраний так, щоб площа одного укусу ≈ TotalArea / totalBites
    // (площа кола ∝ r², тому radius = R / sqrt(totalBites)). Такий укус,
    // розташований на самому березі (відстань R від центру), просто
    // відгризає шматок ззовні, не дотягуючись до протилежного боку -
    // острів erode-иться поступово, шматок за шматком.
    //
    // ОСТАННІЙ укус (i == totalBites) - гарантовано доїдає ВСЕ, що
    // лишилось, великим радіусом (island.WorldSize), незалежно від того,
    // скільки випадково лишилось острова після попередніх укусів. Саме
    // цей останній укус і дає гарантію "після totalBites укусів острова
    // не буде", а не математика на кожному проміжному кроці.
    void DoBite()
    {
        float angleDeg = Random.Range(0f, 360f);
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));

        float islandRadius = island.EffectiveRadius;
        float radius;

        if (bitesDone >= totalBites)
        {
            // Останній запланований укус - гарантовано доїдає все, що лишилось.
            radius = island.WorldSize;
        }
        else
        {
            // Звичайний укус - помірний шматок, площа ≈ TotalArea / totalBites.
            radius = islandRadius / Mathf.Sqrt(totalBites) * BiteRadiusSafetyMargin;
        }

        // Довжина dir тут не має значення - HeightmapIsland сам спроєктує
        // цей напрямок рівно на берег острова (EffectiveRadius).
        Vector3 bitePos = island.transform.position + dir;

        if (shark != null)
        {
            shark.RequestBite(
                angleDeg,
                () => island.BiteAt(bitePos, radius, BiteDepthBelowSea, BiteDuration),
                BiteDuration
            );
        }
        else
        {
            island.BiteAt(bitePos, radius, BiteDepthBelowSea, BiteDuration);
        }
    }

    /// <summary>
    /// Миттєво "зїдає" острів одним великим укусом (замість поступових укусів
    /// у DoBite). Викликається при смерті гравця (з PlayerDeathHandler). Зупиняє
    /// подальші заплановані укуси та виконує один фінальний BiteAt з радіусом,
    /// що покриває весь острів.
    /// </summary>
    public void DevourWholeIslandNow()
    {
        // Підстраховка: навіть якщо метод помилково викличуть не на MasterClient,
        // сам укус (island.BiteAt) не піде далі - логіка керується лише MasterClient.
        if (!PhotonNetwork.IsMasterClient) return;

        // Зупиняємо подальші заплановані укуси в Update().
        bitesDone = totalBites;

        if (island == null) return;

        Vector3 center = island.transform.position;
        float radius = island.WorldSize; // з запасом, щоб гарантовано покрити весь острів

        if (shark != null)
        {
            shark.RequestBite(
                0f,
                () => island.BiteAt(center, radius, BiteDepthBelowSea, BiteDuration),
                BiteDuration
            );
        }
        else
        {
            island.BiteAt(center, radius, BiteDepthBelowSea, BiteDuration);
        }
    }
}