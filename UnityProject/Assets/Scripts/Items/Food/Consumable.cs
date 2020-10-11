using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Item that can be drinked or eaten by player
/// Also supports force feeding other player
/// </summary>
public abstract class Consumable : MonoBehaviour, ICheckedInteractable<HandApply>
{
	public void ServerPerformInteraction(HandApply interaction)
	{
		var targetPlayer = interaction.TargetObject.GetComponent<PlayerScript>();
		if (targetPlayer == null)
		{
			return;
		}

		PlayerScript feeder = interaction.PerformerPlayerScript;
		var feederSlot = feeder.ItemStorage.GetActiveHandSlot();
		if (feederSlot.Item == null)
		{   //Already been eaten or the food is no longer in hand
			return;
		}

		PlayerScript eater = targetPlayer;
		TryConsume(feeder.gameObject, eater.gameObject);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//this item shouldn't be a target
		if (Validations.IsTarget(gameObject, interaction)) return false;

		if (!DefaultWillInteract.Default(interaction, side)) return false;

		return CanBeConsumedBy(interaction.TargetObject);
	}

	/// <summary>
	/// Check thats eater can consume this item
	/// </summary>
	/// <param name="eater">Player that want to eat item</param>
	/// <returns></returns>
	public virtual bool CanBeConsumedBy(GameObject eater)
	{
		//todo: support npc force feeding
		var targetPlayer = eater.GetComponent<PlayerScript>();
		if (targetPlayer == null || targetPlayer.IsDeadOrGhost)
		{
			return false;
		}

		return true;
	}


	/// <summary>
	/// Try to consume this item by eater. Server side only.
	/// </summary>
	/// <param name="eater">Player that want to eat item</param>
	public void TryConsume(GameObject eater)
	{
		TryConsume(eater, eater);
	}

	/// <summary>
	/// Try to consume this item by eater. Server side only.
	/// </summary>
	/// <param name="feeder">Player that feed eater. Can be same as eater.</param>
	/// <param name="eater">Player that is going to eat item</param>
	public abstract void TryConsume(GameObject feeder, GameObject eater);
}
