using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class Reaction
{
	public string Name { get; set; }

	/// <summary>
	/// Example "Reagent Name":2, "Reagent Name2":1, Will return the amount specified if all Reagents are present for ReagentsAndRatio 
	/// </summary>
	public Dictionary<string, float> Results { get; set; }

	/// <summary>
	/// Example "Reagent Name":2, "Reagent Name2":1, note if there is a catalyst it has to be specified in here as well
	/// </summary>
	public Dictionary<string, float> ReagentsAndRatio { get; set; }

	/// <summary>
	/// Example "Reagent Name":0, "Reagent Name2":0, note catalysts Have to be specified in ReagentsAndRatio
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
				Name = reaction["Name"].ToString(),
				Results = JsonConvert.DeserializeObject<Dictionary<string, float>>(reaction["Results"].ToString()),
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
		foreach (var reaction in ChemistryGlobals.reactions)
		{
			foreach (string reagent in reaction.ReagentsAndRatio.Keys)
			{
				if (!ChemistryGlobals.reactionsStoreDictionary.ContainsKey(reagent))
				{
					ChemistryGlobals.reactionsStoreDictionary[reagent] = new HashSet<Reaction>();
				}
				ChemistryGlobals.reactionsStoreDictionary[reagent].Add(reaction);
			}
		}
	}
}

public static class Calculations
{
	/// <summary>
	/// Decimal point all reagent will be rounded to
	/// </summary>
	private const int REAGENT_SIGNIFICANT_DECIMAL_POINTS = 3;

	/// <summary>
	/// Ok, so you're wondering how to call it <see cref="Reaction"/>
	/// <paramref name="reagents"/> would look something like this {"Reagent":5,"Another reagent":2}
	/// It will return The modified <paramref name="reagents"/>
	/// </summary>
	public static Dictionary<string, float> Reactions(Dictionary<string, float> reagents, float Temperature)
	{
		bool reactionsFinished = false;

		do
		{
			var nextReaction = GetValidReaction(reagents, Temperature); // Get next possible reaction
			if (nextReaction != null)
			{
				DoReaction(reagents, nextReaction);
			}
			else
			{
				reactionsFinished = true;
			}

		} while (!reactionsFinished); // Do reactions until all have finished

		return RoundReagents(reagents); // Round reagents to a significant decimal point
	}

	/// <summary>
	/// Removes all reagents from <paramref name="reagents"/> where the value is not more than zero
	/// </summary>
	public static Dictionary<string, float> RemoveEmptyReagents(Dictionary<string, float> reagents)
	{
		foreach (var reagent in reagents.ToArray()) //ToArray neccesary because we can't modify the IEnumerable we are iterating over
		{
			if (reagent.Value <= 0)
			{
				reagents.Remove(reagent.Key);
			}
		}
		return reagents;
	}

	private static void DoReaction(Dictionary<string, float> reagents, Reaction reaction)
	{
		var leadingReagent = GetLeadingReagent(reagents, reaction); // Get a reagent to lead the reaction
		float leadingQuantity = reagents[leadingReagent.Key]; // Store the original reaction quantity for reference as it will be removed in the reaction process
		RemoveReagents(reagents, reaction, leadingReagent, leadingQuantity);
		AddResults(reagents, reaction, leadingReagent, leadingQuantity);
	}

	private static void AddResults(Dictionary<string, float> reagents, Reaction reaction, KeyValuePair<string, float> leadingReagent, float leadingQuantity)
	{
		foreach (var reagent in reaction.Results)
		{
			if (!reagents.ContainsKey(reagent.Key)) //if result of the reaction doesn't already exist initialize it
			{
				reagents[reagent.Key] = 0;
			}
			reagents[reagent.Key] += leadingQuantity / leadingReagent.Value * reagent.Value; //then add the result adjusted by the leading reagent
		}
	}

	/// <summary>
	/// Does some mathematics to work out how much of each element to take away
	/// </summary>
	private static void RemoveReagents(Dictionary<string, float> reagents, Reaction reaction, KeyValuePair<string, float> leadingReagent, float leadingQuantity)
	{
		foreach (var reagent in reaction.ReagentsAndRatio)
		{
			if (reaction.Catalysts.ContainsKey(reagent.Key))
			{
				continue;
			}
			reagents[reagent.Key] -= leadingQuantity / leadingReagent.Value * reagent.Value; // remove reagents adjusted by the leading reagent
		}
	}

	private static Reaction GetValidReaction(Dictionary<string, float> reagents, float temperature)
	{
		foreach (string reagent in reagents.Keys)
		{
			if (!ChemistryGlobals.reactionsStoreDictionary.ContainsKey(reagent))
			{
				continue;
			}

			foreach (var reaction in ChemistryGlobals.reactionsStoreDictionary[reagent])
			{ 
				if (temperature >= reaction.MinimumTemperature && ReactionComponentsPresent(reagents, reaction))
				{
					return reaction;
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Checks if all the other reagents are in
	/// </summary>
	private static bool ReactionComponentsPresent(Dictionary<string, float> reagents, Reaction reaction)
	{
		foreach (string requiredReagent in reaction.ReagentsAndRatio.Keys)
		{
			if (!reagents.ContainsKey(requiredReagent) || reagents[requiredReagent] <= 0)
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Find which reagent is going to be used up in the reaction first so it can lead the reaction
	/// </summary>
	private static KeyValuePair<string, float> GetLeadingReagent(Dictionary<string, float> reagents, Reaction reaction)
	{
		KeyValuePair<string, float> leadingReagent = reaction.ReagentsAndRatio.First();
		foreach (var reagent in reaction.ReagentsAndRatio)
		{
			if (reagents[reagent.Key] / reagent.Value < reagents[leadingReagent.Key] / leadingReagent.Value) // Get the the least abundant reagent adjusted for ratios
			{
				leadingReagent = reagent;
			}
		}
		return leadingReagent;
	}

	/// <summary>
	/// Rounds reagents to nearest significant decimal point
	/// </summary>
	private static Dictionary<string, float> RoundReagents(Dictionary<string, float> reagents) // 
	{
		foreach (var reagent in reagents.ToArray())
		{
			reagents[reagent.Key] = Mathf.Round(reagents[reagent.Key] * Mathf.Pow(10.0f, REAGENT_SIGNIFICANT_DECIMAL_POINTS)) / Mathf.Pow(10.0f, REAGENT_SIGNIFICANT_DECIMAL_POINTS);
		}
		return reagents;
	}
}
