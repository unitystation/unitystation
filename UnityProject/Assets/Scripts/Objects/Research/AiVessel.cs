using System;
using System.Collections.Generic;
using System.Text;
using Systems.Ai;
using Systems.Construction;
using Systems.Electricity;
using Messages.Server;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

namespace Objects.Research
{
	/// <summary>
	/// This script controls the AI core object, for core AI job logic see AiPlayer.cs
	/// </summary>
	public class AiVessel : MonoBehaviour, ICheckedInteractable<HandApply>, IServerInventoryMove, IExaminable
	{
		[SerializeField]
		private bool isInteliCard;
		public bool IsInteliCard => isInteliCard;

		[SerializeField]
		private ItemTrait inteliCardTrait = null;

		[SerializeField]
		private GameObject aiCoreFramePrefab = null;

		[FormerlySerializedAs("inteliCardSpriteHandler")]
		[SerializeField]
		private SpriteHandler vesselSpriteHandler = null;
		public SpriteHandler VesselSpriteHandler => vesselSpriteHandler;

		[SerializeField]
		private ItemTrait moduleTrait = null;

		private AiPlayer linkedPlayer;
		public AiPlayer LinkedPlayer => linkedPlayer;

		//Whether the Ai is allowed to perform actions, changed when Ai is carded
		private bool allowRemoteAction;
		public bool AllowRemoteAction => allowRemoteAction;

		//Whether the Ai is allowed to use the radio
		private bool allowRadio;
		public bool AllowRadio => allowRadio;

		[Server]
		public void SetLinkedPlayer(AiPlayer aiPlayer)
		{
			linkedPlayer = aiPlayer;
			vesselSpriteHandler.ChangeSprite(aiPlayer == null ? 0 : 1);

			if (isInteliCard)
			{
				UpdateGui();
			}

			//Set name of vessel to Ai name if we can
			GetComponent<Attributes>().ServerSetArticleName(aiPlayer.OrNull()?.PlayerScript.characterSettings.AiName ??
			                                                      (isInteliCard ? "Intelicard" : "Ai Core"));
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			//This interaction only for Ai core
			if (isInteliCard) return false;

			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver)) return true;

			if (Validations.HasItemTrait(interaction, inteliCardTrait)) return true;

