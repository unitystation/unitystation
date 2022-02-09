using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ScriptableObjects.Research;
using System.IO;

namespace Systems.Research
{
	[Serializable]
	public struct Technology
	{
		public String ID;

		public String DisplayName;

		public String Description;

		/// <summary>
		/// A bool to tell whether or not it should be already researched on load
		/// </summary>
		public bool StartingNode;

		/// <summary>
		/// What technologies "ID" are required to be able to research this Technology
		/// </summary>
		public List<String> RequiredTechnologies;

		/// <summary>
		/// What designs it unlocks as ID
		/// </summary>
		public List<String> DesignIDs;


		/// <summary>
		/// How much it costs to research
		/// </summary>
		public int ResearchCosts;

		/// <summary>
		/// How much money you get from exporting the data disk with it on
		/// </summary>
		public int ExportPrice;

		/// <summary>
		/// Worked out in the initialization, gives a list of potential technologies that can be made available when this technologies researched
		/// </summary>
		public List<String> PotentialUnlocks;

		/// <summary>
		/// The prefab ID of the item we're trying to research. Taken from the prefab tracker's randomly generated ID.
		/// Empty if we're not trying to produce an item from this node.
		/// </summary>
		public String prefabID;

		public TechType techType;
	}

	public enum TechType
	{
		Research = 0,
		Circuit = 1,
		Material = 2,
		Hardware = 3,
		Bio = 4,
	}


	public class Techweb : MonoBehaviour
	{
		public static Techweb Instance;
		[SerializeField] private String researchDataPath = "/GameData/Research/";
		[SerializeField] private String dataFileName = "TechwebData.json";
		[SerializeField] private DefaultTechwebData defaultTechweb;

		private int researchPoints = 10000;
		public int ResearchPoints => researchPoints;

		public class ResearchData
		{
			public bool IsInitialised = false;
			public List<String> ResearchedTechnology = new List<string>();
			public List<String> AvailableTechnology = new List<string>();
			public List<String> AvailableDesigns = new List<string>();
			public Dictionary<String, Technology> IDSearchDictionary = new Dictionary<string, Technology>();
			public List<Technology> Technologies = new List<Technology>();
		}

