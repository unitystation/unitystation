using System.Collections;
using System.Collections.Generic;
using Communications;
using UnityEngine;
using Mirror;
using ScriptableObjects.Communications;
using Random = System.Random;

namespace Managers
{
	public class SignalsManager : NetworkBehaviour
	{
		private static SignalsManager signalsManager;
		public static SignalsManager Instance
		{
			get
			{
				if (!signalsManager)
				{
					signalsManager = FindObjectOfType<SignalsManager>();
				}

				return signalsManager;
			}
		}

		public List<SignalReciver> Recivers = new List<SignalReciver>();

		[Server]
		public void SendSignal(SignalEmitter emitter, SignalType type, SignalDataSO signalDataSo)
		{
			foreach (SignalReciver receiver in Recivers)
			{
				if (receiver.SignalTypeToReceive != type) return;

				if (receiver.SignalTypeToReceive == SignalType.PING && receiver.Emitter == emitter)
				{
					if (signalDataSo.UsesRange) { SignalStrengthHandler(receiver, emitter, signalDataSo); break; }
					receiver.RecieveSignal(SignalStrength.HEALTHY);
					break;
				}
				if (receiver.SignalTypeToReceive == SignalType.RADIO && AreOnTheSameFrequancy(receiver, emitter))
				{
					if (signalDataSo.UsesRange) { SignalStrengthHandler(receiver, emitter, signalDataSo); break; }
					receiver.RecieveSignal(SignalStrength.HEALTHY);
					continue;
				}

				if (receiver.SignalTypeToReceive == SignalType.BOUNCED && AreOnTheSameFrequancy(receiver, emitter))
				{
					receiver.RecieveSignal(SignalStrength.HEALTHY);
				}
			}
		}

		private bool AreOnTheSameFrequancy(SignalReciver receiver , SignalEmitter emitter)
		{
			return receiver.Frequency == emitter.Frequancy;
		}

		private void SignalStrengthHandler(SignalReciver receiver, SignalEmitter emitter, SignalDataSO signalDataSo)
		{
			SignalStrength strength = GetStrength(receiver, emitter, signalDataSo.SignalRange);
			if (strength == SignalStrength.HEALTHY) receiver.RecieveSignal(strength);
			if (strength == SignalStrength.TOOFAR) emitter.SignalFailed();


			if (strength == SignalStrength.DELAYED)
			{
				StartCoroutine(DelayedSignalRecevie(receiver.DelayTime, receiver, strength));
				return;
			}
			if (strength == SignalStrength.WEAK)
			{
				Random chance = new Random();
				if (DMMath.Prob(chance.Next(0, 100)))
				{
					StartCoroutine(DelayedSignalRecevie(receiver.DelayTime, receiver, strength));
				}
				emitter.SignalFailed();
			}
		}

		private IEnumerator DelayedSignalRecevie(float waitTime, SignalReciver receiver, SignalStrength strength)
		{
			yield return new WaitForSeconds(waitTime);
			if (receiver.gameObject == null)
			{
				//In case the object despawns before the signal reaches it
				yield break;
			}
			receiver.RecieveSignal(strength);
		}

		/// <summary>
		/// gets the signal strength between a receiver and an emitter
		/// </summary>
		/// <returns>SignalStrength</returns>
		public SignalStrength GetStrength(SignalReciver receiver, SignalEmitter emitter, int range)
		{
			int distance = (int)Vector3.Distance(receiver.gameObject.AssumedWorldPosServer(), emitter.gameObject.AssumedWorldPosServer());
			Logger.Log($"{distance}");
			if (range / 4 <= distance)
			{
				return SignalStrength.DELAYED;
			}

			if (range / 2 <= distance)
			{
				return SignalStrength.WEAK;
			}

			if (distance > range)
			{
				return SignalStrength.TOOFAR;
			}

			return SignalStrength.HEALTHY;
		}
	}

	public enum SignalType
	{
		PING, //Signal is meant for a target object
		RADIO, //Signal is meant to be connected via a reciever to relay to other devices
		BOUNCED //Signal is meant to be sent to all nearby devices without a middle man
	}

	public enum SignalStrength
	{
		HEALTHY, //The signal is not far from the receiver
		DELAYED, //The signal is a bit far from the receiver and will be slightly delayed
		WEAK, //The signal is too far from the receiver and has a chance to not go through
		TOOFAR //The signal is out of range and will not be sent.
	}
}


