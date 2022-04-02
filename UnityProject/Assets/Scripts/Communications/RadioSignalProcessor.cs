using System;
using Items.Storage.VirtualStorage;
using Managers;
using NaughtyAttributes;
using UnityEngine;

namespace Communications
{
	[RequireComponent(typeof(ItemStorage))]
	[RequireComponent(typeof(RadioSignalCommunicator))]
	public class RadioSignalProcessor : SignalReceiver
	{
		public bool requiresPower = false;
		[ShowIf(nameof(requiresPower))] public bool isPowered;
		[SerializeField] private bool requiresDiskStorage;
		[SerializeField, ShowIf(nameof(requiresDiskStorage))] private bool spawnWithDisk;
		[SerializeField, ShowIf(nameof(spawnWithDisk))]
		private GameObject disk;

		protected RadioSignalCommunicator communicator;
		[SerializeField] protected ItemStorage diskStorage;
		protected HardDriveBase virtualStorage;

		private void Awake()
		{
			SignalTypeToReceive = SignalType.RADIOSERVER; //In case people forget to set this in the inspector.
			communicator = GetComponent<RadioSignalCommunicator>();
			if (requiresDiskStorage && diskStorage == null)
			{
				Logger.LogError("SignalProcessor spawned without storage to hold a disk!");
				return;
			}

		}

		public override void ReceiveSignal(SignalStrength strength, SignalEmitter responsibleEmitter, ISignalMessage message = null)
		{
			if(message == null) return; //(Max) : I see no point in doing anything really if there isn't a message we can use something with.
			//tho other servers might have some use for signals that have empty messages so they might want to override this function
			ServerProcessSignal(strength, responsibleEmitter, message);
		}

		public virtual void ServerProcessSignal(SignalStrength strength, SignalEmitter responsibleEmitter, ISignalMessage message) { }
	}
}