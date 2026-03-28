using UnityEngine;

public class DeathSceneAudio : MonoBehaviour
{
    public AudioClip deathClip;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = deathClip;
        audioSource.loop = false;
        audioSource.Play();
    }
}
