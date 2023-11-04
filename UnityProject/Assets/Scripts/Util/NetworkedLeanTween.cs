using System;
using Mirror;
using UnityEngine;

namespace Util
{
	/// <summary>
	/// A class that lets you write clean LeanTween code that needs to be synced across all clients.
	/// </summary>

	public class NetworkedLeanTween : NetworkBehaviour
	{
		[Tooltip("Set this to the object you want to animate or leave it null (unset) to animate the enite object this NTL compontent is on.")]
		public Transform Target;

		public enum Axis
		{
			X,
			Y,
			XY,
			Z
		}

		private void Awake() {
			if(Target == null)
			{
				Target = transform;
			}
		}

		[ClientRpc]
		public void RpcsetTarget(Transform t)
		{
			Target = t;
		}

		[ClientRpc]
		public void RpcStopAll(bool state)
		{
			LeanTween.cancelAll(state);
		}

		[ClientRpc]
		public void RpcCancelObject(GameObject gameObject, bool callOnComplete)
		{
			LeanTween.cancel(gameObject, callOnComplete);
		}

		[ClientRpc]
		public void RpcAlphaGameObject(float to, float time)
		{
			LeanTween.alpha(Target.gameObject, to, time);
		}

		[ClientRpc]
		public void RpcMoveGMToTransform(Transform transform, float time)
		{
			LeanTween.move(Target.gameObject, transform, time);
		}

		[ClientRpc]
		public void RpcMoveGMToVector3Local(Vector3 vector, float time)
		{
			LeanTween.moveLocal(Target.gameObject, vector, time);
		}

		[ClientRpc]
		public void RpcMove(Axis axis, Vector3 vector, float time)
		{
			switch (axis)
			{
				case (Axis.X):
					LeanTween.moveX(Target.gameObject, vector.x, time);
					break;
				case (Axis.Y):
					LeanTween.moveY(Target.gameObject, vector.y, time);
					break;
				case (Axis.Z):
					LeanTween.moveZ(Target.gameObject, vector.z, time);
					break;
				case (Axis.XY):
					LeanTween.move(Target.gameObject, vector, time);
					break;
			}
		}

		[ClientRpc]
		public void RpcLocalMove(Axis axis, Vector3 vector, float time)
		{
			LocalMove(axis, vector, time);
		}

		public void LocalMove(Axis axis, Vector3 vector, float time)
		{
			switch (axis)
			{
				case (Axis.X):
					LeanTween.moveLocalX(Target.gameObject, vector.x, time);
					break;
				case (Axis.Y):
					LeanTween.moveLocalY(Target.gameObject, vector.y, time);
					break;
				case (Axis.Z):
					LeanTween.moveLocalZ(Target.gameObject, vector.z, time);
					break;
				case (Axis.XY):
					LeanTween.moveLocal(Target.gameObject, vector, time);
					break;
			}
		}

		[ClientRpc]
		public void RpcRotateGameObject(Vector3 vector, float time)
		{
			LeanTween.rotate(Target.gameObject, vector, time);
		}

		public void RotateGameObject(Vector3 vector, float time, GameObject otherTarget = null)
		{
			LeanTween.rotate(otherTarget == null ? Target.gameObject : otherTarget.gameObject, vector, time);
		}

		[ClientRpc]
		public void RpcScaleGameObject(Vector3 vector, float time)
		{
			LeanTween.scale(Target.gameObject, vector, time);
		}

		[ClientRpc]
		public void RpcValueFloat(float from, float to, float time)
		{
			LeanTween.value(Target.gameObject, from, to, time);
		}

		[ClientRpc]
		public void RpcValueVector2(Vector2 from, Vector2 to, float time)
		{
			LeanTween.value(Target.gameObject, from, to, time);
		}

		[ClientRpc]
		public void RpcValueVector3(Vector2 from, Vector2 to, float time)
		{
			LeanTween.value(Target.gameObject, from, to, time);
		}

		[ClientRpc]
		public void RpcValueColor(Color from, Color to, float time)
		{
			LeanTween.value(Target.gameObject, from, to, time);
		}
	}
}
