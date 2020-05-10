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
	
	//The two float values below will likely be identical most of the time, but can't hurt to keep them seperate just in case.
	[Tooltip("Time taken to secure this.")]
		[SerializeField]
		private float secondsToSecure = 4f;
	
	[Tooltip("Time taken to unsecure this.")]
		[SerializeField]
		private float secondsToUnsecure = 4f;

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
		//only try to interact if the user has a wrench in their hand
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
					Chat.AddExamineMsg(interaction.Performer, "A floor must be present to secure the object!");
					return;
				}
				//Need to replace "object" with the target object's name eventually.
				if (!ServerValidations.IsAnchorBlocked(interaction))
				{
					ToolUtils.ServerUseToolWithActionMessages(interaction, secondsToSecure,
						"You start securing the object...",
						$"{interaction.Performer.ExpensiveName()} starts securing the object...",
						"You secure the object.",
						$"{interaction.Performer.ExpensiveName()} secures the object.",
						() => objectBehaviour.ServerSetAnchored(true, interaction.Performer));
				}
			}
			else
			{
				//unsecure it
				ToolUtils.ServerUseToolWithActionMessages(interaction, secondsToUnsecure,
					"You start unsecuring the object...",
					$"{interaction.Performer.ExpensiveName()} starts unsecuring the object...",
					"You unsecure the object.",
					$"{interaction.Performer.ExpensiveName()} unsecures the object.",
					() => objectBehaviour.ServerSetAnchored(false, interaction.Performer));
			}
		}
	}
}
