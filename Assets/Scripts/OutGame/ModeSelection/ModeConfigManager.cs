using System.Collections.Generic;
using UnityEngine;

public class ModeConfigManager : MonoBehaviour
{
    public static ModeConfigManager Instance { get; private set; }
    public int LethalPuzzleIndex { get => lethalPuzzleIndex; set => lethalPuzzleIndex = value; }

    [Header("���݂̃Q�[�����[�h")]
    public GameMode currentGameMode = GameMode.NONE;

    [Header("�l�����f�[�^���X�g")]
    public List<LethalPuzzleData> lethalPuzzleList = new List<LethalPuzzleData>();

    [Header("2Pick�f�[�^���X�g")]
    public List<TwoPickData> twoPickList = new List<TwoPickData>();

    [Header("���[�O���C�N�f�[�^���X�g")]
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
    /// �w��ID�̋l�����f�[�^���擾
    /// </summary>
    public LethalPuzzleData GetLethalPuzzleData(int id)
    {
        return lethalPuzzleList.Find(x => x.puzzleId == id);
    }

    /// <summary>
    /// �w��C���f�b�N�X��2Pick�f�[�^���擾
    /// </summary>
    public TwoPickData GetTwoPickData(int index)
    {
        if (index >= 0 && index < twoPickList.Count)
            return twoPickList[index];
        return null;
    }

    /// <summary>
    /// �w��C���f�b�N�X�̃��[�O���C�N�f�[�^���擾
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
    NONE,           // ���I��
    CPU_BATTLE,     // CPU��
    LETHAL_PUZZLE,  // �l����
    TWO_PICK,       // 2Pick
    ROGUELIKE       // ���[�O���C�N
}

/*
 * �����ꂽ��ɂ��邱��
 * �����ꂽ���̂��烂�[�h�̍��ڂ��擾����GameManager�ɓn��
 * ���X�g�擾�N���X
 * 
 * 
 * 
*/