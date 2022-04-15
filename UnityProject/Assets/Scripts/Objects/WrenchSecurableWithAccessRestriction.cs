using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Objects
{
	[RequireComponent(typeof(AccessRestrictions))]
	[RequireComponent(typeof(ObjectAttributes))]
	[RequireComponent(typeof(Construction.WrenchSecurable))]
	public class WrenchSecurableWithAccessRestriction : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		private AccessRestrictions accessRestrictions;

		private ObjectAttributes objectAttributes;

		void Awake()
		{
			objectAttributes = GetComponent<ObjectAttributes>();
			accessRestrictions = GetComponent<AccessRestrictions>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			//start with the default HandApply WillInteract logic.
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return !(accessRestrictions.CheckAccess(interaction.Performer) && Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench));
		}

		public void ClientPredictInteraction(HandApply interaction)
		{

		}


		public void ServerRollbackClient(HandApply interaction)
		{

		}

		//invoked when the server recieves the interaction request and WIllinteract returns true
		public async void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
			{
				string objectName = objectAttributes.name;
				//Notify player that they are unable to wrench down barrier
				Chat.AddActionMsgToChat(interaction, "You try to wrench down the " + objectName + ", access is denied", "");
			}
			else {
				Chat.AddActionMsgToChat(interaction, "ACCESS DENIED","");
			}
			SoundManager.PlayAtPosition(CommonSounds.Instance.AccessDenied, transform.position);
		}
	}
}