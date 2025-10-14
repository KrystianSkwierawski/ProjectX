using UnityEngine;
using VContainer.Unity;

public class MusicPlayerController : MonoBehaviour, ITickable 
{
    public AudioClip[] MusicList;
    public float Volume = 0.05f;

    [SerializeField] private AudioSource _audioSource;

    public void Tick()
    {
#if UNITY_EDITOR
        if (_audioSource.isPlaying)
        {
            return;
        }

        PlayRandomClip();
#endif
    }

    private void PlayRandomClip()
    {
        var clip = MusicList[Random.Range(0, MusicList.Length)];
        _audioSource.PlayOneShot(clip, Volume);
    }
}
