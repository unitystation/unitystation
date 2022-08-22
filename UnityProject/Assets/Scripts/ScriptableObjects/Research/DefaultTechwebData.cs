using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Research;
using NaughtyAttributes;
using System.IO;
using Newtonsoft.Json;
using Systems.Research.Data;

namespace ScriptableObjects.Research
{
	[CreateAssetMenu(fileName = "DefaultTechWebData", menuName = "ScriptableObjects/Systems/Techweb/DefaultTechwebData")]
	public class DefaultTechwebData : ScriptableObject
	{
		public List<Technology> technologies = new List<Technology>();

		[Button("Generate default data locally")]
		public void GenerateDefaultData()
		{

			string jsonData = JsonConvert.SerializeObject(technologies);
			string path = $"{Application.persistentDataPath}/GameData/Research/";
			string fileName = "TechwebData.json";
			Debug.Log(jsonData);

			if (Directory.Exists($"{path}") == false)
			{
				Debug.Log($"{path} not found, making one..");
				Directory.CreateDirectory(path);
			}
			if (File.Exists($"{path}{fileName}"))
			{
				File.Delete(path + fileName);
				File.WriteAllText(path + fileName, jsonData);
				return;
			}
			File.WriteAllText(path + fileName, jsonData);
		}
	}

}
