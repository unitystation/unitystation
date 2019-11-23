using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(RightClickAppearance))]
public class ReagentContainer : Container, IRightClickable {
	public float CurrentCapacity { get; private set; }
	public List<string> Reagents; //Specify reagent
	public List<float> Amounts;  //And how much

	private RegisterTile registerTile;

	private void Awake()
	{
		GetComponent<IItemAttributes>().AddTrait(CommonTraits.Instance.ReagentContainer);
	}

	void Start() //Initialise the contents if there are any
	{
		if(Reagents == null)
		{
			return;
		}
		for (int i = 0; i < Reagents.Count; i++)
		{
			Contents[Reagents[i]] = Amounts[i];
		}
		CurrentCapacity = AmountOfReagents(Contents);

		registerTile = GetComponent<RegisterTile>();
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		var result = RightClickableResult.Create()
			//contents can always be viewed
			.AddElement("Contents", LogReagents);

		//Pour / add can only be done if in reach
		if ( PlayerScript.IsInReach(registerTile, PlayerManager.LocalPlayerScript.registerTile, false))
		{
			result.AddElement("PourOut", RemoveSome)
				.AddElement("AddTo", AddTo);
		}

		return result;
	}

	public void AddReagents(Dictionary<string, float> reagents, float temperatureContainer) //Automatic overflow If you Don't want to lose check before adding
	{
		CurrentCapacity = AmountOfReagents(Contents);
		float totalToAdd = AmountOfReagents(reagents);
		if (CurrentCapacity + totalToAdd > MaxCapacity)
		{
			Logger.Log("The container overflows spilling the excess", Category.Chemistry);
		}
		float divideAmount = Math.Min((MaxCapacity - CurrentCapacity), totalToAdd) / totalToAdd;
		foreach (KeyValuePair<string, float> reagent in reagents)
		{
			float amountToAdd = reagent.Value * divideAmount;
			Contents[reagent.Key] = (Contents.TryGetValue(reagent.Key, out float val) ? val : 0f) + amountToAdd;
		}
		float oldCapacity = CurrentCapacity;
		Contents = Calculations.Reactions(Contents, Temperature);
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
			reagent => reagent.Key,
			reagent => divideAmount > 1 ? reagent.Value : (reagent.Value * divideAmount)
		);
		foreach(var reagent in transfering)
		{
			Contents[reagent.Key] -= reagent.Value;
		}
		Contents = Calculations.RemoveEmptyReagents(Contents);
		CurrentCapacity = AmountOfReagents(Contents);
		target?.AddReagents(transfering, Temperature);
	}

	public float AmountOfReagents(Dictionary<string, float> Reagents) => Reagents.Select(reagent => reagent.Value).Sum();

	private void LogReagents()
	{
		foreach (var reagent in Contents)
		{
			Logger.Log(reagent.Key + " at " + reagent.Value.ToString(), Category.Chemistry);
		}
	}

	private void AddTo()
	{
		var transfering = new Dictionary<string, float>
		{
			["ethanol"] = 10f,
			["toxin"] = 15f,
			["ammonia"] = 5f
		};
		AddReagents(transfering, 20f);
	}

	private void RemoveSome() => MoveReagentsTo(10);
}
