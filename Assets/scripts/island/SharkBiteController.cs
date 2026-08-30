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

    private float timer;
    private float interval;
    private int bitesDone = 0;

    void Start()
    {
        interval = totalDurationSeconds / totalBites;
    }

    void Update()
    {
        // Лише MasterClient вирішує, коли і де відбувається наступний укус.
        // На інших клієнтах цей скрипт не робить нічого - результат укусу
        // вони отримають готовим через RPC від HeightmapIsland.
        if (!PhotonNetwork.IsMasterClient) return;

        if (bitesDone >= totalBites) return;

        timer += Time.deltaTime;
        if (timer < interval) return;

        if (shark != null && shark.IsBusyWithBite) return;

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
}
