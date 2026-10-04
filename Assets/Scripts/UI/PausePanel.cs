using Lean.Gui;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PausePanel : MonoBehaviour
{

    [SerializeField] LeanToggle joysticToggle;
    [SerializeField] LeanToggle musicToggle;
    [SerializeField] LeanToggle soundToggle;

    private void Start()
    {
        //Setup toggles on save states for each variable
        SetupToggles();
    }

    private void SetupToggles()
    {
        joysticToggle.Set(HUDManager.Instance.GetJoysticState());
        musicToggle.Set(AudioManager.Instance.GetMusicState());
        soundToggle.Set(AudioManager.Instance.GetSFXState());
    }

    public void OnSoundToggle(bool state)
    {
        if (state)
        {
            AudioManager.Instance.SetSFXVolume(1);
        }
        else
        {
            AudioManager.Instance.SetSFXVolume(0);
        }
    }


    public void OnMusicToggle(bool state)
    {
        if (state)
        {
            AudioManager.Instance.SetMusicVolume(1);
        }
        else
        {
            AudioManager.Instance.SetMusicVolume(0);
        }
    }


    public void OnJoyStickToggle(bool state)
    {
        HUDManager.Instance.ActivateJoystic(state);
    }
}
