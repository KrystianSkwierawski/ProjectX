using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.Areas.Shared.Enums;

namespace Assets.Scripts.Areas.Shared.Mono
{
    public class AudioManager : MonoSingleton<AudioManager>
    {
        public readonly IDictionary<AudioTypeEnum, AudioClip> AudioClips = new Dictionary<AudioTypeEnum, AudioClip>();

        private AudioSource _mainAudioSource;

        private readonly AudioTypeEnum[] _musicTypes = new AudioTypeEnum[] { AudioTypeEnum.BacgroundMusic, AudioTypeEnum.BacgroundMusic2 };

        [SerializeField] private bool _musicPlayer;

        private void Update()
        {
            if (_mainAudioSource == null || _mainAudioSource.isPlaying)
            {
                return;
            }

            if (_musicPlayer)
            {
                PlayRandomMusic();
            }
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
                var audioClip = type == AudioTypeEnum.None ? null : Resources.Load<AudioClip>($"Audios/{type}");

                if (audioClip != null)
                {
                    Debug.Log($"AudioManager -> Add. Type: {type}, Name: {audioClip.name}, Length: {audioClip.length}");

                    AudioClips.Add(type, audioClip);
                }
            }
        }

        public void TryPlayOneShot(AudioTypeEnum type, float volume = 1f)
        {
            TryPlayOneShot(_mainAudioSource, type);
        }

        public void TryPlayOneShot(AudioSource audioSource, AudioTypeEnum type, float volume = 1f)
        {
            if (AudioClips.TryGetValue(type, out var audioClip))
            {
                Debug.Log($"AudioManager -> PlayOneShot. Volume: {volume}, Name: {audioClip.name}, Length: {audioClip.length}");

                audioSource.PlayOneShot(audioClip, volume);
            }
        }
    }
}