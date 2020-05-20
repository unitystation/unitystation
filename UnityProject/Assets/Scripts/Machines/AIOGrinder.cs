using System;
using UnityEngine;
using Mirror;
using Chemistry;
using Chemistry.Components;

/// <summary>
/// A machine into which players can insert certain food items.
/// Upon being inserted, they will be ground into another material.
/// </summary>
[RequireComponent(typeof(ReagentContainer))]
public class AIOGrinder : NetworkBehaviour
{
	/// <summary>
	/// Result of the grinding.
	/// </summary>
	[HideInInspector]
	public string grind;

	private ReagentContainer grinderStorage;

	private int outputAmount;

	private SpriteRenderer spriteRenderer;

	/// <summary>
	/// AudioSource for playing the grinding sound.
	/// </summary>
	private AudioSource audioSourceGrind;

	/// <summary>
	/// Set up the AudioSource.
	/// </summary>
	private void Start()
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		audioSourceGrind = GetComponent<AudioSource>();
	}

	/// <summary>
	/// Count remaining time to microwave previously inserted food.
	/// </summary>
	public void SetServerStackAmount(int stackAmount)
	{
		outputAmount = stackAmount;
	}

	public void ServerSetOutputMeal(string mealName)
	{
		grind = mealName;
	}

	/// <summary>
	/// Grind up the object.
	/// </summary>
	public void GrindFood()
	{
		if (isServer)
		{
			audioSourceGrind.Play();
			grinderStorage = GetComponent<ReagentContainer>();
			grinderStorage.Add(new ReagentMix(CraftingManager.Grind.FindOutputReagent(grind), outputAmount));
		}
		grind = null;
	}
}