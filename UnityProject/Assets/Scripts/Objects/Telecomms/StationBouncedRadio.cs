using System;
using System.Collections;
using System.Collections.Generic;
using Communications;
using Managers;
using UnityEngine;
using Util;
using Items;

namespace Objects.Telecomms
{
	public class StationBouncedRadio : SignalReceiver, ICheckedInteractable<HandApply>, IRightClickable
	{
		[SerializeField] private int hearableRange = 3;
		[SerializeField] private LayerMask layerToCheck;
		[SerializeField] private bool canChangeEncryption = true;
		private LocalRadioListener radioListener;
		private bool broadcastToNearbyTiles = true;
		private Pickupable pickupable;
		private ItemStorage keyStorage;
		private bool isScrewed = true;



		public bool BroadcastToNearbyTiles
		{
			get => broadcastToNearbyTiles;
			set => broadcastToNearbyTiles = value;
		}

		private void Awake()
		{
			keyStorage = GetComponent<ItemStorage>();
			pickupable = GetComponent<Pickupable>();
			radioListener = GetComponent<LocalRadioListener>();
			UpdateManager.Add(SyncFrequencyWithListner, 0.5f);
		}

		private void SyncFrequencyWithListner()
		{
			Frequency = radioListener.Frequency;
		}

		public override void ReceiveSignal(SignalStrength strength, SignalEmitter responsibleEmitter, ISignalMessage message = null)
		{
			if (message is RadioMessage msg)
			{
				if(broadcastToNearbyTiles) {ShowChatterToNearbyPeople(msg);}
				if (pickupable.ItemSlot == null) return;
				if(pickupable.ItemSlot.Player == null) return;
				Chat.AddExamineMsg(pickupable.ItemSlot.ItemStorage.Player.gameObject, HandleText(msg));
			}
		}

		private void ShowChatterToNearbyPeople(RadioMessage message)
		{
			var scan = Physics2D.OverlapCircleAll(gameObject.AssumedWorldPosServer(), hearableRange, layerToCheck);
			foreach (var player in scan)
			{
				if (player.gameObject.TryGetComponent<PlayerScript>(out var connectedPlayer) &&
				    MatrixManager.Linecast(gameObject.AssumedWorldPosServer(),LayerTypeSelection.Walls,
					    layerToCheck, player.gameObject.AssumedWorldPosServer()).ItHit == false)
				{
					Chat.AddExamineMsg(connectedPlayer.gameObject, HandleText(message));
				}
			}
		}

		private string HandleText(RadioMessage message)
		{
			string messageSender = message.Sender;
			string messageContent = message.Message;
			bool encrypted = message.IsEncrypted;
			if (encrypted && EncryptionData != null)
			{
				if (EncryptionUtils.Decrypt(messageSender, EncryptionData.EncryptionSecret) !=
				    message.OriginalSenderName)
				{
					messageSender = "???";
				}
				else
				{
					messageSender = message.OriginalSenderName;
				}
				messageContent = EncryptionUtils.Decrypt(messageContent, EncryptionData.EncryptionSecret);
			}
			return $"<b><color=#{ColorUtility.ToHtmlStringRGBA(Chat.Instance.commonColor)}>[{Frequency}]" +
			       $" - {messageSender} says \"{messageContent}\"</color></b>.";
		}

		public void AddEncryptionKey(EncryptionKey key)
		{
			if (keyStorage.ServerTryTransferFrom(key.gameObject) && isScrewed == false)
			{
				EncryptionData = key.EncryptionDataSo;
				radioListener.EncryptionData = key.EncryptionDataSo;
			}
		}

		public void RemoveEncryptionKey()
		{
			keyStorage.ServerDropAll();
			EncryptionData = null;
			radioListener.EncryptionData = null;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if(canChangeEncryption == false) return false;
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.HandObject == null) return false;
			return true;
		}

		public void ScrewInteraction(GameObject Performer)
		{
			isScrewed = !isScrewed;
			string status = isScrewed ? "screw" : "unscrew";
			Chat.AddExamineMsg(Performer, $"You {status} the {gameObject.ExpensiveName()}.");
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.UsedObject.TryGetComponent<Screwdriver>(out var _))
			{
				ScrewInteraction(interaction.Performer);
				return;
			}
			if(interaction.IsAltClick && isScrewed == false) RemoveEncryptionKey();
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			var result = RightClickableResult.Create();
			if (isScrewed) return result;

			return result.AddElement("Remove Encryption", RemoveEncryptionKey);
		}
	}

}
