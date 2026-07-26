using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Plug your Music Audio Source here")]
    public AudioSource levelMusic;

    private void Start()
    {
        // As soon as this specific level loads, play the music
        if (levelMusic != null)
        {
            levelMusic.Play();
        }
    }

    // Optional: Exposed methods just in case you want to hook up 
    // a "Mute" or "Stop" button via UnityEvents later!
    public void StopMusic() { if (levelMusic != null) levelMusic.Stop(); }
    public void PauseMusic() { if (levelMusic != null) levelMusic.Pause(); }
    public void ResumeMusic() { if (levelMusic != null) levelMusic.UnPause(); }
}