using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private static string SavePath
        => Path.Combine(Application.persistentDataPath, "save.json");

    private static string BackupPath
        => Path.Combine(Application.persistentDataPath, "save_backup.json");

    public static void Save(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);

            // 既存のセーブをバックアップに退避してから上書き
            if (File.Exists(SavePath))
                File.Copy(SavePath, BackupPath, true);

            File.WriteAllText(SavePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"セーブデータ書き込みエラー: {e.Message}");
        }
    }

    public static SaveData Load()
    {
        // メインファイルから読み込み試行
        if (File.Exists(SavePath))
        {
            SaveData data = TryLoad(SavePath);
            if (data != null) return data;

            Debug.LogWarning("メインセーブが壊れています。バックアップから復元します");
        }

        // メインが壊れていたらバックアップから復元
        if (File.Exists(BackupPath))
        {
            SaveData data = TryLoad(BackupPath);
            if (data != null)
            {
                // バックアップをメインに復元
                try
                {
                    File.Copy(BackupPath, SavePath, true);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"バックアップ復元エラー: {e.Message}");
                }
                return data;
            }
        }

        Debug.LogWarning("セーブデータが見つかりません。新規データを作成します");
        return new SaveData();
    }

    private static SaveData TryLoad(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            // nullチェック（JSONが空など）
            if (data == null)
            {
                Debug.LogError($"セーブデータがnullです: {path}");
                return null;
            }

            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"セーブデータ読み込みエラー: {path} / {e.Message}");
            return null;
        }
    }

    public static bool HasSaveData()
    {
        return File.Exists(SavePath);
    }

    public static void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);

            if (File.Exists(BackupPath))
                File.Delete(BackupPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"セーブデータ削除エラー: {e.Message}");
        }
    }
}