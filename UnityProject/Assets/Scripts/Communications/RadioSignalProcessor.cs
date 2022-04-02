using System;
using Items.Storage.VirtualStorage;
using Managers;
using NaughtyAttributes;
using Systems.Electricity;
using Systems.ObjectConnection;
using UnityEngine;

namespace Communications
{
	[RequireComponent(typeof(ItemStorage))]
	[RequireComponent(typeof(RadioSignalCommunicator))]
	public class RadioSignalProcessor : SignalReceiver, IAPCPowerable
	{
		public bool requiresPower = false;
		[ShowIf(nameof(requiresPower))] public bool isPowered;
		[SerializeField] private bool requiresDiskStorage;
		[SerializeField] private APCPoweredDevice _APCPoweredDevice = default;

		protected RadioSignalCommunicator communicator;
		[SerializeField] protected ItemStorage diskStorage;
		protected HardDriveBase virtualStorage;

		protected bool onlyAcceptStrongSignal = false;

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
			if(onlyAcceptStrongSignal && strength == SignalStrength.TOOFAR) return;
			if(requiresPower && isPowered == false) return;
			if(message == null) return; //(Max) : I see no point in doing anything really if there isn't a message we can use something with.
			//tho other servers might have some use for signals that have empty messages so they might want to override this function
			ServerProcessSignal(strength, responsibleEmitter, message);
		}

		public virtual void ServerProcessSignal(SignalStrength strength, SignalEmitter responsibleEmitter, ISignalMessage message) { }

		public void PowerNetworkUpdate(float voltage) { }

		public void StateUpdate(PowerState state)
		{
			switch (state)
			{
				case PowerState.Off:
					isPowered = false;
					break;
				case PowerState.LowVoltage:
					onlyAcceptStrongSignal = true;
					break;
				default:
					isPowered = true;
					onlyAcceptStrongSignal = false;
					break;
			}
		}
	}
}