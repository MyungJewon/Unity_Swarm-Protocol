using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class AnimationTextureBaker : EditorWindow
{
    private GameObject targetPrefab;
    private AnimationClip animationClip;
    private int frameRate = 30;
    
    private string textureSavePath = "Assets/_Project/Textures/VAT";
    private string meshSavePath = "Assets/_Project/Models/VAT_Meshes";

    [MenuItem("Tools/LegionCore/VAT Baker (Auto Merge)")]
    public static void ShowWindow()
    {
        GetWindow<AnimationTextureBaker>("VAT Baker");
    }

    private void OnGUI()
    {
        GUILayout.Label("Merge & Bake System", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("여러 파츠로 나뉜 캐릭터를 하나의 메쉬로 합치고 텍스처를 굽습니다.\n(Draw Call 최적화 필수 과정)", MessageType.Info);

        targetPrefab = (GameObject)EditorGUILayout.ObjectField("Target Prefab", targetPrefab, typeof(GameObject), false);
        animationClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", animationClip, typeof(AnimationClip), false);
        frameRate = EditorGUILayout.IntField("Frame Rate", frameRate);

        if (GUILayout.Button("Merge Mesh & Bake Texture"))
        {
            if (targetPrefab == null || animationClip == null) return;
            BakeMergeAndTexture();
        }
    }

    private void BakeMergeAndTexture()
    {
        GameObject tempGO = Instantiate(targetPrefab, Vector3.zero, Quaternion.identity);
        
        tempGO.transform.position = Vector3.zero;
        tempGO.transform.rotation = Quaternion.identity;

        SkinnedMeshRenderer[] smrs = tempGO.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (smrs.Length == 0)
        {
            Debug.LogError("SkinnedMeshRenderer를 찾을 수 없습니다.");
            DestroyImmediate(tempGO);
            return;
        }

        CombineInstance[] combine = new CombineInstance[smrs.Length];
        
        int totalVertexCount = 0;
        for (int i = 0; i < smrs.Length; i++)
        {
            totalVertexCount += smrs[i].sharedMesh.vertexCount;
        }

        int frameCount = (int)(animationClip.length * frameRate);
        
        Texture2D texture = new Texture2D(totalVertexCount, frameCount, TextureFormat.RGBAHalf, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;
        
        Color[] pixels = new Color[totalVertexCount * frameCount];
        Mesh tempBakedMesh = new Mesh();

        for (int frame = 0; frame < frameCount; frame++)
        {
            float time = (float)frame / frameRate;
            animationClip.SampleAnimation(tempGO, time);

            int currentVertexOffset = 0;

            for (int i = 0; i < smrs.Length; i++)
            {
                smrs[i].BakeMesh(tempBakedMesh);
                Vector3[] vertices = tempBakedMesh.vertices;

                for (int v = 0; v < vertices.Length; v++)
                {
                    pixels[frame * totalVertexCount + (currentVertexOffset + v)] = 
                        new Color(vertices[v].x, vertices[v].y, vertices[v].z, 1f);
                }
                
                if (frame == 0)
                {
                    combine[i].mesh = Instantiate(smrs[i].sharedMesh);
                    combine[i].transform = smrs[i].transform.localToWorldMatrix;
                }

                currentVertexOffset += vertices.Length;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        Mesh finalMesh = new Mesh();
        finalMesh.CombineMeshes(combine, true, true);
        finalMesh.name = targetPrefab.name + "_Merged";

        if (!Directory.Exists(textureSavePath)) Directory.CreateDirectory(textureSavePath);
        if (!Directory.Exists(meshSavePath)) Directory.CreateDirectory(meshSavePath);

        string texPath = Path.Combine(textureSavePath, $"{targetPrefab.name}_VAT_Tex.asset");
        string mPath = Path.Combine(meshSavePath, $"{targetPrefab.name}_Merged_Mesh.asset");

        AssetDatabase.CreateAsset(texture, texPath);
        AssetDatabase.CreateAsset(finalMesh, mPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<b>[Success]</b> Mesh Merged (Verts: {totalVertexCount}) & Texture Baked!");
        
        DestroyImmediate(tempGO);
    }
}