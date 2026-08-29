using Photon.Pun;
using UnityEngine;
using System.Collections;

// ВАЖЛИВО про мультиплеєр: цей об'єкт лежить у сцені (не спавниться рантайм),
// тому його PhotonView має бути налаштований у сцені як "Scene Object" -
// власником такого PhotonView автоматично і завжди є поточний MasterClient
// (Photon сам перепризначає власність, якщо MasterClient від'єднається).
// Завдяки цьому photonView.IsMine на цьому скрипті еквівалентно
// PhotonNetwork.IsMasterClient - саме тому вирішувати, ЩО кусати, дозволено
// тільки MasterClient (див. SharkBiteController), а сам HeightmapIsland
// лише РОЗСИЛАЄ фактичний результат укусу всім через RPC, щоб меш
// деформувався ОДНАКОВО на кожному екрані.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
[RequireComponent(typeof(PhotonView))]
public class HeightmapIsland : MonoBehaviourPun
{
    [Header("Сітка")]
    public int resolution = 100;
    public float worldSize = 40f;

    [Header("Форма гори")]
    public float islandRadius = -1f;
    public float peakHeight = 8f;
    public float noiseScale = 8f;
    public float noiseAmplitude = 1f;

    [Header("Вода")]
    public float seaLevel = 0f;

    public event System.Action OnIslandGenerated;
    public float EffectiveRadius => islandRadius > 0f ? islandRadius : worldSize / 2f;
    public event System.Action<Vector3, float> OnBite;

    private Mesh mesh;
    private Vector3[] vertices;
    private int topVertCount;

    void Start()
    {
        // Генерація мешу - суто детермінований локальний розрахунок (без Random),
        // тому кожен клієнт може згенерувати його самостійно при старті сцени -
        // результат гарантовано однаковий на всіх, синхронізувати нема потреби.
        GenerateMesh();
    }

