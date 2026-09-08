using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LethalPuzzleStageItem : MonoBehaviour
{
    [Header("テキスト表示")]
    [SerializeField] private Text puzzleNameText;

    [Header("ボタン")]
    [SerializeField] private Button selectButton;

    private int puzzleIndex;
    private System.Action<int> onClicked;

    public void Setup(int index, LethalPuzzleData data, System.Action<int> onClickedCallback)
    {
        puzzleIndex = index;
        onClicked = onClickedCallback;

        puzzleNameText.text = $"Puzzle {index + 1:D2}";

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => onClicked?.Invoke(puzzleIndex));
    }
}
