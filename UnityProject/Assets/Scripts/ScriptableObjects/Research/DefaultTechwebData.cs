using System.Collections.Generic;
using UnityEngine;
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

			string jsonData = JsonConvert.SerializeObject(technologies, Formatting.Indented);
			string path = Path.Combine(Application.streamingAssetsPath, "TechWeb");


			Debug.Log(jsonData);

			if (Directory.Exists($"{path}") == false)
			{
				Debug.Log($"{path} not found, making one..");
				Directory.CreateDirectory(path);
			}

			path = Path.Combine(path, "TechwebData.json");

			if (File.Exists($"{path}"))
			{
				File.Delete(path);
				File.WriteAllText(path, jsonData);
				return;
			}
			File.WriteAllText(path, jsonData);
		}
	}

}
