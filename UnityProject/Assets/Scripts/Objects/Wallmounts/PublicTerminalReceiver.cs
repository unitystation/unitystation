using UnityEngine;
using Communications;
using Managers;

namespace Objects.Wallmounts
{
	public class PublicTerminalReceiver : SignalReceiver
	{

		public override void ReceiveSignal(SignalStrength strength, SignalEmitter responsibleEmitter, ISignalMessage message = null)
		{
			if (this.gameObject == null) return;

			PublicDepartmentTerminal emitter = responsibleEmitter.gameObject.GetComponent<PublicDepartmentTerminal>();
			PublicDepartmentTerminal receiver = gameObject.GetComponent<PublicDepartmentTerminal>();

			if (emitter == null || receiver == null) return;

			if (emitter == receiver ) return;

			if (emitter.sendMessageData.targetDepartment != receiver.Department) return; //If this is not the target terminal.

			receiver.receivedMessageData.Add(emitter.sendMessageData);

		}
	}
}