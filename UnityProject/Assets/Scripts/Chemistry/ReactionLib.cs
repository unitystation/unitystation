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
	private const int REAGENT_SIGNIFICANT_DECIMAL_POINTS = 3; // Decimal point all reagents will be rounded to

	/// <summary>
	/// Ok, so you're wondering how to call it <see cref="Reaction"/>
	/// <paramref name="reagents"/> would look something like this {"Reagent":5,"Another reagent":2}
	/// It will return The modified <paramref name="reagents"/>
	/// </summary>
	public static Dictionary<string, float> Reactions(Dictionary<string, float> reagents, float temperature)
	{
		Dictionary<string, float> modifiedReagents = new Dictionary<string, float>(reagents); // Creates a copy that will be returned after all the reactions are done so that the original is not modified
		Reaction reaction;
		while ((reaction = GetValidReaction(modifiedReagents, temperature)) != null)
		{
			DoReaction(modifiedReagents, reaction);
		}

		modifiedReagents = RemoveEmptyReagents(modifiedReagents);

		return modifiedReagents;
	}

	/// <summary>
	/// Removes all reagents from <paramref name="reagents"/> where the value is not more than zero
	/// </summary>
	public static Dictionary<string, float> RemoveEmptyReagents(Dictionary<string, float> reagents)
	{
		Dictionary<string, float> modifiedReagents = new Dictionary<string, float>(reagents);
		foreach (var reagent in reagents)
		{
			if (reagent.Value <= Mathf.Pow(10.0f, -REAGENT_SIGNIFICANT_DECIMAL_POINTS))
			{
				modifiedReagents.Remove(reagent.Key);
			}
		}
		return modifiedReagents;
	}

	private static void DoReaction(Dictionary<string, float> reagents, Reaction reaction)
	{
		float reactionMultiplier = GetReactionMultiplier(reagents, reaction); // Get the multiplier to multiply the reaction by
		RemoveReagents(reagents, reaction, reactionMultiplier);
		AddResults(reagents, reaction, reactionMultiplier);
	}

	private static void AddResults(Dictionary<string, float> reagents, Reaction reaction, float reactionMultiplier)
	{
		foreach (var reagent in reaction.Results)
		{
			if (!reagents.ContainsKey(reagent.Key)) //if result of the reaction doesn't already exist initialize it
			{
				reagents[reagent.Key] = 0;
			}
			reagents[reagent.Key] += reagent.Value * reactionMultiplier; //then add the result adjusted by the leading reagent
		}
	}

	/// <summary>
	/// Does some mathematics to work out how much of each element to take away
	/// </summary>
	private static void RemoveReagents(Dictionary<string, float> reagents, Reaction reaction, float reactionMultiplier)
	{
		foreach (var reagent in reaction.ReagentsAndRatio)
		{
			if (reaction.Catalysts.ContainsKey(reagent.Key))
			{
				continue;
			}
			reagents[reagent.Key] -= reagent.Value * reactionMultiplier; // remove reagents adjusted by the leading reagent
		}
	}


	/// <summary>
	/// Gets a reaction that is possible with <paramref name="reagents"/>
	/// </summary>
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
	/// Checks if all the reagents for the reaction are present
	/// </summary>
	private static bool ReactionComponentsPresent(Dictionary<string, float> reagents, Reaction reaction)
	{
		foreach (string requiredReagent in reaction.ReagentsAndRatio.Keys)
		{
				if (!reagents.ContainsKey(requiredReagent) || reagents[requiredReagent] <= 0)
				{
					return false;
				}
				else if (reaction.Catalysts.ContainsKey(requiredReagent) && reagents[requiredReagent] < reaction.ReagentsAndRatio[requiredReagent]) // If Catalyst, there should be atleast the ammount of catalyst specified in the recipe
				{
					return false;
				}
		}
		return true;
	}

	/// <summary>
	/// Calculate the reaction multiplier to magnify the reaction by
	/// </summary>
	private static float GetReactionMultiplier(Dictionary<string, float> reagents, Reaction reaction)
	{
		KeyValuePair<string, float> leadingReagent = reaction.ReagentsAndRatio.First(); //
		foreach (var reagent in reaction.ReagentsAndRatio)
		{
			if (reagents[reagent.Key] / reagent.Value < reagents[leadingReagent.Key] / leadingReagent.Value) // Find the leading reagent by getting the the least abundant reagent adjusted for ratios
			{
				leadingReagent = reagent;
			}
		}
		return reagents[leadingReagent.Key] / leadingReagent.Value; // Calculate the multiplier
	}

	/// <summary>
	/// Rounds reagents to nearest significant decimal point
	/// </summary>
	public static Dictionary<string, float> RoundReagents(Dictionary<string, float> reagents) // 
	{
		Dictionary<string, float> modifiedReagents = new Dictionary<string, float>(reagents);
		foreach (var reagent in reagents)
		{
			modifiedReagents[reagent.Key] = Mathf.Round(reagents[reagent.Key] * Mathf.Pow(10.0f, REAGENT_SIGNIFICANT_DECIMAL_POINTS)) / Mathf.Pow(10.0f, REAGENT_SIGNIFICANT_DECIMAL_POINTS);
		}
		return modifiedReagents;
	}
}
