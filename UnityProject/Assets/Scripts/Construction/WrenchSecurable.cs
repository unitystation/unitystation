using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// PM: A component for securing and unsecuring objects with a wrench. Meant to be generic.
/// Based on Girder.cs. Comments mostly preserved.
/// </summary>
public class WrenchSecurable : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	private RegisterObject registerObject;
	private ObjectBehaviour objectBehaviour;

	private void Start(){
		registerObject = GetComponent<RegisterObject>();
		objectBehaviour = GetComponent<ObjectBehaviour>();
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//start with the default HandApply WillInteract logic.
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//only care about interactions targeting us
		if (interaction.TargetObject != gameObject) return false;
		//only try to interact if the user has a wrench, screwdriver, metal, or plasteel in their hand
		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench)) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.TargetObject != gameObject) return;

		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
		{
			if (objectBehaviour.IsPushable)
			{
				//secure it if there's floor (or table, might want a check for the latter). 
				if (MatrixManager.IsSpaceAt(registerObject.WorldPositionServer, true))
				{
					//Need to find a way to get the name from item attributes.
					Chat.AddExamineMsg(interaction.Performer, "A floor must be present to secure the object!");
					return;
				}

				if (!ServerValidations.IsAnchorBlocked(interaction))
				{
					SoundManager.PlayNetworkedAtPos("Wrench", gameObject.WorldPosServer(), 1f, sourceObj: gameObject);
					objectBehaviour.ServerSetAnchored(true, interaction.Performer);
				}
			}
			else
			{
				//unsecure it
				SoundManager.PlayNetworkedAtPos("Wrench", gameObject.WorldPosServer(), 1f, sourceObj: gameObject);
				objectBehaviour.ServerSetAnchored(false, interaction.Performer);
			}
		}
	}
}
