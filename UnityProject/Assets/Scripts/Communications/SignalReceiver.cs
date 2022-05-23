using System;
using Managers;
using Mirror;
using ScriptableObjects.Communications;
using UnityEngine;

namespace Communications
{
	public abstract class SignalReceiver : NetworkBehaviour, IServerDespawn, IServerSpawn
	{
		public SignalType SignalTypeToReceive = SignalType.PING;
		public float Frequency = 122F;
		public SignalEmitter Emitter;
		public float DelayTime = 3f; //How many seconds of delay before the SignalReceive logic happens for weak signals
		public int PassCode;
		public bool ListenToEncryptedData = false; //For devices that are designed for spying and hacking


		public void OnSpawnServer(SpawnInfo info)
		{
			SignalsManager.Instance.Receivers.Add(this);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			RemoveSelfFromManager();
		}

		/// <summary>
		/// Sometimes OnDisable() gets overriden or doesn't get called properly when called using the Despawn class so manually call this in your extended script.
		/// Or when you need to remove this receiver from the manager for whatever reason.
		/// </summary>
		protected void RemoveSelfFromManager()
		{
			if(CustomNetworkManager.IsServer == false) return;
			SignalsManager.Instance.Receivers.Remove(this);
		}

		/// <summary>
		/// Logic to do when
		/// </summary>
		public abstract void ReceiveSignal(SignalStrength strength, SignalEmitter responsibleEmitter, ISignalMessage message = null);


		/// <summary>
		/// Optional. If ReceiveSignal logic has been successful we can respond to the emitter with some logic.
		/// </summary>
		public virtual void Respond(SignalEmitter signalEmitter) { }
	}
}