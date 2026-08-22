using UnityEngine;

public class SharkBiteController : MonoBehaviour
{
    public HeightmapIsland island;

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
        if (bitesDone >= totalBites) return;

        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            bitesDone++;
            DoRandomBite();
        }
    }

    void DoRandomBite()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        // Радіус кола, з якого кусаємо, поступово зменшується - акула "заходить" глибше
        float progress = (float)bitesDone / totalBites;
        float currentShoreRadius = island.WorldSize / 2f * (1f - progress * 0.5f);

        Vector3 bitePos = island.transform.position + new Vector3(
            Mathf.Cos(angle) * currentShoreRadius,
            0,
            Mathf.Sin(angle) * currentShoreRadius
        );

        float radius = Random.Range(biteRadiusMin, biteRadiusMax);
        island.BiteAt(bitePos, radius, biteDepthBelowSea, biteDuration);
    }
}