using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class Reaction
{
	public string Name { get; set; }

	/// <summary>
	/// Example "Chemical Name":2, "Chemical Name2":1, Will return the amount specified if all Chemicals are present for ReagentsAndRatio 
	/// </summary>
	public Dictionary<string, float> Results { get; set; }

	/// <summary>
	/// Example "Chemical Name":2, "Chemical Name2":1, note if there is a catalyst it has to be specified in here as well
	/// </summary>
	public Dictionary<string, float> ReagentsAndRatio { get; set; }

	/// <summary>
	/// Example "Chemical Name":0, "Chemical Name2":0, note catalysts Have to be specified in ReagentsAndRatio
	/// </summary>
	public Dictionary<string, float> Catalysts { get; set; }

	/// <summary>
	/// is the minimum temperature that the reaction will happen at
	/// </summary>
	public float MinimumTemperature { get; set; }
}

public static class ChemistryGlobals
{
	public static bool isInitialised = false;
	public static Dictionary<string, HashSet<Reaction>> reactionsStoreDictionary = new Dictionary<string, HashSet<Reaction>>();
	public static List<Reaction> reactions = new List<Reaction>();
}

public static class Initialization
{
	public static void Run()
	{
		JsonImportInitialization();
		ChemInitialization();
	}

	private static void JsonImportInitialization()
	{
		var json = (Resources.Load(@"Metadata\Reactions") as TextAsset).ToString();
		var reactions = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
		foreach (var reaction in reactions)
		{
			ChemistryGlobals.reactions.Add(new Reaction
			{
				Name             = reaction["Name"].ToString(),
				Results          = JsonConvert.DeserializeObject<Dictionary<string, float>>(reaction["Results"].ToString()),
				ReagentsAndRatio = JsonConvert.DeserializeObject<Dictionary<string, float>>(reaction["Reagents_and_ratio"].ToString()),

				Catalysts = reaction.ContainsKey("Catalysts") ?
					JsonConvert.DeserializeObject<Dictionary<string, float>>(reaction["Catalysts"].ToString()) :
					new Dictionary<string, float>(),

				MinimumTemperature = reaction.ContainsKey("Minimum_temperature") ?
					float.Parse(reaction["Minimum_temperature"].ToString()) :
					0.0f
			});
		}
		Logger.Log("JsonImportInitialization done!", Category.Chemistry);
	}

	private static void ChemInitialization()
	{
		foreach(var reaction in ChemistryGlobals.reactions)
		{
			foreach (string chemical in reaction.ReagentsAndRatio.Keys)
			{
				if (!ChemistryGlobals.reactionsStoreDictionary.ContainsKey(chemical))
				{
					ChemistryGlobals.reactionsStoreDictionary[chemical] = new HashSet<Reaction>();
				}
				ChemistryGlobals.reactionsStoreDictionary[chemical].Add(reaction);
			}
		}
	}
}

public static class Calculations
{
	/// <summary>
	/// Ok, so you're wondering how to call it <see cref="Reaction"/>
	/// <paramref name="area"/> would look something like this {"Chemical":5,"Another chemical":2}
	/// It will return The modified <paramref name="area"/>
	/// </summary>
	public static Dictionary<string, float> Reactions(Dictionary<string, float> area, float Temperature)
	{
		var reactionBuffer = ValidReactions(area, Temperature).ToArray(); //Force evaluate or else it will throw a InvalidOperationException for IEnumerable modification

		//Logger.Log (reactionBuffer.Count() + " < ReactionBuffer");
		foreach (var reaction in reactionBuffer)
		{
			var compatibleChem = CompatibleChemicals(area, reaction).FirstOrDefault();
			if (compatibleChem == null) continue;

			//Logger.Log (area[compatibleChem] + " < Area [CompatibleChem ");
			DoReaction(area, reaction, compatibleChem);
		}

		RemoveEmptyChemicals(area);

		if (reactionBuffer.Any())
		{
			area = Reactions(area, Temperature);
		}
		return area;
	}

