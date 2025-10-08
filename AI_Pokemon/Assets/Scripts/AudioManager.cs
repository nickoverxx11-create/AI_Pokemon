// In a file named AudioManager.cs
using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    // --- MODIFIED: We now use two AudioSources for crossfading ---
    public AudioSource _musicSource1;
    public AudioSource _musicSource2;
    private bool _isSource1Active; // Tracks which source is currently playing

    [Header("Music Tracks")]
    public AudioClip meadowMusic;
    public AudioClip coastMusic;
    public AudioClip woodsMusic;
    public AudioClip desertMusic;
    public AudioClip summitMusic;

    private bool _isPaused = false;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Create and configure both AudioSources
        _musicSource1.loop = true;
        _musicSource2.loop = true;
        
        _isSource1Active = true; // Start with source 1 as the active one
    }

    public void PlayMusicForZone(GameZone zone, float fadeDuration = 1.0f)
    {
        AudioClip clipToPlay = null;
        switch (zone)
        {
            case GameZone.Zone1: clipToPlay = meadowMusic; break;
            case GameZone.Zone2: clipToPlay = coastMusic; break;
            case GameZone.Zone3: clipToPlay = woodsMusic; break;
            case GameZone.Zone4: clipToPlay = desertMusic; break;
            case GameZone.Zone5: clipToPlay = summitMusic; break;
        }

        // Get the currently active source
        var activeSource = _isSource1Active ? _musicSource1 : _musicSource2;

        // Only switch music if the new clip is different from the currently playing one
        if (clipToPlay != null && clipToPlay != activeSource.clip)
        {
            // Stop any previous fading coroutines
            StopAllCoroutines();
            StartCoroutine(CrossfadeMusic(clipToPlay, fadeDuration));
        }
    }

    // --- REWRITTEN: The new crossfading logic ---
    private IEnumerator CrossfadeMusic(AudioClip newClip, float duration)
    {
        // Determine which source is active and which is inactive
        AudioSource activeSource = _isSource1Active ? _musicSource1 : _musicSource2;
        AudioSource inactiveSource = _isSource1Active ? _musicSource2 : _musicSource1;

        // 1. Set up the new (inactive) source
        inactiveSource.clip = newClip;
        inactiveSource.volume = 0;
        inactiveSource.Play(); // Start playing silently

        // 2. Crossfade over the specified duration
        float timer = 0f;
        float startVolume = activeSource.volume; // Usually 1.0

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            
            // Fade out the old source
            activeSource.volume = Mathf.Lerp(startVolume, 0f, progress);
            // Fade in the new source
            inactiveSource.volume = Mathf.Lerp(0f, startVolume, progress);

            yield return null;
        }

        // 3. Clean up
        activeSource.Stop();
        activeSource.clip = null; // Clear the clip from the old source
        inactiveSource.volume = startVolume; // Ensure the new source is at full volume

        // 4. Swap the active source for the next transition
        _isSource1Active = !_isSource1Active;
    }
    
    public void SetMusicPaused(bool pause)
    {
        if (_isPaused == pause) return;
        _isPaused = pause;
        
        if (_musicSource1 != null)
        {
            if (pause) { if (_musicSource1.isPlaying) _musicSource1.Pause(); }
            else
            {
                if (_musicSource1.clip != null) _musicSource1.UnPause();
            }
        }

        if (_musicSource2 != null)
        {
            if (pause) { if (_musicSource2.isPlaying) _musicSource2.Pause(); }
            else
            {
                if (_musicSource2.clip != null) _musicSource2.UnPause();
            }
        }
    }
}