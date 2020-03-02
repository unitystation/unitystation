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

		//only care about interactions targeting us
		if (interaction.TargetObject != gameObject) return false;
		//only try to interact if the user has more than 2 rods
		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Rods) &&
			(interaction.HandObject.GetComponent<Stackable>().Amount < 2)) { return false; }
		return true;
	}
	//SoundManager.PlayNetworkedAtPos("GlassHit", exposure.ExposedWorldPosition.To3Int(), Random.Range(0.9f, 1.1f));
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
