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
    public int sizeX = 128;
    public int sizeY = 48;
    public int sizeZ = 128;
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
    [Tooltip("If >0 used as radius (world units) for Sphere/Hemisphere, otherwise radius defaults to half smallest grid dim * voxelSize")]
    public float explicitRadius = 0f;

    [Header("Spikes")]
    public float spikeFrequency = 6f;
    public float spikeStrength = 12f;
    [Range(0f, 1f)] public float spikeChance = 0.1f;

    [Header("Coloring")]
    public Gradient heightGradient;
    [Tooltip("Local Y corresponding to gradient t=0")]
    public float minHeightColor = 0f;
    [Tooltip("Local Y corresponding to gradient t=1")]
    public float maxHeightColor = 12f;

    [Header("Color quantization")]
    public bool quantizeColors = true;
    [Range(2, 256)] public int colorBands = 4;

    [Header("Shader selection")]
    public bool useUnlit = false;
    public bool enableOutline = false;

    [Header("Outline / Visual")]
    [Range(0.001f, 0.08f)] public float outlineThickness = 0.02f;
    public Color outlineColor = Color.black;
    public OutlineMode outlineMode = OutlineMode.DarkenFromVertex;
    [Range(0f, 1f)] public float outlineDarken = 0.4f;
    [Range(0f, 1f)] public float outlineBlend = 0.5f;

    [Header("Lit shader properties")]
    public Color specularColor = Color.white;
    [Range(0f, 1f)] public float specularStrength = 0f;
    [Range(1f, 256f)] public float shininess = 256f;
    public Color rimColor = new Color(1f, 1f, 1f, 1f);
    [Range(0.5f, 8f)] public float rimPower = 5f;
    [Range(0f, 1f)] public float aoStrength = 0.5f;
    [Range(0f, 8f)] public float emissionStrength = 1f;
    [Range(0f, 1f)] public float emissionThreshold = 0f;

    [Header("Baked vertex shadows")]
    public bool bakeShadows = true;
    [Tooltip("Direction from which shadows are cast")]
    public Vector3 shadowDirection = new Vector3(0.1f, -1f, 0.2f);
    [Tooltip("How many voxel steps to check along the light ray")]
    public int shadowMaxSteps = 6;
    [Tooltip("Size of step in world units")]
    public float shadowStepSize = 2f;
    [Range(0f, 1f)] public float shadowDarken = 0.3f;

    [Header("Boundary / Mountain Barrier")]
    public bool enableBoundary = false;
    public float boundaryWidth = 7f;
    public float boundaryHeight = 32f;
    [Range(0f, 1f)]
    public float boundarySmoothness = 0.5f;
    public float boundaryNoiseScale = 14f;
    [Range(0f, 1f)]
    public float boundaryNoiseStrength = 0.5f;

    [Header("Output / Colliders")]
    public MeshFilter targetMeshFilter;
    public MeshRenderer targetMeshRenderer;
    public Material material;
    public bool useBoxColliders = true;
    public string colliderChildPrefix = "VoxelCollider_";

    [NonSerialized] public byte[,,] voxels;
    [NonSerialized] private bool materialCreatedByGenerator = false;

    private void Awake() => EnsureRendererAndMaterial();

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
        shadowMaxSteps = Mathf.Max(0, shadowMaxSteps);
        shadowStepSize = Mathf.Max(0.001f, shadowStepSize);
        boundaryWidth = Mathf.Max(0f, boundaryWidth);
        boundaryHeight = Mathf.Max(0f, boundaryHeight);
        boundaryNoiseScale = Mathf.Max(0.001f, boundaryNoiseScale);
    }
    public void EnsureRendererAndMaterial()
    {
        if (targetMeshFilter == null)
        {
            targetMeshFilter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        }

        if (targetMeshRenderer == null)
        {
            targetMeshRenderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
        }
        if (material != null && !materialCreatedByGenerator)
        {
            if (material.shader == null)
            {
                Debug.LogWarning("[VoxelTerrain] Assigned material has no shader.");
            }
            else if (!material.shader.isSupported)
            {
                Debug.LogWarning($"[VoxelTerrain] Assigned shader '{material.shader.name}' is not supported.");
            }

            targetMeshRenderer.sharedMaterial = material;
            return;
        }
        List<string> candidates = new List<string>();
        if (useUnlit)
        {
            if (enableOutline)
            {
                candidates.Add("Custom/URP/UnlitVertexColor_Outline");
            }
            candidates.Add("Custom/URP/UnlitVertexColor");
            candidates.Add("Universal Render Pipeline/Unlit");
        }
        else
        {
            if (enableOutline)
            {
                candidates.Add("Custom/URP/VertexLit_VertexColor_AO_Outline");
            }
            candidates.Add("Custom/URP/VertexLit_VertexColor_AO");
        }

        if (enableOutline)
        {
            candidates.Add("Custom/UnlitVertexColor_Outline");
        }

        candidates.Add("Custom/UnlitVertexColor");

        if (enableOutline)
        {
            candidates.Add("Custom/VertexLit_VertexColor_AO_Outline");
        }
        candidates.Add("Custom/VertexLit_VertexColor_AO");
        candidates.Add("Sprites/Default");
        candidates.Add("Unlit/Color");
        candidates.Add("Standard");
        candidates.Add("Hidden/InternalErrorShader");

        Shader chosen = null;
        string chosenName = null;
        foreach (string sname in candidates)
        {
            Shader s = Shader.Find(sname);
            if (s == null)
            {
                continue;
            }
            if (!s.isSupported)
            {
                Debug.LogWarning($"[VoxelTerrain] Shader '{sname}' found but not supported.");
                continue;
            }
            chosen = s;
            chosenName = sname;
            break;
        }

        if (chosen == null)
        {
            Debug.LogError("[VoxelTerrain] No suitable shader found. Tried: " + string.Join(", ", candidates));
            Shader fallback = Shader.Find("Hidden/InternalErrorShader");
            material = new Material(fallback) { name = "VoxelTerrain_FallbackMat" };
            materialCreatedByGenerator = true;
            targetMeshRenderer.sharedMaterial = material;
            return;
        }

        if (material != null && materialCreatedByGenerator)
        {
            if (material.shader != null && material.shader.name == chosenName && material.shader.isSupported)
            {
                targetMeshRenderer.sharedMaterial = material;
                return;
            }
            else
            {
                if (Application.isPlaying)
                {
                    Destroy(material);
                }
                else
                {
                    DestroyImmediate(material);
                }
                material = null;
                materialCreatedByGenerator = false;
            }
        }

        material = new Material(chosen) { name = "VoxelTerrain_Preview_Mat" };
        materialCreatedByGenerator = true;
        targetMeshRenderer.sharedMaterial = material;

        if (material.HasProperty("_OutlineThickness"))
        {
            material.SetFloat("_OutlineThickness", outlineThickness);
        }

        if (material.HasProperty("_OutlineColor"))
        {
            material.SetColor("_OutlineColor", outlineColor);
        }

        if (material.HasProperty("_OutlineMode"))
        {
            material.SetFloat("_OutlineMode", (float)outlineMode);
        }

        if (material.HasProperty("_OutlineDarken"))
        {
            material.SetFloat("_OutlineDarken", outlineDarken);
        }

        if (material.HasProperty("_OutlineBlend"))
        {
            material.SetFloat("_OutlineBlend", outlineBlend);
        }

        if (material.HasProperty("_SpecColor"))
        {
            material.SetColor("_SpecColor", specularColor);
        }

        if (material.HasProperty("_SpecStrength"))
        {
            material.SetFloat("_SpecStrength", specularStrength);
        }

        if (material.HasProperty("_Shininess"))
        {
            material.SetFloat("_Shininess", shininess);
        }

        if (material.HasProperty("_RimColor"))
        {
            material.SetColor("_RimColor", rimColor);
        }

        if (material.HasProperty("_RimPower"))
        {
            material.SetFloat("_RimPower", rimPower);
        }

        if (material.HasProperty("_AO_Strength"))
        {
            material.SetFloat("_AO_Strength", aoStrength);
        }

        if (material.HasProperty("_EmissionStrength"))
        {
            material.SetFloat("_EmissionStrength", emissionStrength);
        }

        if (material.HasProperty("_EmissionThreshold"))
        {
            material.SetFloat("_EmissionThreshold", emissionThreshold);
        }

        Debug.Log("[VoxelTerrain] Created preview material using shader: " + chosenName);
    }

    public void GenerateAndApply()
    {
        EnsureRendererAndMaterial();
        GenerateVoxels();
        if (voxels == null) 
        { 
            Debug.LogError("Voxels null"); 
            return; 
        }

        Mesh mesh = NaiveMesher.BuildMeshFromVoxels(
            voxels,
            voxelSize,
            Vector3.zero,
            heightGradient,
            minHeightColor,
            maxHeightColor,
            quantizeColors,
            colorBands,
            bakeShadows,
            shadowDirection.normalized,
            shadowMaxSteps,
            shadowStepSize,
            shadowDarken
        );

        if (targetMeshFilter == null)
        {
            targetMeshFilter = GetComponent<MeshFilter>();
        }

        if (targetMeshFilter != null)
        {
            targetMeshFilter.sharedMesh = mesh;
        }

        if (targetMeshRenderer == null)
        {
            targetMeshRenderer = GetComponent<MeshRenderer>();
        }

        if (targetMeshRenderer != null)
        {
            if (material == null)
            {
                EnsureRendererAndMaterial();
            }

            targetMeshRenderer.sharedMaterial = material;

            if (material.HasProperty("_OutlineThickness"))
            {
                material.SetFloat("_OutlineThickness", outlineThickness);
            }

            if (material.HasProperty("_OutlineColor"))
            {
                material.SetColor("_OutlineColor", outlineColor);
            }

            if (material.HasProperty("_OutlineMode"))
            {
                material.SetFloat("_OutlineMode", (float)outlineMode);
            }

            if (material.HasProperty("_OutlineDarken"))
            {
                material.SetFloat("_OutlineDarken", outlineDarken);
            }

            if (material.HasProperty("_OutlineBlend"))
            {
                material.SetFloat("_OutlineBlend", outlineBlend);
            }

            if (material.HasProperty("_SpecColor"))
            {
                material.SetColor("_SpecColor", specularColor);
            }

            if (material.HasProperty("_SpecStrength"))
            {
                material.SetFloat("_SpecStrength", specularStrength);
            }

            if (material.HasProperty("_Shininess"))
            {
                material.SetFloat("_Shininess", shininess);
            }

            if (material.HasProperty("_RimColor"))
            {
                material.SetColor("_RimColor", rimColor);
            }

            if (material.HasProperty("_RimPower"))
            {
                material.SetFloat("_RimPower", rimPower);
            }

            if (material.HasProperty("_AO_Strength"))
            {
                material.SetFloat("_AO_Strength", aoStrength);
            }

            if (material.HasProperty("_EmissionStrength"))
            {
                material.SetFloat("_EmissionStrength", emissionStrength);
            }

            if (material.HasProperty("_EmissionThreshold"))
            {
                material.SetFloat("_EmissionThreshold", emissionThreshold);
            }
        }

        RemoveOldColliderChildren();
        if (useBoxColliders)
        {
            CreateMergedBoxColliders();
        }
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
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        Vector3 vc = new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize, (z + 0.5f) * voxelSize);
                        if ((vc - center).sqrMagnitude <= r2)
                        {
                            voxels[x, y, z] = 1;
                        }
                    }
                }
            }

            return;
        }

        if (mode == Mode.Hemisphere)
        {
            Vector3 center = new Vector3(sizeX * 0.5f * voxelSize, 0f, sizeZ * 0.5f * voxelSize);
            float r2 = radius * radius;
            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    for (int y = 0; y < sizeY; y++)
                    {
                        Vector3 vc = new Vector3((x + 0.5f) * voxelSize, (y + 0.5f) * voxelSize, (z + 0.5f) * voxelSize);
                        if (vc.y >= 0f && (vc - center).sqrMagnitude <= r2)
                        {
                            voxels[x, y, z] = 1;
                        }
                    }
                }
            }

            return;
        }

        System.Random rng2 = new System.Random(seed);
        float dx = (float)rng2.NextDouble() * 10000f;
        float dz = (float)rng2.NextDouble() * 10000f;

        float mapWorldWidthX = sizeX * voxelSize;
        float mapWorldWidthZ = sizeZ * voxelSize;

        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                float nx = (x + dx) / Mathf.Max(0.0001f, scale);
                float nz = (z + dz) / Mathf.Max(0.0001f, scale);
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
                        float spikeCandidate = Mathf.PerlinNoise((x + dx) * spikeFrequency / Mathf.Max(1f, sizeX),
                                                                 (z + dz) * spikeFrequency / Mathf.Max(1f, sizeZ));
                        if (spikeCandidate > 1f - spikeChance)
                        {
                            float s = Mathf.Pow(spikeCandidate, 3f) * spikeStrength;
                            height += s;
                        }
                        break;
                }
                if (enableBoundary && boundaryWidth > 0f)
                {
                    float centerX_world = (x + 0.5f) * voxelSize;
                    float centerZ_world = (z + 0.5f) * voxelSize;
                    float distX = Mathf.Min(centerX_world, mapWorldWidthX - centerX_world);
                    float distZ = Mathf.Min(centerZ_world, mapWorldWidthZ - centerZ_world);
                    float distEdge = Mathf.Min(distX, distZ);

                    if (distEdge < boundaryWidth)
                    {
                        float inv = Mathf.Clamp01((boundaryWidth - distEdge) / Mathf.Max(0.0001f, boundaryWidth));
                        float smoothInv = Mathf.Pow(inv, Mathf.Lerp(1f, 0.2f, boundarySmoothness));
                        float falloff = Mathf.SmoothStep(0f, 1f, smoothInv);
                        float bnx = (x + dx) / Mathf.Max(0.0001f, boundaryNoiseScale);
                        float bnz = (z + dz) / Mathf.Max(0.0001f, boundaryNoiseScale);
                        float bnoise = FBM(bnx, bnz, Mathf.Max(1, octaves / 2), persistence, lacunarity);
                        float extraScale = Mathf.Lerp(1f, bnoise, Mathf.Clamp01(boundaryNoiseStrength));

                        float extraH = boundaryHeight * falloff * extraScale;
                        height = Mathf.Min(sizeY - 1, height + extraH);
                    }
                }

                int h = Mathf.Clamp(Mathf.RoundToInt(height), 0, sizeY - 1);
                for (int y = 0; y <= h; y++)
                {
                    voxels[x, y, z] = 1;
                }
            }
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

    private void RemoveOldColliderChildren()
    {
        List<Transform> toRemove = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform c = transform.GetChild(i);
            if (c.name.StartsWith(colliderChildPrefix))
            {
                toRemove.Add(c);
            }
        }
        foreach (Transform t in toRemove)
        {
            DestroyImmediate(t.gameObject);
        }
    }

    private void CreateMergedBoxColliders()
    {
        if (voxels == null)
        {
            return;
        }

        int sx = voxels.GetLength(0), sy = voxels.GetLength(1), sz = voxels.GetLength(2);
        bool[,,] processed = new bool[sx, sy, sz];
        int id = 0;
        for (int y0 = 0; y0 < sy; y0++)
        {
            for (int z0 = 0; z0 < sz; z0++)
            {
                for (int x0 = 0; x0 < sx; x0++)
                {
                    if (processed[x0, y0, z0])
                    {
                        continue;
                    }

                    if (voxels[x0, y0, z0] == 0) 
                    { 
                        processed[x0, y0, z0] = true; 
                        continue; 
                    }

                    int x1 = x0;
                    while (x1 + 1 < sx && voxels[x1 + 1, y0, z0] != 0 && !processed[x1 + 1, y0, z0])
                    {
                        x1++;
                    }

                    int z1 = z0;
                    bool zOk = true;
                    while (z1 + 1 < sz && zOk)
                    {
                        for (int xi = x0; xi <= x1; xi++)
                        {
                            if (voxels[xi, y0, z1 + 1] == 0 || processed[xi, y0, z1 + 1]) { zOk = false; break; }
                        }

                        if (zOk)
                        {
                            z1++;
                        }
                    }

                    int y1 = y0;
                    bool yOk = true;
                    while (y1 + 1 < sy && yOk)
                    {
                        for (int zi = z0; zi <= z1; zi++)
                        {
                            for (int xi = x0; xi <= x1; xi++) 
                            { 
                                if (voxels[xi, y1 + 1, zi] == 0 || processed[xi, y1 + 1, zi]) 
                                { 
                                    yOk = false; 
                                    break; 
                                } 
                                
                                if (!yOk)
                                {
                                    break;
                                }
                            }
                        }

                        if (yOk) y1++;
                    }

                    for (int yy = y0; yy <= y1; yy++)
                    {
                        for (int zz = z0; zz <= z1; zz++)
                        {
                            for (int xx = x0; xx <= x1; xx++)
                            {
                                processed[xx, yy, zz] = true;
                            }
                        }
                    }

                    Vector3 size = new Vector3((x1 - x0 + 1) * voxelSize, (y1 - y0 + 1) * voxelSize, (z1 - z0 + 1) * voxelSize);
                    Vector3 minLocal = new Vector3(x0 * voxelSize, y0 * voxelSize, z0 * voxelSize);
                    Vector3 centerLocal = minLocal + size * 0.5f;
                    GameObject go = new GameObject(colliderChildPrefix + id++);
                    go.transform.parent = transform;
                    go.transform.localPosition = centerLocal;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                    BoxCollider bc = go.AddComponent<BoxCollider>();
                    bc.center = Vector3.zero;
                    bc.size = size;
                }
            }
        }
    }

#if UNITY_EDITOR
    public Texture2D GenerateNoiseTexture()
    {
        return GenerateNoiseTexture(sizeX, sizeZ);
    }
    public Texture2D GenerateNoiseTexture(int outW, int outH)
    {
        System.Random rng2 = new System.Random(seed);
        float dx = (float)rng2.NextDouble() * 10000f;
        float dz = (float)rng2.NextDouble() * 10000f;

        Texture2D tex = new Texture2D(outW, outH, TextureFormat.RGBA32, false, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int j = 0; j < outH; j++)
        {
            for (int i = 0; i < outW; i++)
            {
                float sampleX = (outW == 1) ? 0f : (float)i / (outW - 1) * (sizeX - 1);
                float sampleZ = (outH == 1) ? 0f : (float)j / (outH - 1) * (sizeZ - 1);
                float nx = (sampleX + dx) / Mathf.Max(0.0001f, scale);
                float nz = (sampleZ + dz) / Mathf.Max(0.0001f, scale);
                float v = FBM(nx, nz, octaves, persistence, lacunarity);
                v = Mathf.Clamp01(v);
                Color c = new Color(v, v, v, 1f);
                tex.SetPixel(i, j, c);
            }
        }
        tex.Apply();
        return tex;
    }
#endif
}
