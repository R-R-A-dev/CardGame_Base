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

    private TwoPickCardSelector cardSelector;
    private TwoPickProgress pickProgress;

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
        //cardSelector = new TwoPickCardSelector(twoPickData);
        //pickProgress = new TwoPickProgress(totalPicks);
    }

    /// <summary>
    /// 2Pick開始
    /// </summary>
    public void StartPick()
    {
        //pickProgress.Reset();
        //ShowNextPick();
        twoPickUI.ShowPickCard();
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