			//Allow law changes directly on core
			if (Validations.HasItemTrait(interaction.HandObject, moduleTrait)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			//Deconstruct core
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver))
			{
				TryDeconstruct(interaction);
				return;
			}

			//Upload law to core
			if (Validations.HasItemTrait(interaction, moduleTrait))
			{
				TryUpload(interaction);
				return;
			}

			//Try card/uncard
			var cardScript = interaction.HandObject.GetComponent<AiVessel>();
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

		private void TryAddAiToCard(HandApply interaction, AiVessel cardScript)
		{
			//Check to see if core has AI
			if (linkedPlayer == null)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "There is no Ai inside this core");
				return;
			}

			cardScript.SetLinkedPlayer(linkedPlayer);
			cardScript.LinkedPlayer.ServerSetNewVessel(cardScript.gameObject);
			cardScript.linkedPlayer.ServerSetPermissions(allowRemoteAction, allowRadio);
			SetLinkedPlayer(null);
		}

		private void TryAddAiToCore(HandApply interaction, AiVessel cardScript)
		{
			if (linkedPlayer != null)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "There is already an Ai inside this intelicard.");
				return;
			}

			SetLinkedPlayer(cardScript.LinkedPlayer);
			linkedPlayer.ServerSetNewVessel(gameObject);
			linkedPlayer.ServerSetPermissions(true, true);
			cardScript.SetLinkedPlayer(null);
		}

		private void TryDeconstruct(HandApply interaction)
		{
			//Only deconstruct if no AI inside
			if (linkedPlayer != null)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Transfer the Ai to an intelicard before you deconstruct");
				return;
			}

			ToolUtils.ServerUseToolWithActionMessages(interaction, 5f,
				"You start unscrewing in the glass on the Ai core...",
				$"{interaction.Performer.ExpensiveName()} starts unscrewing the glass on the Ai core...",
				"You unscrew the glass on the Ai core.",
				$"{interaction.Performer.ExpensiveName()} unscrews the glass on the Ai core.",
				() =>
				{
					var newCoreFrame = Spawn.ServerPrefab(aiCoreFramePrefab, SpawnDestination.At(gameObject), 1);

					if (newCoreFrame.Successful)
					{
						newCoreFrame.GameObject.GetComponent<AiCoreFrame>().SetUp();
					}

					_ = Despawn.ServerSingle(gameObject);
				});
		}

		private void TryUpload(HandApply interaction)
		{
			//Only upload if AI inside
			if (linkedPlayer == null)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "No Ai program running on core");
				return;
			}

			//Try to upload law module
			linkedPlayer.UploadLawModule(interaction);
		}

		//Move camera to item position/ root container
		public void OnInventoryMoveServer(InventoryMove info)
		{
			if(isInteliCard == false || LinkedPlayer == null || info.MovedObject.gameObject != gameObject) return;

			//Leaving inventory and to no slot, therefore going to floor
			if (info.ToRootPlayer == null && info.ToSlot == null)
			{
				linkedPlayer.ServerSetCameraLocation(gameObject, true, false);
				return;
			}

			//Going to a new player
			if (info.ToRootPlayer != null)
			{
				linkedPlayer.ServerSetCameraLocation(info.ToRootPlayer.gameObject, true, false);
				return;
			}

			//Else must be container so follow container
			linkedPlayer.ServerSetCameraLocation(info.ToSlot.GetRootStorageOrPlayer().gameObject, true, false);
		}

		public void ChangeRemoteActionState(bool newState)
		{
			allowRemoteAction = newState;

			if (linkedPlayer != null)
			{
				linkedPlayer.ServerSetPermissions(allowRemoteAction, allowRadio);
			}

			UpdateGui();
		}

		public void ChangeRadioState(bool newState)
		{
			allowRadio = newState;

			if (linkedPlayer != null)
			{
				linkedPlayer.ServerSetPermissions(allowRemoteAction, allowRadio);
			}

			UpdateGui();
		}

		public void UpdateGui()
		{
			var peppers = NetworkTabManager.Instance.GetPeepers(gameObject, NetTabType.InteliCard);
			if(peppers.Count == 0) return;

			List<ElementValue> valuesToSend = new List<ElementValue>();

			valuesToSend.Add(new ElementValue() { Id = "SliderRemote", Value = Encoding.UTF8.GetBytes((allowRemoteAction ? 1 * 100 : 0).ToString()) });
			valuesToSend.Add(new ElementValue() { Id = "SliderRadio", Value = Encoding.UTF8.GetBytes((allowRadio ? 1 * 100 : 0).ToString()) });
			valuesToSend.Add(new ElementValue() { Id = "SliderIntegrity", Value = Encoding.UTF8.GetBytes(LinkedPlayer == null ? "0" : LinkedPlayer.Integrity.ToString()) });

			var lawText = "This intelicard holds no Ai";

			if (LinkedPlayer != null)
			{
				lawText = LinkedPlayer.IsPurging ? "<color=red>Is Being Purged...\n</color>" : "";
				lawText += LinkedPlayer.GetLawsString();
			}

			valuesToSend.Add(new ElementValue() { Id = "LawText", Value = Encoding.UTF8.GetBytes(lawText) });

			valuesToSend.Add(new ElementValue() { Id = "PurgeText", Value = Encoding.UTF8.GetBytes(LinkedPlayer == null ?
				"No AI to Purge" : LinkedPlayer.IsPurging ?
					"Stop Purging" : "Start Purging") });

			// Update all UI currently opened.
			TabUpdateMessage.SendToPeepers(gameObject, NetTabType.InteliCard, TabAction.Update, valuesToSend.ToArray());
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			if (LinkedPlayer != null && linkedPlayer.connectionToClient == null)
			{
				//Player is disconnected
				return "Core.exe has stopped responding! NTOS is searching for a solution to the problem...";
			}

			if (isInteliCard == false && TryGetComponent<APCPoweredDevice>(out var apc) && apc.RelatedAPC == null)
			{
				return "Ai core is not connected to an APC!";
			}

			return LinkedPlayer == null ? "Contains no activate Artificial Intelligence" : $"Contains the Artificial Intelligence: {LinkedPlayer.gameObject.ExpensiveName()}";
		}
	}
}
