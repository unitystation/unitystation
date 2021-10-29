using System.Collections;
using System.Collections.Generic;
using Communications;
using UnityEngine;


///NOTE : THIS ITEM IS CURRENTLY HERE FOR TESTING REASONS ONLY
///PLEASE REMOVE THIS (OR POLISH IT AND ADD IT TO THE LIST OF THINGS SCIENCE CAN MAKE) ONCE WE MAKE THE MOVE TO FULLY USE THE SIGNAL MANAGER
namespace Items
{
	public class ButtonSignalEmitter : SignalEmitter, ICheckedInteractable<HandApply>, ICheckedInteractable<HandActivate>
	{
		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.TargetObject.TryGetComponent<ButtonSignalReceiver>(out var receiver))
			{
				receiver.Emitter = this;
				Chat.AddExamineMsg(interaction.Performer.gameObject, "You assign the receiver to this emitter.");
			}
		}

		public override void SignalFailed()
		{
			Chat.AddLocalMsgToChat("Bzzt!", gameObject);
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			base.SendSignal();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{

			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.TargetObject.TryGetComponent<ButtonSignalReceiver>(out var _)) return true;
			return false;
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			return true;
		}
	}
}

