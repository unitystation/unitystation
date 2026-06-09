using Communications;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Managers;
using US13.Objects.Wallmounts.Switches;

///NOTE : THIS ITEM IS CURRENTLY HERE FOR TESTING REASONS ONLY
///PLEASE REMOVE THIS (OR POLISH IT AND ADD IT TO THE LIST OF THINGS SCIENCE CAN MAKE) ONCE WE MAKE THE MOVE TO FULLY USE THE SIGNAL MANAGER
namespace US13.Items.Devices
{
	public class ButtonSignalReceiver : SignalReceiver, ICheckedInteractable<HandApply>
	{
		public DoorSwitch doorSwitch;

		public override void ReceiveSignal(SignalStrength strength, SignalEmitter responsibleEmitter, ISignalMessage message = null)
		{
			if (doorSwitch != null)
			{
				doorSwitch.RunDoorController();
				Respond(Emitter);
				return;
			}
			Emitter.SignalFailed();
		}


		public override void Respond(SignalEmitter signalEmitter)
		{
			Chat.AddActionMsgToChat(signalEmitter.gameObject, "Signal received!");
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if(interaction.IsAltClick == false) return;
			if (interaction.TargetObject.TryGetComponent<DoorSwitch>(out var @switch))
			{
				Chat.AddExamineMsg(interaction.Performer.gameObject, "You assign the switch to the receiver.");
				doorSwitch = @switch;
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.TargetObject.TryGetComponent<DoorSwitch>(out var _)) return true;
			return false;
		}
	}
}

