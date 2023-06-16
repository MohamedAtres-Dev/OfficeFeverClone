using DG.Tweening;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class GameManager : Singlton<GameManager>
{
    public AudioClip backGroundMusic;

    void Start()
    {
#if !UNITY_EDITOR
        Debug.unityLogger.logEnabled = false; //Disable any debug log for better performance
#endif
        AudioManager.Instance.PlayMusic(backGroundMusic);


    }


}


