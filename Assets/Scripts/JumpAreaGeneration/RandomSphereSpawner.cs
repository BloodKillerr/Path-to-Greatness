using System.Collections.Generic;
using UnityEngine;
public class RandomSphereSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject spherePrefab;
    public GameObject portalPrefab;

    [Header("Area & Count")]
    public Vector3 areaCenter = Vector3.zero;
    public Vector3 areaSize = new Vector3(20f, 6f, 20f);
    [Range(1, 1000)]
    public int count = 30;

    [Header("Scale")]
    public float minScale = 0.5f;
    public float maxScale = 2.5f;

    [Header("Placement")]
    public Transform startPoint;
    public bool avoidOverlap = false;
    public float minDistanceBetweenSpheres = 0.6f;
    public float maxDistanceBetweenSpheres = 0f;
    public int maxPlacementAttempts = 25;

    [Header("Player / Path settings")]
    public float playerMaxHorizontalJump = 4f;
    public float playerMaxUpwards = 2f;
    [Range(0.5f, 0.99f)]
    public float pathSpacingFactor = 0.9f;

    [Header("Runtime")]
    public bool executeOnStart = false;
    public bool clearPrevious = true;

    [Header("Radius detection")]
    public bool useRendererBoundsForRadius = true;
    List<GameObject> spawned = new List<GameObject>();

    const float defaultMeshBaseRadius = 0.5f;

    public int nextSceneIndex = 6;

    [ContextMenu("Generate Spheres")]
    public void Generate()
    {
        if (spherePrefab == null)
        {
            Debug.LogWarning("RandomSphereSpawner: spherePrefab is not assigned.");
            return;
        }

        if (minScale <= 0f)
        {
            minScale = 0.1f;
        }

        if (maxScale < minScale)
        {
            maxScale = minScale;
        }

        if (clearPrevious)
        {
            ClearSpawned();
        }

        spawned.Clear();

        Vector3 localHalf = areaSize * 0.5f;

        List<(Vector3 pos, float radius, GameObject go)> placed = new List<(Vector3, float, GameObject)>();
        int remaining = count;
        if (startPoint != null)
        {
            Vector3 spawnPos = startPoint.position;
            float spawnScale = Random.Range(minScale, maxScale);
            GameObject startSphere = Instantiate(spherePrefab, spawnPos, Quaternion.identity, transform);
            startSphere.transform.localScale = Vector3.one * spawnScale;
            spawned.Add(startSphere);

            float finalRadius = defaultMeshBaseRadius * spawnScale;
            if (useRendererBoundsForRadius)
            {
                Renderer rend = startSphere.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    finalRadius = rend.bounds.extents.y;
                }
            }

            placed.Add((spawnPos, finalRadius, startSphere));
            remaining = Mathf.Max(0, count - 1);
        }
        for (int i = 0; i < remaining; i++)
        {
            Vector3 samplePos = Vector3.zero;
            float sampleScale = 1f;
            bool placedOk = false;

            for (int attempt = 0; attempt < (avoidOverlap ? maxPlacementAttempts : 1); attempt++)
            {
                float lx = Random.Range(-localHalf.x, localHalf.x);
                float ly = Random.Range(-localHalf.y, localHalf.y);
                float lz = Random.Range(-localHalf.z, localHalf.z);
                Vector3 localSample = areaCenter + new Vector3(lx, ly, lz);
                samplePos = transform.TransformPoint(localSample);

                sampleScale = Random.Range(minScale, maxScale);

                if (!avoidOverlap && maxDistanceBetweenSpheres <= 0f)
                {
                    placedOk = true; 
                    break;
                }

                float sampleRadius = defaultMeshBaseRadius * sampleScale;

                bool overlap = false;
                float nearestDist = float.MaxValue;
                foreach (var p in placed)
                {
                    float centerDist = Vector3.Distance(p.pos, samplePos);
                    float minAllow = sampleRadius + p.radius + minDistanceBetweenSpheres;
                    if (centerDist < minAllow)
                    {
                        overlap = true; 
                        break;
                    }

                    nearestDist = Mathf.Min(nearestDist, centerDist - (sampleRadius + p.radius));
                }
                if (!overlap && maxDistanceBetweenSpheres > 0f)
                {
                    if (nearestDist > maxDistanceBetweenSpheres)
                    {
                        overlap = true;
                    }
                }

                if (!overlap)
                {
                    placedOk = true; 
                    break;
                }
            }

            if (!placedOk)
            {
                float lx = Random.Range(-localHalf.x, localHalf.x);
                float ly = Random.Range(-localHalf.y, localHalf.y);
                float lz = Random.Range(-localHalf.z, localHalf.z);
                Vector3 localSample = areaCenter + new Vector3(lx, ly, lz);
                samplePos = transform.TransformPoint(localSample);
                sampleScale = Random.Range(minScale, maxScale);
            }

            GameObject go = Instantiate(spherePrefab, samplePos, Quaternion.identity, transform);
            go.transform.localScale = Vector3.one * sampleScale;
            spawned.Add(go);

            float finalRadius2 = defaultMeshBaseRadius * sampleScale;
            if (useRendererBoundsForRadius)
            {
                Renderer rend = go.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    finalRadius2 = rend.bounds.extents.y;
                }
            }

            placed.Add((samplePos, finalRadius2, go));
        }
        Vector3 origin = startPoint != null ? startPoint.position : Vector3.zero;
        float bestDist = -1f;
        GameObject bestGo = null;
        int bestIndex = -1;

        for (int i = 0; i < placed.Count; i++)
        {
            var p = placed[i];
            float d = (p.pos - origin).sqrMagnitude;
            if (d > bestDist)
            {
                bestDist = d;
                bestGo = p.go;
                bestIndex = i;
            }
        }
        if (bestGo != null)
        {
            bestGo.transform.localScale = Vector3.one * maxScale;
            float radius = defaultMeshBaseRadius * maxScale * Mathf.Max(Mathf.Abs(bestGo.transform.lossyScale.x), Mathf.Abs(bestGo.transform.lossyScale.y), Mathf.Abs(bestGo.transform.lossyScale.z));
            if (useRendererBoundsForRadius)
            {
                Renderer rend = bestGo.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    radius = rend.bounds.extents.y;
                }
            }

            placed[bestIndex] = (bestGo.transform.position, radius, bestGo);
        }
        List<Vector3> tops = new List<Vector3>(placed.Count);

        for (int i = 0; i < placed.Count; i++)
        {
            tops.Add(placed[i].pos + Vector3.up * placed[i].radius);
        }

        int FindNearestIndexToStart()
        {
            if (startPoint == null)
            {
                return -1;
            }

            float best = float.MaxValue; int idx = -1;
            for (int i = 0; i < placed.Count; i++)
            {
                float d = Vector3.SqrMagnitude(placed[i].pos - startPoint.position);
                if (d < best) 
                {
                    best = d; 
                    idx = i; 
                }
            }
            return idx;
        }

        bool PathExists()
        {
            if (startPoint == null || bestGo == null)
            {
                return false;
            }

            int startIdx = FindNearestIndexToStart();
            int targetIdx = bestIndex;
            if (startIdx == -1 || targetIdx == -1)
            {
                return false;
            }

            int n = placed.Count;
            List<int>[] adj = new List<int>[n];
            for (int i = 0; i < n; i++)
            {
                adj[i] = new List<int>();
            }

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    Vector3 fromTop = placed[i].pos + Vector3.up * placed[i].radius;
                    Vector3 toTop = placed[j].pos + Vector3.up * placed[j].radius;
                    if (CanJump(fromTop, toTop))
                    {
                        adj[i].Add(j);
                    }
                }
            }
            Queue<int> q = new Queue<int>();
            bool[] vis = new bool[n];
            q.Enqueue(startIdx); vis[startIdx] = true;
            while (q.Count > 0)
            {
                int u = q.Dequeue();
                if (u == targetIdx)
                {
                    return true;
                }

                foreach (int v in adj[u])
                {
                    if (!vis[v]) 
                    { 
                        vis[v] = true; 
                        q.Enqueue(v);
                    }
                }
            }
            return false;
        }

        if (!PathExists())
        {
            Debug.Log("RandomSphereSpawner: no reachable path found — creating helper spheres along straight line.");
            if (startPoint != null && bestGo != null)
            {
                int startIdx = FindNearestIndexToStart();
                Vector3 startTop = placed[startIdx].pos + Vector3.up * placed[startIdx].radius;
                Vector3 endTop = placed[bestIndex].pos + Vector3.up * placed[bestIndex].radius;

                Vector2 startHZ = new Vector2(startTop.x, startTop.z);
                Vector2 endHZ = new Vector2(endTop.x, endTop.z);
                float horizontalDist = Vector2.Distance(startHZ, endHZ);
                float maxStep = Mathf.Max(0.001f, playerMaxHorizontalJump * pathSpacingFactor);
                int steps = Mathf.Max(1, Mathf.CeilToInt(horizontalDist / maxStep));

                for (int s = 1; s < steps; s++)
                {
                    float t = (float)s / (float)steps;
                    Vector3 desiredTop = Vector3.Lerp(startTop, endTop, t);
                    float helperRadius = defaultMeshBaseRadius * maxScale * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));
                    Vector3 helperCenter = desiredTop - Vector3.up * helperRadius;
                    Vector3 local = transform.InverseTransformPoint(helperCenter);
                    Vector3 clampedLocal = new Vector3(
                        Mathf.Clamp(local.x, areaCenter.x - localHalf.x, areaCenter.x + localHalf.x),
                        Mathf.Clamp(local.y, areaCenter.y - localHalf.y, areaCenter.y + localHalf.y),
                        Mathf.Clamp(local.z, areaCenter.z - localHalf.z, areaCenter.z + localHalf.z)
                    );
                    helperCenter = transform.TransformPoint(clampedLocal);

                    GameObject helper = Instantiate(spherePrefab, helperCenter, Quaternion.identity, transform);
                    helper.transform.localScale = Vector3.one * maxScale;
                    spawned.Add(helper);

                    placed.Add((helperCenter, helperRadius, helper));
                }
                if (!PathExists())
                {
                    Debug.LogWarning("RandomSphereSpawner: helper spheres added but path still not reachable. Consider increasing player jump parameters or adding more helpers.");
                }
                else
                {
                    Debug.Log("RandomSphereSpawner: helper spheres added and path is now reachable.");
                }
            }
        }
        if (bestGo != null && portalPrefab != null)
        {
            float sphereTopY = bestGo.transform.position.y + (placed[bestIndex].radius);
            Renderer sphereRend = bestGo.GetComponentInChildren<Renderer>();
            if (sphereRend != null)
            {
                sphereTopY = sphereRend.bounds.max.y;
            }

            float portalYOffset = 0.02f;
            GameObject portalInstance = Instantiate(portalPrefab, new Vector3(bestGo.transform.position.x, sphereTopY + portalYOffset, bestGo.transform.position.z), Quaternion.identity, transform);
            portalInstance.GetComponent<SceneChangeDoor>().SceneIndex = nextSceneIndex;
            Renderer portalRend = portalInstance.GetComponentInChildren<Renderer>();
            float portalBottomY = float.MinValue;
            if (portalRend != null)
            {
                portalBottomY = portalRend.bounds.min.y;
            }
            else
            {
                portalBottomY = portalInstance.transform.position.y;
            }

            float delta = (sphereTopY + portalYOffset) - portalBottomY;
            portalInstance.transform.position += Vector3.up * delta;
        }

        Debug.Log($"RandomSphereSpawner: spawned {spawned.Count} spheres (including helpers if any). Portal placed on: {bestGo?.name ?? "(none)"}.");
    }

    [ContextMenu("Clear Spawned")]
    public void ClearSpawned()
    {
        List<GameObject> toDestroy = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject c = transform.GetChild(i).gameObject;
            toDestroy.Add(c);
        }

        for (int i = 0; i < toDestroy.Count; i++)
        {
            if (Application.isPlaying)
            {
                Destroy(toDestroy[i]);
            }
            else
            {
                DestroyImmediate(toDestroy[i]);
            }
        }
    }

    void Start()
    {
        if (executeOnStart)
        {
            Generate();
        }
    }
    void OnValidate()
    {
        if (minScale <= 0f)
        {
            minScale = 0.1f;
        }

        if (maxScale < minScale)
        {
            maxScale = minScale;
        }

        if (areaSize.x < 0f)
        {
            areaSize.x = 0f;
        }

        if (areaSize.y < 0f)
        {
            areaSize.y = 0f;
        }

        if (areaSize.z < 0f)
        {
            areaSize.z = 0f;
        }

        if (count < 0)
        {
            count = 0;
        }

        if (minDistanceBetweenSpheres < 0f)
        {
            minDistanceBetweenSpheres = 0f;
        }

        if (maxDistanceBetweenSpheres < 0f)
        {
            maxDistanceBetweenSpheres = 0f;
        }

        if (maxPlacementAttempts < 1)
        {
            maxPlacementAttempts = 1;
        }

        if (playerMaxHorizontalJump <= 0f)
        {
            playerMaxHorizontalJump = 0.1f;
        }

        if (playerMaxUpwards < 0f)
        {
            playerMaxUpwards = 0f;
        }
    }
    void OnDrawGizmos()
    {
        Matrix4x4 oldMat = Gizmos.matrix;

        Vector3 boxCenterWorld = transform.position + transform.rotation * areaCenter;
        Quaternion rot = transform.rotation;
        Vector3 scale = transform.lossyScale;

        Gizmos.matrix = Matrix4x4.TRS(boxCenterWorld, rot, scale);

        Color fill = new Color(0f, 1f, 1f, 0.08f);
        Color outline = new Color(0f, 1f, 1f, 0.9f);

        Gizmos.color = fill;
        Gizmos.DrawCube(Vector3.zero, areaSize);

        Gizmos.color = outline;
        Gizmos.DrawWireCube(Vector3.zero, areaSize);
        Gizmos.matrix = oldMat;
        float s = Mathf.Min(Mathf.Min(areaSize.x * Mathf.Abs(scale.x), areaSize.y * Mathf.Abs(scale.y)), areaSize.z * Mathf.Abs(scale.z)) * 0.03f;
        Vector3 worldCenter = boxCenterWorld;
        Gizmos.color = outline;
        Gizmos.DrawLine(worldCenter - Vector3.right * s, worldCenter + Vector3.right * s);
        Gizmos.DrawLine(worldCenter - Vector3.up * s, worldCenter + Vector3.up * s);
        Gizmos.DrawLine(worldCenter - Vector3.forward * s, worldCenter + Vector3.forward * s);
    }
    bool CanJump(Vector3 fromTop, Vector3 toTop)
    {
        Vector3 d = toTop - fromTop;
        float horiz = new Vector2(d.x, d.z).magnitude;
        float vert = d.y;

        if (vert > playerMaxUpwards)
        {
            return false;
        }

        return horiz <= playerMaxHorizontalJump;
    }
}
