using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

public class GSheetToRGSheet : NetworkBehaviour, ICheckedInteractable<HandApply>
{

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//start with the default HandApply WillInteract logic.
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		GameObject ObjectInHand = interaction.HandObject;
		//only care about interactions targeting us
		//only try to interact if the user has more than 2 rods
		if (Validations.HasItemTrait(ObjectInHand, CommonTraits.Instance.Rods)&&
			(ObjectInHand.GetComponent<Stackable>().Amount >= 2)) { return true; }
			//if((interaction.HandObject.GetComponent<Stackable>().Amount < 2)) { return false; }
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
		Spawn.ServerPrefab("ReinforcedGlassSheet", gameObject.WorldPosServer() , count: 1);
		gameObject.GetComponent<Stackable>().ServerConsume(1);
		interaction.HandObject.GetComponent<Stackable>().ServerConsume(2);
	}

}
