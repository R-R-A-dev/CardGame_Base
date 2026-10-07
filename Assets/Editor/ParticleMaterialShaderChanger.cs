// Assets/Editor/ParticleMaterialShaderChanger.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// パーティクルのマテリアルを UIParticle（URP の Canvas）で表示できるシェーダーへ一括変換する。
/// 元シェーダーの種類・ブレンドモードに応じて Additive / AlphaBlend を振り分ける。
/// </summary>
public class ParticleMaterialShaderChanger : EditorWindow
{
    private const string AdditiveShaderName = "Custom/UIParticleAdditive";
    private const string AlphaBlendShaderName = "Custom/UIParticleAlphaBlend";

    // 1行に1フォルダ
    private string targetFolderPaths = "Assets/ImportedAssets/Effects\nAssets/Matthew Guz";
    private Vector2 scroll;

    private enum BlendType { Additive, AlphaBlend }

    private enum PlanKind
    {
        Skip,        // 対象外 or 変換不要
        Convert,     // 未変換 → テクスチャ・色を移してシェーダー変更
        Reclassify,  // 変換済み → ブレンドだけ付け替え（テクスチャ・色はそのまま）
    }

    private struct Plan
    {
        public PlanKind kind;
        public BlendType blend;
        public string reason;
    }

    [MenuItem("Tools/Particle Shader Changer")]
    public static void ShowWindow()
    {
        GetWindow<ParticleMaterialShaderChanger>("Particle Shader Changer");
    }

