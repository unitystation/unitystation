using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HackingWireInteraction : MonoBehaviour, IPredictedCheckedInteractable<HandApply>
{
	public void ClientPredictInteraction(HandApply interaction)
	{
		Debug.Log("HelpClient");
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter))
		{
			//Destroy(gameObject);
			Debug.Log("Help");
		}
	}

	public void ServerRollbackClient(HandApply interaction)
	{
		
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		Debug.Log("HelpCheck");
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (interaction.TargetObject != gameObject) return false;

		return true;
	}

}
