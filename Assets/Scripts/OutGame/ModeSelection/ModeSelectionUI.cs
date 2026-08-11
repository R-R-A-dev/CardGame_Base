using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeSelectionUI : MonoBehaviour
{
    [Header("�p�l��")]
    [SerializeField] private GameObject modeSelectPanel;      // ���[�h�I�����C���p�l��
    [SerializeField] private GameObject lethalPuzzlePanel;    // �l�����I���p�l��
    [SerializeField] private GameObject twoPickPanel;         // 2Pick�I���p�l��
    [SerializeField] private GameObject roguelikePanel;       // ���[�O���C�N�I���p�l��

    [Header("�V�[�����ݒ�")]
    [SerializeField] private string battleScene = "Battle";
    [SerializeField] private string titleScene = "Title";

    private void Start()
    {
        // ������ԁF���[�h�I���p�l���̂ݕ\��
        ShowModeSelectPanel();
    }

    // ========================================
    // �p�l���\������
    // ========================================

    /// <summary>
    /// ���[�h�I���p�l����\��
    /// </summary>
    public void ShowModeSelectPanel()
    {
        modeSelectPanel.SetActive(true);
        lethalPuzzlePanel.SetActive(false);
        twoPickPanel.SetActive(false);
        roguelikePanel.SetActive(false);
    }

    /// <summary>
    /// �l�����I���p�l����\��
    /// </summary>
    private void ShowLethalPuzzlePanel()
    {
        modeSelectPanel.SetActive(false);
        lethalPuzzlePanel.SetActive(true);
        twoPickPanel.SetActive(false);
        roguelikePanel.SetActive(false);
    }

    /// <summary>
    /// 2Pick�I���p�l����\��
    /// </summary>
    private void ShowTwoPickPanel()
    {
        modeSelectPanel.SetActive(false);
        lethalPuzzlePanel.SetActive(false);
        twoPickPanel.SetActive(true);
        roguelikePanel.SetActive(false);
    }

    /// <summary>
    /// ���[�O���C�N�I���p�l����\��
    /// </summary>
    private void ShowRoguelikePanel()
    {
        modeSelectPanel.SetActive(false);
        lethalPuzzlePanel.SetActive(false);
        twoPickPanel.SetActive(false);
        roguelikePanel.SetActive(true);
    }

    // ========================================
    // ���[�h�I���{�^���i���C���p�l���j
    // ========================================

    /// <summary>
    /// �l�������[�h�{�^��
    /// </summary>
    public void OnClickLethalPuzzleMode()
    {
        ShowLethalPuzzlePanel();
    }

    /// <summary>
    /// 2Pick���[�h�{�^��
    /// </summary>
    public void OnClickTwoPickMode()
    {
        ShowTwoPickPanel();
    }

    /// <summary>
    /// ���[�O���C�N���[�h�{�^��
    /// </summary>
    public void OnClickRoguelikeMode()
    {
        ShowRoguelikePanel();
    }

    /// <summary>
    /// �^�C�g���ɖ߂�
    /// </summary>
    public void OnClickBackToTitle()
    {
        SceneManager.LoadScene(titleScene);
    }

    // ========================================
    // �߂�{�^���i�e�T�u�p�l���j
    // ========================================

    /// <summary>
    /// ���[�h�I���ɖ߂�
    /// </summary>
    public void OnClickBackToModeSelect()
    {
        ShowModeSelectPanel();
    }
}