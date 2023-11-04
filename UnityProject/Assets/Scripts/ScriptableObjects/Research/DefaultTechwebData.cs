using System.Collections.Generic;
using System.IO;
using SecureStuff;
using UnityEngine;
using NaughtyAttributes;
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
			string path = "TechWeb";


			Debug.Log(jsonData);

			path = Path.Combine("TechWeb", "TechwebData.json");

			if (AccessFile.Exists($"{path}"))
			{
				AccessFile.Delete(path);
			}
			AccessFile.Save(path, jsonData);
		}
	}

}
