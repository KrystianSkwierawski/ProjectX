using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public AudioClip[] MusicList;
    public float Volume = 0.05f;

    private AudioSource _audioSource;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (_audioSource.isPlaying)
        {
            return;
        }

        //PlayRandomClip();
    }

    private void PlayRandomClip()
    {
        var clip = MusicList[Random.Range(0, MusicList.Length)];
        _audioSource.PlayOneShot(clip, Volume);
    }
}
