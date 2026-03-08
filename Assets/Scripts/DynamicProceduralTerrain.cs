using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class DynamicProceduralTerrain : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int width = 100;
    [SerializeField] private int height = 100;
    [SerializeField] private float cellSize = 1f;

    [Header("Noise Settings")]
    [SerializeField] private float frequency = 0.2f;
    [SerializeField] private float amplitude = 5f;
    [SerializeField] private int octaves = 3;
    [SerializeField] private float lacunarity = 2f;
    [SerializeField] private float persistence = 0.5f;

    [Header("Animation Settings")]
    [SerializeField] private float morphDuration = 8f;
    [SerializeField] private float waitBetween = 1f;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private Vector3[] vertices;
    private Vector3[] baseVertices;

    private float[] currentHeights;
    private float[] targetHeights;

    [Header("Peak Object Settings")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField, Range(0f, 1f)] private float peakThreshold = 0.8f;

    private Dictionary<int, GameObject> activeCoins = new Dictionary<int, GameObject>();

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        mesh = new Mesh();
        mesh.name = "Procedural Mesh";

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;

        GenerateGrid();

        currentHeights = GenerateRandomHeights();
        targetHeights = GenerateRandomHeights();

        ApplyHeightsInstant(currentHeights);

        StartCoroutine(MorphRoutine());
    }

    void GenerateGrid()
    {
        int vertCountX = width + 1;
        int vertCountZ = height + 1;

        baseVertices = new Vector3[vertCountX * vertCountZ];
        vertices = new Vector3[baseVertices.Length];

        Vector2[] uv = new Vector2[baseVertices.Length];
        int[] triangles = new int[width * height * 6];

        for (int z = 0; z < vertCountZ; z++)
        {
            for (int x = 0; x < vertCountX; x++)
            {
                int i = z * vertCountX + x;
                baseVertices[i] = new Vector3(x * cellSize, 0, z * cellSize);
                vertices[i] = baseVertices[i];
                uv[i] = new Vector2((float)x / width, (float)z / height);
            }
        }

        int t = 0;
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = z * vertCountX + x;

                triangles[t++] = i;
                triangles[t++] = i + vertCountX;
                triangles[t++] = i + 1;

                triangles[t++] = i + 1;
                triangles[t++] = i + vertCountX;
                triangles[t++] = i + vertCountX + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
    }

    IEnumerator MorphRoutine()
    {
        while (true)
        {
            float timer = 0f;

            while (timer < morphDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, timer / morphDuration);
                UpdateMesh(t);
                yield return null;
            }

            currentHeights = targetHeights;
            targetHeights = GenerateRandomHeights();

            yield return new WaitForSeconds(waitBetween);
        }
    }

    void UpdateMesh(float lerpValue)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            float height = Mathf.Lerp(currentHeights[i], targetHeights[i], lerpValue);
            vertices[i].y = height;
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();

        // Forzar update collider correctamente
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;

        CheckPeaks();
    }

    void CheckPeaks()
    {
        float heightLimit = amplitude * peakThreshold;

        for (int i = 0; i < vertices.Length; i++)
        {
            float height = vertices[i].y;

            bool isPeak = height >= heightLimit;

            // Si es pico y no tiene moneda ? crear
            if (isPeak && !activeCoins.ContainsKey(i))
            {
                Vector3 worldPos = transform.TransformPoint(vertices[i]);
                GameObject coin = Instantiate(coinPrefab, worldPos + Vector3.up * 0.5f, Quaternion.identity);
                activeCoins.Add(i, coin);
            }

            // Si ya no es pico pero tenía moneda ? destruir
            if (!isPeak && activeCoins.ContainsKey(i))
            {
                Destroy(activeCoins[i]);
                activeCoins.Remove(i);
            }
        }
    }

    float[] GenerateRandomHeights()
    {
        float[] heights = new float[baseVertices.Length];

        float offsetX = Random.Range(0f, 9999f);
        float offsetZ = Random.Range(0f, 9999f);

        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 v = baseVertices[i];

            float noiseHeight = 0f;
            float currentFrequency = frequency;
            float currentAmplitude = 1f;

            for (int o = 0; o < octaves; o++)
            {
                float sampleX = (v.x + offsetX) * currentFrequency;
                float sampleZ = (v.z + offsetZ) * currentFrequency;

                float perlin = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f;
                noiseHeight += perlin * currentAmplitude;

                currentFrequency *= lacunarity;
                currentAmplitude *= persistence;
            }

            heights[i] = noiseHeight * amplitude;
        }

        return heights;
    }

    void ApplyHeightsInstant(float[] heights)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].y = heights[i];
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }
}