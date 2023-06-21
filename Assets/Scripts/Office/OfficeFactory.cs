using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class OfficeFactory : Singlton<OfficeFactory>
{
    public OfficeSettings officeSettings;

    public Office officePrefab;
    public Transform officesParent;
    public Transform[] spawnPoints;

    public string officeDataFileName = "office.dat";
    private Offices offices = new Offices();
    private List<Office> allOfficesPrefabs = new List<Office>();
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
            offices = (Offices)formatter.Deserialize(stream);
            stream.Close();

            foreach (var office in offices.allOffices)
            {
                // Generate office at saved position
                GenerateOffice(office.Key, office.Value);
            }
        }
        catch (SerializationException e)
        {
            Debug.LogError("Failed to deserialize office data: " + e.Message);
            offices.allOffices.Clear();
        }
        catch (FileNotFoundException)
        {
            Debug.Log("No saved office data found.");
            //Generate Default One
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                var officeState = new OfficeState();
                offices.allOffices.Add(i, officeState);
                if(i == 0)
                {
                    officeState.isGenerated = true;
                }
                officeState.officePrice = officeSettings.GetRandomPrice();
                GenerateOffice(i, officeState);
            }

        }
    }


    /// <summary>
    /// I will Generate the offices at the begining of the game when loading the game.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="isGenereted"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="generatedMoney"></param>
    /// <param name="numOfWaitingPaper"></param>
    /// <param name="officePrice"></param>
    private void GenerateOffice(int index, OfficeState officeState)
    {
        Office newOffice = Instantiate(officePrefab, spawnPoints[index].position, spawnPoints[index].rotation, officesParent);
        officeState.officeId = index;

        // Show or hide UI
        if (officeState.isGenerated)
        {
            // TODO: Hide UI for existing office
            newOffice.UIObject.SetActive(false);
            newOffice.officeWorker.gameObject.SetActive(true);
            newOffice.GenerateMoney(officeState.generatedMoney);
            newOffice.GeneratePaper(officeState.numOfWaitingPaper);
        }
        else
        {
            // TODO: Show UI for new office
            newOffice.UIObject.SetActive(true);
            newOffice.officeWorker.gameObject.SetActive(false);
            newOffice.SetOfficePrice(officeState.officePrice , officeState.moneyPaid);
            offices.allOffices[index].officePrice = officeState.officePrice;
        }
        newOffice.SetOfficeState(officeState);
        allOfficesPrefabs.Add(newOffice);
    }

    /// <summary>
    /// To Create new office this will be called from outside
    /// </summary>
    /// <param name="spawnPos"></param>
    public void CreateOffice(Office office)
    {
        office.UIObject.SetActive(false);
        office.officeWorker.gameObject.SetActive(true);

        if (offices.allOffices.ContainsKey(office.GetOfficeId()))
            offices.allOffices[office.GetOfficeId()].isGenerated = true;
        else
        {
            OfficeState newOffice = new OfficeState()
            {
                officeId = offices.allOffices.Count,
                isGenerated = true,
                position = new SerializableVector3(office.transform.position),
                rotation = new SerializableQuaternion(office.transform.rotation)
            };

            offices.allOffices.Add(offices.allOffices.Count, newOffice);
        }    
        SaveOffices();
    }


    private void SaveOffices()
    {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(Application.persistentDataPath + "/" + officeDataFileName, FileMode.Create);
        formatter.Serialize(stream, offices);
        stream.Close();
    }

    private void OnDisable()
    {
        foreach (var item in allOfficesPrefabs)
        {
            offices.allOffices[item.GetOfficeId()].moneyPaid = item.GetMoneyPaid();
            offices.allOffices[item.GetOfficeId()].generatedMoney = item.GetMoneyGeneratedCount();
            offices.allOffices[item.GetOfficeId()].numOfWaitingPaper = item.GetPaperCount();
        }
        SaveOffices();
    }
}

[Serializable]
public class Offices
{
    public GenericDictionary<int, OfficeState> allOffices = new GenericDictionary<int, OfficeState>();
}

[Serializable]
public class OfficeState
{
    public int officeId;
    public bool isGenerated;
    public int officePrice;
    public int generatedMoney;
    public int numOfWaitingPaper;
    public bool isWorking;
    public int moneyPaid;
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