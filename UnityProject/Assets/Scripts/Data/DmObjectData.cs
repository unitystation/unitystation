using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

[CreateAssetMenu(fileName = "DmObjectData")]
public class DmObjectData : ScriptableObject
{
	private static List<Dictionary<string, string>> objectList = new List<Dictionary<string, string>>();

	public List<Dictionary<string, string>> ObjectList => objectList;

	private void OnEnable()
	{
		if (objectList.Count != 0)
		{
			return;
		}
		DeserializeJson();
	}

	//Scans hierarchy for attributes
	public Dictionary<string, string> getObject(string hierarchy)
	{
		// i.e. we have /obj/item/clothing/tie/armband/cargo
		List<string> path = hierarchy.Split('/').ToList();
		Dictionary<string, string> ancAttr = new Dictionary<string, string>();
		//        StringBuilder digLog = new StringBuilder();

		for (int i = path.Count; i-- > 2;)
		{
			string ancHier = string.Join("/", path.ToArray());
			//            digLog.AppendLine("scanning " + ancHier);

			Dictionary<string, string> foundAttributes = lookupObject(ancHier);
			if (foundAttributes.Count == 0 && !hierarchy.Equals(ancHier))
			{
				//                Logger.Log(digLog.AppendLine("Stopped digging further than " + ancHier).ToString());
				break;
			}

			ancAttr = ancAttr.Concat(foundAttributes)
				.GroupBy(d => d.Key)
				.ToDictionary(d => d.Key, d => d.First().Value);
			path.RemoveAt(i);
		}
		if (ancAttr.Count == 0)
		{
			Logger.LogError("Didn't find any attributes for hierarchy " + hierarchy, Category.DmMetadata);
		}
		return ancAttr;
	}

	private Dictionary<string, string> lookupObject(string hierarchy)
	{
		foreach (Dictionary<string, string> obj in objectList)
		{
			if ( /*obj.ContainsKey("hierarchy") && */obj["hierarchy"].Equals(hierarchy))
			{
				return obj;
			}
		}
		return new Dictionary<string, string>();
	}

	public static void DeserializeJson()
	{
		TextAsset asset = Resources.Load(Path.Combine("metadata", "dm")) as TextAsset;
		if (asset != null)
		{
			objectList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>( asset.text );
		}
		else
		{
			throw new FileNotFoundException();
		}
	}
}