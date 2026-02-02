using UnityEngine;

public class SoundEffectManager : MonoBehaviour
{
    public static SoundEffectManager Instance => _instance;
    private static SoundEffectManager _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(_instance);
        }

        _instance = this;
    }

    /// <summary>
    /// Insatantiates a audio source to play the sound and to be destroyed later
    /// </summary>
    public void PlaySoundEffect(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        var instance = new GameObject().AddComponent<AudioSource>();
        if (instance == null) return;

        instance.clip = clip;
        instance.volume = volume;
        instance.pitch = pitch;
        instance.Play();

        Destroy(instance.gameObject, clip.length);
    }
}
