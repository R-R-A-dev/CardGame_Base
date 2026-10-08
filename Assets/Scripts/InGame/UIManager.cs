using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] GameObject resultPanel;
    [SerializeField] TextMeshProUGUI resultText;

    [SerializeField] TextMeshProUGUI playerHeroHpText;
    [SerializeField] TextMeshProUGUI enemyHeroHpText;

    [SerializeField] TextMeshProUGUI playerManaCostText;
    [SerializeField] TextMeshProUGUI enemyManaCostText;

    [SerializeField] TextMeshProUGUI timeCountText;

    // 山札の残り枚数（手札は含まない）
    [SerializeField] TextMeshProUGUI playerDeckNumText;
    [SerializeField] TextMeshProUGUI enemyDeckNumText;

    [SerializeField] public GameObject descriptionObj;
    [SerializeField] public Text descriptionText;
    [SerializeField] public Text nameText;
    [SerializeField] public TextMeshProUGUI costText;
    [SerializeField] public TextMeshProUGUI attackText;
    [SerializeField] public TextMeshProUGUI hpText;
    [SerializeField] public Image characterImage;
    [SerializeField] GameObject attackPanel;
    [SerializeField] GameObject hpPanel;

    [Header("2Pick 勝利後の選択パネル")]
    [SerializeField] GameObject twoPickResultPanel;
    [SerializeField] GameObject twoPickNextBattleButton;
    [SerializeField] TextMeshProUGUI twoPickResultText;



    public void ShowDescriptionPanel(CardController card)
    {
        nameText.text = card.model.name;
        descriptionText.text = card.model.description;
        costText.text = card.model.cost.ToString();
        attackText.text = card.model.at.ToString();
        hpText.text = card.model.hp.ToString();
        characterImage.sprite = card.model.icon;
        if (!card.IsSpell)
        {
            attackPanel.SetActive(true);
            hpPanel.SetActive(true);

        }
        else if(card.IsSpell)
        {
            attackPanel.SetActive(false);
            hpPanel.SetActive(false);
        }
        descriptionObj.SetActive(true);
    }
    public void CloseDescriptionPanel()
    {
        descriptionObj.SetActive(false);
    }

    public void HideResultPanel()
    {
        resultPanel.SetActive(false);
    }

    public void ShowManaCost(int playerManaCost, int enemyManaCost)
    {
        playerManaCostText.text = playerManaCost.ToString();
        enemyManaCostText.text = enemyManaCost.ToString();
    }

    public void UpdateTime(int timeCount)
    {
        timeCountText.text = timeCount.ToString();
    }

    public void ShowDeckNum(int playerDeckNum, int enemyDeckNum)
    {
        if (playerDeckNumText != null)
            playerDeckNumText.text = "× " + playerDeckNum;
        if (enemyDeckNumText != null)
            enemyDeckNumText.text = "× " + enemyDeckNum;
    }

    public void ShowHeroHP(int playerHeroHp, int enemyHeroHp)
    {
        playerHeroHpText.text = playerHeroHp.ToString();
        enemyHeroHpText.text = enemyHeroHp.ToString();
    }

    public void ShowResultPanel(int heroHp)
    {
        resultPanel.SetActive(true);
        if (heroHp <= 0)
        {
            resultText.text = "LOSE";
        }
        else
        {
            resultText.text = "WIN";
        }
    }

    /// <summary>
    /// 2Pick勝利時：「次の対戦へ」「やめる」を選択させるパネルを表示する
    /// </summary>
    /// <param name="hasNextBattle">次の対戦（敵デッキ）が残っているか。falseの場合は「次の対戦へ」ボタンを隠す</param>
    public void ShowTwoPickResultPanel(bool hasNextBattle)
    {
        twoPickResultPanel.SetActive(true);
        if (twoPickNextBattleButton != null)
            twoPickNextBattleButton.SetActive(hasNextBattle);
        if (twoPickResultText != null)
            twoPickResultText.text = hasNextBattle ? "WIN" : "終了";
    }

    public void HideTwoPickResultPanel()
    {
        if (twoPickResultPanel != null)
            twoPickResultPanel.SetActive(false);
    }
}