		public ResearchData Data = new ResearchData();

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(this);
			}
			if (JsonImportInitialization() == false)
			{
				defaultTechweb.GenerateDefaultData();
				if (JsonImportInitialization() == false) Logger.LogError("Unable to generate or find techweb data!");
			}
			if (GameManager.Instance.TechwebServer == null) GameManager.Instance.TechwebServer = this;
		}

		private bool JsonImportInitialization()
		{
			var path = $"{Application.persistentDataPath}{researchDataPath}{dataFileName}";
			if(File.Exists(path) == false) return false;
			string json = File.ReadAllText(path);
			if(json == null || json.Length < 1) return false;
			var JsonTechnologies = JsonConvert.DeserializeObject<List<Dictionary<String, System.Object>>>(json);
			for (var i = 0; i < JsonTechnologies.Count(); i++)
			{
				Technology TechnologyPass = new Technology();
				TechnologyPass.ID = JsonTechnologies[i]["ID"].ToString();
				TechnologyPass.DisplayName = JsonTechnologies[i]["DisplayName"].ToString();
				TechnologyPass.Description = JsonTechnologies[i]["Description"].ToString();

				if (JsonTechnologies[i].ContainsKey("DesignIDs"))
				{ TechnologyPass.DesignIDs = JsonConvert.DeserializeObject<List<string>>(JsonTechnologies[i]["DesignIDs"].ToString()); }
				else
				{
					List<string> EmptyDesignIDs = new List<string>();
					TechnologyPass.DesignIDs = EmptyDesignIDs;
				}

				if (JsonTechnologies[i].ContainsKey("ResearchCosts"))
				{ TechnologyPass.ResearchCosts = int.Parse(JsonTechnologies[i]["ResearchCosts"].ToString()); }
				else
				{ TechnologyPass.ResearchCosts = 0; }

				if (JsonTechnologies[i].ContainsKey("ExportPrice"))
				{ TechnologyPass.ExportPrice = int.Parse(JsonTechnologies[i]["ExportPrice"].ToString()); }
				else
				{ TechnologyPass.ExportPrice = 0; }


				if (JsonTechnologies[i].ContainsKey("RequiredTechnologies"))
				{ TechnologyPass.RequiredTechnologies = JsonConvert.DeserializeObject<List<string>>(JsonTechnologies[i]["RequiredTechnologies"].ToString()); }
				else
				{
					List<string> EmptyRequiredTechnologies = new List<string>();
					TechnologyPass.RequiredTechnologies = EmptyRequiredTechnologies;
				}

				if (JsonTechnologies[i].ContainsKey("StartingNode"))
				{ TechnologyPass.StartingNode = bool.Parse(JsonTechnologies[i]["StartingNode"].ToString()); }
				else
				{ TechnologyPass.StartingNode = false; }

				if (JsonTechnologies[i].ContainsKey("PotentialUnlocks"))
				{
					TechnologyPass.PotentialUnlocks = JsonConvert.DeserializeObject<List<string>>(JsonTechnologies[i]["PotentialUnlocks"].ToString());
				}
				else
				{
					var EmptyPotentialUnlocks = new List<string>();
					EmptyPotentialUnlocks.Add("");
					TechnologyPass.PotentialUnlocks = EmptyPotentialUnlocks;
				}
				
				Data.Technologies.Add(TechnologyPass);
			}
			Logger.Log("JsonImportInitialization Techwebs done!", Category.Research);
			return true;
		}
		private void Initialization()
		{
			//Logger.Log(Globals.Technologies.Count().ToString() + " yo", Category.Research);
			foreach (Technology oneTechnology in Data.Technologies)
			{
				if (oneTechnology.StartingNode)
				{
					//Logger.Log(oneTechnology.ID + " added", Category.Research);
					Data.ResearchedTechnology.Add(oneTechnology.ID);
					Data.AvailableDesigns.AddRange(oneTechnology.DesignIDs);
					//Logger.Log(Globals.ResearchedTechnology.Count().ToString() + " yo", Category.Research);
				}
				Data.IDSearchDictionary[oneTechnology.ID] = oneTechnology;
				//Logger.Log(oneTechnology.ID + " boo", Category.Research);
			}
			foreach (Technology oneTechnology in Data.Technologies)
			{
				//Logger.Log(oneTechnology.ID + " Yoo", Category.Research);
				if (oneTechnology.RequiredTechnologies.Any())
				{
					bool AllPresent = true;
					foreach (string RequiredTechnology in oneTechnology.RequiredTechnologies)
					{

						Data.IDSearchDictionary[RequiredTechnology].PotentialUnlocks.Add(oneTechnology.ID);
						if (!(Data.ResearchedTechnology.Contains(RequiredTechnology)))
						{
							AllPresent = false;
						}

					}
					if (AllPresent)
					{
						Data.AvailableTechnology.Add(oneTechnology.ID);
					}
				}
			}
		}
		public void Research(String TechnologyID)
		{
			if (Data.AvailableTechnology.Contains(TechnologyID))
			{
				if (true) //Placeholder until I can work out how to calculate point gain
				{
					Data.AvailableTechnology.Remove(TechnologyID);
					Data.ResearchedTechnology.Add(TechnologyID);
					Data.AvailableDesigns.AddRange(Data.IDSearchDictionary[TechnologyID].DesignIDs);
					if (Data.IDSearchDictionary[TechnologyID].PotentialUnlocks.Any())
					{
						foreach (string PotentialUnlockID in Data.IDSearchDictionary[TechnologyID].PotentialUnlocks)
						{
							bool AllPresent = true;
							foreach (string NeededResearchID in Data.IDSearchDictionary[PotentialUnlockID].RequiredTechnologies)
							{
								if (!(Data.ResearchedTechnology.Contains(NeededResearchID)))
								{
									AllPresent = false;
								}
							}
							if (AllPresent)
							{
								Data.AvailableTechnology.Add(PotentialUnlockID);
							}
						}
					}
				}
			}
		}
	}
}
