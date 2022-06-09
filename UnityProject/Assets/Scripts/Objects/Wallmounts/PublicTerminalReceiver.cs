using Communications;
using Managers;
using UnityEngine;

namespace Objects.Wallmounts
{
	public class PublicTerminalReceiver : SignalReceiver
	{
		[SerializeField]
		private PublicDepartmentTerminal OwnEmitter;

		public override void ReceiveSignal(SignalStrength strength, SignalEmitter responsibleEmitter, ISignalMessage message = null)
		{
			PublicDepartmentTerminal emitter = responsibleEmitter.gameObject.GetComponent<PublicDepartmentTerminal>();

			if (emitter.sendMessageData.targetDepartment != OwnEmitter.Department.DisplayName) return; //If this is not the target terminal, can't send to self

			Chat.AddLocalMsgToChat("Public Terminal - Request Received", gameObject); //Gives a notification to nearby players if a message comes through

			OwnEmitter.receivedMessageData.Add(emitter.sendMessageData);

		}
	}
}