    private void OnGUI()
    {
        GUILayout.Label("パーティクル Shader 一括変換", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        GUILayout.Label("対象フォルダ（1行に1つ）");
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(60));
        targetFolderPaths = EditorGUILayout.TextArea(targetFolderPaths, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        GUILayout.Label("振り分けルール", EditorStyles.helpBox);
        GUILayout.Label("Particles/Standard 系 : _Mode 4(Additive) → Additive / それ以外 → AlphaBlend");
        GUILayout.Label("Legacy Particles 系   : 名前に Additive → Additive / それ以外 → AlphaBlend");
        GUILayout.Label("Shader Graphs        : 名前に Alpha → AlphaBlend / それ以外 → Additive");
        GUILayout.Label("変換済み(Custom/UIParticle*) : 旧 _Mode が残っていれば付け替え");
        GUILayout.Label("上記以外（Standard, URP/Lit, URP/Unlit 等）: 変更しない");
        EditorGUILayout.Space();

        if (GUILayout.Button("解析のみ（変更しない）"))
        {
            Run(false);
        }
        if (GUILayout.Button("一括変換実行"))
        {
            Run(true);
        }
    }

    private void Run(bool apply)
    {
        Shader additive = Shader.Find(AdditiveShaderName);
        Shader alphaBlend = Shader.Find(AlphaBlendShaderName);
        if (additive == null || alphaBlend == null)
        {
            EditorUtility.DisplayDialog("エラー", $"{AdditiveShaderName} または {AlphaBlendShaderName} が見つかりません。", "OK");
            return;
        }

        string[] folders = targetFolderPaths
            .Split('\n')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToArray();
        string[] invalid = folders.Where(f => !AssetDatabase.IsValidFolder(f)).ToArray();
        if (folders.Length == 0 || invalid.Length > 0)
        {
            EditorUtility.DisplayDialog("エラー", $"フォルダが見つかりません:\n{string.Join("\n", invalid)}", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", folders);
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("エラー", "マテリアルが見つかりませんでした。", "OK");
            return;
        }

        // 「現在のシェーダー → 結果」ごとの件数
        var summary = new SortedDictionary<string, int>();
        int changed = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            string oldShaderName = mat.shader.name;
            Plan plan = Classify(mat);
            Shader target = plan.blend == BlendType.Additive ? additive : alphaBlend;

            string result = plan.kind == PlanKind.Skip ? $"スキップ（{plan.reason}）" : $"{target.name}（{plan.reason}）";
            string key = $"{oldShaderName} → {result}";
            summary[key] = summary.TryGetValue(key, out int c) ? c + 1 : 1;

            if (plan.kind == PlanKind.Skip) continue;

            if (apply)
            {
                if (plan.kind == PlanKind.Convert)
                    Convert(mat, target);
                else
                    mat.shader = target; // 同じプロパティ構成なのでテクスチャ・色は保持される

                EditorUtility.SetDirty(mat);
            }
            Debug.Log($"[{(apply ? "変換" : "予定")}] {path} : {oldShaderName} → {target.name}（{plan.reason}）");
            changed++;
        }

        if (apply) AssetDatabase.SaveAssets();

        string report = string.Join("\n", summary.Select(kv => $"{kv.Value,4} 件 : {kv.Key}"));
        Debug.Log($"[Particle Shader Changer] {(apply ? "変換結果" : "解析結果")}\n{report}");
        EditorUtility.DisplayDialog(
            "完了",
            $"{(apply ? "変換" : "変換予定")}: {changed} / {guids.Length} 件\n詳細は Console を確認してください。",
            "OK");
    }

    /// <summary>
    /// マテリアルの現在のシェーダーと保存済みプロパティから変換方針を決める
    /// </summary>
    private static Plan Classify(Material mat)
    {
        string name = mat.shader.name;

        // 変換済み：シェーダー変更後もマテリアルに残っている旧プロパティから元のブレンドを推定する
        if (name == AdditiveShaderName || name == AlphaBlendShaderName)
        {
            BlendType current = name == AdditiveShaderName ? BlendType.Additive : BlendType.AlphaBlend;

            // Shader Graph 由来（Texture2D_xxx が残っている）は旧 _Mode が既定値のままで当てにならない
            if (GetSavedTextureNames(mat).Any(n => n.StartsWith("Texture2D_")))
                return new Plan { kind = PlanKind.Skip, blend = current, reason = "変換済み・Shader Graph 由来" };

            if (!GetSavedFloats(mat).TryGetValue("_Mode", out float mode))
                return new Plan { kind = PlanKind.Skip, blend = current, reason = "変換済み・元のブレンド不明" };

            BlendType blend = BlendFromStandardMode(mode, out string modeName);
            if (blend == current)
                return new Plan { kind = PlanKind.Skip, blend = blend, reason = $"変換済み・旧 _Mode {modeName}" };
            return new Plan { kind = PlanKind.Reclassify, blend = blend, reason = $"旧 _Mode {modeName}" };
        }

        // Particles/Standard Unlit / Particles/Standard Surface
        if (name.StartsWith("Particles/Standard"))
        {
            float mode = mat.HasProperty("_Mode") ? mat.GetFloat("_Mode") : 2f;
            BlendType blend = BlendFromStandardMode(mode, out string modeName);
            return new Plan { kind = PlanKind.Convert, blend = blend, reason = $"_Mode {modeName}" };
        }

        // Legacy Shaders/Particles/Additive, Alpha Blended など
        if (name.StartsWith("Legacy Shaders/Particles/") || name.StartsWith("Mobile/Particles/") || name.StartsWith("Particles/"))
        {
            BlendType blend = name.Contains("Additive") ? BlendType.Additive : BlendType.AlphaBlend;
            return new Plan { kind = PlanKind.Convert, blend = blend, reason = "Legacy Particles" };
        }

        // Shader Graph（CartoonVFX9X_Shader1_Addictive / _Alpha など）
        if (name.StartsWith("Shader Graphs/"))
        {
            BlendType blend = name.Contains("Alpha") ? BlendType.AlphaBlend : BlendType.Additive;
            return new Plan { kind = PlanKind.Convert, blend = blend, reason = "Shader Graph" };
        }

        // Standard（床などパーティクル以外）、URP/Unlit（Hit_03 のように既に表示できるもの）などは触らない
        return new Plan { kind = PlanKind.Skip, reason = "対象外シェーダー" };
    }

    /// <summary>
    /// Standard 系の _Mode（0:Opaque 1:Cutout 2:Fade 3:Transparent 4:Additive 5:Subtractive 6:Modulate）からブレンドを決める
    /// </summary>
    private static BlendType BlendFromStandardMode(float mode, out string modeName)
    {
        switch (Mathf.RoundToInt(mode))
        {
            case 0: modeName = "Opaque"; return BlendType.AlphaBlend;
            case 1: modeName = "Cutout"; return BlendType.AlphaBlend;
            case 2: modeName = "Fade"; return BlendType.AlphaBlend;
            case 3: modeName = "Transparent"; return BlendType.AlphaBlend;
            case 4: modeName = "Additive"; return BlendType.Additive;
            case 5: modeName = "Subtractive(非対応→AlphaBlend)"; return BlendType.AlphaBlend;
            case 6: modeName = "Modulate(非対応→AlphaBlend)"; return BlendType.AlphaBlend;
            default: modeName = $"不明({mode})"; return BlendType.AlphaBlend;
        }
    }

    /// <summary>
    /// 未変換マテリアルのテクスチャ・色を _BaseMap / _BaseColor へ移してシェーダーを変更する
    /// </summary>
    private static void Convert(Material mat, Shader newShader)
    {
        // メインテクスチャを事前に取得（優先順に探す）
        Texture mainTex = null;
        Vector2 texScale = Vector2.one;
        Vector2 texOffset = Vector2.zero;
        foreach (string prop in GetMainTextureCandidates(mat))
        {
            Texture tex = mat.GetTexture(prop);
            if (tex == null) continue;
            mainTex = tex;
            texScale = mat.GetTextureScale(prop);
            texOffset = mat.GetTextureOffset(prop);
            break;
        }

        // 色を事前に取得。Legacy Particles の _TintColor はシェーダー内で2倍されている
        Color color = Color.white;
        if (mat.HasProperty("_TintColor"))
            color = mat.GetColor("_TintColor") * 2f;
        else if (mat.HasProperty("_Color"))
            color = mat.GetColor("_Color");
        else if (mat.HasProperty("_BaseColor"))
            color = mat.GetColor("_BaseColor");

        // Shader 変更
        mat.shader = newShader;
        mat.shaderKeywords = new string[0];
        mat.renderQueue = -1; // シェーダー側の Transparent を使う

        if (mainTex != null)
        {
            mat.SetTexture("_BaseMap", mainTex);
            mat.SetTextureScale("_BaseMap", texScale);
            mat.SetTextureOffset("_BaseMap", texOffset);
        }
        mat.SetColor("_BaseColor", NormalizeHdr(color));
    }

    /// <summary>
    /// メインテクスチャの候補プロパティ名（優先順）
    /// </summary>
    private static IEnumerable<string> GetMainTextureCandidates(Material mat)
    {
        Shader shader = mat.shader;
        var names = new List<string>();
        int propCount = ShaderUtil.GetPropertyCount(shader);
        for (int i = 0; i < propCount; i++)
        {
            if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                names.Add(ShaderUtil.GetPropertyName(shader, i));
        }

        foreach (string preferred in new[] { "_BaseMap", "_MainTex", "MainTexture" })
        {
            if (names.Contains(preferred)) yield return preferred;
        }
        // ShaderGraphs の GUID 形式プロパティ名
        foreach (string n in names.Where(n => n.StartsWith("Texture2D_")))
        {
            yield return n;
        }
    }

    /// <summary>
    /// Canvas は LDR で 1 を超える成分が白に飛ぶため、HDR カラーは色味を保ったまま 0〜1 に収める
    /// </summary>
    private static Color NormalizeHdr(Color c)
    {
        float max = Mathf.Max(c.r, c.g, c.b);
        if (max <= 1f) return c;
        return new Color(c.r / max, c.g / max, c.b / max, Mathf.Clamp01(c.a));
    }

    /// <summary>
    /// 現在のシェーダーに無いものも含め、マテリアルに保存されている float プロパティを取得する
    /// </summary>
    private static Dictionary<string, float> GetSavedFloats(Material mat)
    {
        var result = new Dictionary<string, float>();
        var floats = new SerializedObject(mat).FindProperty("m_SavedProperties.m_Floats");
        for (int i = 0; i < floats.arraySize; i++)
        {
            var e = floats.GetArrayElementAtIndex(i);
            result[e.FindPropertyRelative("first").stringValue] = e.FindPropertyRelative("second").floatValue;
        }
        return result;
    }

    /// <summary>
    /// 現在のシェーダーに無いものも含め、マテリアルに保存されているテクスチャプロパティ名を取得する
    /// </summary>
    private static List<string> GetSavedTextureNames(Material mat)
    {
        var result = new List<string>();
        var texEnvs = new SerializedObject(mat).FindProperty("m_SavedProperties.m_TexEnvs");
        for (int i = 0; i < texEnvs.arraySize; i++)
        {
            result.Add(texEnvs.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue);
        }
        return result;
    }
}
