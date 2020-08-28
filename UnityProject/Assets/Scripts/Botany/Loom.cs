using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

public class Loom : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	[Tooltip("How many bundles of raw cotton/durathread to consume.")]
	[SerializeField]
	private int bundles = 4;

	[Tooltip("How many sheets of fabric to get.")]
	[SerializeField]
	private int sheets = 1;
	
	[Tooltip("When raw cotton bundles are processed, this will be created.")]
	[SerializeField]
	private GameObject cottonSheet;

	[Tooltip("When raw durathread bundles are processed, this will be created.")]
	[SerializeField]
	private GameObject durathreadSheet;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//start with the default HandApply WillInteract logic.
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		GameObject ObjectInHand = interaction.HandObject;

		//only try to interact if the user has at least 4 bundles.
		if (Validations.HasItemTrait(ObjectInHand, CommonTraits.Instance.Loomable)&&
			(ObjectInHand.GetComponent<Stackable>().Amount >= bundles)) { return true; }
		return false;
	}
	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.TargetObject != gameObject) return;

		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Loomable))
		{
			//Converts 4 bundles into fabric
			Conversion(interaction);
		}

	}

	private void Production(GameObject result, HandApply interaction)
	{
		Spawn.ServerPrefab(result, interaction.Performer.WorldPosServer(), count: sheets);
		interaction.HandObject.GetComponent<Stackable>().ServerConsume(bundles);
	}

	private void Conversion(HandApply interaction)
	{
		//Stackable stack = gameObject.GetComponent<Stackable>();
		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.RawCottonBundle))
		{
			ToolUtils.ServerUseToolWithActionMessages(interaction, 3f,
				"You start weaving raw cotton through the loom...",
				$"{interaction.Performer.ExpensiveName()} starts weaving raw cotton through the loom...",
				"You weave the raw cotton into a workable fabric.",
				$"{interaction.Performer.ExpensiveName()} weaves the raw cotton into a workable fabric.",
				() => Production(cottonSheet, interaction));
		}
		else if(Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.RawDurathreadBundle))
		{
			ToolUtils.ServerUseToolWithActionMessages(interaction, 3f,
				"You start weaving raw durathread through the loom...",
				$"{interaction.Performer.ExpensiveName()} starts weaving raw durathread through the loom...",
				"You weave the raw durathread into a workable fabric.",
				$"{interaction.Performer.ExpensiveName()} weaves the raw durathread into a workable fabric.",
				() => Production(durathreadSheet, interaction));
		}
	}

}
