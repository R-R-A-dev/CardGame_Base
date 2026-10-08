// Assets/Editor/CardEntityDuplicator.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CardEntityDuplicator : EditorWindow
{
    private const string DefaultFolderPath = "Assets/Resources/CardEntityList";
    private const string FilePrefix = "Card";

    private CardEntity sourceEntity;
    private string targetFolderPath = DefaultFolderPath;
    private int startNo = 5;
    private int endNo = 45;
    private bool assignSerialNo = true;
    private bool clearNameAndDescription = false;
    private bool overwriteExisting = false;

    [MenuItem("Tools/CardEntity 連番複製")]
    public static void ShowWindow()
    {
        GetWindow<CardEntityDuplicator>("CardEntity Duplicator");
    }

    private void OnEnable()
    {
        if (sourceEntity == null)
        {
            sourceEntity = AssetDatabase.LoadAssetAtPath<CardEntity>($"{DefaultFolderPath}/{FilePrefix}4.asset");
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("CardEntity 連番複製", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        sourceEntity = (CardEntity)EditorGUILayout.ObjectField("複製元", sourceEntity, typeof(CardEntity), false);
        targetFolderPath = EditorGUILayout.TextField("出力フォルダ", targetFolderPath);

        EditorGUILayout.Space();
        startNo = EditorGUILayout.IntField("開始番号", startNo);
        endNo = EditorGUILayout.IntField("終了番号", endNo);

        EditorGUILayout.Space();
        assignSerialNo = EditorGUILayout.Toggle("no を連番で設定", assignSerialNo);
        clearNameAndDescription = EditorGUILayout.Toggle("name/description をクリア", clearNameAndDescription);
        overwriteExisting = EditorGUILayout.Toggle("既存ファイルを上書き", overwriteExisting);

        EditorGUILayout.Space();
        int count = Mathf.Max(0, endNo - startNo + 1);
        EditorGUILayout.HelpBox(
            $"{FilePrefix}{startNo}.asset ～ {FilePrefix}{endNo}.asset（{count} 個）を作成します。" +
            (overwriteExisting ? "" : "\n既存ファイルはスキップされます。"),
            MessageType.Info);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(sourceEntity == null || count == 0))
        {
            if (GUILayout.Button("一括作成"))
            {
                Execute();
            }
        }
    }

    private void Execute()
    {
        if (sourceEntity == null)
        {
            EditorUtility.DisplayDialog("エラー", "複製元の CardEntity を指定してください。", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(targetFolderPath))
        {
            EditorUtility.DisplayDialog("エラー", $"フォルダ '{targetFolderPath}' が見つかりません。", "OK");
            return;
        }

        string sourcePath = AssetDatabase.GetAssetPath(sourceEntity);
        int created = 0;
        int skipped = 0;
        List<string> failed = new List<string>();

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int no = startNo; no <= endNo; no++)
            {
                string assetName = $"{FilePrefix}{no}";
                string destPath = $"{targetFolderPath}/{assetName}.asset";

                if (AssetDatabase.LoadAssetAtPath<CardEntity>(destPath) != null)
                {
                    if (!overwriteExisting)
                    {
                        skipped++;
                        continue;
                    }
                    AssetDatabase.DeleteAsset(destPath);
                }

                if (!AssetDatabase.CopyAsset(sourcePath, destPath))
                {
                    failed.Add(destPath);
                    continue;
                }

                created++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        // コピー後に中身を書き換える（StartAssetEditing 中はロードできないため分離）
        for (int no = startNo; no <= endNo; no++)
        {
            string assetName = $"{FilePrefix}{no}";
            string destPath = $"{targetFolderPath}/{assetName}.asset";
            CardEntity entity = AssetDatabase.LoadAssetAtPath<CardEntity>(destPath);
            if (entity == null) continue;

            SerializedObject so = new SerializedObject(entity);
            if (assignSerialNo)
            {
                so.FindProperty("no").intValue = no;
            }
            if (clearNameAndDescription)
            {
                so.FindProperty("name").stringValue = string.Empty;
                so.FindProperty("description").stringValue = string.Empty;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // ScriptableObject 側のオブジェクト名をファイル名に合わせる
            // （CardEntity は name フィールドで Object.name を隠しているためキャストして扱う）
            UnityEngine.Object assetObject = entity;
            if (assetObject.name != assetName)
            {
                assetObject.name = assetName;
            }
            EditorUtility.SetDirty(entity);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message = $"作成: {created} 個\nスキップ: {skipped} 個";
        if (failed.Count > 0)
        {
            message += $"\n失敗: {failed.Count} 個";
            foreach (string path in failed)
            {
                Debug.LogError($"[複製失敗] {path}");
            }
        }
        EditorUtility.DisplayDialog("完了", message, "OK");
    }
}
