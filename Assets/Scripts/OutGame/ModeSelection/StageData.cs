using System.Collections.Generic;
using UnityEngine;

// ========================================
// �X�e�[�W�^�C�v
// ========================================
public enum StageType
{
    NORMAL_BATTLE,  // �ʏ�퓬
    ELITE_BATTLE,   // �G���[�g�퓬
    BOSS_BATTLE,    // �{�X�퓬
    TREASURE,       // �󔠁i��V�̂݁j
    SHOP,           // �V���b�v
    EVENT,          // �C�x���g
    REST,           // �x�e�iHP�񕜁j
    DAMAGE,         // �_���[�W
    CARD_LOSS       // �J�[�h���X�g
}

// ========================================
// �p�����[�^�ύX�^�C�v
// ========================================
public enum ParameterModifierType
{
    None,
    HP_BOOST,           // �ő�HP����
    HP_REDUCTION,       // �ő�HP����
    MANA_BOOST,         // �ő�}�i����
    MANA_REDUCTION,     // �ő�}�i����
    ATTACK_BOOST,       // �U���͑���
    ATTACK_REDUCTION,   // �U���͌���
    DEFENSE_BOOST,      // �h��͑���
    DEFENSE_REDUCTION,  // �h��͌���
    CARD_DRAW_BOOST,    // �h���[��������
    CARD_DRAW_REDUCTION // �h���[��������
}

// ========================================
// �p�����[�^�ύX�ݒ�
// ========================================
[System.Serializable]
public class ParameterModifier
{
    [Header("�ύX�^�C�v")]
    public ParameterModifierType modifierType;

    [Header("�ύX�l")]
    public int value; // �����l
    public bool isPercentage; // �p�[�Z���g�w�肩�ifalse�Ȃ�Œ�l�j

    [Header("�K�p�Ώ�")]
    public bool applyToPlayer = true; // �v���C���[�ɓK�p
    public bool applyToEnemy = false;  // �G�ɓK�p

    [Header("����")]
    [TextArea(1, 2)]
    public string description; // UI�\���p
}

// ========================================
// �X�e�[�W�f�[�^ ScriptableObject
// ========================================
[CreateAssetMenu(fileName = "StageData", menuName = "Roguelike/StageData")]
public class StageData : ScriptableObject
{
    [Header("=== �X�e�[�W��{��� ===")]
    public string stageName = "Stage 1";
    public int stageNumber = 1;
    public StageType stageType = StageType.NORMAL_BATTLE;

    [TextArea(2, 4)]
    public string stageDescription;

    public Sprite stageIcon; // �X�e�[�W�A�C�R��
    public Sprite stageBackground; // �w�i�摜

    [Header("=== �G�ݒ� ===")]
    [Tooltip("�G�̖��O")]
    public string enemyName = "Goblin";

    [Tooltip("�G��HP")]
    public int enemyHP = 20;

    [Tooltip("�G�̏����}�i")]
    public int enemyInitialMana = 1;

    [Tooltip("�G���g�p����f�b�L�i�J�[�hID�̃��X�g�j")]
    public List<int> enemyDeck = new List<int>();

    [Header("=== �p�����[�^�ύX�M�~�b�N ===")]
    [Tooltip("���̃X�e�[�W�œK�p�����p�����[�^�ύX")]
    public List<ParameterModifier> parameterModifiers = new List<ParameterModifier>();

    [Header("=== ��V�ݒ� ===")]
    [Tooltip("�N���A���̃S�[���h��V")]
    public int goldReward = 50;

    [Tooltip("�N���A���ɑI�ׂ�J�[�h��")]
    public int cardRewardCount = 3;

    [Tooltip("��V�J�[�h�v�[���i��̏ꍇ�͋��ʃv�[������j")]
    public List<int> rewardCardPool = new List<int>();

    [Header("=== ����ݒ� ===")]
    [Tooltip("�x�e�n�_�̏ꍇ�̉񕜗�")]
    public int healAmount = 10;

    [Tooltip("�V���b�v�̏ꍇ�̔̔��J�[�h��")]
    public int shopCardCount = 5;

    [Tooltip("�V���b�v�̏ꍇ�̔̔��J�[�h���X�g")]
    public List<int> shopCardList = new List<int>();

    [Tooltip("�󔠂̏ꍇ�̃J�[�h�l����")]
    public int treasureCardCount = 1;
}

/*
 * �X�e�[�W���
 * �}�b�v���
 * �X�e�[�W���
 * �G���
 * �o�t�f�o�t
 * ��V
 * 
 * 
*/