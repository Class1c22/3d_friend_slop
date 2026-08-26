using UnityEngine;

// Періодично ініціює укуси острова. Обирає випадковий градус на колі акули,
// рахує від нього точку на березі острова і просить акулу зупинитись саме
// там (SharkController.RequestBite) - акула сама допливе туди своїм звичайним
// патрулюванням і лише тоді вкусить.
public class SharkBiteController : MonoBehaviour
{
    public HeightmapIsland island;
    public SharkController shark; // якщо не задано - укуси відбуваються миттєво, без анімації, у випадковому напрямку

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
        if (timer < interval) return;

        // Якщо акула ще "дожовує" попередній укус - чекаємо, поки звільниться,
        // а не пропускаємо цикл і не накладаємо укуси один на одного.
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

        // Радіус кола, з якого кусаємо, поступово зменшується - острів "тане" глибше до центру.
        float progress = (float)bitesDone / totalBites;
        float currentShoreRadius = island.WorldSize / 2f * (1f - progress * 0.5f);

        Vector3 bitePos = island.transform.position + dir * currentShoreRadius;
        float radius = Random.Range(biteRadiusMin, biteRadiusMax);

        if (shark != null)
        {
            // Акула сама допливе до цього градуса на своєму колі, зупиниться,
            // вкусить (саме тоді провалюється шматок острова), поїсть і попливе далі.
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