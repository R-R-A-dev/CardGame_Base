using UnityEngine;

/// <summary>
/// 暫定の全体ミュート。設定画面（AudioMixerによるBGM/SE個別制御）が入るまでのつなぎ。
///
/// AudioListenerコンポーネントを非アクティブにする方式は採らない。
/// このプロジェクトではAudioListenerがField/Game両シーンのMain Cameraに付いており、
/// 切るとシーンにリスナーが0個になって警告が出続けるため、
/// マスター音量（AudioListener.volume）を0にしている。
///
/// シーンには何も追加していないので、不要になったらこのファイルごと削除してよい。
/// ただし削除前に MUTE_ON_STARTUP を false にして一度再生すること（下のコメント参照）。
/// </summary>
public static class TemporaryMute
{
    // 音を戻したいときはここを false にする。
    // ファイルを消すだけでは戻らない場合がある（AudioListener.volumeはエディタの
    // 再生セッションをまたいで保持されることがあるため）。必ず一度falseで再生して復帰させること
    private const bool MUTE_ON_STARTUP = true;

    private const float MUTED_VOLUME = 0f;
    private const float DEFAULT_VOLUME = 1f;

    public static bool IsMuted => Mathf.Approximately(AudioListener.volume, MUTED_VOLUME);

    // シーンに配置しなくても、どちらのシーンから再生しても必ず走らせるためのフック
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyOnStartup()
    {
        SetMute(MUTE_ON_STARTUP);
    }

    /// <summary>
    /// マスター音量を切り替える。AudioSourceの再生自体は止まらず、出力だけが無音になる
    /// （BGMの再生位置は進み続ける）。完全に止めたい場合は AudioListener.pause を使うこと
    /// </summary>
    public static void SetMute(bool mute)
    {
        AudioListener.volume = mute ? MUTED_VOLUME : DEFAULT_VOLUME;
        Debug.Log($"[TemporaryMute] マスター音量を {AudioListener.volume} に設定しました");
    }

    public static void Toggle() => SetMute(!IsMuted);
}
