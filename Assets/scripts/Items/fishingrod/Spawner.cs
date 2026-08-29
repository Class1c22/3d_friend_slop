using UnityEngine;

/// <summary>
/// Ставить один префаб вудки (Pickupable) у валідній точці на поверхні острова
/// одразу після генерації мешу. Використовує ту саму логіку пошуку точки,
/// що й PalmSpawner (рейкаст зверху вниз, перевірка на висоту/схил/відступ від краю),
/// і додатково намагається не поставити вудку впритул до пальми.
///
/// Повісити на той самий об'єкт, де HeightmapIsland, або окремий порожній GameObject.
/// </summary>
public class FishingRodSpawner : MonoBehaviour
{
    [Header("Посилання")]
    public HeightmapIsland island;
    [Tooltip("Шар острова (MeshCollider) - для рейкасту зверху вниз")]
    public LayerMask islandLayer;
    [Tooltip("Необов'язково: якщо задано - вудка намагатиметься не з'являтись впритул до вже поставлених пальм")]
    public PalmSpawner palmSpawner;

    [Header("Префаб вудки")]
    [Tooltip("Префаб з компонентом Pickupable (+ Collider, за потреби Rigidbody), напр. equipAnimTrigger = EquipRod")]
    public GameObject rodPrefab;

    [Header("Пошук точки")]
    public int maxAttempts = 40;
    [Tooltip("Не ставити вудку ближче ніж це до самого краю острова")]
    public float edgeMargin = 4f;
    [Tooltip("Максимальний кут нахилу поверхні (градуси), на якому ще можна поставити вудку")]
    public float maxSlopeAngle = 25f;
    [Tooltip("Мінімальна відстань від найближчої пальми (якщо palmSpawner задано)")]
    public float minDistanceFromPalms = 1.5f;

    private GameObject spawnedRod;

    void Awake()
    {
        if (island == null)
            island = GetComponent<HeightmapIsland>();

        if (island != null)
            island.OnIslandGenerated += SpawnRod;
        else
            Debug.LogWarning("[FishingRodSpawner] Не задано HeightmapIsland - нема з чиєю поверхнею працювати.");
    }

    void OnDestroy()
    {
        if (island != null)
            island.OnIslandGenerated -= SpawnRod;
    }

    private void SpawnRod()
    {
        if (rodPrefab == null)
        {
            Debug.LogWarning("[FishingRodSpawner] Не задано rodPrefab.");
            return;
        }

        if (spawnedRod != null) return; // на випадок повторного виклику - вудку ставимо лише раз

        float usableRadius = Mathf.Max(0f, island.EffectiveRadius - edgeMargin);

        if (!TryFindValidPoint(usableRadius, out Vector3 point, out Vector3 normal))
        {
            Debug.LogWarning("[FishingRodSpawner] Не вдалось знайти валідну точку для вудки за відведену кількість спроб.");
            return;
        }

        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        spawnedRod = Instantiate(rodPrefab, point, rotation, island.transform);
        spawnedRod.name = rodPrefab.name; // прибираємо автоматичний суфікс "(Clone)" для чистішої ієрархії
    }

    private bool TryFindValidPoint(float usableRadius, out Vector3 point, out Vector3 normal)
    {
        point = Vector3.zero;
        normal = Vector3.up;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Mathf.Sqrt(Random.value) * usableRadius;

            Vector3 candidateXZ = island.transform.position + new Vector3(
                Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

            Vector3 rayOrigin = candidateXZ + Vector3.up * 100f;
            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 200f, islandLayer))
                continue;

            if (hit.point.y < island.seaLevel) continue;

            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope > maxSlopeAngle) continue;

            if (IsTooCloseToPalms(hit.point)) continue;

            point = hit.point;
            normal = hit.normal;
            return true;
        }

        return false;
    }

    private bool IsTooCloseToPalms(Vector3 point)
    {
        if (palmSpawner == null) return false;

        foreach (GameObject palm in palmSpawner.SpawnedPalms)
        {
            if (palm == null) continue;

            if (Vector3.Distance(palm.transform.position, point) < minDistanceFromPalms)
                return true;
        }

        return false;
    }
}