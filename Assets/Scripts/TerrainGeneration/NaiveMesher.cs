using System.Collections.Generic;
using UnityEngine;
public static class NaiveMesher
{
    private static readonly Vector3Int[] FaceChecks = new Vector3Int[6]
    {
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1)
    };

    private static readonly Vector3[][] FaceVertices = new Vector3[6][]
    {
        new Vector3[] { new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(1,1,1), new Vector3(0,1,1) },
        new Vector3[] { new Vector3(0,0,0), new Vector3(0,0,1), new Vector3(1,0,1), new Vector3(1,0,0) },
        new Vector3[] { new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(0,1,1), new Vector3(0,0,1) },
        new Vector3[] { new Vector3(1,0,0), new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(1,1,0) },
        new Vector3[] { new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(1,1,1), new Vector3(1,0,1) },
        new Vector3[] { new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(0,1,0) }
    };

    private static readonly Vector3[] FaceNormals = new Vector3[6]
    {
        Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back
    };

    public static Mesh BuildMeshFromVoxels(
        byte[,,] voxels,
        float voxelSize,
        Vector3 localOffset,
        Gradient gradient,
        float minH,
        float maxH,
        bool quantizeColors,
        int bands,
        bool bakeShadows,
        Vector3 shadowDir,
        int shadowSteps,
        float shadowStepSize,
        float shadowDarken)
    {
        int sx = voxels.GetLength(0);
        int sy = voxels.GetLength(1);
        int sz = voxels.GetLength(2);

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Color32> cols = new List<Color32>();
        List<Vector3> norms = new List<Vector3>();
        List<Vector2> uv0 = new List<Vector2>();
        List<Vector2> uv1 = new List<Vector2>();
        List<Vector2> uv2 = new List<Vector2>();
        List<Vector2> uv3 = new List<Vector2>();

        Vector2 qa = new Vector2(0f, 0f);
        Vector2 qb = new Vector2(1f, 0f);
        Vector2 qc = new Vector2(1f, 1f);
        Vector2 qd = new Vector2(0f, 1f);

        float sampleEdgeOffset = voxelSize * 0.45f;
        Vector3 shadowStepDir = shadowDir.normalized;

        for (int x = 0; x < sx; x++)
        {
            for (int y = 0; y < sy; y++)
            {
                for (int z = 0; z < sz; z++)
                {
                    if (voxels[x, y, z] == 0)
                    {
                        continue;
                    }

                    float voxelCenterY = (y + 0.5f) * voxelSize + localOffset.y;
                    float t = (maxH - minH) == 0f ? 0f : Mathf.InverseLerp(minH, maxH, voxelCenterY);
                    if (quantizeColors && bands > 1)
                    {
                        int idx = Mathf.Clamp(Mathf.FloorToInt(t * bands), 0, bands - 1);
                        t = (float)idx / (float)(bands - 1);
                    }
                    Color voxelColor = gradient != null ? gradient.Evaluate(t) : Color.white;
                    Color32 voxelColor32 = (Color32)voxelColor;

                    for (int f = 0; f < 6; f++)
                    {
                        Vector3Int npos = new Vector3Int(x, y, z) + FaceChecks[f];
                        bool neighborSolid = (npos.x >= 0 && npos.x < sx && npos.y >= 0 && npos.y < sy && npos.z >= 0 && npos.z < sz) && voxels[npos.x, npos.y, npos.z] != 0;
                        if (neighborSolid)
                        {
                            continue;
                        }
                        Vector3 a = (new Vector3(x, y, z) + FaceVertices[f][0]) * voxelSize + localOffset;
                        Vector3 b = (new Vector3(x, y, z) + FaceVertices[f][1]) * voxelSize + localOffset;
                        Vector3 c = (new Vector3(x, y, z) + FaceVertices[f][2]) * voxelSize + localOffset;
                        Vector3 d = (new Vector3(x, y, z) + FaceVertices[f][3]) * voxelSize + localOffset;
                        Vector3 faceNormal = FaceNormals[f];
                        int edgeMask = 0;
                        Vector3[] edgeV0 = new Vector3[] { a, b, c, d };
                        Vector3[] edgeV1 = new Vector3[] { b, c, d, a };

                        for (int eIdx = 0; eIdx < 4; eIdx++)
                        {
                            Vector3 v0 = edgeV0[eIdx];
                            Vector3 v1 = edgeV1[eIdx];
                            Vector3 mid = (v0 + v1) * 0.5f;
                            Vector3 edgeVec = (v1 - v0);
                            Vector3 n = Vector3.Cross(faceNormal, edgeVec).normalized;
                            if (n == Vector3.zero)
                            {
                                n = Vector3.up;
                            }

                            Vector3 s1 = mid + n * sampleEdgeOffset;
                            Vector3 s2 = mid - n * sampleEdgeOffset;

                            int ix1 = Mathf.FloorToInt(s1.x / voxelSize), iy1 = Mathf.FloorToInt(s1.y / voxelSize), iz1 = Mathf.FloorToInt(s1.z / voxelSize);
                            int ix2 = Mathf.FloorToInt(s2.x / voxelSize), iy2 = Mathf.FloorToInt(s2.y / voxelSize), iz2 = Mathf.FloorToInt(s2.z / voxelSize);

                            bool s1Solid = (ix1 >= 0 && ix1 < sx && iy1 >= 0 && iy1 < sy && iz1 >= 0 && iz1 < sz) && voxels[ix1, iy1, iz1] != 0;
                            bool s2Solid = (ix2 >= 0 && ix2 < sx && iy2 >= 0 && iy2 < sy && iz2 >= 0 && iz2 < sz) && voxels[ix2, iy2, iz2] != 0;

                            bool edgeExposed = !(s1Solid && s2Solid);
                            if (edgeExposed)
                            {
                                edgeMask |= (1 << eIdx);
                            }
                        }

                        float maskFloat = (float)edgeMask;
                        float aoA = ComputeVertexAO(a, voxels, voxelSize);
                        float aoB = ComputeVertexAO(b, voxels, voxelSize);
                        float aoC = ComputeVertexAO(c, voxels, voxelSize);
                        float aoD = ComputeVertexAO(d, voxels, voxelSize);
                        float shFace = 1f;
                        if (bakeShadows && shadowSteps > 0)
                        {
                            Vector3 faceCenter = (a + b + c + d) * 0.25f;
                            shFace = ComputeShadowFactor(faceCenter, voxels, voxelSize, shadowStepDir, shadowSteps, shadowStepSize, shadowDarken);
                        }

                        {
                            Vector3 v0 = a, v1 = b, v2 = c;
                            Vector3 triNormal = Vector3.Cross(v1 - v0, v2 - v0);
                            if (Vector3.Dot(triNormal, faceNormal) < 0f)
                            {
                                Vector3 tmp = v0; v0 = v1; v1 = tmp;
                            }

                            int bi = verts.Count;

                            verts.Add(v0); 
                            verts.Add(v1); 
                            verts.Add(v2);

                            Vector3 nrm = Vector3.Cross(v1 - v0, v2 - v0).normalized;

                            norms.Add(nrm); 
                            norms.Add(nrm); 
                            norms.Add(nrm);

                            cols.Add(voxelColor32); 
                            cols.Add(voxelColor32); 
                            cols.Add(voxelColor32);

                            uv0.Add(new Vector2(0f, 0f)); 
                            uv0.Add(new Vector2(1f, 0f)); 
                            uv0.Add(new Vector2(1f, 1f));

                            uv1.Add(qa); 
                            uv1.Add(qb); 
                            uv1.Add(qc);

                            uv2.Add(new Vector2(maskFloat, aoA)); 
                            uv2.Add(new Vector2(maskFloat, aoB)); 
                            uv2.Add(new Vector2(maskFloat, aoC));

                            uv3.Add(new Vector2(shFace, 0f)); 
                            uv3.Add(new Vector2(shFace, 0f));
                            uv3.Add(new Vector2(shFace, 0f));

                            tris.Add(bi + 0); 
                            tris.Add(bi + 1); 
                            tris.Add(bi + 2);
                        }

                        {
                            Vector3 v0 = a, v1 = c, v2 = d;
                            Vector3 triNormal = Vector3.Cross(v1 - v0, v2 - v0);
                            if (Vector3.Dot(triNormal, faceNormal) < 0f) 
                            {
                                Vector3 tmp = v0; v0 = v1; v1 = tmp; 
                            }

                            int bi = verts.Count;

                            verts.Add(v0); 
                            verts.Add(v1); 
                            verts.Add(v2);

                            Vector3 nrm = Vector3.Cross(v1 - v0, v2 - v0).normalized;

                            norms.Add(nrm); 
                            norms.Add(nrm); 
                            norms.Add(nrm);

                            cols.Add(voxelColor32); 
                            cols.Add(voxelColor32); 
                            cols.Add(voxelColor32);

                            uv0.Add(new Vector2(0f, 0f)); 
                            uv0.Add(new Vector2(1f, 1f)); 
                            uv0.Add(new Vector2(0f, 1f));

                            uv1.Add(qa); 
                            uv1.Add(qc); 
                            uv1.Add(qd);

                            uv2.Add(new Vector2(maskFloat, aoA));
                            uv2.Add(new Vector2(maskFloat, aoC)); 
                            uv2.Add(new Vector2(maskFloat, aoD));

                            uv3.Add(new Vector2(shFace, 0f)); 
                            uv3.Add(new Vector2(shFace, 0f)); 
                            uv3.Add(new Vector2(shFace, 0f));

                            tris.Add(bi + 0); 
                            tris.Add(bi + 1); 
                            tris.Add(bi + 2);
                        }
                    }
                }
            }
        }
        Mesh mesh = new Mesh();
        mesh.indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetColors(cols);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uv0);
        mesh.SetUVs(1, uv1);
        mesh.SetUVs(2, uv2);
        mesh.SetUVs(3, uv3);
        mesh.RecalculateBounds();
        mesh.UploadMeshData(false);
        return mesh;
    }
    private static float ComputeVertexAO(Vector3 vpos, byte[,,] voxels, float voxelSize)
    {
        int sx = voxels.GetLength(0), sy = voxels.GetLength(1), sz = voxels.GetLength(2);
        Vector3[] offsets = new Vector3[]
        {
            new Vector3(voxelSize * 0.5f, 0, 0),
            new Vector3(-voxelSize * 0.5f, 0, 0),
            new Vector3(0, voxelSize * 0.5f, 0),
            new Vector3(0, -voxelSize * 0.5f, 0),
            new Vector3(0,0, voxelSize * 0.5f),
            new Vector3(0,0, -voxelSize * 0.5f)
        };

        int occupied = 0;
        int total = offsets.Length;
        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 s = vpos + offsets[i];
            int ix = Mathf.FloorToInt(s.x / voxelSize);
            int iy = Mathf.FloorToInt(s.y / voxelSize);
            int iz = Mathf.FloorToInt(s.z / voxelSize);
            if (ix >= 0 && ix < sx && iy >= 0 && iy < sy && iz >= 0 && iz < sz)
            {
                if (voxels[ix, iy, iz] != 0)
                {
                    occupied++;
                }
            }
        }
        float ao = 1.0f - ((float)occupied / (float)total);
        return Mathf.Clamp01(ao);
    }
    private static float ComputeShadowFactor(Vector3 pStart, byte[,,] voxels, float voxelSize, Vector3 shadowDir, int steps, float stepSize, float shadowDarken)
    {
        int sx = voxels.GetLength(0), sy = voxels.GetLength(1), sz = voxels.GetLength(2);

        Vector3 p = pStart;
        for (int s = 1; s <= steps; s++)
        {
            p += shadowDir * stepSize;
            int ix = Mathf.FloorToInt(p.x / voxelSize);
            int iy = Mathf.FloorToInt(p.y / voxelSize);
            int iz = Mathf.FloorToInt(p.z / voxelSize);
            if (ix >= 0 && ix < sx && iy >= 0 && iy < sy && iz >= 0 && iz < sz)
            {
                if (voxels[ix, iy, iz] != 0)
                {
                    return Mathf.Clamp01(shadowDarken);
                }
            }
        }
        return 1f;
    }
}
