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

    public GameObject officeWorkerPrefab;

    public string officeDataFileName = "officeData.dat";
    private List<OfficeState> officeStates = new List<OfficeState>();

    void Start()
    {
#if !UNITY_EDITOR
        Debug.unityLogger.logEnabled = false; //Disable any debug log for better performance
#endif
        AudioManager.Instance.PlayMusic(backGroundMusic);

        LoadOffices();
    }

    private void LoadOffices()
    {
        BinaryFormatter formatter = new BinaryFormatter();
        try
        {
            FileStream stream = new FileStream(Application.persistentDataPath + "/" + officeDataFileName, FileMode.Open);
            officeStates = (List<OfficeState>)formatter.Deserialize(stream);
            stream.Close();

            foreach (OfficeState state in officeStates)
            {
                Instantiate(officeWorkerPrefab, state.position.ToVector3(), state.rotation.ToQuaternion());
            }
        }
        catch (SerializationException e)
        {
            Debug.LogError("Failed to deserialize office data: " + e.Message);
            officeStates.Clear();
        }
        catch (FileNotFoundException)
        {
            Debug.Log("No saved office data found.");
        }
    }

    
    public void CreateOffice(Vector3 officePosition)
    {
        Vector3 newPosition = new Vector3(officePosition.x, 1f, officePosition.z);

        GameObject newOffice = Instantiate(officeWorkerPrefab, newPosition, Quaternion.identity);

        // create a scale effect using DOTween
        newOffice.transform.localScale = Vector3.zero;
        newOffice.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutElastic);

        // save the state of the new office object
        officeStates.Add(new OfficeState
        {
            position = new SerializableVector3(newPosition),
            rotation = new SerializableQuaternion(Quaternion.identity)
        });
        SaveOffices();
    }

    private void SaveOffices()
    {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(Application.persistentDataPath + "/" + officeDataFileName, FileMode.Create);
        formatter.Serialize(stream, officeStates);
        stream.Close();
    }
}

[Serializable]
public class OfficeState
{
    public SerializableVector3 position;
    public SerializableQuaternion rotation;
}

[Serializable]
public class SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[Serializable]
public class SerializableQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;

    public SerializableQuaternion(Quaternion quaternion)
    {
        x = quaternion.x;
        y = quaternion.y;
        z = quaternion.z;
        w = quaternion.w;
    }

    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }
}
