using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

public class GSheetToRGSheet : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	[Header("How many Rods and Glass Sheets to get Reinforced Glass Sheets")]
	[Tooltip("How many rods to consume.")]
	[SerializeField]
	private int rods = 2;

	[Tooltip("How many glass sheets to convert.")]
	[SerializeField]
	private int sheetsGlass = 1;

	[Tooltip("How many reinforced glass sheets to get.")]
	[SerializeField]
	private int sheetsReinforcedGlass = 1;
	
	[Tooltip("What kind of reinforced glass to make.")]
	public GameObject sheetsReinforcedGlassType;


	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//start with the default HandApply WillInteract logic.
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		GameObject ObjectInHand = interaction.HandObject;

		//only try to interact if the user has more than 2 rods
		if (Validations.HasItemTrait(ObjectInHand, CommonTraits.Instance.Rods)&&
			(ObjectInHand.GetComponent<Stackable>().Amount >= rods)) { return true; }
		return false;
	}
	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.TargetObject != gameObject) return;

		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Rods))
		{
			//Turn Glass sheet and 2 Rods into Reinforced Glass Sheet
			convertGlass(interaction);
		}

	}
	[Server]
	private void convertGlass(HandApply interaction)
	{
		Stackable stack = gameObject.GetComponent<Stackable>();
		if (stack.Amount >= sheetsGlass)
		{
			Spawn.ServerPrefab(sheetsReinforcedGlassType, interaction.Performer.WorldPosServer(), count: sheetsReinforcedGlass);
			stack.ServerConsume(sheetsGlass);
			interaction.HandObject.GetComponent<Stackable>().ServerConsume(rods); ;
		}
	}

}
