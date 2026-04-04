using Mirror;
using UnityEngine;
using US13.Managers;

namespace US13.Messages.Server.SoundMessages
{
	/// <summary>
	/// Message that tells a client to fade a sound's volume over time.
	/// The fade runs entirely client-side, only one network message is sent.
	/// The actual fade logic lives in SoundManager.ClientFade.
	/// </summary>
	public class FadeSoundMessage : ServerMessage<FadeSoundMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string SoundSpawnToken;
			public float TargetVolume;
			public float Duration;
			public bool StopOnComplete;
		}

		public override void Process(NetMessage msg)
		{
			SoundManager.ClientFade(msg.SoundSpawnToken, msg.TargetVolume, msg.Duration, msg.StopOnComplete);
		}

		public static NetMessage SendToAll(string soundSpawnToken, float targetVolume, float duration, bool stopOnComplete = false)
		{
			NetMessage msg = new NetMessage
			{
				SoundSpawnToken = soundSpawnToken,
				TargetVolume = targetVolume,
				Duration = duration,
				StopOnComplete = stopOnComplete
			};

			SendToAll(msg);
			return msg;
		}

		public static NetMessage Send(GameObject recipient, string soundSpawnToken, float targetVolume, float duration, bool stopOnComplete = false)
		{
			NetMessage msg = new NetMessage
			{
				SoundSpawnToken = soundSpawnToken,
				TargetVolume = targetVolume,
				Duration = duration,
				StopOnComplete = stopOnComplete
			};

			SendTo(recipient, msg);
			return msg;
		}
	}
}
