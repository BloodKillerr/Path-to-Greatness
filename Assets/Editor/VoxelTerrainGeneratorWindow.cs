#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class VoxelTerrainGeneratorWindow : EditorWindow
{
    VoxelTerrainGenerator previewGenerator;
    Vector2 scroll;
    int noiseExportScale = 4;

    [MenuItem("Tools/Voxel Terrain Generator")]
    static void OpenWindow() => GetWindow<VoxelTerrainGeneratorWindow>("Voxel Generator");

    private void OnEnable()
    {
        previewGenerator = FindFirstObjectByType<VoxelTerrainGenerator>();
        if (previewGenerator == null)
        {
            GameObject go = new GameObject("VoxelTerrainPreview");
            go.hideFlags = HideFlags.DontSave;

            if (go.GetComponent<MeshFilter>() == null)
            {
                go.AddComponent<MeshFilter>();
            }

            if (go.GetComponent<MeshRenderer>() == null)
            {
                go.AddComponent<MeshRenderer>();
            }

            previewGenerator = go.AddComponent<VoxelTerrainGenerator>();
        }
        else
        {
            if (previewGenerator.GetComponent<MeshFilter>() == null)
            {
                previewGenerator.gameObject.AddComponent<MeshFilter>();
            }

            if (previewGenerator.GetComponent<MeshRenderer>() == null)
            {
                previewGenerator.gameObject.AddComponent<MeshRenderer>();
            }
        }
    }

    private void OnGUI()
    {
        if (previewGenerator == null)
        {
            OnEnable();
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        Editor editor = Editor.CreateEditor(previewGenerator);
        editor.OnInspectorGUI();

        GUILayout.Space(8);
        if (GUILayout.Button("Generate & Preview"))
        {
            Undo.RecordObject(previewGenerator, "Generate Voxel Terrain");
            previewGenerator.GenerateAndApply();
            EditorUtility.SetDirty(previewGenerator);
            SceneView.RepaintAll();
        }

        GUILayout.Space(6);
        GUILayout.Label("Export", EditorStyles.boldLabel);

        if (GUILayout.Button("Export Mesh Asset"))
        {
            Mesh mesh = previewGenerator.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null)
            {
                EditorUtility.DisplayDialog("No Mesh", "Generate mesh first.", "OK");
            }
            else
            {
                string path = EditorUtility.SaveFilePanelInProject("Save Mesh", "VoxelTerrainMesh", "asset", "Choose location to save Mesh asset");
                if (!string.IsNullOrEmpty(path))
                {
                    Mesh meshCopy = Instantiate(mesh);
                    AssetDatabase.CreateAsset(meshCopy, path);
                    AssetDatabase.SaveAssets();
                    EditorUtility.DisplayDialog("Saved", "Mesh saved to: " + path, "OK");
                }
            }
        }

        if (GUILayout.Button("Export Prefab"))
        {
            Mesh mesh = previewGenerator.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null)
            {
                EditorUtility.DisplayDialog("No Mesh", "Generate mesh first.", "OK");
            }
            else
            {
                Material mat = previewGenerator.material;
                if (mat != null)
                {
                    string matPath = AssetDatabase.GetAssetPath(mat);
                    if (string.IsNullOrEmpty(matPath))
                    {
                        string folder = "Assets/Generated";
                        if (!AssetDatabase.IsValidFolder(folder))
                        {
                            AssetDatabase.CreateFolder("Assets", "Generated");
                        }
                        matPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, "VoxelTerrain_Mat.mat"));
                        AssetDatabase.CreateAsset(Instantiate(mat), matPath);
                        AssetDatabase.SaveAssets();
                        previewGenerator.material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        previewGenerator.targetMeshRenderer.sharedMaterial = previewGenerator.material;
                    }
                }

                GameObject go = previewGenerator.gameObject;
                GameObject copy = Instantiate(go);
                copy.name = go.name;
                copy.hideFlags = HideFlags.None;
                string prefabPath = EditorUtility.SaveFilePanelInProject("Save Prefab", "VoxelTerrainPrefab", "prefab", "Choose location to save Prefab");
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    PrefabUtility.SaveAsPrefabAsset(copy, prefabPath);
                    EditorUtility.DisplayDialog("Saved", "Prefab saved to: " + prefabPath, "OK");
                }
                DestroyImmediate(copy);
                AssetDatabase.SaveAssets();
            }
        }

        GUILayout.Space(6);
        GUILayout.Label("Noise export", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Export the FBM noise used for the terrain. Use the scale slider to produce a larger PNG (sizeX * scale × sizeZ * scale).", MessageType.Info);
        noiseExportScale = EditorGUILayout.IntSlider("Noise Export Scale", noiseExportScale, 1, 16);

        if (GUILayout.Button($"Export Noise PNG (scale ×{noiseExportScale})"))
        {
            if (previewGenerator == null)
            {
                EditorUtility.DisplayDialog("No generator", "Create or select a GameObject with VoxelTerrainGenerator first.", "OK");
            }
            else
            {
                int outW = Mathf.Max(1, previewGenerator.sizeX * noiseExportScale);
                int outH = Mathf.Max(1, previewGenerator.sizeZ * noiseExportScale);
                Texture2D tex = previewGenerator.GenerateNoiseTexture(outW, outH);
                if (tex == null)
                {
                    EditorUtility.DisplayDialog("Error", "Could not generate noise texture.", "OK");
                }
                else
                {
                    string path = EditorUtility.SaveFilePanel("Save Noise PNG", "Assets", "noise.png", "png");
                    if (!string.IsNullOrEmpty(path))
                    {
                        byte[] png = tex.EncodeToPNG();
                        File.WriteAllBytes(path, png);
                        AssetDatabase.Refresh();
                        EditorUtility.DisplayDialog("Saved", "Noise PNG saved to: " + path, "OK");
                    }
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }
}
#endif
