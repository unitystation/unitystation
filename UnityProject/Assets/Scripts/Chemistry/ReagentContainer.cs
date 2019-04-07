using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ReagentContainer : Container {
	public float CurrentCapacity { get; private set; }
	public List<string> Chemicals; //Specify chemical
	public List<float> Amounts;  //And how much

	void Start() //Initialise the contents if there are any
	{
		if(Chemicals == null)
		{
			return;
		}
		for (int i = 0; i < Chemicals.Count; i++)
		{
			Contents[Chemicals[i]] = Amounts[i];
		}
		CurrentCapacity = AmountOfReagents(Contents);
	}

	public void AddReagents(Dictionary<string, float> reagents, float temperatureContainer) //Automatic overflow If you Don't want to lose check before adding
	{
		CurrentCapacity = AmountOfReagents(Contents);
		float totalToAdd = AmountOfReagents(reagents);
		if (CurrentCapacity + totalToAdd > MaxCapacity)
		{
			Logger.Log("The container overflows spilling the excess");
		}
		float divideAmount = Math.Min((MaxCapacity - CurrentCapacity), totalToAdd) / totalToAdd;
		foreach (KeyValuePair<string, float> Chemical in reagents)
		{
			float amountToAdd = Chemical.Value * divideAmount;
			Contents[Chemical.Key] = (Contents.TryGetValue(Chemical.Key, out float val) ? val : 0f) + amountToAdd;
		}
		float oldCapacity = CurrentCapacity;
		Contents = Calculations.Reactions(Contents, Temperature);
		RemoveEmptyChemicals();
		CurrentCapacity = AmountOfReagents(Contents);
		totalToAdd = ((CurrentCapacity - oldCapacity) * temperatureContainer) + (oldCapacity * Temperature);
		Temperature = totalToAdd / CurrentCapacity;
	}

	public void MoveReagentsTo(int amount, ReagentContainer target = null)
	{
		CurrentCapacity = AmountOfReagents(Contents);
		float toMove = target == null ? amount : Math.Min(target.MaxCapacity - target.CurrentCapacity, amount);
		float divideAmount = toMove / CurrentCapacity;
		var transfering = Contents.ToDictionary(
			chemical => chemical.Key,
			chemical => divideAmount > 1 ? chemical.Value : (chemical.Value * divideAmount)
		);
		foreach(var chemical in transfering)
		{
			Contents[chemical.Key] -= chemical.Value;
		}
		RemoveEmptyChemicals();
		CurrentCapacity = AmountOfReagents(Contents);
		target?.AddReagents(transfering, Temperature);
	}

	public float AmountOfReagents(Dictionary<string, float> Reagents) => Reagents.Select(chemical => chemical.Value).Sum();

	private void RemoveEmptyChemicals()
	{
		var toRemove = Contents.Where(chemical => chemical.Value < 0.0000001f).Select(chemical => chemical.Key).ToList();
		toRemove.ForEach(chemical => Contents.Remove(chemical));
	}

	[ContextMethod("Contents", "Science_flask")]
	public void LogReagents()
	{
		foreach (var chemical in Contents)
		{
			Logger.Log(chemical.Key + " at " + chemical.Value.ToString(), Category.Chemistry);
		}
	}

	[ContextMethod("Add to", "Pour_into")]
	public void AddTo()
	{
		var transfering = new Dictionary<string, float>
		{
			["ethanol"] = 10f,
			["toxin"] = 15f,
			["ammonia"] = 5f
		};
		AddReagents(transfering, 20f);
	}

	[ContextMethod("Pour out", "Pour_away")]
	public void RemoveSome() => MoveReagentsTo(10);
}
