using System.Collections;
using System.Collections.Generic;
using System.IO;
using FullSerializer;
using UnityEngine;
[CreateAssetMenu(fileName = "DmObjectData")]
public class DmObjectData : ScriptableObject {
private static List<Dictionary<string, string>> objectList;

    private void OnEnable()
    {
        DeserializeJson();
        Debug.Log("DM: Deserialized json!");
    }
    public static void DeserializeJson()
    {
        var asset = Resources.Load(Path.Combine("metadata", "dm")) as TextAsset;
        var data = fsJsonParser.Parse(asset.text);
        var serializer = new fsSerializer();// psr
        serializer.TryDeserialize(data, ref objectList).AssertSuccessWithoutWarnings();

        
    }
}
