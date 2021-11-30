using System;
using System.Collections;
using System.Collections.Generic;
using Communications;
using Managers;
using UnityEngine;

namespace Objects.Telecomms
{
	public class StationBouncedRadio : SignalReceiver
	{
		[SerializeField] private int hearableRange = 2;
		[SerializeField] private LayerMask layerToCheck;
		private LocalRadioListener radioListener;
		private bool broadcastToNearbyTiles = true;
		private Pickupable pickupable;

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
			var scan = Physics2D.OverlapCircleAll(gameObject.AssumedWorldPosServer(), hearableRange, layerToCheck);
			foreach (var player in scan)
			{
				if (player.gameObject.TryGetComponent<PlayerScript>(out var connectedPlayer))
				{
					//We're doing this this way for now until we discuss how we are going to handle Chat.cs
					//for proper telecomms
					Chat.AddExamineMsg(connectedPlayer.gameObject, HandleText(message));
				}
			}
		}

		private string HandleText(RadioMessage message)
		{
			return $"<b><color=#{ColorUtility.ToHtmlStringRGBA(Chat.Instance.commonColor)}>[{Frequency}] - {message.Sender} says \"{message.Message}\"</color></b>";
		}

	}

}
