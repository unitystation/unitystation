using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

public class RodsToMetal : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	[Header("How many Metal Sheets you get from Rods.")]
	[Tooltip("How many metal sheets.")]
	[SerializeField]
	private int metal = 1;

	[Tooltip("How many Rods needed.")]
	[SerializeField]
	private int rods = 2;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		GameObject ObjectInHand = interaction.HandObject;

		//only active welder will transform rods
		if (Validations.HasItemTrait(ObjectInHand, CommonTraits.Instance.Welder) &&
			Validations.HasUsedActiveWelder(interaction)) return true;
		return false;
	}
	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.TargetObject != gameObject) return;

		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Welder))
		{
			//Turn Rods to Metal
			convertRods(interaction);
		}

	}
	[Server]
	private void convertRods(HandApply interaction)
	{
		Stackable stack = gameObject.GetComponent<Stackable>();
		if (stack.Amount >= rods)
		{
			Spawn.ServerPrefab("Metal", interaction.Performer.WorldPosServer(), count: metal);
			stack.ServerConsume(rods);
		}
	}

}