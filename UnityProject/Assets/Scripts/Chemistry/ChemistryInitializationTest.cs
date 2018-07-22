using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ChemistryInitializationTest : MonoBehaviour {

		// Use this for initialization
		void Awake () {
			Chemistry.Initialization.run();
			ChemistryTest();
		}


	
		public static void ChemistryTest()
		{
			var List_of_reactions = Chemistry.Globals.List_of_reactions;
			for (var i = 0; i < List_of_reactions.Count; i++) 
			{
				var OriginDictionary = List_of_reactions [i].Reagents_and_ratio;
				Dictionary<string,float> RequiredChemicals = new Dictionary<string,float>();
				foreach (KeyValuePair<string, float> pair in OriginDictionary)
				{
					RequiredChemicals.Add(pair.Key, pair.Value);
				}
				var Temperature = List_of_reactions [i].Minimum_temperature;
				var ReturnedResults = Chemistry.Calculations.Reactions (RequiredChemicals, Temperature);

				foreach (string Chemical in List_of_reactions[i].Results.Keys) 
				{
					if (ReturnedResults.ContainsKey(Chemical))
					{
					if (!(List_of_reactions [i].Catalysts.ContainsKey (Chemical)))
						{
							if (!(ReturnedResults [Chemical] == List_of_reactions [i].Results [Chemical]))
							{
								Logger.LogWarning (List_of_reactions [i].Name + " Did not produce the right amount of chemical,  " + Chemical, Category.Chemistry);
							}
						}
					}
					else
					{
						Logger.LogWarning(List_of_reactions[i].Name +" Returned reagent does not contain the results of reaction. It is missing " + Chemical,Category.Chemistry);
					}

				}
				foreach (string Chemical in List_of_reactions[i].Reagents_and_ratio.Keys) 
				{
					if (!(List_of_reactions[i].Catalysts.ContainsKey(Chemical))) 
					{
						if (ReturnedResults.ContainsKey(Chemical))
						{
						Logger.LogWarning(List_of_reactions[i].Name + " Residual chemical from reaction, " + Chemical + ReturnedResults[Chemical].ToString(),Category.Chemistry);
						}
					}
				}
				Logger.LogTrace ( "Successfully tested" + List_of_reactions [i].Name, Category.Chemistry);
			}
		Logger.Log ( "Successfully tested all recipes", Category.Chemistry);
		}
	}