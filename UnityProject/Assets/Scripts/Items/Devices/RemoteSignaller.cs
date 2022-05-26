using System;
using System.Collections;
using System.Collections.Generic;
using Communications;
using UnityEngine;
using Messages.Server;
using Objects;
using UI.Objects.Telecomms;

namespace Items.Devices
{
	public class RemoteSignaller : SignalEmitter, IInteractable<HandActivate>, ITrapComponent
	{
		private Pickupable pickupable;

		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
		}

		protected override bool SendSignalLogic()
		{
			if(signalData == null) return false;
			return true;
		}

		public override void SignalFailed()
		{
			if (pickupable.ItemSlot != null && pickupable.ItemSlot.Player != null)
			{
				UpdateChatMessage.Send(pickupable.ItemSlot.Player.gameObject, ChatChannel.Examine, ChatModifier.None, "You feel your signaler vibrate.");
			}
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			Chat.AddExamineMsg(interaction.Performer, $"You press a button and send a signal through the {gameObject.ExpensiveName()}");
			TrySendSignal();
		}

		public void TriggerTrap()
		{
			SendSignalLogic();
		}


	}

}
