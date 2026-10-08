// Assets/Editor/OutGameTransitionSetup.cs
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// アウトゲームの画面遷移演出（シーン遷移の暗転・モーダルの登場演出）を Field シーンへ設定する。
/// メニューから1回実行すれば設定が完了する。既に設定済みの項目は変更せずにスキップするため、再実行しても問題ない。
/// 実行後はシーンを保存すること（自動では保存しない）。
/// </summary>
public static class OutGameTransitionSetup
{
    private const string MenuPath = "Tools/演出/アウトゲーム遷移演出をセットアップ";
    private const string TargetSceneName = "Field";
    private const string CanvasRootName = "Canvas";
    private const string SceneTransitionObjectName = "SceneTransition";
    private const string LogPrefix = "[OutGameTransitionSetup] ";

    // Canvas からの相対パス
    private const string ModeSelection = "ShakeObj/ModeSelectionPanel";
    private const string Roguelike = ModeSelection + "/RoguelikeSceneController/RogulikePanle";
    private const string MapPanels = Roguelike + "/MapPanells";
    private const string TwoPick = ModeSelection + "/TwoPickPanel";
    private const string LethalPuzzle = ModeSelection + "/LethalPuzzleBG";
    private const string CpuBattle = ModeSelection + "/CpuBattlePanel";
    private const string Gacha = ModeSelection + "/GachaPanel";
    private const string DeckStructuring = "ShakeObj/DeckEdit/DeckStructuring";

    /// <summary>ModalEntranceAnimator を付けるルートと、拡大させるウィンドウ本体（ルートからの相対パス。null はルート自身）</summary>
    private static readonly (string label, string root, string window)[] Modals =
    {
        ("ローグライク Deck Select",        Roguelike + "/DeckPanelBG",                "DeckListPanle"),
        ("ローグライク Deck Details",       Roguelike + "/DeckConfirmPanelBG",         "DeckConfirmPanle"),
        ("ローグライク Stage Select",       Roguelike + "/StageConfirmPanelBG",        "StageConfirmPanle"),
        ("ショップ",                        MapPanels + "/ShopMapPanel",               "ShopListPannel/DeckCheckPannel"),
        ("宝箱",                            MapPanels + "/TreasureMapPanelBack",       "TreasureListPannel/DeckCheckPannel"),
        ("報酬",                            MapPanels + "/RewardMapPanelBack",         "RewardListPannel/DeckCheckPannel"),
        ("デッキ確認（ローグライク）",      Roguelike + "/DeckCheckPannelBG",          "DeckCheckPannel"),
        ("デッキ確認（2Pick）",             TwoPick + "/TwoPickSelectPanel/DeckCheckPannelBG", "DeckCheckPannel"),
        ("カード詳細（2Pick）",             TwoPick + "/TwoPickSelectPanel/CardInfoPannelBG",  "CardInfoPannel"),
        ("カード詳細（デッキ確認）",        Roguelike + "/DeckCheckPannelBG/DeckCheckPannel/CardInfoPannel", null),
        ("カード詳細（宝箱）",              MapPanels + "/TreasureMapPanelBack/TreasureListPannel/DeckCheckPannel/TreasureCardSelectPanel", "CardInfoPannel"),
        ("カード詳細（報酬）",              MapPanels + "/RewardMapPanelBack/RewardListPannel/DeckCheckPannel/RewardCardSelectPanel",       "CardInfoPannel"),
        ("カード詳細（ショップ）",          MapPanels + "/ShopMapPanel/ShopListPannel/DeckCheckPannel/ShopCardInfoPanel",                   "CardInfoPannel"),
        ("カード詳細（ガチャ）",            Gacha + "/CardInfoPannelBG/CardInfoPannel", null),
        ("ガチャ購入確認",                  Gacha + "/GachaUI/ConfirmPanelBG",         "ConfirmPanle"),
        ("ガチャ結果",                      Gacha + "/GachaResultUI",                  "PackResultPannelBG/DeckCheckPannel"),
        ("2Pick 確認",                      TwoPick + "/ConfirmPanelBG",               "ConfirmPanle"),
        ("Lethal Puzzle 確認",              LethalPuzzle + "/ConfirmPanelBG",          "ConfirmPanle"),
        ("デッキ編集フィルター",            DeckStructuring + "/FilterBG",             "CardFilter"),
        ("CPU Battle Deck Select",          CpuBattle + "/DeckPanelBG",                "DeckListPanle"),
        ("CPU Battle Deck Details",         CpuBattle + "/DeckPanelBG/DeckListPanle/DeckConfirmPanelBG", "DeckConfirmPanle"),
    };

    /// <summary>PanelEntranceAnimator のスライド対象から外すモーダルのルート（パネルからの相対パス）</summary>
    private static readonly (string panel, string[] excludes)[] EntranceExcludes =
    {
        (TwoPick,      new[] { "ConfirmPanelBG" }),
        (LethalPuzzle, new[] { "ConfirmPanelBG" }),
        (CpuBattle,    new[] { "DeckPanelBG" }),
        (Roguelike,    new[] { "DeckPanelBG", "DeckConfirmPanelBG", "StageConfirmPanelBG", "DeckCheckPannelBG" }),
        (Gacha,        new[] { "GachaResultUI" }),
    };

