using System.Collections;
using System.Collections.Generic;
using Logs;
using Managers;
using ScriptableObjects.Communications;
using UnityEngine;
using Mirror;
using NaughtyAttributes;
using UnityEngine.Serialization;

namespace Communications
{
	public abstract class SignalEmitter : NetworkBehaviour, IExaminable
	{
		[SerializeField]
		[Required("A signalSO is required for this to work.")]
		protected List<SignalDataSO> emmitableSignalData;
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

		[SerializeField] protected bool canExamineFrequency = false;

		[SerializeField] protected float minimumDamageBeforeObfuscation = 12f;

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

		public List<SignalDataSO> EmmitableSignalData => emmitableSignalData;
		public bool RequiresPower => requiresPower;

		/// <summary>
		/// Tells the SignalManager to send a signal to a receiver
		/// </summary>
		public void TrySendSignal(SignalDataSO signalData = null, ISignalMessage message = null)
		{
			if (emmitableSignalData == null || emmitableSignalData.Count == 0)
			{
				Loggy.LogError("[Singals] - No emmitable signal data detected!");
				return;
			}
			//if no signalData is given, always use the first signal SO in the list as it's considered the main signal.
			if (signalData == null)
			{
				signalData = emmitableSignalData[0];
			}
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

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			if (canExamineFrequency == false)
			{
				return "There is a signal emitter on this device. Though its unclear what frequency it is transmitting to.";
			}
			return $"The emitter on this device is sending a frequency of {frequency}Khz.";
		}
	}
}

