using System.Collections.Generic;
using UnityEngine;


public class ChemistryInitializationTest : MonoBehaviour
{

	// Use this for initialization
	void Awake ()
	{
		if (!(ChemistryGlobals.isInitialised)) {
			Initialization.Run ();
			ChemistryGlobals.isInitialised = true;
			ChemistryTest ();
		}
	}

	public static void ChemistryTest ()
	{
		var reactions = ChemistryGlobals.reactions;
		for (var i = 0; i < reactions.Count; i++) {
			var OriginDictionary = reactions [i].ReagentsAndRatio;
			Dictionary<string,float> RequiredReagents = new Dictionary<string,float> ();
			foreach (KeyValuePair<string, float> pair in OriginDictionary) {
				RequiredReagents.Add (pair.Key, pair.Value);
			}
			var Temperature = reactions [i].MinimumTemperature;
			var ReturnedResults = Calculations.Reactions (RequiredReagents, Temperature);

			foreach (string reagent in reactions[i].Results.Keys) {
				if (ReturnedResults.ContainsKey (reagent)) {
					if (!(reactions [i].Catalysts.ContainsKey (reagent))) {
						if (!(ReturnedResults [reagent] == reactions [i].Results [reagent])) {
							Logger.LogWarning (reactions [i].Name + " Did not produce the right amount of reagent,  " + reagent, Category.Chemistry);
						}
					}
				} else {
					Logger.LogWarning (reactions [i].Name + " Returned reagent does not contain the results of reaction. It is missing " + reagent, Category.Chemistry);
				}
			}

			foreach (string reagent in reactions[i].ReagentsAndRatio.Keys) {
				if (!(reactions [i].Catalysts.ContainsKey (reagent))) {
					if (ReturnedResults.ContainsKey (reagent)) {
						Logger.LogWarning (reactions [i].Name + " Residual reagent from reaction, " + reagent + ReturnedResults [reagent].ToString (), Category.Chemistry);
					}
				}
			}
			Logger.LogTrace ("Successfully tested" + reactions [i].Name, Category.Chemistry);
		}
		Logger.Log ("Successfully tested all recipes", Category.Chemistry);
	}
}