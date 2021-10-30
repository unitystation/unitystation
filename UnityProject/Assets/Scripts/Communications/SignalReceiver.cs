using System;
using System.Collections;
using System.Collections.Generic;
using Managers;
using Mirror;
using UnityEngine;
using ScriptableObjects.Communications;

namespace Communications
{
	public class SignalReceiver : NetworkBehaviour
	{
		public SignalType SignalTypeToReceive = SignalType.PING;
		public float Frequency = 122F;
		public SignalEmitter Emitter;
		public float DelayTime = 3f; //How many seconds of delay before the SignalReceive logic happens for weak signals


		private void OnEnable()
		{
			if(CustomNetworkManager.Instance._isServer == false) return;
			SignalsManager.Instance.Receivers.Add(this);
		}

		private void OnDisable()
		{
			if(CustomNetworkManager.Instance._isServer == false) return;
			SignalsManager.Instance.Receivers.Remove(this);
		}

		/// <summary>
		/// Logic to do when
		/// </summary>
		public virtual void ReceiveSignal(SignalStrength strength) { }


		/// <summary>
		/// Not required. If ReceiveSignal logic has been succesful we can respond to the emitter with some logic.
		/// </summary>
		public virtual void Respond(SignalEmitter signalEmitter) { }
	}
}