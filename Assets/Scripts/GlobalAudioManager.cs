using UnityEngine;
using System.Collections.Generic;

public class GlobalAudioManager : MonoBehaviour
{
    public static GlobalAudioManager Instance { get; private set; }

    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;

    [Header("Global Clips")]
    [SerializeField] private List<AudioClip> bgmClips;
    [SerializeField] private List<AudioClip> seClips;

    private Dictionary<string, AudioClip> bgmDict = new();
    private Dictionary<string, AudioClip> seDict = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

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
