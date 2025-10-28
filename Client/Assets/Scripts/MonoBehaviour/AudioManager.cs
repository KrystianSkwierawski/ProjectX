using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioClip[] _musicList;

    private AudioSource _mainAudioSource;

    private IDictionary<AudioTypeEnum, AudioClip> _audioClips = new Dictionary<AudioTypeEnum, AudioClip>();

    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (_mainAudioSource == null || _mainAudioSource.isPlaying)
        {
            return;
        }

        PlayRandomMusic();
    }

    private void PlayRandomMusic()
    {
        var audioClip = _musicList[UnityEngine.Random.Range(0, _musicList.Length)];
        _mainAudioSource.PlayOneShot(audioClip, 0.05f);
    }

    public void Init(AudioSource audioSource)
    {
        Debug.Log("AudioManager -> Init");

        _mainAudioSource = audioSource;

        foreach (var type in Enum.GetValues(typeof(AudioTypeEnum)).Cast<AudioTypeEnum>())
        {
            var audioClip = Resources.Load<AudioClip>($"Audio/{type}");

            if (audioClip != null)
            {
                Debug.Log($"AudioManager -> Add. Type: {type}, Name: {audioClip.name}");

                _audioClips.Add(type, audioClip);
            }
        }
    }

    public void PlayOneShot(AudioTypeEnum type, float volume = 1f)
    {
        PlayOneShot(_mainAudioSource, type);
    }

    public void PlayOneShot(AudioSource audioSource, AudioTypeEnum type, float volume = 1f)
    {
        if (_audioClips.TryGetValue(type, out var audioClip))
        {
            Debug.Log($"AudioManager -> PlayOneShot. Type: {type}, Volume: {volume}, Name: {audioClip.name}");

            audioSource.PlayOneShot(audioClip, volume);
        }
    }
}

