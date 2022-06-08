using System.Collections;
using System.Collections.Generic;
using Communications;
using UnityEngine;
using Mirror;
using ScriptableObjects.Communications;
using Random = System.Random;

namespace Managers
{
	public class SignalsManager : SingletonManager<SignalsManager>
	{

		public HashSet<SignalReceiver> Receivers = new HashSet<SignalReceiver>();

		private void Start()
		{
			base.Start();
			EventManager.AddHandler(Event.SceneUnloading, () => Receivers.Clear());
		}

		/// <summary>
		/// Called from the server as the Receivers list is only available for the host and to avoid clients from cheating.
		/// Loops through all receivers and sends the signal if they match the signal type and/or frequancy
		/// </summary>
		[Server]
		public void SendSignal(SignalEmitter emitter, SignalType type, SignalDataSO signalDataSo, ISignalMessage signalMessage = null)
		{
			Receivers.Remove(null);

			foreach (SignalReceiver receiver in Receivers)
			{
				if (receiver.gameObject == null) continue;
				//So servers don't accidentally send data back to themselves
				if(emitter.gameObject == receiver.gameObject) continue;
				if (receiver.SignalTypeToReceive != type) continue;

				//If the signals are not on the same frequency or requires encryption
				if (receiver.Frequency.IsBetween(signalDataSo.MinMaxFrequancy.x, signalDataSo.MinMaxFrequancy.y) == false) continue;
				if (receiver.ListenToEncryptedData == false && MatchingEncryption(receiver, emitter) == false) continue;

				//Bounced radios always have a limited range. Has no limit on the number of devices it can send a signal to.
				if (receiver.SignalTypeToReceive == SignalType.BOUNCED && AreOnTheSameFrequancy(receiver, emitter))
				{
					SignalStrengthHandler(receiver, emitter, signalDataSo, signalMessage);
					continue;
				}

				//Radio signals are designed to send and process data and commands back and fourth.
				if ((receiver.SignalTypeToReceive == SignalType.RADIOCLIENT || receiver.SignalTypeToReceive == SignalType.RADIOSERVER)
					&& AreOnTheSameFrequancy(receiver, emitter))
				{
					if (signalDataSo.UsesRange) { SignalStrengthHandler(receiver, emitter, signalDataSo, signalMessage); continue; }
					receiver.ReceiveSignal(SignalStrength.HEALTHY, emitter, signalMessage);
					continue;
				}

				//PING signals are meant for communicating to a single device only
				//These are meant for admin, special items and map specific cases where other signals cannot interfere with this
				if (receiver.SignalTypeToReceive == SignalType.PING && receiver.Emitter == emitter)
				{
					if (signalDataSo.UsesRange) { SignalStrengthHandler(receiver, emitter, signalDataSo); break; }
					receiver.ReceiveSignal(SignalStrength.HEALTHY, emitter, signalMessage);
					break;
				}
			}
		}

		private bool MatchingEncryption(SignalReceiver receiver, SignalEmitter emitter)
		{
			if(receiver.PassCode == 0) return true;
			return emitter.Passcode == receiver.PassCode;
		}

		private bool AreOnTheSameFrequancy(SignalReceiver receiver , SignalEmitter emitter)
		{
			return Mathf.Approximately(receiver.Frequency, emitter.Frequency);
		}

		private void SignalStrengthHandler(SignalReceiver receiver, SignalEmitter emitter, SignalDataSO signalDataSo, ISignalMessage signalMessage = null)
		{
			SignalStrength strength = GetStrength(receiver, emitter, signalDataSo.SignalRange);
			if (strength == SignalStrength.HEALTHY) receiver.ReceiveSignal(strength, emitter, signalMessage);
			if (strength == SignalStrength.TOOFAR) emitter.SignalFailed();


			if (strength == SignalStrength.DELAYED)
			{
				StartCoroutine(DelayedSignalRecevie(receiver.DelayTime, receiver, emitter, strength, signalMessage));
				return;
			}
			if (strength == SignalStrength.WEAK)
			{
				Random chance = new Random();
				if (DMMath.Prob(chance.Next(0, 100)))
				{
					StartCoroutine(DelayedSignalRecevie(receiver.DelayTime, receiver, emitter, strength, signalMessage));
				}
				emitter.SignalFailed();
			}
		}

		private IEnumerator DelayedSignalRecevie(float waitTime, SignalReceiver receiver, SignalEmitter emitter, SignalStrength strength, ISignalMessage signalMessage = null)
		{
			yield return WaitFor.Seconds(waitTime);
			if (receiver.gameObject == null)
			{
				//In case the object despawns before the signal reaches it
				yield break;
			}
			receiver.ReceiveSignal(strength, emitter, signalMessage);
		}

		/// <summary>
		/// gets the signal strength between a receiver and an emitter
		/// the further the object is the weaker the signal.
		/// </summary>
		/// <returns>SignalStrength</returns>
		public SignalStrength GetStrength(SignalReceiver receiver, SignalEmitter emitter, int range)
		{
			int distance = (int)Vector3.Distance(receiver.gameObject.AssumedWorldPosServer(), emitter.gameObject.AssumedWorldPosServer());
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
		RADIOSERVER, //Signal from a machine that processes client signals and updates other clients
		RADIOCLIENT, //Signal from a machine that communicates with another machine
		BOUNCED //Signal is meant to be sent to all nearby devices without a middle man
	}

	public enum SignalStrength
	{
		HEALTHY, //The signal is not far from the receiver
		DELAYED, //The signal is a bit far from the receiver and will be slightly delayed
		WEAK, //The signal is too far from the receiver and has a chance to not go through
		TOOFAR //The signal is out of range and will not be sent.
	}

	public interface ISignalMessage{}

	public struct RadioMessage : ISignalMessage
	{
		public string Sender;
		public string Message;
		public int Code;
	}
}


