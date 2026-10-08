using UnityEngine;
using System.Collections.Generic;

public class BattleAudioManager : MonoBehaviour
{
    public static BattleAudioManager Instance { get; private set; }

    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;

    [Header("Battle Clips")]
    [SerializeField] private List<AudioClip> bgmClips;
    [SerializeField] private List<AudioClip> seClips;

    private Dictionary<string, AudioClip> bgmDict = new();
    private Dictionary<string, AudioClip> seDict = new();

    private void Awake()
    {
        Instance = this;

        foreach (var clip in bgmClips)
            bgmDict[clip.name] = clip;

        foreach (var clip in seClips)
            seDict[clip.name] = clip;
    }

    public void PlayBGM(string name, bool loop = true)
    {
        if (!bgmDict.ContainsKey(name)) return;
        bgmSource.clip = bgmDict[name];
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void PlaySE(string name)
    {
        if (!seDict.ContainsKey(name)) return;
        seSource.PlayOneShot(seDict[name]);
    }

    public void StopBGM() => bgmSource.Stop();
}
