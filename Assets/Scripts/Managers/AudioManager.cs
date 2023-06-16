using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : Singlton<AudioManager>
{
    public AudioMixer audioMixer;
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup musicGroup;

    private AudioSource musicSource;

    public int poolSize = 10;

    public AudioSource audioSourcePrefab;

    private List<AudioSource> pool = new List<AudioSource>();

    protected override void Awake()
    {
        base.Awake();


        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.outputAudioMixerGroup = musicGroup;
        musicSource.loop = true;

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource audioSource = CreateAudioSource();
            pool.Add(audioSource);
        }
    }

    public void PlayMusic(AudioClip clip, float volume = 1f)
    {
        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.Play();
    }

    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
    }

    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
    }

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
    }

    public void PauseMusic()
    {
        musicSource.Pause();
    }

    public void ResumeMusic()
    {
        musicSource.UnPause();
    }

    public void PlaySFX(AudioClip audioClip, float volume = 1f)
    {
        AudioSource audioSource = GetAvailableAudioSource();
        if (audioSource != null)
        {
            audioSource.clip = audioClip;
            audioSource.Play();
            StartCoroutine(ReturnAudioSourceToPoolAfterPlaying(audioSource));
        }
    }

    private AudioSource GetAvailableAudioSource()
    {
        foreach (AudioSource audioSource in pool)
        {
            if (!audioSource.isPlaying)
            {
                return audioSource;
            }
        }
        return null;
    }

    private IEnumerator ReturnAudioSourceToPoolAfterPlaying(AudioSource audioSource)
    {
        while (audioSource.isPlaying)
        {
            yield return null;
        }
        audioSource.clip = null;
        audioSource.Stop();
    }

    private AudioSource CreateAudioSource()
    {
        AudioSource audioSource = Instantiate(audioSourcePrefab, transform);
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.outputAudioMixerGroup = sfxGroup;
        audioSource.spatialBlend = 0f;
        return audioSource;
    }
}
