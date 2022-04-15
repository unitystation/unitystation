using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Objects.Security
{
	[RequireComponent(typeof(AccessRestrictions))]
	[RequireComponent(typeof(Construction.WrenchSecurable))]
	public class SecBarrier : MonoBehaviour, ICheckedInteractable<HandApply>
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

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			//start with the default HandApply WillInteract logic.
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			//Only go through with interaction if correct access restriction
			if (AccessRestrictions.CheckAccess(interaction.Performer))
			{
				//Player needs wrench to interact
				if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
				{
					return false;
				}
			}

			return true;
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
				//Notify player that they are unable to wrench down barrier
				Chat.AddActionMsgToChat(interaction, "You try to wrench down the barrier...",
					"The barrier denies you access.");
			}
			else {
				Chat.AddActionMsgToChat(interaction, "ACCESS DENIED","");
			}
			SoundManager.PlayAtPosition(CommonSounds.Instance.AccessDenied, transform.position);
		}
	}
}