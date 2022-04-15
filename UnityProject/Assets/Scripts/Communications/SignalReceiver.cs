using Managers;
using Mirror;
using ScriptableObjects.Communications;

namespace Communications
{
	public abstract class SignalReceiver : NetworkBehaviour
	{
		public SignalType SignalTypeToReceive = SignalType.PING;
		public float Frequency = 122F;
		public SignalEmitter Emitter;
		public float DelayTime = 3f; //How many seconds of delay before the SignalReceive logic happens for weak signals
		public EncryptionDataSO EncryptionData;
		public bool ListenToEncryptedData = false; //For devices that are designed for spying and hacking


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
		public abstract void ReceiveSignal(SignalStrength strength, SignalEmitter responsibleEmitter, ISignalMessage message = null);


		/// <summary>
		/// Optional. If ReceiveSignal logic has been succesful we can respond to the emitter with some logic.
		/// </summary>
		public virtual void Respond(SignalEmitter signalEmitter) { }
	}
}