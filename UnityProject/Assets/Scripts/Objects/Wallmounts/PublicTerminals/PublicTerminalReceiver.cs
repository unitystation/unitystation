using Communications;
using Managers;
using UnityEngine;
using AddressableReferences;

namespace Objects.Wallmounts.PublicTerminals
{
	public class PublicTerminalReceiver : SignalReceiver
	{
		[SerializeField]
		private PublicDepartmentTerminal OwnEmitter;

		public DepartmentList departmentList;

		public AddressableAudioSource NotificationSound;

		public override void ReceiveSignal(SignalStrength strength, SignalEmitter responsibleEmitter, ISignalMessage message = null)
		{
			if (responsibleEmitter.gameObject.TryGetComponent<PublicDepartmentTerminal>(out var emitter) == false) return;

			if (emitter.SendMessageData.targetDepartment != (int)OwnEmitter.Department) return; //If this is not the target terminal, can't send to self

			SoundManager.PlayNetworkedAtPosAsync(NotificationSound, GetComponent<RegisterTile>().WorldPositionServer, sourceObj: this.gameObject);

			Chat.AddActionMsgToChat(gameObject, "Public Terminal - Request Received.");

			OwnEmitter.ReceivedMessageData.Add(emitter.SendMessageData);

		}
	}
}