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
		[SyncVar] public float Frequency = 0F;
		[SyncVar] public SignalEmitter Emitter;
		public float DelayTime = 3f; //How many seconds of delay before the SignalRecieve logic happens for weak signals


		private void Awake()
		{
			if(isClient) return;
			SignalsManager.Instance.Recivers.Add(this);
		}

		private void OnDestroy()
		{
			if(isClient) return;
			SignalsManager.Instance.Recivers.Remove(this);
		}

		/// <summary>
		/// Logic to do when
		/// </summary>
		public virtual void RecieveSignal(SignalStrength strength) { }


		/// <summary>
		/// Not required. If RecieveSignal logic has been succesful we can respond to the emitter with some logic.
		/// </summary>
		public virtual void Respond(SignalEmitter signalEmitter) { }
	}
}