	private static void DoReaction(Dictionary<string, float> area, Reaction reaction, string compatibleChem)
	{
		var originalAmount = area[compatibleChem];
		RemoveReagents(area, reaction, compatibleChem);
		AddResults(area, reaction, compatibleChem, originalAmount);
	}

	private static void AddResults(Dictionary<string, float> area, Reaction reaction, string compatibleChem, float originalAmount)
	{
		foreach (string chemical in reaction.Results.Keys)
		{
			if (!area.ContainsKey(chemical)) { area[chemical] = 0; } //if result of the reaction doesn't already exist initialize it

			area[chemical] += reaction.Results[chemical] * originalAmount / reaction.ReagentsAndRatio[compatibleChem]; //then adds the result
		}
	}

	/// <summary>
	/// Does some mathematics to work out how much of each element to take away
	/// </summary>
	private static void RemoveReagents(Dictionary<string, float> area, Reaction reaction, string compatibleChem)
	{
		var originalAmount = area[compatibleChem];
		var reAndRa = reaction.ReagentsAndRatio;

		foreach (string chemical in reaction.ReagentsAndRatio.Keys)
		{
			if (reaction.Catalysts.ContainsKey(chemical)) { continue; }

			area[chemical] -= originalAmount * SwapFix(reAndRa[compatibleChem], reAndRa[chemical]);
		}
	}

	/// <summary>
	/// Finds the best chemical to do the reaction formula
	/// </summary>
	private static IEnumerable<string> CompatibleChemicals(Dictionary<string, float> area, Reaction reaction)
	{
		foreach (string chemical in reaction.ReagentsAndRatio.Keys)
		{
			if (!ReactionCompatible(area, reaction, chemical)) { continue; }

			yield return chemical;
		}
	}

	/// <summary>
	/// Removes all chemicals from <paramref name="area"/> where the value is not more than zero
	/// </summary>
	private static void RemoveEmptyChemicals(Dictionary<string, float> area)
	{
		foreach (var chemical in area.ToArray()) //ToArray neccesary because we can't modify the IEnumerable we are iterating over
		{
			if (chemical.Value > 0) { continue; }

			area.Remove(chemical.Key);
		}
	}

	/// <summary>
	/// Checks if ratios work out
	/// </summary>
	private static bool ReactionCompatible(Dictionary<string, float> area, Reaction reaction, string chemicalKey)
	{
		var reRa = reaction.ReagentsAndRatio;

		var areaChem = area[chemicalKey];
		var reRaChem = reRa[chemicalKey];

		foreach (var subChemicalKey in reRa.Keys)
		{
			var areaSub = area[subChemicalKey];
			var reRaSub = reRa[subChemicalKey];

			if (areaChem * (reRaSub / reRaChem) > areaSub) return false;
			if (areaChem <= 0) return false;
		}
		return true;
	}

	private static IEnumerable<Reaction> ValidReactions(Dictionary<string, float> area, float temperature)
	{
		foreach (string chemical in area.Keys)
		{
			if (!ChemistryGlobals.reactionsStoreDictionary.ContainsKey(chemical)) { continue; }

			foreach (var reaction in ChemistryGlobals.reactionsStoreDictionary[chemical])
			{ //so A list of every reaction that that chemical can be in
				if (temperature < reaction.MinimumTemperature) { continue; }
				if (!ReactionComponentsPresent(area, reaction)) { continue; }

				yield return reaction; //then adds it
			}
		}
	}

	/// <summary>
	/// Checks if all the other chemicals are in
	/// </summary>
	private static bool ReactionComponentsPresent(Dictionary<string, float> area, Reaction reaction)
	{
		foreach (string requiredChemical in reaction.ReagentsAndRatio.Keys)
		{
			if (!area.ContainsKey(requiredChemical))
			{
				return false;
			}
		}
		return true;
	}

	private static float SwapFix(float n1, float n2)
	{
		if (n1 > n2)
		{
			return (n1 / n2);
		}
		return (n2 / n1);
	}
}
