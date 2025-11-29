using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private SoundSO SO;
    public AudioSource audioSource;

    private Dictionary<SoundType, SoundList> _lookup;

    public static SoundManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        _lookup = new Dictionary<SoundType, SoundList>();
        foreach (var entry in SO.sounds)
        {
            _lookup[entry.type] = entry;
        }
    }

    public static void PlaySound(SoundType sound, AudioSource source = null, float volume = 1)
    {
        SoundList soundList = Instance.SO.sounds[(int)sound];
        AudioClip[] clips = soundList.sounds;

        if (clips.Length <= 0)
        {
            return;
        }

        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];

        if (source)
        {
            source.outputAudioMixerGroup = soundList.mixer;
            source.clip = randomClip;
            source.volume = volume * soundList.volume;
            source.Play();
        }
        else
        {
            Instance.audioSource.outputAudioMixerGroup = soundList.mixer;
            Instance.audioSource.PlayOneShot(randomClip, volume * soundList.volume);
        }
    }
}

[Serializable]
public struct SoundList
{
    public SoundType type;
    [Range(0, 1)] public float volume;
    public AudioMixerGroup mixer;
    public AudioClip[] sounds;
}