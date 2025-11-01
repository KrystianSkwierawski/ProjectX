using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Enums;
using Assets.Scripts.Shared;
using UnityEngine;

namespace Assets.Scripts.Mono
{
    public class AudioManager : MonoSingleton<AudioManager>
    {
        private AudioSource _mainAudioSource;

        public readonly IDictionary<AudioTypeEnum, AudioClip> AudioClips = new Dictionary<AudioTypeEnum, AudioClip>();

        private readonly AudioTypeEnum[] _musicTypes = new AudioTypeEnum[] { AudioTypeEnum.BacgroundMusic, AudioTypeEnum.BacgroundMusic2 };

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
            var randomType = _musicTypes[UnityEngine.Random.Range(0, _musicTypes.Length)];

            var audioClip = AudioClips[randomType];

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
                    Debug.Log($"AudioManager -> Add. Type: {type}, Name: {audioClip.name}, Length: {audioClip.length}");

                    AudioClips.Add(type, audioClip);
                }
            }
        }

        public void PlayOneShot(AudioTypeEnum type, float volume = 1f)
        {
            PlayOneShot(_mainAudioSource, type);
        }

        public void PlayOneShot(AudioSource audioSource, AudioTypeEnum type, float volume = 1f)
        {
            if (AudioClips.TryGetValue(type, out var audioClip))
            {
                Debug.Log($"AudioManager -> PlayOneShot. Type: {type}, Volume: {volume}, Name: {audioClip.name}");

                audioSource.PlayOneShot(audioClip, volume);
            }
        }
    }
}