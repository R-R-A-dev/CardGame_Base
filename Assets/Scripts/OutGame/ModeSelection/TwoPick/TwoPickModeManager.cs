using DG.Tweening;
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
        //開始時のカード表示と
        twoPickUI.CreateCard(twoPickData.twoPickCards, pickCount);
    }

    public void OnLeftButtonClick()
    {
        twoPickUI.CreateCard(twoPickData.twoPickCards, pickCount);
    }

    public void OnRightButtonClick()
    {
        twoPickUI.CreateCard(twoPickData.twoPickCards, pickCount);
    }

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
    /// カードが選択された時
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
}
/*次にすること
 * カードクリック後に増える数値
 * 次のカードの表示
 *  ボタンを押す
 * 表示されたカードの種類が統計に追加される
 * 
 * 
*/