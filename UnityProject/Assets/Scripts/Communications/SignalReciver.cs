using System;
using System.Collections;
using System.Collections.Generic;
using Managers;
using UnityEngine;
using ScriptableObjects.Communications;

namespace Communications
{
	public class SignalReciver : MonoBehaviour
	{
		public SignalType SignalTypeToReceive = SignalType.PING;
		public float Frequency = 0F;
		public SignalEmitter Emitter;
		public bool RequiresPower = false; //Does this reciver require power to be able to operate?
		public float DelayTime = 3f; //How many seconds of delay before the SignalRecieve logic happens for weak signals


		private void Awake()
		{
			SignalsManager.Instance.Recivers.Add(this);
		}

		private void OnDestroy()
		{
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