using System;
using System.Collections;
using System.Collections.Generic;
using Communications;
using Managers;
using UnityEngine;
using Util;

namespace Objects.Telecomms
{
	public class StationBouncedRadio : SignalReceiver
	{
		[SerializeField] private int hearableRange = 2;
		[SerializeField] private LayerMask layerToCheck;
		private LocalRadioListener radioListener;
		private bool broadcastToNearbyTiles = true;
		private Pickupable pickupable;
		public bool BroadcastToNearbyTiles
		{
			get => broadcastToNearbyTiles;
			set => broadcastToNearbyTiles = value;
		}

		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
			radioListener = GetComponent<LocalRadioListener>();
			UpdateManager.Add(SyncFrequencyWithListner, 0.5f);
		}

		private void SyncFrequencyWithListner()
		{
			Frequency = radioListener.Frequency;
		}

		public override void ReceiveSignal(SignalStrength strength, SignalMessage message = null)
		{
			if (message is RadioMessage msg)
			{
				if(broadcastToNearbyTiles) {ShowChatterToNearbyPeople(msg); return;}

				if (pickupable.ItemSlot?.Player.OrNull() == true)
				{
					Chat.AddExamineMsg(pickupable.ItemSlot.ItemStorage.Player.gameObject, HandleText(msg));
				}
			}
		}

		private void ShowChatterToNearbyPeople(RadioMessage message)
		{
			Chat.AddActionMsgToChat(gameObject, HandleText(message), HandleText(message));
		}

		private string HandleText(RadioMessage message)
		{
			string messageSender = message.Sender;
			string messageContent = message.Message;
			bool encrypted = message.IsEncrypted;
			if (encrypted && EncryptionData != null)
			{
				messageSender = EncryptionUtils.Decrypt(messageSender, EncryptionData.EncryptionSecret);
				messageContent = EncryptionUtils.Decrypt(messageContent, EncryptionData.EncryptionSecret);
			}
			return $"<b><color=#{ColorUtility.ToHtmlStringRGBA(Chat.Instance.commonColor)}><sprite=\"RadioIcon\" name=\"radio_walkietalkie\">" +
			       $" -{messageSender} says \"{messageContent}\"</color></b>";
		}

	}

}
