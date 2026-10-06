// Assets/Editor/StageInfoPanelSetup.cs
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ローグライクのマップに置いた StageInfoPanel の中身（子オブジェクト・参照・種類ごとの表示設定）を Field シーンへ設定する。
/// メニューから1回実行すれば設定が完了する。既にある子オブジェクトや設定済みの項目は変更せずにスキップするため、再実行しても問題ない。
/// パネル自身の Image（背景）には触れない。実行後はシーンを保存すること（自動では保存しない）。
/// </summary>
public static class StageInfoPanelSetup
{
    private const string MenuPath = "Tools/ローグライク/ステージ情報パネルをセットアップ";
    private const string TargetSceneName = "Field";
    private const string CanvasRootName = "Canvas";
    private const string LogPrefix = "[StageInfoPanelSetup] ";

    // Canvas からの相対パス
    private const string Stage1 = "ShakeObj/ModeSelectionPanel/RoguelikeSceneController/RogulikePanle/MapUI/Stage_1";
    private const string SourceMapManager = "MapManager";               // 作成済みの StageInfoPanel があるマップ
    private static readonly string[] CopyMapManagers = { "MapManager (1)" }; // パネルを複製して持たせるマップ
    private const string PanelName = "StageInfoPanel";
    private const string StatusUIName = "StatusUI";                      // フォントをそろえる元

