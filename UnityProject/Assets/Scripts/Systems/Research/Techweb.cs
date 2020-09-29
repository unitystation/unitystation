using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class Technology
{
	public String ID { get; set; }

	public String DisplayName { get; set; }

	public String Description { get; set; }

	/// <summary>
	/// A bool to tell whether or not it should be already researched on load
	/// </summary>
	public bool StartingNode { get; set; }

	/// <summary>
	/// What technologies "ID" are required to be able to research this Technology
	/// </summary>
	public List<String> RequiredTechnologies { get; set; }

	/// <summary>
	/// What designs it unlocks as ID
	/// </summary>
	public List<String> DesignIDs { get; set; }


	/// <summary>
	/// How much it costs to research
	/// </summary>
	public int ResearchCosts{ get; set; }

	/// <summary>
	/// How much money you get from exporting the data disk with it on
	/// </summary>
	public int ExportPrice{ get; set; }

	/// <summary>
	/// Worked out in the initialization, gives a list of potential technologies that can be made available when this technologies researched
	/// </summary>
	public List<String> PotentialUnlocks { get; set; }

}


public class Techweb : MonoBehaviour
{
	// Use this for initialization
	// void Awake()
	// {
		// if (!(Globals.IsInitialised))
		// {
			// JsonImportInitialization();
			// Initialization();
			// Globals.IsInitialised = true;
		// }
		//Logger.Log(Globals.ResearchedTechnology.Count().ToString() + " yo2", Category.Research);
		// string Something1 = string.Join(",", Globals.ResearchedTechnology);
		// string Something2 = string.Join(",", Globals.AvailableTechnology);
		// string Something3 = string.Join(",", Globals.AvailableDesigns);

		//Logger.Log(Something1 + " sResearchedTechnology", Category.Research);
		//Logger.Log(Something2 + " sAvailableTechnology", Category.Research);
		//Logger.Log(Something3 + " sAvailableDesigns", Category.Research);

		// Research("datatheory");

		// Something1 = string.Join(",", Globals.ResearchedTechnology);
		// Something2 = string.Join(",", Globals.AvailableTechnology);
		// Something3 = string.Join(",", Globals.AvailableDesigns);

		//Logger.Log(Globals.ResearchedTechnology.Count().ToString() + " yo3", Category.Research);
		//Logger.Log(Something1 + " ResearchedTechnology", Category.Research);
		//Logger.Log(Something2 + " AvailableTechnology", Category.Research);
		//Logger.Log(Something3 + " AvailableDesigns", Category.Research);

	// }

	public static class Globals
	{
		public static bool IsInitialised = false;
		public static List<String> ResearchedTechnology = new List<String>();
		public static List<String> AvailableTechnology = new List<String>();
		public static List<String> AvailableDesigns = new List<String>();
		public static Dictionary<String, Technology> IDSearchDictionary = new Dictionary<String, Technology>();
		public static List<Technology> Technologies = new List<Technology>();
	}
	private static void JsonImportInitialization ()
	{
		string json = (Resources.Load (@"Metadata\Technologies") as TextAsset).ToString ();
		var JsonTechnologies = JsonConvert.DeserializeObject<List<Dictionary<String,System.Object>>> (json);
		for (var i = 0; i < JsonTechnologies.Count (); i++) {
			Technology TechnologyPass = new Technology ();
			TechnologyPass.ID = JsonTechnologies [i] ["ID"].ToString ();
			TechnologyPass.DisplayName = JsonTechnologies [i] ["display_name"].ToString ();
			TechnologyPass.Description = JsonTechnologies [i] ["description"].ToString ();

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

			Globals.Technologies.Add (TechnologyPass);
		}
		Logger.Log ("JsonImportInitialization Techwebs done!", Category.Research);
	}
	private static void Initialization()
	{
		//Logger.Log(Globals.Technologies.Count().ToString() + " yo", Category.Research);
		foreach (Technology oneTechnology in  Globals.Technologies)
		{
			if (oneTechnology.StartingNode)
			{
				//Logger.Log(oneTechnology.ID + " added", Category.Research);
				Globals.ResearchedTechnology.Add(oneTechnology.ID);
				Globals.AvailableDesigns.AddRange(oneTechnology.DesignIDs);
				//Logger.Log(Globals.ResearchedTechnology.Count().ToString() + " yo", Category.Research);
			}
			Globals.IDSearchDictionary[oneTechnology.ID] = oneTechnology;
			//Logger.Log(oneTechnology.ID + " boo", Category.Research);
		}
		foreach (Technology oneTechnology in  Globals.Technologies)
		{
			//Logger.Log(oneTechnology.ID + " Yoo", Category.Research);
			if (oneTechnology.RequiredTechnologies.Any())
			{
				bool AllPresent = true;
				foreach (string RequiredTechnology in oneTechnology.RequiredTechnologies)
				{

					Globals.IDSearchDictionary[RequiredTechnology].PotentialUnlocks.Add(oneTechnology.ID);
					if (!(Globals.ResearchedTechnology.Contains(RequiredTechnology)))
					{
						AllPresent = false;
					}

				}
				if (AllPresent)
				{
					Globals.AvailableTechnology.Add(oneTechnology.ID);
				}
			}
		}
	}
	public static void Research(String TechnologyID)
	{
		if (Globals.AvailableTechnology.Contains(TechnologyID))
		{
			if (true) //Placeholder until I can work out how to calculate point gain
			{
				Globals.AvailableTechnology.Remove(TechnologyID);
				Globals.ResearchedTechnology.Add(TechnologyID);
				Globals.AvailableDesigns.AddRange(Globals.IDSearchDictionary[TechnologyID].DesignIDs);
				if (Globals.IDSearchDictionary[TechnologyID].PotentialUnlocks.Any())
				{
					foreach (string PotentialUnlockID in Globals.IDSearchDictionary[TechnologyID].PotentialUnlocks)
					{
						bool AllPresent = true;
						foreach (string NeededResearchID in Globals.IDSearchDictionary[PotentialUnlockID].RequiredTechnologies)
						{
							if (!(Globals.ResearchedTechnology.Contains(NeededResearchID)))
							{
								AllPresent = false;
							}
						}
						if (AllPresent)
						{
							Globals.AvailableTechnology.Add(PotentialUnlockID);
						}
					}
				}
			}
		}
	}
}