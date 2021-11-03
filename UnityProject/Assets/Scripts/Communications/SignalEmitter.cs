using System.Collections;
using System.Collections.Generic;
using Managers;
using ScriptableObjects.Communications;
using UnityEngine;
using Mirror;

namespace Communications
{
	public abstract class SignalEmitter : NetworkBehaviour
	{
		[SerializeField] protected SignalDataSO signalData;
		[SerializeField] protected float frequency = 122f;

		public float Frequency
		{
			get => frequency;
			set => frequency = value;
		}

		/// <summary>
		/// Tells the SignalManager to send a signal to a receiver
		/// </summary>
		public void SendSignal()
		{
			if (SendSignalLogic())
			{
				SignalsManager.Instance.SendSignal(this, signalData.EmittedSignalType, signalData);
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

