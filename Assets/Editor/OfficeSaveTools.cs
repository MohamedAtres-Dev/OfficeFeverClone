using System.IO;
using UnityEditor;
using UnityEngine;

public static class OfficeSaveTools
{
    // matches OfficeFactory.officeDataFileName's default
    private const string OfficeDataFileName = "office.dat";

    [MenuItem("Tools/Office/Reset Workstations")]
    private static void ResetWorkstations()
    {
        if (EditorApplication.isPlaying)
        {
            // OfficeFactory.OnDisable re-saves on exit, which would recreate the file
            Debug.LogWarning("Stop Play mode first, then reset the workstations.");
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, OfficeDataFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Workstations reset (deleted " + path + "). Coins and upgrades are untouched.");
        }
        else
        {
            Debug.Log("No saved workstations found at " + path);
        }
    }
}
