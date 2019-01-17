using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;



//Initialization.run();
//var area = new Dictionary<String, float>();
//area.Add("potassium", 59.0f);
//area.Add("oxygen", 49.0f);
//area.Add("sugar", 45.0f);
//area.Add("iodine", 20.0f);

//area.Add("virusfood",59f);
//area.Add("blood",49f);
//float Temperature = 400f;
//Dictionary<string, float> area_new = Calculations.Reactions(area,Temperature);


public class Reaction
{
	public String Name { get; set; }

	/// <summary>
	/// Example "Chemical Name":2, "Chemical Name2":1, Will return the amount specified if all Chemicals are present for ReagentsAndRatio 
	/// </summary>
	public Dictionary<String, float> Results { get; set; }

	/// <summary>
	/// Example "Chemical Name":2, "Chemical Name2":1, note if there is a catalyst it has to be specified in here as well
	/// </summary>
	public Dictionary<String, float> ReagentsAndRatio { get; set; }

	/// <summary>
	/// Example "Chemical Name":0, "Chemical Name2":0, note catalysts Have to be specified in ReagentsAndRatio
	/// </summary>
	public Dictionary<String, float> Catalysts { get; set; }

	/// <summary>
	/// is the minimum temperature that the reaction will happen at
	/// </summary>
	public float MinimumTemperature{ get; set; }
}

public static class ChemistryGlobals
{
	public static bool IsInitialised = false;
	public static Dictionary<String, HashSet<Reaction>> ReactionsStoreDictionary = new Dictionary<String, HashSet<Reaction>> ();
	public static List<Reaction> reactions = new List<Reaction> ();
}

public static class Initialization
{
	public static void run ()
	{
		Initialization.JsonImportInitialization ();
		Initialization.CemInitialization ();
	}

	private static void JsonImportInitialization ()
	{
		string json = (Resources.Load (@"Metadata\Reactions") as TextAsset).ToString ();
		var JsonReactions = JsonConvert.DeserializeObject<List<Dictionary<String,System.Object>>> (json);
		for (var i = 0; i < JsonReactions.Count (); i++) {
			Reaction ReactionPass = new Reaction ();
			ReactionPass.Name = JsonReactions [i] ["Name"].ToString ();
			ReactionPass.Results = JsonConvert.DeserializeObject<Dictionary<string, float>> (JsonReactions [i] ["Results"].ToString ());
			ReactionPass.ReagentsAndRatio = JsonConvert.DeserializeObject<Dictionary<string, float>> (JsonReactions [i] ["Reagents_and_ratio"].ToString ());

			if (JsonReactions [i].ContainsKey ("Catalysts")) {
				ReactionPass.Catalysts = JsonConvert.DeserializeObject<Dictionary<string, float>> (JsonReactions [i] ["Catalysts"].ToString ());
			} 
			else 
			{
				Dictionary<string, float> EmptyCatalyst = new Dictionary<string, float>();
				ReactionPass.Catalysts = EmptyCatalyst;
			}

			if (JsonReactions [i].ContainsKey ("Minimum_temperature")) {
				ReactionPass.MinimumTemperature = float.Parse (JsonReactions [i] ["Minimum_temperature"].ToString ());
			} 
			else
			{
				ReactionPass.MinimumTemperature = 0.0f;
			}

			ChemistryGlobals.reactions.Add (ReactionPass);
		}
		Logger.Log ("JsonImportInitialization done!", Category.Chemistry);
	}

	private static void CemInitialization ()
	{
		for (var i = 0; i < ChemistryGlobals.reactions.Count (); i++) {

			foreach (string Chemical in ChemistryGlobals.reactions[i].ReagentsAndRatio.Keys) {
				if (!(ChemistryGlobals.ReactionsStoreDictionary.ContainsKey (Chemical))) {
					ChemistryGlobals.ReactionsStoreDictionary [Chemical] = new HashSet<Reaction> ();
				}
				ChemistryGlobals.ReactionsStoreDictionary [Chemical].Add (ChemistryGlobals.reactions [i]);
			}
		}
	}
}

