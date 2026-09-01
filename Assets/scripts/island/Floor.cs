using UnityEngine;

// Статичне дно навколо острова.
// На відміну від HeightmapIsland, цей меш НІКОЛИ не деформується акулою:
// SharkBiteController / HeightmapIsland.RPC_BiteAt працюють виключно зі
// своїм власним масивом vertices (полем "vertices" у HeightmapIsland),
// тому навіть якщо OceanFloor лежить прямо під островом чи навколо нього -
// у RPC_BiteAt просто немає посилання на цей об'єкт і фізично нема як
// його зачепити.
// Саме тому компонент навіть не потребує PhotonView: він суто
// декоративний (і опційно дає колайдер-підлогу для риб/фізики), а отже
// кожен клієнт спокійно генерує його локально - результат детермінований
// (як і в GenerateMesh() острова), синхронізувати мережею нема потреби.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OceanFloor : MonoBehaviour
{
    [Header("Розмір дна")]
    public float floorSize = 400f;   // загальний розмір квадрата дна
    public int resolution = 20;      // сегментів на сторону (для легкого рельєфу)
    public float depth = -20f;       // глибина дна по Y - зазвичай = baseDepth острова

    [Header("Рельєф (опційно)")]
    public float noiseScale = 40f;
    public float noiseAmplitude = 0.5f;

    [Header("Колайдер")]
    // Акулі колайдер дна не потрібен (вона кусає лише HeightmapIsland),
    // вмикайте лише якщо потрібна фізична підлога під водою (човни, риби тощо).
    public bool addCollider = false;

    [Header("Матеріал")]
    // Перетягніть сюди острів (об'єкт з HeightmapIsland) - дно автоматично
    // візьме той самий sharedMaterial з його MeshRenderer, щоб не було
    // видно шва між островом і дном. Якщо не заповнити - лишиться
    // матеріал, виставлений вручну на MeshRenderer цього об'єкта.
    public HeightmapIsland islandToMatchMaterial;

    void Start()
    {
        GenerateMesh();
        ApplyIslandMaterial();
    }

    void ApplyIslandMaterial()
    {
        if (islandToMatchMaterial == null) return;

        MeshRenderer islandRenderer = islandToMatchMaterial.GetComponent<MeshRenderer>();
        if (islandRenderer == null) return;

        GetComponent<MeshRenderer>().sharedMaterials = islandRenderer.sharedMaterials;
    }

    void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        GetComponent<MeshFilter>().mesh = mesh;

        int vertsPerSide = resolution + 1;
        int vertCount = vertsPerSide * vertsPerSide;
        float half = floorSize / 2f;
        float step = floorSize / resolution;

        Vector3[] vertices = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];

        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                int i = z * vertsPerSide + x;
                float px = x * step - half;
                float pz = z * step - half;

                float noiseValue = Mathf.PerlinNoise(px / noiseScale, pz / noiseScale) - 0.5f;
                float y = depth + noiseValue * noiseAmplitude;

                vertices[i] = new Vector3(px, y, pz);
                uvs[i] = new Vector2((float)x / resolution, (float)z / resolution);
            }
        }

        int[] triangles = new int[resolution * resolution * 6];
        int t = 0;
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = z * vertsPerSide + x;
                triangles[t++] = i;
                triangles[t++] = i + vertsPerSide;
                triangles[t++] = i + 1;
                triangles[t++] = i + 1;
                triangles[t++] = i + vertsPerSide;
                triangles[t++] = i + vertsPerSide + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (addCollider)
        {
            MeshCollider col = GetComponent<MeshCollider>();
            if (col == null) col = gameObject.AddComponent<MeshCollider>();
            col.sharedMesh = mesh;
        }
    }
}