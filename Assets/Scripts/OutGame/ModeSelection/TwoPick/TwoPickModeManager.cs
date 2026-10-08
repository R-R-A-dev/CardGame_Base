using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class TwoPickModeManager : MonoBehaviour
{
    public static TwoPickModeManager Instance { get; private set; }

    [Header("設定")]
    [SerializeField] private int totalPicks = 15; // 選択回数
    [SerializeField] private TwoPickData twoPickData; // 使用するカードプールデータ

    [Header("UI参照")]
    [SerializeField] private TwoPickUI twoPickUI;
    [SerializeField] private DeckStatisticsUI deckStatisticsUI;
    [SerializeField] private SortCards sortCards;

    int pickCount = 0;
    public TwoPickCardSelector cardSelector;
    public TwoPickProgress pickProgress;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // 各システムを初期化
        cardSelector = new TwoPickCardSelector();
        pickProgress = new TwoPickProgress();
    }

    /// <summary>
    /// 2Pick開始
    /// </summary>
    public void StartPick()
    {
        //pickProgress.Reset();
        //ShowNextPick();
        PickCountReset();

        // このステージの敵デッキ情報と対戦インデックスをセッションにセット
        GameSession.TwoPickData = twoPickData;
        GameSession.TwoPickBattleIndex = 0;

        //開始時のカード表示
        twoPickUI.CreateCard(twoPickData.twoPickCards, pickCount);
    }

    /// <summary>
    /// 次の4枚を表示する。ピック演出の途中で呼ぶため、統計の更新とは分けている
    /// </summary>
    public void ShowNextCards()
    {
        if (pickCount < 20)
            twoPickUI.CreateCard(twoPickData.twoPickCards, pickCount);
    }

    /// <summary>
    /// 選択したカードの合計を渡し、コストの棒を伸ばしながら統計を更新する
    /// </summary>
    public void RefreshStatistics(List<int> selectedCards)
    {
        deckStatisticsUI.RefreshStatistics(0, selectedCards, animate: true);
    }

    public float StatisticsAnimationDuration => deckStatisticsUI.CostBarAnimationDuration;

    public void PickCountUp()
    {
        pickCount++;
    }

    public void PickCountReset()
    {
        pickCount = 0;
    }

    public int GetPickCount()
    {
        return pickCount;
    }




    /// <summary>
    /// カードが選択されたら
    /// </summary>
/*    public void OnCardSelected(int cardId)
    {
        // 選択されたカードをデッキに追加
        pickProgress.AddSelectedCard(cardId);

        Debug.Log($"選択: {cardId} ({pickProgress.CurrentPick}/{pickProgress.TotalPicks})");

        // 全選択完了チェック
        if (pickProgress.IsComplete())
        {
            OnPickComplete();
        }
        else
        {
            // 次の選択へ
            ShowNextPick();
        }
    }*/



    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartGame()
    {
        SceneTransition.Load("Game");
    }

}
/*後にやること
 * カードクリック時に増える数値
 * 次のカードの表示
 *  ボタン処理
 * 表示されたカードの種類を合計に追加させる
 *
 *
*/