    // Start ボタンの画像と Color Tint の色
    private const string ButtonNormalSprite = "Btn_Blue_n";
    private static readonly ColorBlock StartButtonColors = new ColorBlock
    {
        normalColor = Color.white,
        highlightedColor = new Color(0.85f, 0.95f, 1f, 1f),     // カーソルを乗せると少し青白く
        pressedColor = new Color(0.65f, 0.75f, 0.85f, 1f),      // 押した瞬間は沈んだ色
        selectedColor = Color.white,                            // クリック後に色が残らないよう通常と同じ
        disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.6f),    // 押せない時は暗く半透明にして区別する
        colorMultiplier = 1f,
        fadeDuration = 0.1f,
    };

    /// <summary>種類ごとの表示設定。色・アイコン・説明文は凡例（legend が null なら空のまま）から読み取る</summary>
    private static readonly (StageType type, string legend, string tag, string stageName)[] Displays =
    {
        (StageType.NORMAL_BATTLE, "LegentItem_Battle",   "戦闘 COMBAT",     "戦闘ステージ"),
        (StageType.ELITE_BATTLE,  "LegentItem_Battle",   "戦闘 COMBAT",     "戦闘ステージ"),
        (StageType.TREASURE,      "LegentItem_Treasure", "宝箱 TREASURE",   "宝箱"),
        (StageType.REST,          "LegentItem_Rest",     "休憩 REST",       "休憩所"),
        (StageType.SHOP,          "LegentItem_Shop",     "ショップ SHOP",   "ショップ"),
        (StageType.EVENT,         null,                  "イベント EVENT",  "イベント"),
        (StageType.BOSS_BATTLE,   "LegentItem_Boss",     "ボス BOSS",       "ボスステージ"),
    };
    private const string DiamondSpriteLegend = "LegentItem_Battle";     // ひし形の画像を借りる凡例

    private static int changeCount;
    private static int errorCount;

    private static TMP_FontAsset font;
    private static Material fontMaterial;

    [MenuItem(MenuPath)]
    public static void Run()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("ステージ情報パネル", "再生中は実行できません。再生を停止してから実行してください。", "OK");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != TargetSceneName)
        {
            EditorUtility.DisplayDialog("ステージ情報パネル",
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
        Undo.SetCurrentGroupName("ステージ情報パネルをセットアップ");
        int undoGroup = Undo.GetCurrentGroup();

        Debug.Log(LogPrefix + "===== セットアップ開始 =====");

        Setup(canvas);

        Undo.CollapseUndoOperations(undoGroup);

        if (changeCount > 0)
            EditorSceneManager.MarkSceneDirty(scene);

        string summary = $"===== セットアップ完了：変更 {changeCount} 件、エラー {errorCount} 件 =====";
        if (errorCount > 0)
            Debug.LogWarning(LogPrefix + summary + "（エラーの項目は設定されていません）");
        else
            Debug.Log(LogPrefix + summary + (changeCount > 0 ? "　シーンを保存してください" : "　既にすべて設定済みです"));
    }

    private static void Setup(Transform canvas)
    {
        Transform stage = Find(canvas, Stage1);
        if (stage == null) return;

        Transform sourceManager = Find(stage, SourceMapManager, Stage1);
        if (sourceManager == null) return;

        Transform panelTransform = Find(sourceManager, PanelName, $"{Stage1}/{SourceMapManager}");
        if (panelTransform == null) return;

        LoadFont(sourceManager);

        StageInfoPanel panel = panelTransform.GetComponent<StageInfoPanel>();
        if (panel == null)
        {
            panel = Undo.AddComponent<StageInfoPanel>(panelTransform.gameObject);
            LogChange($"{PanelName} に StageInfoPanel を追加");
        }

        BuildChildren(panel);
        SetupDisplays(canvas, panel);
        AssignPanel(sourceManager, panel);

        foreach (string managerName in CopyMapManagers)
        {
            Transform manager = Find(stage, managerName, Stage1);
            if (manager == null) continue;

            StageInfoPanel copy = GetOrCopyPanel(manager, panel);
            AssignPanel(manager, copy);

            // 以前のセットアップで複製済みのパネルにも Color Tint を反映する
            Transform copyButton = copy != null ? FindUniqueChild(copy.transform, "StartButton", out _) : null;
            if (copyButton != null && copyButton.TryGetComponent(out Button button))
                ApplyStartButtonColorTint(button, manager.name);
        }
    }

    // ========================================
    // 1. 子オブジェクトを作成して参照を設定
    // ========================================

    private static void BuildChildren(StageInfoPanel panel)
    {
        Transform root = panel.transform;
        SerializedObject so = new SerializedObject(panel);

        // タグ
        RectTransform typeTag = GetOrCreate(root, "TypeTag", TopCenter(0f, -40f, 240f, 44f), out bool created);
        if (created)
        {
            Image tagImage = typeTag.gameObject.AddComponent<Image>();
            tagImage.color = new Color(0f, 0f, 0f, 0.45f);
            tagImage.raycastTarget = false;
        }
        TextMeshProUGUI typeTagText = GetOrCreateText(typeTag, "TypeTagText", Stretch(), "戦闘 COMBAT", 24f, FontStyles.Bold);

        // ひし形とアイコン
        RectTransform diamond = GetOrCreate(root, "Diamond", TopCenter(0f, -110f, 160f, 160f), out created);
        if (created)
        {
            Image diamondImage = diamond.gameObject.AddComponent<Image>();
            diamondImage.sprite = FindLegendDiamondSprite(root);
            diamondImage.raycastTarget = false;
        }
        RectTransform icon = GetOrCreate(diamond, "Icon", Center(80f, 80f), out created);
        if (created)
        {
            Image iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        // ステージ名・説明文
        TextMeshProUGUI stageNameText = GetOrCreateText(root, "StageNameText", TopCenter(0f, -290f, 440f, 56f), "戦闘ステージ", 36f, FontStyles.Bold);
        TextMeshProUGUI descriptionText = GetOrCreateText(root, "DescriptionText", TopCenter(0f, -350f, 440f, 60f), "敵と戦う", 24f, FontStyles.Normal);

        // 主な報酬
        RectTransform rewardGroup = GetOrCreate(root, "RewardGroup", TopCenter(0f, -420f, 440f, 150f), out _);
        GetOrCreateText(rewardGroup, "RewardHeader", TopCenter(0f, 0f, 440f, 36f), "主な報酬", 24f, FontStyles.Bold);
        TextMeshProUGUI rewardText = GetOrCreateText(rewardGroup, "RewardText", TopCenter(0f, -42f, 440f, 100f), "G +10〜29\nカード 3枚から1枚選択", 26f, FontStyles.Normal);
        rewardText.alignment = TextAlignmentOptions.Top;

        // Start ボタン
        Button startButton = GetOrCreateStartButton(root);

        AssignReference(so, "typeTagText", typeTagText);
        AssignReference(so, "diamondImage", diamond.GetComponent<Image>());
        AssignReference(so, "iconImage", icon.GetComponent<Image>());
        AssignReference(so, "stageNameText", stageNameText);
        AssignReference(so, "descriptionText", descriptionText);
        AssignReference(so, "rewardGroup", rewardGroup.gameObject);
        AssignReference(so, "rewardText", rewardText);
        AssignReference(so, "startButton", startButton);
        so.ApplyModifiedProperties();
    }

    private static Button GetOrCreateStartButton(Transform root)
    {
        RectTransform rect = GetOrCreate(root, "StartButton", BottomCenter(0f, 40f, 260f, 80f), out bool created);
        if (created)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(ButtonNormalSprite);
            if (image.sprite != null && image.sprite.border != Vector4.zero)
                image.type = Image.Type.Sliced;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            LogChange($"StartButton：Image（{ButtonNormalSprite}）と Button を設定");
        }

        GetOrCreateText(rect, "Text", Stretch(), "Start", 32f, FontStyles.Bold);

        Button result = rect.GetComponent<Button>();
        if (result == null)
        {
            LogError("StartButton に Button がありません");
            return null;
        }

        ApplyStartButtonColorTint(result, rect.parent.parent.name);
        return result;
    }

    /// <summary>
    /// Start ボタンを Color Tint にする。Color Tint になっていないときだけ変更するので、
    /// 変更後にインスペクターで色を調整しても再実行で上書きされない
    /// </summary>
    private static void ApplyStartButtonColorTint(Button button, string ownerLabel)
    {
        if (button.transition == Selectable.Transition.ColorTint)
        {
            LogSkip($"{ownerLabel}/StartButton：既に Color Tint のためスキップ");
            return;
        }

        Undo.RecordObject(button, "StartButton Color Tint");
        button.transition = Selectable.Transition.ColorTint;
        button.colors = StartButtonColors;
        LogChange($"{ownerLabel}/StartButton：Transition を Color Tint に変更");
    }

    // ========================================
    // 2. 種類ごとの表示設定を凡例から読み取って登録
    // ========================================

    private static void SetupDisplays(Transform canvas, StageInfoPanel panel)
    {
        SerializedObject so = new SerializedObject(panel);
        SerializedProperty list = so.FindProperty("stageTypeDisplays");

        foreach ((StageType type, string legendName, string tag, string stageName) in Displays)
        {
            if (ContainsStageType(list, type))
            {
                LogSkip($"表示設定：{type} は登録済みのためスキップ");
                continue;
            }

            Color diamondColor = Color.white;
            Color stageNameColor = Color.white;
            Sprite icon = null;
            string description = "";

            if (legendName != null)
            {
                Transform legend = FindByNameRecursive(canvas, legendName);
                if (legend == null)
                {
                    LogError($"表示設定：{type} の凡例 {legendName} が見つからないためスキップ");
                    continue;
                }

                Image legendImage = legend.GetComponent<Image>();
                if (legendImage != null) diamondColor = legendImage.color;

                foreach (Transform child in legend)
                {
                    if (child.name == "Image" && child.TryGetComponent(out Image iconImage))
                        icon = iconImage.sprite;
                    else if (child.name.StartsWith("Label") && child.TryGetComponent(out Text label))
                        stageNameColor = label.color;
                    else if (child.name.StartsWith("Description") && child.TryGetComponent(out Text desc))
                        description = desc.text;
                }
            }

            list.arraySize++;
            SerializedProperty element = list.GetArrayElementAtIndex(list.arraySize - 1);
            element.FindPropertyRelative("stageType").enumValueIndex = (int)type;
            element.FindPropertyRelative("tagText").stringValue = tag;
            element.FindPropertyRelative("stageName").stringValue = stageName;
            element.FindPropertyRelative("description").stringValue = description;
            element.FindPropertyRelative("diamondColor").colorValue = diamondColor;
            element.FindPropertyRelative("stageNameColor").colorValue = stageNameColor;
            element.FindPropertyRelative("icon").objectReferenceValue = icon;

            LogChange(legendName != null
                ? $"表示設定：{type} を追加（色・アイコン・説明文は {legendName} から）"
                : $"表示設定：{type} を追加（凡例が無いため色・アイコン・説明文は未設定）");
        }

        so.ApplyModifiedProperties();
    }

    private static bool ContainsStageType(SerializedProperty list, StageType type)
    {
        for (int i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).FindPropertyRelative("stageType").enumValueIndex == (int)type)
                return true;
        }
        return false;
    }

    private static Sprite FindLegendDiamondSprite(Transform panelRoot)
    {
        Transform canvas = panelRoot.root;
        Transform legend = FindByNameRecursive(canvas, DiamondSpriteLegend);
        Image image = legend != null ? legend.GetComponent<Image>() : null;
        if (image == null || image.sprite == null)
        {
            LogError($"ひし形の画像を {DiamondSpriteLegend} から取得できませんでした。Diamond の Source Image を手動で設定してください");
            return null;
        }
        return image.sprite;
    }

    // ========================================
    // 3. 他のマップへパネルを複製し、MapManager へ参照を設定
    // ========================================

    private static StageInfoPanel GetOrCopyPanel(Transform manager, StageInfoPanel source)
    {
        Transform existing = FindUniqueChild(manager, PanelName, out int count);
        if (count > 1)
        {
            LogError($"{manager.name} の下に {PanelName} が {count} 個あるため複製をスキップ");
            return null;
        }
        if (existing != null)
        {
            LogSkip($"{manager.name}：{PanelName} が既にあるため複製をスキップ");
            return existing.GetComponent<StageInfoPanel>();
        }

        // 複製先のマップも同じ大きさなので、ローカル座標のまま複製する。参照は複製後の子オブジェクトに付け替わる
        GameObject copy = Object.Instantiate(source.gameObject, manager, false);
        copy.name = PanelName;
        Undo.RegisterCreatedObjectUndo(copy, "Copy StageInfoPanel");
        LogChange($"{manager.name}：{SourceMapManager} の {PanelName} を複製");
        return copy.GetComponent<StageInfoPanel>();
    }

    private static void AssignPanel(Transform manager, StageInfoPanel panel)
    {
        if (panel == null) return;

        MapManager mapManager = manager.GetComponent<MapManager>();
        if (mapManager == null)
        {
            LogError($"{manager.name} に MapManager がありません");
            return;
        }

        SerializedObject so = new SerializedObject(mapManager);
        AssignReference(so, "stageInfoPanel", panel, manager.name);
        so.ApplyModifiedProperties();
    }

    // ========================================
    // 作成・参照設定
    // ========================================

    private struct Layout
    {
        public Vector2 anchorMin, anchorMax, pivot, position, size;
    }

    private static Layout TopCenter(float x, float y, float width, float height) => new Layout
    {
        anchorMin = new Vector2(0.5f, 1f), anchorMax = new Vector2(0.5f, 1f), pivot = new Vector2(0.5f, 1f),
        position = new Vector2(x, y), size = new Vector2(width, height),
    };

    private static Layout BottomCenter(float x, float y, float width, float height) => new Layout
    {
        anchorMin = new Vector2(0.5f, 0f), anchorMax = new Vector2(0.5f, 0f), pivot = new Vector2(0.5f, 0f),
        position = new Vector2(x, y), size = new Vector2(width, height),
    };

    private static Layout Center(float width, float height) => new Layout
    {
        anchorMin = new Vector2(0.5f, 0.5f), anchorMax = new Vector2(0.5f, 0.5f), pivot = new Vector2(0.5f, 0.5f),
        position = Vector2.zero, size = new Vector2(width, height),
    };

    private static Layout Stretch() => new Layout
    {
        anchorMin = Vector2.zero, anchorMax = Vector2.one, pivot = new Vector2(0.5f, 0.5f),
        position = Vector2.zero, size = Vector2.zero,
    };

    /// <summary>同名の子があればそのまま使い（配置も変えない）、無ければ作成する</summary>
    private static RectTransform GetOrCreate(Transform parent, string name, Layout layout, out bool created)
    {
        Transform existing = FindUniqueChild(parent, name, out int count);
        if (count > 0)
        {
            if (count > 1)
                LogError($"{parent.name} の下に {name} が {count} 個あります。1つにしてから再実行してください");
            created = false;
            return (RectTransform)(existing != null ? existing : parent.Find(name));
        }

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = layout.anchorMin;
        rect.anchorMax = layout.anchorMax;
        rect.pivot = layout.pivot;
        rect.anchoredPosition = layout.position;
        rect.sizeDelta = layout.size;

        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        LogChange($"{parent.name} の下に {name} を作成");
        created = true;
        return rect;
    }

    private static TextMeshProUGUI GetOrCreateText(Transform parent, string name, Layout layout, string text, float fontSize, FontStyles style)
    {
        RectTransform rect = GetOrCreate(parent, name, layout, out bool created);
        if (!created)
        {
            TextMeshProUGUI existing = rect.GetComponent<TextMeshProUGUI>();
            if (existing == null)
                LogError($"{name} に TextMeshPro - Text (UI) がありません");
            return existing;
        }

        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            tmp.font = font;
            tmp.fontSharedMaterial = fontMaterial;
        }
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    /// <summary>未設定のときだけ参照を設定する（手動で付け替えた参照は上書きしない）</summary>
    private static void AssignReference(SerializedObject so, string propertyName, Object value, string ownerLabel = PanelName)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            LogError($"{ownerLabel} に {propertyName} の項目がありません");
            return;
        }
        if (value == null) return;

        if (prop.objectReferenceValue != null)
        {
            LogSkip($"{ownerLabel}.{propertyName} は設定済みのためスキップ");
            return;
        }

        prop.objectReferenceValue = value;
        LogChange($"{ownerLabel}.{propertyName} に {value.name} を設定");
    }

    // ========================================
    // アセット・検索・ログ
    // ========================================

    /// <summary>マップの StatusUI と同じフォントを使う（日本語が表示できるフォント設定にそろえるため）</summary>
    private static void LoadFont(Transform mapManager)
    {
        font = null;
        fontMaterial = null;

        Transform statusUI = FindUniqueChild(mapManager, StatusUIName, out _);
        TextMeshProUGUI sample = statusUI != null ? statusUI.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        if (sample == null)
        {
            LogError($"{mapManager.name}/{StatusUIName} のテキストが見つからないため、TextMeshPro の既定のフォントを使います");
            return;
        }

        font = sample.font;
        fontMaterial = sample.fontSharedMaterial;
    }

    private static Sprite LoadSprite(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets($"{name} t:Sprite"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != name) continue;

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
        }

        LogError($"画像 {name} が見つかりません。StartButton に手動で設定してください");
        return null;
    }

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

    private static Transform FindByNameRecursive(Transform root, string name)
    {
        List<Transform> found = new List<Transform>();
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name)
                found.Add(t);
        }
        if (found.Count > 1)
            Debug.LogWarning(LogPrefix + $"{name} が {found.Count} 個あるため、最初に見つかったものを使います");
        return found.Count > 0 ? found[0] : null;
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
