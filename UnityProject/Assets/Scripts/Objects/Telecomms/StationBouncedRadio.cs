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


		private Pickupable pickupable;

		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
		}

		public override void ReceiveSignal(SignalStrength strength, SignalMessage message = null)
		{
			if (message is RadioMessage msg)
			{
				ShowChatterToNearbyPeople(msg);
			}
		}

		private void ShowChatterToNearbyPeople(RadioMessage message)
		{
			ContactPoint2D[] list = new ContactPoint2D[]{};
			var scan = Physics2D.OverlapCircle(gameObject.AssumedWorldPosServer(), hearableRange, layerToCheck).GetContacts(list);
			foreach (var player in list)
			{
				if (player.collider.gameObject.TryGetComponent<ConnectedPlayer>(out var connectedPlayer))
				{
					//We're doing this this way for now until we discuss how we are going to handle Chat.cs
					//for proper telecomms
					Chat.AddExamineMsg(connectedPlayer.GameObject, HandleText(message));
				}
			}
		}

		private string HandleText(RadioMessage message)
		{
			return $"<color={Chat.Instance.commonColor}>[{Frequency}] - {message.Sender} says \"{message.Message}\"";
		}

	}

}
