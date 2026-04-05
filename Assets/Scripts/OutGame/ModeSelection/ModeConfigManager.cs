using System.Collections.Generic;
using UnityEngine;

public class ModeConfigManager : MonoBehaviour
{
    public static ModeConfigManager Instance { get; private set; }
    public int LethalPuzzleIndex { get => lethalPuzzleIndex; set => lethalPuzzleIndex = value; }

    [Header("現在のゲームモード")]
    public GameMode currentGameMode = GameMode.NONE;

    [Header("詰将棋データリスト")]
    public List<LethalPuzzleData> lethalPuzzleList = new List<LethalPuzzleData>();

    [Header("2Pickデータリスト")]
    public List<TwoPickData> twoPickList = new List<TwoPickData>();

    [Header("ローグライクデータリスト")]
    public List<RoguelikeStageData> roguelikeList = new List<RoguelikeStageData>();

    int lethalPuzzleIndex = 0;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 指定IDの詰将棋データを取得
    /// </summary>
    public LethalPuzzleData GetLethalPuzzleData(int id)
    {
        return lethalPuzzleList.Find(x => x.puzzleId == id);
    }

    /// <summary>
    /// 指定インデックスの2Pickデータを取得
    /// </summary>
    public TwoPickData GetTwoPickData(int index)
    {
        if (index >= 0 && index < twoPickList.Count)
            return twoPickList[index];
        return null;
    }

    /// <summary>
    /// 指定インデックスのローグライクデータを取得
    /// </summary>
    public RoguelikeStageData GetRoguelikeData(int index)
    {
        if (index >= 0 && index < roguelikeList.Count)
            return roguelikeList[index];
        return null;
    }

    public void ChangeMode(GameMode mode)
    {
        currentGameMode = mode;
    }



}

public enum GameMode
{
    NONE,           // 未選択
    LETHAL_PUZZLE,  // 詰将棋
    TWO_PICK,       // 2Pick
    ROGUELIKE       // ローグライク
}

/*
 * 押された後にすること
 * 押されたものからモードの項目を取得してGameManagerに渡す
 * リスト取得クラス
 * 
 * 
 * 
*/