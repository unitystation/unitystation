using System;
using System.Collections;
using System.Collections.Generic;
using Logs;
using Mirror;
using Objects;
using UnityEngine;

namespace Messages.Server
{
	/// <summary>
	/// Tell all clients + server to play particle effect for provided gameObject. Object should have ParticleSystem in hierarchy
	/// </summary>
	public class PlayParticleMessage : ServerMessage<PlayParticleMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			/// <summary>
			/// GameObject containing ParticleSystem
			/// </summary>
			public uint 	ParticleObject;
			public uint 	ParentObject;
			public Vector2	TargetVector;
		}

		///To be run on client
		public override void Process(NetMessage msg)
		{
			if (msg.ParticleObject.Equals(NetId.Invalid)) {
				//Failfast
				Loggy.LogWarning("PlayParticle NetId invalid, processing stopped", Category.Particles);
				return;
			}

			//Dont play particles on headless server
			if(CustomNetworkManager.IsHeadless) return;

			LoadMultipleObjects(new uint[] {msg.ParticleObject, msg.ParentObject});

			GameObject particleObject = NetworkObjects[0];
			GameObject parentObject = NetworkObjects[1];

			Effect.ClientPlayParticle(particleObject, parentObject, msg.TargetVector);
		}

		/// <summary>
		/// Tell all clients + server to play particle effect for provided gameObject
		/// </summary>
		public static NetMessage SendToAll(GameObject obj, Vector2 targetVector)
		{
			GameObject topContainer = null;
			NetMessage msg = new NetMessage();
			try
			{
				var Parent = obj.GetRootGameObject();
				msg = new NetMessage {
					ParticleObject = obj.NetId(),
					ParentObject = Parent == null ? NetId.Invalid : Parent.NetId(),
					TargetVector = targetVector,
				};
			}
			catch (Exception ignored)
			{
				Loggy.LogError($"PlayParticleMessage threw an exception {ignored} which has been ignored.", Category.Particles);
			}

			SendToAll(msg);
			return msg;
		}

	}

	/// <summary>
	/// Sets provided component to certain parent after a delay
	/// </summary>
	public class ParentReclaimer : MonoBehaviour
	{
		private Coroutine handle;
		private Component lastComponent;
		private Transform lastParent;

		private IEnumerator ReclaimParent(float delaySec, Component componentToParent, Transform parent)
		{
			lastComponent = componentToParent;
			lastParent = parent;
			yield return WaitFor.Seconds(delaySec);
			ReclaimNow();
		}

		/// <summary>
		/// Starts a coroutine to set proper parent for component in question after a delay
		/// </summary>
		/// <param name="delaySec"></param>
		/// <param name="componentToParent"></param>
		/// <param name="parent"></param>
		public void ReclaimWithDelay(float delaySec, Component componentToParent, Transform parent)
		{
			this.RestartCoroutine( ReclaimParent(delaySec, componentToParent, parent), ref handle );
		}

		/// <summary>
		/// Stops coroutine and sets parent immediately
		/// </summary>
		public void ReclaimNow()
		{
			this.TryStopCoroutine(ref handle);
			if (lastComponent == null || lastParent == null)
			{
				return;
			}
			lastComponent.transform.SetParent(lastParent, false);
			lastComponent = null;
			lastParent = null;
		}
	}
}
