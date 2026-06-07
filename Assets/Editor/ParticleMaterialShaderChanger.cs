// Assets/Editor/ParticleMaterialShaderChanger.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ParticleMaterialShaderChanger : EditorWindow
{
    private string targetFolderPath = "Assets/Effects";

    [MenuItem("Tools/Particle Shader Changer")]
    public static void ShowWindow()
    {
        GetWindow<ParticleMaterialShaderChanger>("Particle Shader Changer");
    }

    private void OnGUI()
    {
        GUILayout.Label("パーティクル Shader 一括変換", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetFolderPath = EditorGUILayout.TextField("対象フォルダ", targetFolderPath);

        EditorGUILayout.Space();
        GUILayout.Label("適用される設定", EditorStyles.helpBox);
        GUILayout.Label("Shader      : Universal Render Pipeline/Unlit");
        GUILayout.Label("Surface Type: Transparent");
        GUILayout.Label("Blend Mode  : Additive");
        GUILayout.Label("Render Face : Front");
        EditorGUILayout.Space();

        if (GUILayout.Button("一括変換実行"))
        {
            Execute();
        }
    }

    private void Execute()
    {
        Shader newShader = Shader.Find("Custom/UIParticleAdditive");
        if (newShader == null)
        {
            EditorUtility.DisplayDialog("エラー", "Universal Render Pipeline/Unlit が見つかりません。", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { targetFolderPath });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("エラー", $"フォルダ '{targetFolderPath}' にマテリアルが見つかりませんでした。", "OK");
            return;
        }

        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;


            // テクスチャを事前に保存
            var savedTextures = new Dictionary<string, Texture>();
            Shader oldShader = mat.shader;
            int propCount = ShaderUtil.GetPropertyCount(oldShader);
            for (int i = 0; i < propCount; i++)
            {
                if (ShaderUtil.GetPropertyType(oldShader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    string propName = ShaderUtil.GetPropertyName(oldShader, i);
                    Texture tex = mat.GetTexture(propName);
                    if (tex != null)
                        savedTextures[propName] = tex;
                }
            }

            // Shader 変更
            mat.shader = newShader;



            // テクスチャ再設定
            foreach (var kv in savedTextures)
            {
                if (mat.HasProperty(kv.Key))
                    mat.SetTexture(kv.Key, kv.Value);

                if (kv.Key == "_MainTex" && mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", kv.Value);
                if (kv.Key == "MainTexture" && mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", kv.Value);

                // ShaderGraphsのGUID形式プロパティ名 → _BaseMap へリマップ
                if (kv.Key.StartsWith("Texture2D_") && mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", kv.Value);
            }

            EditorUtility.SetDirty(mat);
            Debug.Log($"[変換完了] {path}");
            count++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("完了", $"{count} 個のマテリアルを変換しました", "OK");
    }
}