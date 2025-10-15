using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip[] _musicList;
    [SerializeField] private float _volume = 0.05f;

    private AudioSource _audioSource;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (_audioSource.isPlaying)
        {
            return;
        }

        PlayRandomClip();
    }

    private void PlayRandomClip()
    {
        var clip = _musicList[Random.Range(0, _musicList.Length)];
        _audioSource.PlayOneShot(clip, _volume);
    }
}
