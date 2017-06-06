using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FullSerializer;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class TgObjectTest {
	#if UNITY_EDITOR
    [MenuItem("Tools/Test object")]
	#endif
    public static void DeserializeJson()
    {
        var asset = Resources.Load(Path.Combine("metadata", "dm")) as TextAsset;
        var data = fsJsonParser.Parse(asset.text);
        List<Dictionary<string, string>> diclist = null;
        var serializer = new fsSerializer();// psr
        serializer.TryDeserialize(data, ref diclist).AssertSuccessWithoutWarnings();
        Debug.Log(diclist.ToArray()[0].Keys.Aggregate("", (current, key) => current + (key + ": ") + diclist.ToArray()[0][key] + "\n"));   
    }

    public static void DeserializeJsonMini()
    {
        var asset = Resources.Load(Path.Combine("metadata", "dm1")) as TextAsset;
        var data = fsJsonParser.Parse(asset.text);
        Dictionary<string, string> dic = null;
        var serializer = new fsSerializer();// psr
        serializer.TryDeserialize(data, ref dic).AssertSuccessWithoutWarnings();
        Debug.Log(dic.Keys.Aggregate("", (current, key) => current + (key + ": ") + dic[key] + "\n")); 
    }
		
    [Serializable]
    private class Wrapper<T>
    {
        public List<T> list;
    }
}

