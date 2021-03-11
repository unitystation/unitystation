using UnityEngine;
using Mirror;

/// <summary>
/// Handles syncing common variables used in animations.
/// </summary>

public class NetworkedLeanTween : NetworkBehaviour
{
	[SyncVar]
	public Transform target;
	[SyncVar]
	public AnimType animType = AnimType.MOVEMENT;

	public enum Axis
	{
		X,
		Y,
		XY,
		Z
	}

	public enum AnimType
	{
		MOVEMENT,
		COLOR
	}

	private void Awake() {
		if(target == null)
		{
		    target = this.transform;
		}
	}

	[ClientRpc]
	public void RpcsetTarget(Transform t)
	{
		target = t;
	}

	[ClientRpc]
	public void RpcsetAnimType(AnimType type)
	{
		animType = type;
	}

	//The functions below just handle playing animations on target.
	//Mainly used to help people not worry about getting the target to animate and make cleaner code.

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
		LeanTween.alpha(target.gameObject, to, time);
	}

	[ClientRpc]
	public void RpcMoveGMToTransform(Transform transform, float time)
	{
		LeanTween.move(target.gameObject, transform, time);
	}

	[ClientRpc]
	public void RpcMoveGMToVector3Local(Vector3 vector, float time)
	{
		LeanTween.moveLocal(target.gameObject, vector, time);
	}

	[ClientRpc]
	public void RpcMove(Axis axis, Vector3 vector, float time)
	{
		switch (axis)
		{
			case (Axis.X):
				LeanTween.moveX(target.gameObject, vector.x, time);
				break;
			case (Axis.Y):
				LeanTween.moveY(target.gameObject, vector.y, time);
				break;
			case (Axis.Z):
				LeanTween.moveZ(target.gameObject, vector.z, time);
				break;
			case (Axis.XY):
				LeanTween.move(target.gameObject, vector, time);
				break;
		}
	}

	[ClientRpc]
	public void RpcLocalMove(Axis axis, Vector3 vector, float time)
	{
		switch (axis)
		{
			case (Axis.X):
				LeanTween.moveLocalX(target.gameObject, vector.x, time);
				break;
			case (Axis.Y):
				LeanTween.moveLocalY(target.gameObject, vector.y, time);
				break;
			case (Axis.Z):
				LeanTween.moveLocalZ(target.gameObject, vector.z, time);
				break;
			case (Axis.XY):
				LeanTween.moveLocal(target.gameObject, vector, time);
				break;
		}
	}

	[ClientRpc]
	public void RpcRotateGameObject(Vector3 vector, float time)
	{
		LeanTween.rotate(target.gameObject, vector, time);
	}

	[ClientRpc]
	public void RpcScaleGameObject(Vector3 vector, float time)
	{
		LeanTween.scale(target.gameObject, vector, time);
	}

	[ClientRpc]
	public void RpcValueFloat(float from, float to, float time)
	{
		LeanTween.value(target.gameObject, from, to, time);
	}

	[ClientRpc]
	public void RpcValueVector2(Vector2 from, Vector2 to, float time)
	{
		LeanTween.value(target.gameObject, from, to, time);
	}

	[ClientRpc]
	public void RpcValueVector3(Vector2 from, Vector2 to, float time)
	{
		LeanTween.value(target.gameObject, from, to, time);
	}

	[ClientRpc]
	public void RpcValueColor(Color from, Color to, float time)
	{
		LeanTween.value(target.gameObject, from, to, time);
	}
}
