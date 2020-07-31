using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

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

	void Awake()
	{
		//if (!(Globals.IsInitialised))
		//{
		//	JsonImportInitialization();
		//	Globals.IsInitialised = true;
		//}
	}
	public static class Globals
	{
		public static bool IsInitialised = false;
		public static List<string> PathsList = new List<string> {
			@"Metadata\Designs\AIDesigns",
			@"Metadata\Designs\BiogeneratorDesigns",
			@"Metadata\Designs\BluespaceDesigns",
			@"Metadata\Designs\CompBoardDesigns",
			@"Metadata\Designs\ComputerPartDesigns",
			@"Metadata\Designs\ElectronicsDesigns",
			@"Metadata\Designs\LimbgrowerDesigns",
			@"Metadata\Designs\MachineDesigns",
			@"Metadata\Designs\MechaDesigns",
			@"Metadata\Designs\MechfabricatorDesigns",
			@"Metadata\Designs\MedicalDesigns",
			@"Metadata\Designs\MiningDesigns",
			@"Metadata\Designs\MiscDesigns",
			@"Metadata\Designs\PowerDesigns",
			@"Metadata\Designs\StockPartsDesigns",
			@"Metadata\Designs\TelecommsDesigns",
			@"Metadata\Designs\WeaponDesigns",
		};
		public static Dictionary<string,Design> InternalIDSearch;


	}
	private static void JsonImportInitialization ()
	{
		Globals.InternalIDSearch =  new Dictionary<string,Design>();
		foreach (string FilePath in Globals.PathsList) {
			string json = (Resources.Load (FilePath) as TextAsset).ToString ();
			var JsonTechnologies = JsonConvert.DeserializeObject<List<Dictionary<String,System.Object>>> (json);
			for (var i = 0; i < JsonTechnologies.Count (); i++) {
				Design DesignPass = new Design ();
				DesignPass.InternalID = JsonTechnologies [i] ["Internal_ID"].ToString ();
				DesignPass.Name = JsonTechnologies [i] ["name"].ToString ();
				DesignPass.ItemID = JsonTechnologies [i] ["Item_ID"].ToString ();

				if (JsonTechnologies [i].ContainsKey ("Description")) {
					DesignPass.Description = JsonTechnologies [i] ["Description"].ToString ();
				} else {
					DesignPass.Description = " *Breathes in through teeth* nahh, no description here mate ";
				}


				if (JsonTechnologies [i].ContainsKey ("Category")) {
					DesignPass.Category = JsonConvert.DeserializeObject<List<string>> (JsonTechnologies [i] ["Category"].ToString ());
				} else {
					DesignPass.Category = new List<string> ();
				}

				if (JsonTechnologies [i].ContainsKey ("Compatible_machinery")) {
					DesignPass.CompatibleMachinery = JsonConvert.DeserializeObject<List<string>> (JsonTechnologies [i] ["Compatible_machinery"].ToString ());
				} else {
					DesignPass.CompatibleMachinery = new List<string> ();
				}

				DesignPass.MachineryType = JsonConvert.DeserializeObject<List<string>> (JsonTechnologies [i] ["Machinery_type"].ToString ());

				if (JsonTechnologies [i].ContainsKey ("Required_reagents")) {
					DesignPass.RequiredReagents = JsonConvert.DeserializeObject<Dictionary<string,int>> (JsonTechnologies [i] ["Required_reagents"].ToString ());
				} else {
					DesignPass.RequiredReagents = new Dictionary<string,int> ();
				}

				if (JsonTechnologies [i].ContainsKey ("Make_reagents")) {
					DesignPass.ReagentsToMake = JsonConvert.DeserializeObject<Dictionary<string,int>> (JsonTechnologies [i] ["Make_reagents"].ToString ());
				} else {
					DesignPass.ReagentsToMake = new Dictionary<string,int> ();
				}

				Globals.InternalIDSearch[DesignPass.InternalID] = DesignPass;
			}

		}
		Logger.Log ("JsonImportInitialization for designs is done!", Category.Research);
	}

}