    /// <summary>アクティブのまま保存されてしまっていたオブジェクト</summary>
    private static readonly string[] Deactivates =
    {
        Roguelike,
        MapPanels + "/RewardMapPanelBack",
    };

    /// <summary>ガチャ購入確認で、ウィンドウ本体（ConfirmPanle）の兄弟になっている中身</summary>
    private const string GachaConfirmRoot = Gacha + "/GachaUI/ConfirmPanelBG";
    private const string GachaConfirmWindow = "ConfirmPanle";
    private static readonly string[] GachaConfirmContents =
    {
        "PackNameText", "QuantityText", "TotalPriceText", "MinusButton",
        "PlusButton", "PlayButton", "CloseButton", "Title",
    };

    private static int changeCount;
    private static int errorCount;

    [MenuItem(MenuPath)]
    public static void Run()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("アウトゲーム遷移演出", "再生中は実行できません。再生を停止してから実行してください。", "OK");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != TargetSceneName)
        {
            EditorUtility.DisplayDialog("アウトゲーム遷移演出",
                $"{TargetSceneName} シーンを開いてから実行してください。（現在: {scene.name}）", "OK");
            return;
        }

        Transform canvas = FindRoot(scene, CanvasRootName);
        if (canvas == null)
        {
            Debug.LogError(LogPrefix + $"ルートの {CanvasRootName} が見つからないため中断しました");
            return;
        }

        changeCount = 0;
        errorCount = 0;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("アウトゲーム遷移演出をセットアップ");
        int undoGroup = Undo.GetCurrentGroup();

        Debug.Log(LogPrefix + "===== セットアップ開始 =====");

        SetupSceneTransition(scene);
        ReparentGachaConfirmContents(canvas);
        SetupModals(canvas);
        SetupEntranceExcludes(canvas);
        DeactivateObjects(canvas);

        Undo.CollapseUndoOperations(undoGroup);

        if (changeCount > 0)
            EditorSceneManager.MarkSceneDirty(scene);

        string summary = $"===== セットアップ完了：変更 {changeCount} 件、エラー {errorCount} 件 =====";
        if (errorCount > 0)
            Debug.LogWarning(LogPrefix + summary + "（エラーの項目は設定されていません）");
        else
            Debug.Log(LogPrefix + summary + (changeCount > 0 ? "　シーンを保存してください" : "　既にすべて設定済みです"));
    }

    // ========================================
    // 1. シーン遷移の暗転
    // ========================================

    private static void SetupSceneTransition(Scene scene)
    {
        Transform existing = FindRoot(scene, SceneTransitionObjectName);
        if (existing != null)
        {
            if (existing.GetComponent<SceneTransition>() != null)
            {
                LogSkip($"{SceneTransitionObjectName}：既に存在するためスキップ");
                return;
            }

            Undo.AddComponent<SceneTransition>(existing.gameObject);
            LogChange($"{SceneTransitionObjectName}：既存のオブジェクトに SceneTransition を追加");
            return;
        }

        GameObject go = new GameObject(SceneTransitionObjectName);
        SceneManager.MoveGameObjectToScene(go, scene);
        Undo.RegisterCreatedObjectUndo(go, "Create SceneTransition");
        go.AddComponent<SceneTransition>();
        LogChange($"{SceneTransitionObjectName}：ルートにオブジェクトを作成し SceneTransition を追加");
    }

    // ========================================
    // 2. ガチャ購入確認の中身をウィンドウ本体の子へ移動
    // ========================================

    private static void ReparentGachaConfirmContents(Transform canvas)
    {
        Transform root = Find(canvas, GachaConfirmRoot);
        if (root == null)
            return;

        Transform window = Find(root, GachaConfirmWindow, GachaConfirmRoot);
        if (window == null)
            return;

        foreach (string name in GachaConfirmContents)
        {
            // 移動済みならウィンドウ本体の下にある
            if (FindUniqueChild(window, name, out _) != null)
            {
                LogSkip($"ガチャ購入確認：{name} は既に {GachaConfirmWindow} の子のためスキップ");
                continue;
            }

            Transform content = Find(root, name, GachaConfirmRoot);
            if (content == null)
                continue;

            // 見た目の位置を変えないよう worldPositionStays = true で移動する。
            // 移動先では末尾に追加されるため、元の兄弟順のまま枠の画像より手前に描画される
            Undo.SetTransformParent(content, window, true, "Reparent Gacha Confirm Contents");
            LogChange($"ガチャ購入確認：{name} を {GachaConfirmRoot} から {GachaConfirmWindow} の子へ移動");
        }
    }

    // ========================================
    // 3. モーダルの登場演出
    // ========================================

    private static void SetupModals(Transform canvas)
    {
        foreach ((string label, string rootPath, string windowPath) in Modals)
        {
            Transform root = Find(canvas, rootPath);
            if (root == null)
                continue;

            RectTransform window = null;
            if (windowPath != null)
            {
                Transform windowTransform = Find(root, windowPath, rootPath);
                if (windowTransform == null)
                    continue;

                window = windowTransform as RectTransform;
                if (window == null)
                {
                    LogError($"{label}：{rootPath}/{windowPath} に RectTransform が無いためスキップ");
                    continue;
                }
            }

            string windowLabel = windowPath ?? "（自分自身）";

            ModalEntranceAnimator animator = root.GetComponent<ModalEntranceAnimator>();
            bool added = false;
            if (animator == null)
            {
                animator = Undo.AddComponent<ModalEntranceAnimator>(root.gameObject);
                added = true;
            }

            SerializedObject so = new SerializedObject(animator);
            SerializedProperty windowProp = so.FindProperty("window");
            bool windowChanged = windowProp.objectReferenceValue != window;
            if (windowChanged)
            {
                windowProp.objectReferenceValue = window;
                so.ApplyModifiedProperties();
            }

            if (added)
                LogChange($"{label}：{rootPath} に ModalEntranceAnimator を追加（window = {windowLabel}）");
            else if (windowChanged)
                LogChange($"{label}：{rootPath} の ModalEntranceAnimator の window を {windowLabel} に変更");
            else
                LogSkip($"{label}：{rootPath} は設定済みのためスキップ");
        }
    }

    // ========================================
    // 4. PanelEntranceAnimator のスライド対象からモーダルを除外
    // ========================================

    private static void SetupEntranceExcludes(Transform canvas)
    {
        foreach ((string panelPath, string[] excludes) in EntranceExcludes)
        {
            Transform panel = Find(canvas, panelPath);
            if (panel == null)
                continue;

            PanelEntranceAnimator entrance = panel.GetComponent<PanelEntranceAnimator>();
            if (entrance == null)
            {
                LogError($"{panelPath} に PanelEntranceAnimator が無いためスキップ");
                continue;
            }

            SerializedObject so = new SerializedObject(entrance);
            SerializedProperty list = so.FindProperty("excludeFromSlide");

            foreach (string excludePath in excludes)
            {
                Transform target = Find(panel, excludePath, panelPath);
                if (target == null)
                    continue;

                if (ContainsReference(list, target))
                {
                    LogSkip($"{panelPath} の excludeFromSlide：{excludePath} は登録済みのためスキップ");
                    continue;
                }

                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = target;
                LogChange($"{panelPath} の PanelEntranceAnimator.excludeFromSlide に {excludePath} を追加");
            }

            so.ApplyModifiedProperties();
        }
    }

    private static bool ContainsReference(SerializedProperty list, Object target)
    {
        for (int i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == target)
                return true;
        }
        return false;
    }

    // ========================================
    // 5. アクティブのまま保存されていたオブジェクトを非アクティブに戻す
    // ========================================

    private static void DeactivateObjects(Transform canvas)
    {
        foreach (string path in Deactivates)
        {
            Transform target = Find(canvas, path);
            if (target == null)
                continue;

            if (!target.gameObject.activeSelf)
            {
                LogSkip($"{path} は既に非アクティブのためスキップ");
                continue;
            }

            Undo.RecordObject(target.gameObject, "Deactivate");
            target.gameObject.SetActive(false);
            LogChange($"{path} を非アクティブに変更");
        }
    }

    // ========================================
    // 検索・ログ
    // ========================================

    private static Transform FindRoot(Scene scene, string name)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            if (go.name == name)
                return go.transform;
        }
        return null;
    }

    /// <summary>
    /// 非アクティブを含めてパスで検索する。
    /// 途中の階層に同名のオブジェクトが複数ある場合は、誤ったオブジェクトを変更しないようエラーにする。
    /// </summary>
    private static Transform Find(Transform parent, string path, string parentLabel = CanvasRootName)
    {
        Transform current = parent;
        foreach (string name in path.Split('/'))
        {
            Transform next = FindUniqueChild(current, name, out int count);
            if (count == 0)
            {
                LogError($"{parentLabel}/{path} が見つかりません（{name} が無い）");
                return null;
            }
            if (count > 1)
            {
                LogError($"{parentLabel}/{path} を特定できません（{current.name} の下に {name} が {count} 個ある）");
                return null;
            }
            current = next;
        }
        return current;
    }

    private static Transform FindUniqueChild(Transform parent, string name, out int count)
    {
        Transform found = null;
        count = 0;
        foreach (Transform child in parent)
        {
            if (child.name != name)
                continue;

            found = child;
            count++;
        }
        return count == 1 ? found : null;
    }

    private static void LogChange(string message)
    {
        changeCount++;
        Debug.Log(LogPrefix + "[変更] " + message);
    }

    private static void LogSkip(string message)
    {
        Debug.Log(LogPrefix + "[スキップ] " + message);
    }

    private static void LogError(string message)
    {
        errorCount++;
        Debug.LogError(LogPrefix + "[エラー] " + message);
    }
}
