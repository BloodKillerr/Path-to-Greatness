using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class VoxelTerrainGenerator : MonoBehaviour
{
    public enum Mode { Heightmap, LongAxis, Hemisphere, Sphere, Spikes }
    public enum OutlineMode { Uniform = 0, DarkenFromVertex = 1, BlendUniformAndVertex = 2 }

    [Header("Voxel Settings")]
    public int sizeX = 64;
    public int sizeY = 48;
    public int sizeZ = 64;
    public float voxelSize = 0.5f;

    [Header("Noise (Perlin / FBM)")]
    public int seed = 12345;
    public float scale = 20f;
    public int octaves = 3;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Mode / Shape")]
    public Mode mode = Mode.Heightmap;
    public float longAxisStretch = 3f;
    [Tooltip("If >0 used as radius (world units) for Sphere/Hemisphere, otherwise radius defaults to half the smallest grid dim * voxelSize")]
    public float explicitRadius = 0f;

    [Header("Spikes")]
    public float spikeFrequency = 6f;
    public float spikeStrength = 12f;
    [Range(0f, 1f)] public float spikeChance = 0.03f;

    [Header("Coloring")]
    public Gradient heightGradient;
    [Tooltip("Local Y corresponding to gradient t=0")]
    public float minHeightColor = 0f;
    [Tooltip("Local Y corresponding to gradient t=1")]
    public float maxHeightColor = 24f;

    [Header("Color quantization (for stable colors)")]
    public bool quantizeColors = false;
    [Range(2, 256)] public int colorBands = 8;

    [Header("Outline / Visual")]
    public bool enableOutline = true;
    [Range(0.001f, 0.08f)] public float outlineThickness = 0.02f;
    public Color outlineColor = Color.black;
    public OutlineMode outlineMode = OutlineMode.DarkenFromVertex;
    [Range(0f, 1f)] public float outlineDarken = 0.4f;
    [Range(0f, 1f)] public float outlineBlend = 0.5f;

    [Header("Output / Colliders")]
    public MeshFilter targetMeshFilter;
    public MeshRenderer targetMeshRenderer;
    public Material material;
    public bool useBoxColliders = true;
    public string colliderChildPrefix = "VoxelCollider_";

    [NonSerialized] private bool materialCreatedByGenerator = false;

    [NonSerialized] public byte[,,] voxels;

    private void Awake()
    {
        EnsureRendererAndMaterial();
    }

    private void OnValidate()
    {
        sizeX = Mathf.Max(2, sizeX);
        sizeY = Mathf.Max(2, sizeY);
        sizeZ = Mathf.Max(2, sizeZ);
        voxelSize = Mathf.Max(0.0001f, voxelSize);
        octaves = Mathf.Clamp(octaves, 1, 12);
        lacunarity = Mathf.Max(0.01f, lacunarity);
        persistence = Mathf.Clamp01(persistence);
        colorBands = Mathf.Clamp(colorBands, 2, 256);
    }
    public void EnsureRendererAndMaterial()
    {
        if (targetMeshFilter == null) targetMeshFilter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        if (targetMeshRenderer == null) targetMeshRenderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
        if (material != null && !materialCreatedByGenerator)
        {
            targetMeshRenderer.sharedMaterial = material;
            return;
        }
        string desiredShaderName = enableOutline ? "Custom/UnlitVertexColor_Outline" : "Custom/UnlitVertexColor";
        string[] candidates = enableOutline
            ? new string[] { "Custom/UnlitVertexColor_Outline", "Custom/UnlitVertexColor", "Sprites/Default", "Standard" }
            : new string[] { "Custom/UnlitVertexColor", "Sprites/Default", "Standard" };
        if (material != null && materialCreatedByGenerator)
        {
            if (material.shader != null && material.shader.name == desiredShaderName && material.shader.isSupported)
            {
                targetMeshRenderer.sharedMaterial = material;
                return;
            }
            else
            {
                if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
                material = null;
                materialCreatedByGenerator = false;
            }
        }

        Shader chosen = null; string chosenName = null;
        foreach (var n in candidates)
        {
            Shader s = Shader.Find(n);
            if (s == null) continue;
            if (!s.isSupported) continue;
            chosen = s; chosenName = n; break;
        }

        if (chosen == null)
        {
            Shader fallback = Shader.Find("Sprites/Default") ?? Shader.Find("Standard") ?? Shader.Find("Hidden/InternalErrorShader");
            material = new Material(fallback) { name = "VoxelTerrain_FallbackMat" };
            materialCreatedByGenerator = true;
            targetMeshRenderer.sharedMaterial = material;
            return;
        }

        material = new Material(chosen) { name = "VoxelTerrain_Preview_Mat" };
        materialCreatedByGenerator = true;
        targetMeshRenderer.sharedMaterial = material;
        if (material.HasProperty("_OutlineThickness")) material.SetFloat("_OutlineThickness", outlineThickness);
        if (material.HasProperty("_OutlineColor")) material.SetColor("_OutlineColor", outlineColor);
        if (material.HasProperty("_OutlineMode")) material.SetFloat("_OutlineMode", (float)outlineMode);
        if (material.HasProperty("_OutlineDarken")) material.SetFloat("_OutlineDarken", outlineDarken);
        if (material.HasProperty("_OutlineBlend")) material.SetFloat("_OutlineBlend", outlineBlend);
    }
    public void GenerateAndApply()
    {
        EnsureRendererAndMaterial();
        GenerateVoxels();
        if (voxels == null) { Debug.LogError("Voxels null"); return; }

        Mesh mesh = NaiveMesher.BuildMeshFromVoxels(voxels, voxelSize, Vector3.zero, heightGradient, minHeightColor, maxHeightColor, quantizeColors, colorBands);

        if (targetMeshFilter == null) targetMeshFilter = GetComponent<MeshFilter>();
        if (targetMeshFilter != null) targetMeshFilter.sharedMesh = mesh;

        if (targetMeshRenderer == null) targetMeshRenderer = GetComponent<MeshRenderer>();
        if (targetMeshRenderer != null)
        {
            if (material == null) EnsureRendererAndMaterial();
            targetMeshRenderer.sharedMaterial = material;
            if (material.HasProperty("_OutlineThickness")) material.SetFloat("_OutlineThickness", outlineThickness);
            if (material.HasProperty("_OutlineColor")) material.SetColor("_OutlineColor", outlineColor);
            if (material.HasProperty("_OutlineMode")) material.SetFloat("_OutlineMode", (float)outlineMode);
            if (material.HasProperty("_OutlineDarken")) material.SetFloat("_OutlineDarken", outlineDarken);
            if (material.HasProperty("_OutlineBlend")) material.SetFloat("_OutlineBlend", outlineBlend);
        }

        RemoveOldColliderChildren();

        if (useBoxColliders) CreateMergedBoxColliders();
        else
        {
            MeshCollider mc = GetComponent<MeshCollider>() ?? gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = false;
        }
    }
    public void GenerateVoxels()
    {
        voxels = new byte[sizeX, sizeY, sizeZ];
        System.Random rng = new System.Random(seed);
        float offsetX = (float)rng.NextDouble() * 10000f;
        float offsetZ = (float)rng.NextDouble() * 10000f;
        float defaultRadius = Mathf.Min(sizeX, sizeY, sizeZ) * 0.5f * voxelSize;
        float radius = explicitRadius > 0f ? explicitRadius : defaultRadius;

        if (mode == Mode.Sphere)
        {
            Vector3 center = new Vector3(sizeX * 0.5f * voxelSize, sizeY * 0.5f * voxelSize, sizeZ * 0.5f * voxelSize);
            float r2 = radius * radius;
            for (int x = 0; x < sizeX; x++)
                for (int y = 0; y < sizeY; y++)
                    for (int z = 0; z < sizeZ; z++)
                    {
                        Vector3 vc = new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize, (z + 0.5f) * voxelSize);
                        if ((vc - center).sqrMagnitude <= r2) voxels[x, y, z] = 1;
                    }
            return;
        }

        if (mode == Mode.Hemisphere)
        {
            Vector3 center = new Vector3(sizeX * 0.5f * voxelSize, 0f, sizeZ * 0.5f * voxelSize);
            float r2 = radius * radius;
            for (int x = 0; x < sizeX; x++)
                for (int z = 0; z < sizeZ; z++)
                    for (int y = 0; y < sizeY; y++)
                    {
                        Vector3 vc = new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize, (z + 0.5f) * voxelSize);
                        if (vc.y >= 0f && (vc - center).sqrMagnitude <= r2) voxels[x, y, z] = 1;
                    }
            return;
        }
        for (int x = 0; x < sizeX; x++)
            for (int z = 0; z < sizeZ; z++)
            {
                float nx = (x + offsetX) / Mathf.Max(0.0001f, scale);
                float nz = (z + offsetZ) / Mathf.Max(0.0001f, scale);
                float baseValue = FBM(nx, nz, octaves, persistence, lacunarity);
                float height = 0f;
                switch (mode)
                {
                    case Mode.Heightmap:
                        height = Mathf.Lerp(1f, sizeY - 1f, baseValue);
                        break;
                    case Mode.LongAxis:
                        float nxL = nx;
                        float nzL = nz / Mathf.Max(0.0001f, longAxisStretch);
                        height = Mathf.Lerp(1f, sizeY - 1f, FBM(nxL, nzL, octaves, persistence, lacunarity));
                        break;
                    case Mode.Spikes:
                        height = Mathf.Lerp(1f, sizeY * 0.25f, baseValue);
                        float spikeCandidate = Mathf.PerlinNoise((x + offsetX) * spikeFrequency / Mathf.Max(1f, sizeX),
                                                                 (z + offsetZ) * spikeFrequency / Mathf.Max(1f, sizeZ));
                        if (spikeCandidate > 1f - spikeChance)
                        {
                            float s = Mathf.Pow(spikeCandidate, 3f) * spikeStrength;
                            height += s;
                        }
                        break;
                }
                int h = Mathf.Clamp(Mathf.RoundToInt(height), 0, sizeY - 1);
                for (int y = 0; y <= h; y++) voxels[x, y, z] = 1;
            }
    }
    private void RemoveOldColliderChildren()
    {
        List<Transform> toRemove = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform c = transform.GetChild(i);
            if (c.name.StartsWith(colliderChildPrefix)) toRemove.Add(c);
        }
        foreach (var t in toRemove) DestroyImmediate(t.gameObject);
    }
    private void CreateMergedBoxColliders()
    {
        if (voxels == null) return;
        int sx = voxels.GetLength(0), sy = voxels.GetLength(1), sz = voxels.GetLength(2);
        bool[,,] done = new bool[sx, sy, sz];
        int id = 0;
        for (int y0 = 0; y0 < sy; y0++)
            for (int z0 = 0; z0 < sz; z0++)
                for (int x0 = 0; x0 < sx; x0++)
                {
                    if (done[x0, y0, z0]) continue;
                    if (voxels[x0, y0, z0] == 0) { done[x0, y0, z0] = true; continue; }

                    int x1 = x0;
                    while (x1 + 1 < sx && voxels[x1 + 1, y0, z0] != 0 && !done[x1 + 1, y0, z0]) x1++;
                    int z1 = z0;
                    bool zok = true;
                    while (z1 + 1 < sz && zok)
                    {
                        for (int xi = x0; xi <= x1; xi++) if (voxels[xi, y0, z1 + 1] == 0 || done[xi, y0, z1 + 1]) { zok = false; break; }
                        if (zok) z1++;
                    }
                    int y1 = y0;
                    bool yok = true;
                    while (y1 + 1 < sy && yok)
                    {
                        for (int zi = z0; zi <= z1; zi++) for (int xi = x0; xi <= x1; xi++)
                            { if (voxels[xi, y1 + 1, zi] == 0 || done[xi, y1 + 1, zi]) { yok = false; break; } if (!yok) break; }
                        if (yok) y1++;
                    }

                    for (int yy = y0; yy <= y1; yy++) for (int zz = z0; zz <= z1; zz++) for (int xx = x0; xx <= x1; xx++) done[xx, yy, zz] = true;

                    Vector3 size = new Vector3((x1 - x0 + 1) * voxelSize, (y1 - y0 + 1) * voxelSize, (z1 - z0 + 1) * voxelSize);
                    Vector3 minLocal = new Vector3(x0 * voxelSize, y0 * voxelSize, z0 * voxelSize);
                    Vector3 centerLocal = minLocal + size * 0.5f;
                    var go = new GameObject(colliderChildPrefix + id++);
                    go.transform.parent = transform;
                    go.transform.localPosition = centerLocal;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                    var bc = go.AddComponent<BoxCollider>();
                    bc.center = Vector3.zero;
                    bc.size = size;
                }
    }
    private float FBM(float x, float y, int octs, float pers, float lac)
    {
        float total = 0f, amp = 1f, freq = 1f, max = 0f;
        for (int i = 0; i < octs; i++)
        {
            total += Mathf.PerlinNoise(x * freq, y * freq) * amp;
            max += amp;
            amp *= pers;
            freq *= lac;
        }
        return (max <= 0f) ? 0f : (total / max);
    }
}
