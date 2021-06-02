using System;
using Systems.Ai;
using Messages.Server;
using Mirror;
using UnityEngine;

namespace Objects.Research
{
	/// <summary>
	/// This script controls the AI core object, for core AI job logic see AiPlayer.cs
	/// </summary>
	public class AiCore : MonoBehaviour, ICheckedInteractable<HandApply>, IServerInventoryMove
	{
		[SerializeField]
		private bool isInteliCard;
		public bool IsInteliCard => isInteliCard;

		[SerializeField]
		private ItemTrait inteliCardTrait = null;

		private AiPlayer linkedPlayer;
		public AiPlayer LinkedPlayer => linkedPlayer;

		[Server]
		public void SetLinkedPlayer(AiPlayer aiPlayer)
		{
			linkedPlayer = aiPlayer;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			//This interaction only for Ai core
			if (isInteliCard) return false;

			if (DefaultWillInteract.HandApply(interaction, side) == false) return false;

			if (Validations.HasUsedItemTrait(interaction, inteliCardTrait)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			var cardScript = interaction.HandObject.GetComponent<AiCore>();
			if(cardScript == null) return;

			//If we dont have an Ai inside the card try to get one from core
			if (cardScript.linkedPlayer == null)
			{
				TryAddAiToCard(interaction, cardScript);
				return;
			}

			//Else we must have an AI inside card so try to add to core
			TryAddAiToCore(interaction, cardScript);
		}

		private void TryAddAiToCard(HandApply interaction, AiCore cardScript)
		{
			//Check to see if core has AI
			if (linkedPlayer == null)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "There is no Ai inside this core");
				return;
			}

			cardScript.SetLinkedPlayer(linkedPlayer);
			cardScript.LinkedPlayer.ServerSetNewVessel(cardScript.gameObject);
			SetLinkedPlayer(null);
		}

		private void TryAddAiToCore(HandApply interaction, AiCore cardScript)
		{
			if (linkedPlayer != null)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "There is already an Ai inside this core.");
				return;
			}

			linkedPlayer = cardScript.LinkedPlayer;
			linkedPlayer.ServerSetNewVessel(gameObject);
			cardScript.SetLinkedPlayer(null);
		}

		//Move camera to item position/ root container
		public void OnInventoryMoveServer(InventoryMove info)
		{
			if(isInteliCard == false) return;

			//Leaving inventory and to no slot, therefore going to floor
			if (info.ToRootPlayer == null && info.ToSlot == null)
			{
				linkedPlayer.ServerSetCameraLocation(linkedPlayer.gameObject, true);
				return;
			}

			//Going to a new player
			if (info.ToRootPlayer != null)
			{
				linkedPlayer.ServerSetCameraLocation(info.ToRootPlayer.gameObject, true);
				return;
			}

			//Else must be container so follow container
			linkedPlayer.ServerSetCameraLocation(info.ToSlot.GetRootStorage().gameObject, true);
		}
	}
}
