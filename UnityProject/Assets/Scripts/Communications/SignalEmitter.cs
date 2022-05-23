using System.Collections;
using System.Collections.Generic;
using Managers;
using ScriptableObjects.Communications;
using UnityEngine;
using Mirror;
using NaughtyAttributes;
using UnityEngine.Serialization;

namespace Communications
{
	public abstract class SignalEmitter : NetworkBehaviour
	{
		[SerializeField]
		[Required("A signalSO is required for this to work.")]
		protected SignalDataSO signalData;
		[SerializeField]
		protected int passCode;
		[SerializeField]
		protected float frequency = 122f;
		[SerializeField]
		[Tooltip("For devices that require a power source to operate such as newscasters and wall mounted department radios.")]
		protected bool requiresPower = false;
		[SerializeField]
		[ShowIf(nameof(requiresPower))]
		protected bool isPowered = true;

		public float Frequency
		{
			get => frequency;
			set => frequency = value;
		}

		public bool IsPowered
		{
			get => isPowered;
			set => isPowered = value;
		}

		public int Passcode
		{
			get => passCode;
			set => passCode = value;
		}

		public SignalDataSO SignalData => signalData;
		public bool RequiresPower => requiresPower;

		/// <summary>
		/// Tells the SignalManager to send a signal to a receiver
		/// </summary>
		public void TrySendSignal(ISignalMessage message = null)
		{
			if (requiresPower == true && isPowered == false)
			{
				SignalFailed();
				return;
			}
			if (SendSignalLogic())
			{
				SignalsManager.Instance.SendSignal(this, signalData.EmittedSignalType, signalData, message);
				return;
			}
			SignalFailed();
		}

		/// <summary>
		/// All the checks and logic that needs to be implemented to send a signal to a receiver.
		/// if false, a signal won't be sent.
		/// </summary>
		protected abstract bool SendSignalLogic();

		/// <summary>
		/// If for whatever reason the signal fails and we want to do something afterwards.
		/// </summary>
		public abstract void SignalFailed();

	}
}

