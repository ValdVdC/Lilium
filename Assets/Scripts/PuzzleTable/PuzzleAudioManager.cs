using UnityEngine;

public class PuzzleAudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    
    [Header("Sound Effects")]
    public AudioClip piecePickupSound;
    public AudioClip piecePlaceSound;
    public AudioClip puzzleShuffleSound;
    public AudioClip pieceMovementSound;
    public AudioClip puzzleSolvedSound;
    public AudioClip keyAppearSound;
    public AudioClip keyCollectSound;
    public AudioClip barrelOpenSound;
    public AudioClip barrelCloseSound;
    
    [Header("Music")]
    public AudioClip puzzleMusic;
    public AudioClip solvedMusic;
    public float musicFadeTime = 1.0f;
    
    private float originalMusicVolume;
    
    void Start()
    {
        if (musicSource != null)
            originalMusicVolume = musicSource.volume;
    }
    
    public void PlayBarrelOpen()
    {
        PlaySFX(barrelOpenSound);
    }
    
    public void PlayBarrelClose()
    {
        PlaySFX(barrelCloseSound);
    }
    
    public void PlayPiecePickup()
    {
        PlaySFX(piecePickupSound);
    }
    
    public void PlayPiecePlace()
    {
        PlaySFX(piecePlaceSound);
    }
    
    public void PlayPuzzleShuffle()
    {
        PlaySFX(puzzleShuffleSound);
    }
    
    public void PlayPieceMovement()
    {
        PlaySFX(pieceMovementSound);
    }
    
    public void PlayPuzzleSolved()
    {
        PlaySFX(puzzleSolvedSound);
    }
    
    public void PlayKeyAppear()
    {
        PlaySFX(keyAppearSound);
    }
    
    public void PlayKeyCollect()
    {
        PlaySFX(keyCollectSound);
    }
    
    public void PlayPuzzleMusic()
    {
        if (musicSource != null && puzzleMusic != null)
        {
            musicSource.clip = puzzleMusic;
            musicSource.volume = originalMusicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }
    }
    
    public void PlaySolvedMusic()
    {
        if (musicSource != null && solvedMusic != null)
        {
            musicSource.clip = solvedMusic;
            musicSource.volume = originalMusicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }
    }
    
    public void FadeOutMusic()
    {
        if (musicSource != null)
            StartCoroutine(FadeMusicCoroutine(0f, musicFadeTime));
    }
    
    public void FadeInMusic()
    {
        if (musicSource != null)
            StartCoroutine(FadeMusicCoroutine(originalMusicVolume, musicFadeTime));
    }
    
    private System.Collections.IEnumerator FadeMusicCoroutine(float targetVolume, float duration)
    {
        if (musicSource == null)
            yield break;
            
        float startVolume = musicSource.volume;
        float startTime = Time.time;
        
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }
        
        musicSource.volume = targetVolume;
    }
    
    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}