using Communications;
using Managers;
using UnityEngine;
using AddressableReferences;

namespace Objects.Wallmounts
{
	public class PublicTerminalReceiver : SignalReceiver
	{
		[SerializeField]
		private PublicDepartmentTerminal OwnEmitter;

		public DepartmentList departmentList;

		public AddressableAudioSource NotificationSound;

		public override void ReceiveSignal(SignalStrength strength, SignalEmitter responsibleEmitter, ISignalMessage message = null)
		{
			PublicDepartmentTerminal emitter = responsibleEmitter.gameObject.GetComponent<PublicDepartmentTerminal>();

			if (emitter.sendMessageData.targetDepartment != (int)OwnEmitter.Department) return; //If this is not the target terminal, can't send to self

			SoundManager.PlayNetworkedAtPosAsync(NotificationSound, GetComponent<RegisterTile>().WorldPositionServer, sourceObj: this.gameObject);

			Chat.AddLocalMsgToChat("Public Terminal - Request Received", gameObject); //Gives a notification to nearby players if a message comes through

			OwnEmitter.receivedMessageData.Add(emitter.sendMessageData);

		}
	}
}