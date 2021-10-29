using System.Collections;
using System.Collections.Generic;
using Managers;
using ScriptableObjects.Communications;
using UnityEngine;

namespace Communications
{
	public class SignalEmitter : MonoBehaviour
	{
		[SerializeField] private SignalDataSO signalData;
		public float Frequancy = 0f;

		/// <summary>
		/// Tells the SignalManager to send a signal to a receiver
		/// </summary>
		public virtual void SendSignal()
		{
			if(signalData == null) return;
			SignalsManager.Instance.SendSignal(this, signalData.EmittedSignalType, signalData);
		}

		/// <summary>
		/// If for whatever reason the signal fails and we want to do something afterwards.
		/// </summary>
		public virtual void SignalFailed() { }

	}
}

