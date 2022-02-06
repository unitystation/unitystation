using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ScriptableObjects.Research;

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
		[SerializeField] private String researchDataPath = "/GameData/Research/";
		[SerializeField] private String dataFileName = "TechwebData.json";
		[SerializeField] private DefaultTechwebData defaultTechweb;

		private int researchPoints = 10000;
		public int ResearchPoints => researchPoints;

		public class GlobalResearchData
		{
			public static bool IsInitialised = false;
			public static List<String> ResearchedTechnology = new List<String>();
			public static List<String> AvailableTechnology = new List<String>();
			public static List<String> AvailableDesigns = new List<String>();
			public static Dictionary<String, Technology> IDSearchDictionary = new Dictionary<String, Technology>();
			public static List<Technology> Technologies = new List<Technology>();
		}

		private void Awake()
		{
			if(JsonImportInitialization() == false)
			{
				defaultTechweb.GenerateDefaultData();
				if (JsonImportInitialization() == false) Logger.LogError("Unable to generate or find techweb data!");
			}
			if (GameManager.Instance.TechwebServer == null) GameManager.Instance.TechwebServer = this;
		}

		private bool JsonImportInitialization()
		{
			var path = $"{Application.persistentDataPath}{researchDataPath}{dataFileName}";
			if(System.IO.File.Exists(path) == false) return false;
			string json = (Resources.Load(path) as TextAsset).ToString();
			if(json == null || json.Length < 1) return false;
			var JsonTechnologies = JsonConvert.DeserializeObject<List<Dictionary<String, System.Object>>>(json);
			for (var i = 0; i < JsonTechnologies.Count(); i++)
			{
				Technology TechnologyPass = new Technology();
				TechnologyPass.ID = JsonTechnologies[i]["ID"].ToString();
				TechnologyPass.DisplayName = JsonTechnologies[i]["display_name"].ToString();
				TechnologyPass.Description = JsonTechnologies[i]["description"].ToString();

				if (JsonTechnologies[i].ContainsKey("design_ids"))
				{ TechnologyPass.DesignIDs = JsonConvert.DeserializeObject<List<string>>(JsonTechnologies[i]["design_ids"].ToString()); }
				else
				{
					List<string> EmptyDesignIDs = new List<string>();
					TechnologyPass.DesignIDs = EmptyDesignIDs;
				}

				if (JsonTechnologies[i].ContainsKey("research_costs"))
				{ TechnologyPass.ResearchCosts = int.Parse(JsonTechnologies[i]["research_costs"].ToString()); }
				else
				{ TechnologyPass.ResearchCosts = 0; }

				if (JsonTechnologies[i].ContainsKey("research_costs"))
				{ TechnologyPass.ExportPrice = int.Parse(JsonTechnologies[i]["export_price"].ToString()); }
				else
				{ TechnologyPass.ExportPrice = 0; }


				if (JsonTechnologies[i].ContainsKey("prereq_ids"))
				{ TechnologyPass.RequiredTechnologies = JsonConvert.DeserializeObject<List<string>>(JsonTechnologies[i]["prereq_ids"].ToString()); }
				else
				{
					List<string> EmptyRequiredTechnologies = new List<string>();
					TechnologyPass.RequiredTechnologies = EmptyRequiredTechnologies;
				}

				if (JsonTechnologies[i].ContainsKey("starting_node"))
				{ TechnologyPass.StartingNode = bool.Parse(JsonTechnologies[i]["starting_node"].ToString()); }
				else
				{ TechnologyPass.StartingNode = false; }

				TechnologyPass.PotentialUnlocks = new List<string>();

				GlobalResearchData.Technologies.Add(TechnologyPass);
			}
			Logger.Log("JsonImportInitialization Techwebs done!", Category.Research);
			return true;
		}
		private static void Initialization()
		{
			//Logger.Log(Globals.Technologies.Count().ToString() + " yo", Category.Research);
			foreach (Technology oneTechnology in GlobalResearchData.Technologies)
			{
				if (oneTechnology.StartingNode)
				{
					//Logger.Log(oneTechnology.ID + " added", Category.Research);
					GlobalResearchData.ResearchedTechnology.Add(oneTechnology.ID);
					GlobalResearchData.AvailableDesigns.AddRange(oneTechnology.DesignIDs);
					//Logger.Log(Globals.ResearchedTechnology.Count().ToString() + " yo", Category.Research);
				}
				GlobalResearchData.IDSearchDictionary[oneTechnology.ID] = oneTechnology;
				//Logger.Log(oneTechnology.ID + " boo", Category.Research);
			}
			foreach (Technology oneTechnology in GlobalResearchData.Technologies)
			{
				//Logger.Log(oneTechnology.ID + " Yoo", Category.Research);
				if (oneTechnology.RequiredTechnologies.Any())
				{
					bool AllPresent = true;
					foreach (string RequiredTechnology in oneTechnology.RequiredTechnologies)
					{

						GlobalResearchData.IDSearchDictionary[RequiredTechnology].PotentialUnlocks.Add(oneTechnology.ID);
						if (!(GlobalResearchData.ResearchedTechnology.Contains(RequiredTechnology)))
						{
							AllPresent = false;
						}

					}
					if (AllPresent)
					{
						GlobalResearchData.AvailableTechnology.Add(oneTechnology.ID);
					}
				}
			}
		}
		public static void Research(String TechnologyID)
		{
			if (GlobalResearchData.AvailableTechnology.Contains(TechnologyID))
			{
				if (true) //Placeholder until I can work out how to calculate point gain
				{
					GlobalResearchData.AvailableTechnology.Remove(TechnologyID);
					GlobalResearchData.ResearchedTechnology.Add(TechnologyID);
					GlobalResearchData.AvailableDesigns.AddRange(GlobalResearchData.IDSearchDictionary[TechnologyID].DesignIDs);
					if (GlobalResearchData.IDSearchDictionary[TechnologyID].PotentialUnlocks.Any())
					{
						foreach (string PotentialUnlockID in GlobalResearchData.IDSearchDictionary[TechnologyID].PotentialUnlocks)
						{
							bool AllPresent = true;
							foreach (string NeededResearchID in GlobalResearchData.IDSearchDictionary[PotentialUnlockID].RequiredTechnologies)
							{
								if (!(GlobalResearchData.ResearchedTechnology.Contains(NeededResearchID)))
								{
									AllPresent = false;
								}
							}
							if (AllPresent)
							{
								GlobalResearchData.AvailableTechnology.Add(PotentialUnlockID);
							}
						}
					}
				}
			}
		}
	}
}
