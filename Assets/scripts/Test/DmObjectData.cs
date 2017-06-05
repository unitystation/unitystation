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
    
    public Dictionary<string,string> getObject(string hierarchy)
    {
        foreach (var obj in objectList)
        {
            if (obj.ContainsKey("hierarchy") && obj["hierarchy"].Equals(hierarchy))
            {
                return obj;
            }
        }
        Debug.LogError("could not find object with hierarchy " + hierarchy);
        return new Dictionary<string, string>();
    }
    
    public static void DeserializeJson()
    {
        var asset = Resources.Load(Path.Combine("metadata", "dm")) as TextAsset;
        if (asset != null)
        {
            var data = fsJsonParser.Parse(asset.text);
            var serializer = new fsSerializer();// psr
            serializer.TryDeserialize(data, ref objectList).AssertSuccessWithoutWarnings();
        } else throw new FileNotFoundException();
    }
}
