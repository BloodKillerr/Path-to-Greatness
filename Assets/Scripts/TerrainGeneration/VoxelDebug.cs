using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class VoxelDebug : MonoBehaviour
{
    public MeshFilter mf;
    public MeshRenderer mr;

    [ContextMenu("Log Mesh & Material Info")]
    public void LogInfo()
    {
        mf = mf ?? GetComponent<MeshFilter>();
        mr = mr ?? GetComponent<MeshRenderer>();

        Mesh mesh = mf.sharedMesh;
        if (mesh == null)
        {
            Debug.Log("[VoxelDebug] MeshFilter.sharedMesh is NULL");
            return;
        }

        Debug.Log($"[VoxelDebug] Mesh vertex count: {mesh.vertexCount}");
        var colors = mesh.colors32;
        if (colors == null || colors.Length == 0)
        {
            Debug.Log("[VoxelDebug] Mesh.colors32 is empty (no vertex colors)");
        }
        else
        {
            int sample = Mathf.Min(8, colors.Length);
            string s = "";
            for (int i = 0; i < sample; i++) s += colors[i].ToString() + (i + 1 < sample ? ", " : "");
            Debug.Log($"[VoxelDebug] Mesh.colors32 length={colors.Length}. sample first {sample}: {s}");
        }

        // Check uv2 data presence
        var uvs2 = new System.Collections.Generic.List<Vector2>();
        mesh.GetUVs(2, uvs2);
        if (uvs2 == null || uvs2.Count == 0) Debug.Log("[VoxelDebug] UV2 is empty (no AO/edge mask)");
        else Debug.Log($"[VoxelDebug] UV2 count={uvs2.Count}. sample0={uvs2[0]}");

        if (mr == null) { Debug.Log("[VoxelDebug] MeshRenderer missing"); return; }
        var mat = mr.sharedMaterial;
        if (mat == null) { Debug.Log("[VoxelDebug] MeshRenderer.sharedMaterial is NULL"); return; }
        Debug.Log($"[VoxelDebug] Material assigned: name='{mat.name}', shader='{mat.shader?.name ?? "NULL SHADER"}', isSupported={mat.shader?.isSupported}");
    }
}