public static class Calculations
{
	//ok, so you're wondering how to call it Chemistry.Calculations.Reaction
	//the Area  Would look something like this {"Chemical":5,"Another chemical":2}
	//It will return The modified area
	public static Dictionary<string, float> Reactions (Dictionary<string, float> Area, float Temperature)
	{
		HashSet<Reaction> ReactionBuffer = new HashSet<Reaction> ();
		foreach (string Chemical in Area.Keys) {
			if (ChemistryGlobals.ReactionsStoreDictionary.ContainsKey (Chemical)) {

				foreach (var Reaction in ChemistryGlobals.ReactionsStoreDictionary[Chemical]) { //so A list of every reaction that that chemical can be in
					if (!(Reaction.MinimumTemperature > Temperature)) {
						bool ValidReaction = new bool ();
						ValidReaction = true;
						foreach (string RequiredChemical in Reaction.ReagentsAndRatio.Keys) { //Checks if all the other chemicals are in
							if (!(Area.ContainsKey (RequiredChemical))) {
								ValidReaction = false;
							}

						}
						if (ValidReaction) {
							ReactionBuffer.Add (Reaction); //then adds it
						}
					}
				}
			}
		}
		//Logger.Log (ReactionBuffer.Count.ToString () + " < ReactionBuffer");
		foreach (var Reaction in ReactionBuffer) {
			List<string> CompatibleChemicals = new List<string> ();
			foreach (string Chemical in Reaction.ReagentsAndRatio.Keys) { //Finds the best chemical to do the reaction formula from, By going through each chemical and checking if the ratios work out
				bool Compatible = new bool ();
				Compatible = true;
				foreach (string SubChemical in Reaction.ReagentsAndRatio.Keys) {
					
					if (Area [Chemical] * (Reaction.ReagentsAndRatio [SubChemical] / Reaction.ReagentsAndRatio [Chemical]) > Area [SubChemical]) {
						Compatible = false;
					} else if  (Area [Chemical] <= 0){
						Compatible = false;
					}
				}
				if (Compatible) {
					CompatibleChemicals.Add (Chemical);
				}
			}
			if (CompatibleChemicals.Any ()) {
				var CompatibleChem = CompatibleChemicals [0];
				//Logger.Log (Area [CompatibleChem].ToString() + " < Area [CompatibleChem ");
				var BackUp = Area [CompatibleChem];
				foreach (string Chemical in Reaction.ReagentsAndRatio.Keys) {
					if (!(Reaction.Catalysts.ContainsKey (Chemical))) {
						Area [Chemical] = (Area [Chemical] - BackUp * SwapFix (Reaction.ReagentsAndRatio [CompatibleChem], Reaction.ReagentsAndRatio [Chemical])); // Does some mathematics to work out how much of each element to take away
					}
				}

				foreach (string Chemical in Reaction.Results.Keys) {
					float ChemicalAmount = 0;
					if (Area.ContainsKey (Chemical)) {
						ChemicalAmount = Area [Chemical];
					}
					//Logger.Log (ChemicalAmount.ToString() + " < ChemicalAmount " + Reaction.Results [Chemical].ToString() + " < Reaction.Results [Chemical] " + BackUp.ToString() + " < BackUp " + Reaction.ReagentsAndRatio [CompatibleChem].ToString() + " <  Reaction.ReagentsAndRatio [CompatibleChem] " );
					Area [Chemical] = ChemicalAmount + Reaction.Results [Chemical] * BackUp / Reaction.ReagentsAndRatio [CompatibleChem]; //then adds the result
				}
			}
		}
		foreach (string Chemical in Area.Keys.ToList()) {
			if (!(Area [Chemical] > 0)) { //Cleans out Empty chemicals
				Area.Remove (Chemical);
			}
		}
		if (ReactionBuffer.Count > 0) {
			Area = Reactions (Area, Temperature);
		}
		return (Area);
	}

	private static float SwapFix (float n1, float n2)
	{
		if (n1 > n2) {
			return (n1 / n2);	
		}
		return (n2 / n1);	
	}
}