    void GenerateMesh()
    {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        GetComponent<MeshFilter>().mesh = mesh;

        int vertsPerSide = resolution + 1;
        topVertCount = vertsPerSide * vertsPerSide;
        int perimeterCount = resolution * 4;
        float baseDepth = -20f;
        float half = worldSize / 2f;
        float radius = islandRadius > 0f ? islandRadius : half;

        Vector3[] topVertices = new Vector3[topVertCount];
        Vector2[] topUVs = new Vector2[topVertCount];

        float step = worldSize / resolution;

        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                int i = z * vertsPerSide + x;
                float px = x * step - half;
                float pz = z * step - half;
                float dist = Mathf.Sqrt(px * px + pz * pz);

                float shape = Mathf.Clamp01(1f - dist / radius);
                shape = shape * shape * (3f - 2f * shape);

                float noiseValue = Mathf.PerlinNoise(x / noiseScale, z / noiseScale) - 0.5f;
                float height = seaLevel + shape * peakHeight + noiseValue * noiseAmplitude * shape;

                topVertices[i] = new Vector3(px, height, pz);
                topUVs[i] = new Vector2((float)x / resolution, (float)z / resolution);
            }
        }

        int[] perimeterIndices = new int[perimeterCount];
        int p = 0;
        for (int x = 0; x < resolution; x++) perimeterIndices[p++] = 0 * vertsPerSide + x;
        for (int z = 0; z < resolution; z++) perimeterIndices[p++] = z * vertsPerSide + resolution;
        for (int x = resolution; x > 0; x--) perimeterIndices[p++] = resolution * vertsPerSide + x;
        for (int z = resolution; z > 0; z--) perimeterIndices[p++] = z * vertsPerSide + 0;

        int bottomCornerCount = 4;
        int totalVerts = topVertCount + perimeterCount + bottomCornerCount;
        vertices = new Vector3[totalVerts];
        Vector2[] uvs = new Vector2[totalVerts];
        System.Array.Copy(topVertices, vertices, topVertCount);
        System.Array.Copy(topUVs, uvs, topVertCount);

        int bottomRingStart = topVertCount;
        for (int idx = 0; idx < perimeterCount; idx++)
        {
            Vector3 v = topVertices[perimeterIndices[idx]];
            vertices[bottomRingStart + idx] = new Vector3(v.x, baseDepth, v.z);
            uvs[bottomRingStart + idx] = Vector2.zero;
        }

        int bottomCornersStart = bottomRingStart + perimeterCount;
        vertices[bottomCornersStart + 0] = new Vector3(-half, baseDepth, -half);
        vertices[bottomCornersStart + 1] = new Vector3(half, baseDepth, -half);
        vertices[bottomCornersStart + 2] = new Vector3(half, baseDepth, half);
        vertices[bottomCornersStart + 3] = new Vector3(-half, baseDepth, half);
        for (int i = 0; i < 4; i++) uvs[bottomCornersStart + i] = Vector2.zero;

        int topTriCount = resolution * resolution * 6;
        int wallTriCount = perimeterCount * 6;
        int bottomTriCount = 6;
        int[] triangles = new int[topTriCount + wallTriCount + bottomTriCount];
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

        for (int idx = 0; idx < perimeterCount; idx++)
        {
            int nextIdx = (idx + 1) % perimeterCount;
            int topA = perimeterIndices[idx];
            int topB = perimeterIndices[nextIdx];
            int botA = bottomRingStart + idx;
            int botB = bottomRingStart + nextIdx;

            triangles[t++] = topA; triangles[t++] = botA; triangles[t++] = topB;
            triangles[t++] = topB; triangles[t++] = botA; triangles[t++] = botB;
        }

        int c00 = bottomCornersStart + 0;
        int c10 = bottomCornersStart + 1;
        int c11 = bottomCornersStart + 2;
        int c01 = bottomCornersStart + 3;

        triangles[t++] = c00; triangles[t++] = c10; triangles[t++] = c01;
        triangles[t++] = c10; triangles[t++] = c11; triangles[t++] = c01;

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshCollider>().sharedMesh = mesh;

        OnIslandGenerated?.Invoke();
    }

    private Vector3 GetNearestEdgePointLocal(Vector3 localPos)
    {
        float half = worldSize / 2f;
        float px = localPos.x;
        float pz = localPos.z;

        bool insideBox = px > -half && px < half && pz > -half && pz < half;

        if (!insideBox)
        {
            return new Vector3(Mathf.Clamp(px, -half, half), 0f, Mathf.Clamp(pz, -half, half));
        }

        float distRight = half - px;
        float distLeft = px + half;
        float distTop = half - pz;
        float distBottom = pz + half;
        float minDist = Mathf.Min(Mathf.Min(distRight, distLeft), Mathf.Min(distTop, distBottom));

        if (minDist == distRight) return new Vector3(half, 0f, pz);
        if (minDist == distLeft) return new Vector3(-half, 0f, pz);
        if (minDist == distTop) return new Vector3(px, 0f, half);
        return new Vector3(px, 0f, -half);
    }

    /// <summary>
    /// Точка входу для SharkBiteController. НЕ виконує деформацію напряму -
    /// лише розсилає точні параметри укусу всім клієнтам через RPC, щоб
    /// у всіх меш провалився в ОДНАКОВОМУ місці з ОДНАКОВИМ радіусом.
    /// Викликати має сенс лише той клієнт, для якого photonView.IsMine == true
    /// (тобто MasterClient) - SharkBiteController це вже гарантує.
    /// </summary>
    public void BiteAt(Vector3 worldBitePos, float radius, float targetDepthBelowSea, float duration)
    {
        photonView.RPC(nameof(RPC_BiteAt), RpcTarget.All, worldBitePos, radius, targetDepthBelowSea, duration);
    }

    [PunRPC]
    private void RPC_BiteAt(Vector3 worldBitePos, float radius, float targetDepthBelowSea, float duration)
    {
        Vector3 localBitePos = transform.InverseTransformPoint(worldBitePos);
        Vector3 edgeLocalPos = GetNearestEdgePointLocal(localBitePos);
        Vector3 worldEdgePos = transform.TransformPoint(edgeLocalPos);

        OnBite?.Invoke(worldEdgePos, radius);

        StartCoroutine(BiteCoroutine(worldEdgePos, radius, targetDepthBelowSea, duration));
    }

    private IEnumerator BiteCoroutine(Vector3 worldBitePos, float radius, float targetDepthBelowSea, float duration)
    {
        Vector3 localBitePos = transform.InverseTransformPoint(worldBitePos);

        float[] startHeights = new float[topVertCount];
        for (int i = 0; i < topVertCount; i++)
            startHeights[i] = vertices[i].y;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            for (int i = 0; i < topVertCount; i++)
            {
                float dist = Vector2.Distance(
                    new Vector2(vertices[i].x, vertices[i].z),
                    new Vector2(localBitePos.x, localBitePos.z)
                );

                if (dist > radius) continue;

                float falloff = 1f - Mathf.SmoothStep(0f, 1f, dist / radius);
                float targetY = seaLevel + targetDepthBelowSea;

                vertices[i].y = Mathf.Lerp(startHeights[i], targetY, falloff * t);
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();

            MeshCollider col = GetComponent<MeshCollider>();
            col.sharedMesh = null;
            col.sharedMesh = mesh;

            yield return null;
        }
    }

    public float WorldSize => worldSize;
}
