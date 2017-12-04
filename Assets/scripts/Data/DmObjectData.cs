using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FullSerializer;
using UnityEngine;
[CreateAssetMenu(fileName = "DmObjectData")]
public class DmObjectData : ScriptableObject
{
    private static List<Dictionary<string, string>> objectList = new List<Dictionary<string, string>>();

    private void OnEnable()
    {
        if (objectList.Count != 0) return;
        DeserializeJson();
    }

    public List<Dictionary<string, string>> ObjectList
    {
        get { return objectList; }
    }

    //Scans hierarchy for attributes
    public Dictionary<string, string> getObject(string hierarchy)
    {
        // i.e. we have /obj/item/clothing/tie/armband/cargo
        var path = hierarchy.Split('/').ToList();
        var ancAttr = new Dictionary<string, string>();
        //        StringBuilder digLog = new StringBuilder();

        for (int i = path.Count; i-- > 2;)
        {
            var ancHier = String.Join("/", path.ToArray());
            //            digLog.AppendLine("scanning " + ancHier);

            var foundAttributes = lookupObject(ancHier);
            if (foundAttributes.Count == 0 && !hierarchy.Equals(ancHier))
            {
                //                Debug.Log(digLog.AppendLine("Stopped digging further than " + ancHier).ToString());
                break;
            }

            ancAttr = ancAttr.Concat(foundAttributes)
                .GroupBy(d => d.Key)
                .ToDictionary(d => d.Key, d => d.First().Value);
            path.RemoveAt(i);

        }
        if (ancAttr.Count == 0)
        {
            Debug.LogError("Didn't find any attributes for hierarchy " + hierarchy);
        }
        return ancAttr;
    }

    private Dictionary<string, string> lookupObject(string hierarchy)
    {
        foreach (var obj in objectList)
        {
            if (/*obj.ContainsKey("hierarchy") && */obj["hierarchy"].Equals(hierarchy))
            {
                return obj;
            }
        }
        return new Dictionary<string, string>();
    }

    public static void DeserializeJson()
    {
        var asset = Resources.Load(Path.Combine("metadata", "dm")) as TextAsset;
        if (asset != null)
        {
            var data = fsJsonParser.Parse(asset.text);
            var serializer = new fsSerializer();
            serializer.TryDeserialize(data, ref objectList).AssertSuccessWithoutWarnings();
        }
        else throw new FileNotFoundException();
    }
}
