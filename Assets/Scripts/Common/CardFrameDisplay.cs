using UnityEngine;

/// <summary>
/// カードのフレーム（枠）をレアリティで切り替えるためのヘルパー。
/// カードのオブジェクトにはN/R/SRのフレームをあらかじめ並べておき、
/// レアリティに対応するものだけを活性にする。
/// インゲーム（CardView）とデッキ編成画面（OutGameCardList）で共通して使う。
/// </summary>
public static class CardFrameDisplay
{
    /// <summary>
    /// レアリティに対応するフレームだけを表示し、それ以外は非表示にする。
    /// フレームはN/R/SRの三段階なので、URはSRのフレームを流用する。
    /// フレームを持たないプレハブもあるのでnullチェックする。
    /// </summary>
    public static void Apply(RARE rare, GameObject frameN, GameObject frameR, GameObject frameSR)
    {
        SetActive(frameN, rare == RARE.N);
        SetActive(frameR, rare == RARE.R);
        SetActive(frameSR, rare == RARE.SR || rare == RARE.UR);
    }

    static void SetActive(GameObject frame, bool isActive)
    {
        if (frame == null) return;
        if (frame.activeSelf != isActive) frame.SetActive(isActive);
    }
}
