using System.Collections.Generic;
using UnityEngine;


public class ChemistryInitializationTest : MonoBehaviour
{

	// Use this for initialization
	void Awake ()
	{
		if (!(ChemistryGlobals.IsInitialised)) {
			Initialization.run ();
			ChemistryGlobals.IsInitialised = true;
			ChemistryTest ();
		}
	}



	public static void ChemistryTest ()
	{
		var reactions = ChemistryGlobals.reactions;
		for (var i = 0; i < reactions.Count; i++) {
			var OriginDictionary = reactions [i].ReagentsAndRatio;
			Dictionary<string,float> RequiredChemicals = new Dictionary<string,float> ();
			foreach (KeyValuePair<string, float> pair in OriginDictionary) {
				RequiredChemicals.Add (pair.Key, pair.Value);
			}
			var Temperature = reactions [i].MinimumTemperature;
			var ReturnedResults = Calculations.Reactions (RequiredChemicals, Temperature);

			foreach (string Chemical in reactions[i].Results.Keys) {
				if (ReturnedResults.ContainsKey (Chemical)) {
					if (!(reactions [i].Catalysts.ContainsKey (Chemical))) {
						if (!(ReturnedResults [Chemical] == reactions [i].Results [Chemical])) {
							Logger.LogWarning (reactions [i].Name + " Did not produce the right amount of chemical,  " + Chemical, Category.Chemistry);
						}
					}
				} else {
					Logger.LogWarning (reactions [i].Name + " Returned reagent does not contain the results of reaction. It is missing " + Chemical, Category.Chemistry);
				}

			}
			foreach (string Chemical in reactions[i].ReagentsAndRatio.Keys) {
				if (!(reactions [i].Catalysts.ContainsKey (Chemical))) {
					if (ReturnedResults.ContainsKey (Chemical)) {
						Logger.LogWarning (reactions [i].Name + " Residual chemical from reaction, " + Chemical + ReturnedResults [Chemical].ToString (), Category.Chemistry);
					}
				}
			}
			Logger.LogTrace ("Successfully tested" + reactions [i].Name, Category.Chemistry);
		}
		Logger.Log ("Successfully tested all recipes", Category.Chemistry);
	}
}