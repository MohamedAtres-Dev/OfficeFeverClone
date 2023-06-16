using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class OfficeFactory : Singlton<OfficeFactory>
{
    public GameObject officePrefab;
    public Transform officesParent;
    public Transform[] spawnPoints;

    public string officeDataFileName = "office.dat";
    private List<OfficeState> officeStates = new List<OfficeState>();

    private void Start()
    {
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

            for (int i = 0; i < officeStates.Count; i++)
            {
                if (officeStates[i].isGenerated)
                {
                    // Generate office at saved position
                    GenerateOffice(i, false, officeStates[i].position.ToVector3(), officeStates[i].rotation.ToQuaternion());
                }
                else
                {
                    // Show UI and generate new office at random spawn point
                    GenerateOffice(i, true);
                }
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
            GenerateDefaultOffice();
            //Generate Default One
            for (int i = 1; i < spawnPoints.Length; i++)
            {
                officeStates.Add(new OfficeState());
                GenerateOffice(i, true);
            }
            
        }
    }


    private void GenerateDefaultOffice()
    {
        CreateOffice(spawnPoints[0]);

    }

    private void GenerateOffice(int index, bool showUi, Vector3? position = null, Quaternion? rotation = null)
    {
        GameObject newOffice = Instantiate(officePrefab, spawnPoints[index].position, spawnPoints[index].rotation, officesParent);

        // Set office position and rotation
        if (position.HasValue)
        {
            newOffice.transform.position = position.Value;
        }
        if (rotation.HasValue)
        {
            newOffice.transform.rotation = rotation.Value;
        }

        // Show or hide UI
        if (showUi)
        {
            // TODO: Show UI for new office
            newOffice.GetComponent<Office>().UIObject.SetActive(true);
            newOffice.GetComponent<Office>().officeWorker.SetActive(false);
        }
        else
        {
            // TODO: Hide UI for existing office
            newOffice.GetComponent<Office>().UIObject.SetActive(false);
            newOffice.GetComponent<Office>().officeWorker.SetActive(true);
        }

        if (officeStates.Count > 0)
        {
            // Set office state
            officeStates[index].isGenerated = !showUi;
            officeStates[index].position = new SerializableVector3(newOffice.transform.position);
            officeStates[index].rotation = new SerializableQuaternion(newOffice.transform.rotation);
        }

    }

    public void CreateOffice(Transform spawnPos)
    {
        GameObject newOffice = Instantiate(officePrefab, spawnPos.position, spawnPos.rotation, officesParent);
        newOffice.GetComponent<Office>().UIObject.SetActive(false);
        newOffice.GetComponent<Office>().officeWorker.SetActive(true);
        // Set office state
        officeStates.Add(new OfficeState
        {
            isGenerated = true,
            position = new SerializableVector3(newOffice.transform.position),
            rotation = new SerializableQuaternion(newOffice.transform.rotation)
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


    private void OnDisable()
    {
        SaveOffices();
    }
}


[Serializable]
public class OfficeState
{
    public bool isGenerated;
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