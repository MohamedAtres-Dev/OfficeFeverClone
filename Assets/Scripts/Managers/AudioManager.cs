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

    public bool GetSFXState()
    {
        return PlayerPrefs.GetInt("SFXVolume", 1) == 0 ? false : true;
    }

    public bool GetMusicState()
    {
        return PlayerPrefs.GetInt("MusicVolume", 1) == 0 ? false : true;
    }

    protected override void Awake()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.outputAudioMixerGroup = musicGroup;
        musicSource.loop = true;

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource audioSource = CreateAudioSource();
            pool.Add(audioSource);
        }



    }

    private void Start()
    {
        SetSFXVolume(GetSFXState() == true ? 1 : 0);
        SetMusicVolume(GetMusicState() == true ? 1 : 0);
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
        Debug.Log("Set Sound " + Mathf.Log10(volume) * 20);

        PlayerPrefs.SetInt("SFXVolume", volume == 1 ? 1 : 0);
    }

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
        Debug.Log("Set Music " + Mathf.Log10(volume) * 20);
        PlayerPrefs.SetInt("MusicVolume", volume == 1 ? 1 : 0);
    }

    public void PauseMusic()
    {
        musicSource.Pause();
    }

    public void ResumeMusic()
    {
        musicSource.UnPause();
    }

    public void PlaySFX(AudioClip audioClip, float volume = 1f, float pitch = 1f)
    {
        AudioSource audioSource = GetAvailableAudioSource();
        if (audioSource != null)
        {
            audioSource.clip = audioClip;
            audioSource.pitch = pitch; // pooled sources are reused, so the pitch is always set explicitly
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
