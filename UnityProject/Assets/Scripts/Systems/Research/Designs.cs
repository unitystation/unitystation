using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Logs;
using SecureStuff;

namespace Systems.Research
{
	public class Design
	{
		public String InternalID { get; set; } //

		/// <summary>
		/// The required materials to make it
		/// </summary>
		public Dictionary<string,int> Materials { get; set; }

		public String Name { get; set; } //

		/// <summary>
		/// the ID (hierarchy) of the item
		/// </summary>
		public String ItemID { get; set; } //

		/// <summary>
		/// What type of machine is it?
		/// </summary>
		public List<String> MachineryType { get; set; } //

		/// <summary>
		/// What departments is it compatible with
		/// </summary>
		public List<String> CompatibleMachinery { get; set; } //


		/// <summary>
		/// what sub menu should it be in
		/// </summary>
		public List<String> Category{ get; set; } //

		/// <summary>
		/// Stuff that uses chemicals instead of materials
		/// </summary>
		public Dictionary<string,int> RequiredReagents { get; set; }

		/// <summary>
		/// Stuff at spits out chemicals
		/// </summary>
		public Dictionary<string,int> ReagentsToMake { get; set; } //

		/// <summary>
		/// Self explanatory
		/// </summary>
		public String Description { get; set; } //

	}

	public class Designs : MonoBehaviour
	{

		static List<string> Jsons = new List<string>();

		private static string TechWebFolder => "TechWeb";
		private static string TechWebDesignsFolder => Path.Combine(TechWebFolder, "Designs");

		void Awake()
		{
			if (!(Globals.IsInitialised))
			{
				Jsons.Clear();

				Globals.InternalIDSearch = new Dictionary<string, Design>();

				var files = AccessFile.DirectoriesOrFilesIn(TechWebDesignsFolder);

				foreach(string file in files)
				{
					Jsons.Add(AccessFile.Load(Path.Combine(TechWebDesignsFolder ,file)));
				}

				new Task(JsonImportInitialization).Start();
			}
		}
		public static class Globals
		{
			public static bool IsInitialised = false;

			public static Dictionary<string,Design> InternalIDSearch;

		}
		private static void JsonImportInitialization()
		{
			foreach (string json in Jsons)
			{
				var JsonTechnologies = JsonConvert.DeserializeObject<List<Dictionary<String, System.Object>>>(json);
				for (var i = 0; i < JsonTechnologies.Count(); i++)
				{
					Design DesignPass = new Design();
					DesignPass.InternalID = JsonTechnologies[i]["Internal_ID"].ToString();
					DesignPass.Name = JsonTechnologies[i]["name"].ToString();
					DesignPass.ItemID = JsonTechnologies[i]["Item_ID"].ToString();

					if (JsonTechnologies[i].ContainsKey("Description"))
					{
						DesignPass.Description = JsonTechnologies[i]["Description"].ToString();
					}
					else
					{
						DesignPass.Description = " *Breathes in through teeth* nahh, no description here mate ";
					}


					if (JsonTechnologies[i].ContainsKey("Category"))
					{
						DesignPass.Category = JsonConvert.DeserializeObject<List<string>>(JsonTechnologies[i]["Category"].ToString());
					}
					else
					{
						DesignPass.Category = new List<string>();
					}

					if (JsonTechnologies[i].ContainsKey("Materials"))
					{
						DesignPass.Materials = JsonConvert.DeserializeObject<Dictionary<string, int>>(JsonTechnologies[i]["Materials"].ToString());
					}
					else
					{
						DesignPass.Materials = new Dictionary<string, int>();
					}

					if (JsonTechnologies[i].ContainsKey("Compatible_machinery"))
					{
						DesignPass.CompatibleMachinery = JsonConvert.DeserializeObject<List<string>>(JsonTechnologies[i]["Compatible_machinery"].ToString());
					}
					else
					{
						DesignPass.CompatibleMachinery = new List<string>();
					}

					DesignPass.MachineryType = JsonConvert.DeserializeObject<List<string>>(JsonTechnologies[i]["Machinery_type"].ToString());

					if (JsonTechnologies[i].ContainsKey("Required_reagents"))
					{
						DesignPass.RequiredReagents = JsonConvert.DeserializeObject<Dictionary<string, int>>(JsonTechnologies[i]["Required_reagents"].ToString());
					}
					else
					{
						DesignPass.RequiredReagents = new Dictionary<string, int>();
					}

					if (JsonTechnologies[i].ContainsKey("Make_reagents"))
					{
						DesignPass.ReagentsToMake = JsonConvert.DeserializeObject<Dictionary<string, int>>(JsonTechnologies[i]["Make_reagents"].ToString());
					}
					else
					{
						DesignPass.ReagentsToMake = new Dictionary<string, int>();
					}

					Globals.InternalIDSearch[DesignPass.InternalID] = DesignPass;
				}

			}
			Loggy.Log("JsonImportInitialization for designs is done!", Category.Research);
			Globals.IsInitialised = true;
		}

	}
}



