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
		public AccessRestrictions AccessRestrictions
		{
			get
			{
				if (accessRestrictions == false)
				{
					accessRestrictions = GetComponent<AccessRestrictions>();
				}
				return accessRestrictions;
			}
		}

		private ObjectAttributes objectAttributes;
		public ObjectAttributes ObjectAttributes
		{
			get
			{
				if (objectAttributes == false)
				{
					objectAttributes = GetComponent<ObjectAttributes>();
				}
				return objectAttributes;
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			//start with the default HandApply WillInteract logic.
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			return !(AccessRestrictions.CheckAccess(interaction.Performer) && Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench));
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
				string objectName = ObjectAttributes.name;
				//Notify player that they are unable to wrench down barrier
				Chat.AddActionMsgToChat(interaction, "You try to wrench down the " + objectName + "...",
					"The " + objectName + " denies you access.");
			}
			else {
				Chat.AddActionMsgToChat(interaction, "ACCESS DENIED","");
			}
			SoundManager.PlayAtPosition(CommonSounds.Instance.AccessDenied, transform.position);
		}
	}
}