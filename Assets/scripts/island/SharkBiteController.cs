using Photon.Pun;
using UnityEngine;

// ВАЖЛИВО про мультиплеєр: Random.Range тут (кут укусу, радіус) НЕ синхронний
// між клієнтами - у кожного своя незалежна послідовність випадкових чисел.
// Якби Update() тут виконувався на КОЖНОМУ клієнті, кожен кусав би острів
// у своєму власному випадковому місці - острови розійшлися б з першого ж укусу.
//
// Рішення: логіка "коли і де кусати" виконується ЛИШЕ на MasterClient.
// MasterClient один раз рахує Random-параметри і викликає island.BiteAt(...),
// який сам розсилає вже ГОТОВІ (не випадкові) числа всім через RPC - так усі
// клієнти деформують меш однаково, хоча RNG виконався лише в одному місці.
public class SharkBiteController : MonoBehaviour
{
    public HeightmapIsland island;
    public SharkController shark;

    [Header("Таймінг")]
    public float totalDurationSeconds = 300f;
    public int totalBites = 25;

    [Header("Розмір укусів")]
    public float biteRadiusMin = 2f;
    public float biteRadiusMax = 4f;
    public float biteDuration = 2.5f;
    public float biteDepthBelowSea = -3f;

    [Header("Миттєве поглинання (коли гравець(і) помирають)")]
    [Tooltip("Затримка (сек) між тим, як акула почне 'кусати' (biteTriggerName), і моментом, коли острів реально почне тонути - щоб виглядало як укус, а не просто зникнення.")]
    public float devourImpactDelay = 1f;
    [Tooltip("За скільки секунд тоне ввесь острів після impact-моменту укусу.")]
    public float devourSinkDuration = 1.2f;

    private float timer;
    private float interval;
    private int bitesDone = 0;
    private bool devourTriggered = false;

    void Start()
    {
        interval = totalDurationSeconds / totalBites;

        // Підстраховка: якщо посилання не призначені в інспекторі, острів все
        // одно потоне (island.DevourWholeIsland викликається напряму), але БЕЗ
        // акули не буде анімації укусу - шукаємо обидва компоненти на сцені
        // самостійно, щоб цього не сталось через звичайну неуважність.
        if (shark == null)
            shark = FindObjectOfType<SharkController>();

        if (island == null)
            island = FindObjectOfType<HeightmapIsland>();
    }

    void Update()
    {
        // Лише MasterClient вирішує, коли і де відбувається наступний укус.
        // На інших клієнтах цей скрипт не робить нічого - результат укусу
        // вони отримають готовим через RPC від HeightmapIsland.
        if (!PhotonNetwork.IsMasterClient) return;

        // Острів уже миттєво поглинуто (гравець(і) померли - див.
        // DevourWholeIslandNow() нижче, викликається з PlayerDeathHandler).
        // Продовжувати "поступові" укуси нема сенсу - острова вже немає
        // (або воно ось-ось зникне - devourTriggered виставляється одразу,
        // ще до того, як island.IsDevoured стане true).
        if (devourTriggered || (island != null && island.IsDevoured)) return;

        if (bitesDone >= totalBites) return;

        timer += Time.deltaTime;
        if (timer < interval) return;

        // ФІКС: IsBusyWithBite (isBiting || isEating) стає true лише в момент,
        // коли акула вже РЕАЛЬНО кусає/їсть - але поки вона тільки ПЛИВЕ до
        // потрібного кута (RequestBite вже викликано, pendingTargetAngle
        // виставлено, а isBiting ще false), IsBusyWithBite повертав false.
        // Через це таймер встигав натикати наступний DoBite() ще до того, як
        // попередній укус взагалі стався, і RequestBite() просто перезаписував
        // pendingTargetAngle новим значенням - попередній запит на укус губився
        // без жодного ефекту. Результат: за totalDurationSeconds встигало
        // "запуститись" набагато більше за totalBites запитів, а острів
        // деформувався хаотично й виглядало так, ніби акула з'їдає його миттєво.
        // HasPendingOrActiveBite враховує ще й pendingTargetAngle.HasValue,
        // тому новий запит більше не проходить, поки попередній не завершиться.
        if (shark != null && shark.HasPendingOrActiveBite) return;

        timer = 0f;
        bitesDone++;
        DoBite();
    }

    void DoBite()
    {
        float angleDeg = Random.Range(0f, 360f);
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));

        float progress = (float)bitesDone / totalBites;
        float currentShoreRadius = island.WorldSize / 2f * (1f - progress * 0.5f);

        Vector3 bitePos = island.transform.position + dir * currentShoreRadius;
        float radius = Random.Range(biteRadiusMin, biteRadiusMax);

        if (shark != null)
        {
            shark.RequestBite(
                angleDeg,
                () => island.BiteAt(bitePos, radius, biteDepthBelowSea, biteDuration),
                biteDuration
            );
        }
        else
        {
            island.BiteAt(bitePos, radius, biteDepthBelowSea, biteDuration);
        }
    }

    /// <summary>
    /// Викликається з PlayerDeathHandler, коли гравець (або всі гравці)
    /// помирають: акула одразу "кусає" (грає анімацію Bite -> Eat), а не
    /// просто чекає своєї черги за таймером, і острів тоне ВЕСЬ одразу,
    /// а не шматочками. Захищено від повторного запуску прапорцем
    /// devourTriggered - смерть кількох гравців поспіль не запустить
    /// анімацію укусу знову.
    /// </summary>
    public void DevourWholeIslandNow()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (devourTriggered) return;
        if (island == null || island.IsDevoured) return;

        devourTriggered = true;

        if (shark != null)
        {
            shark.RequestDevourWholeIsland(
                () => island.DevourWholeIsland(devourSinkDuration),
                devourImpactDelay
            );
        }
        else
        {
            // Без акули в сцені показати укус нічим - лишається просто
            // одразу потопити острів.
            island.DevourWholeIsland(devourSinkDuration);
        }
    }
}