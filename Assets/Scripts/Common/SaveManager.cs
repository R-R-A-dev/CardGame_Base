using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private static string SavePath
        => Path.Combine(Application.persistentDataPath, "save.json");

    private static string BackupPath
        => Path.Combine(Application.persistentDataPath, "save_backup.json");



    // ========================================
    // 初回起動時の初期データ作成
    // ========================================
    public static SaveData CreateInitialSaveData()
    {
        SaveData saveData = new SaveData();

        // 初期カードを配布
        saveData.ownedCardCounts = new List<int> { 2, 2, 2, 2 };

        // 初期所持金
        saveData.gold = 100;

        // 最初のパックをアンロック
        saveData.unlockedPackIds = new List<int> { 0 };

        Save(saveData);
        Debug.Log("初期セーブデータを作成しました");

        return saveData;
    }

    // ========================================
    // 初回起動かどうかを確認してロード
    // ========================================
    public static SaveData LoadOrInitialize()
    {
        if (!HasSaveData())
            return CreateInitialSaveData();

        return Load();
    }

    // 以下既存のまま
    public static void Save(SaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);

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
        if (File.Exists(SavePath))
        {
            SaveData data = TryLoad(SavePath);
            if (data != null) return data;

            Debug.LogWarning("メインセーブが壊れています。バックアップから復元します");
        }

        if (File.Exists(BackupPath))
        {
            SaveData data = TryLoad(BackupPath);
            if (data != null)
            {
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