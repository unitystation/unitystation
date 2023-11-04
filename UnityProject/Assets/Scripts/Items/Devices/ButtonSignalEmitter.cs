using System.Collections;
using System.Collections.Generic;
using Communications;
using Managers;
using Objects.Wallmounts;
using UnityEngine;


///NOTE : THIS ITEM IS CURRENTLY HERE FOR TESTING REASONS ONLY
///PLEASE REMOVE THIS (OR POLISH IT AND ADD IT TO THE LIST OF THINGS SCIENCE CAN MAKE) ONCE WE MAKE THE MOVE TO FULLY USE THE SIGNAL MANAGER
namespace Items
{
	public class ButtonSignalEmitter : SignalEmitter, ICheckedInteractable<HandApply>, IInteractable<HandActivate>
	{
		public void ServerPerformInteraction(HandApply interaction)
		{
			if(interaction.TargetObject.TryGetComponent<DoorSwitch>(out var doorSwitch)
			   && doorSwitch.TryGetComponent<ButtonSignalReceiver>(out var s) == false)
			{
				var switchSignal = doorSwitch.gameObject.AddComponent<ButtonSignalReceiver>();
				switchSignal.doorSwitch = doorSwitch;
				switchSignal.SignalTypeToReceive = SignalType.BOUNCED;
				Chat.AddExamineMsg(interaction.Performer, "Added signal receiver successfully to the door switch");
				return;
			}
			Chat.AddExamineMsg(interaction.Performer.gameObject, "You assign the receiver to this emitter.");
		}

		protected override bool SendSignalLogic()
		{
			return true;
		}

		public override void SignalFailed()
		{
			Chat.AddActionMsgToChat(gameObject, "Bzzt!");
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			TrySendSignal();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)
			    || interaction.TargetObject == null || gameObject.PickupableOrNull().ItemSlot == null) return false;
			return true;
		}
	}
}

