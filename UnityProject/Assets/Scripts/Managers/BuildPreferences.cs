using UnityEngine;
using System.IO;

[ExecuteInEditMode]
public class BuildPreferences
{
    public static bool IsForRelease
    {
        get
        {
            TextAsset json = Resources.Load("BuildPrefs") as TextAsset;
            if (json == null)
            {
                return false;
            }
            else
            {
                var buildPrefs = JsonUtility.FromJson<BuildPrefs>(json.ToString());
                return buildPrefs.isForRelease;
            }
        }
    }

    public static bool IsSteamServer
    {
        get
        {
            TextAsset json = Resources.Load("BuildPrefs") as TextAsset;
            if (json == null)
            {
                return false;
            }
            else
            {
                var buildPrefs = JsonUtility.FromJson<BuildPrefs>(json.ToString());
                return buildPrefs.isSteamServer;
            }
        }
    }

    /// <summary>
    /// To be called by the BuildScript only! Also sets steamserver to on
    /// </summary>
    public static void SetRelease(bool isOn)
    {
        string filePath = Application.dataPath + "/Resources/";
		var buildPrefs = new BuildPrefs
		{
			isForRelease = isOn,
			isSteamServer = isOn
		};
		var json = JsonUtility.ToJson(buildPrefs);
        File.WriteAllText(filePath + "BuildPrefs.json", json);
    }
}
[System.Serializable]
public class BuildPrefs
{
    public bool isForRelease;
    public bool isSteamServer;
}




