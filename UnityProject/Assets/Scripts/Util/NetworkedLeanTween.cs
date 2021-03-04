using UnityEngine;
using Mirror;

/// <summary>
/// This contains a bunch of LeanTween functions that are can be called from the server to play on other clients.
/// Remember to set the target or it will only apply the effect on the object it's on.
/// </summary>

public class NetworkedLeanTween : NetworkBehaviour
{
	[SyncVar]
	public bool isAnim = false;
	[SyncVar]
	public Transform target;
	[SyncVar]
	private Vector3 pos;
	[SyncVar]
	private Quaternion rotation;

	public enum Axis
	{
		X,
		Y,
		XY,
		Z
	}

	private void Awake() {
		if(target == null){ target = this.transform;}
	}

	private void FixedUpdate() {
		if(isAnim)
		{
			pos = target.position;
			rotation = target.rotation;
		}
	}

	public void setTarget(Transform t)
	{
		target = t;
	}

	public void RpcStopAll(bool state)
	{
		LeanTween.cancelAll(state);
	}

	public void RpcCancelObject(GameObject gameObject, bool callOnComplete)
	{
		LeanTween.cancel(gameObject, callOnComplete);
	}

	public void RpcAlphaGameObject(float to, float time)
	{
		LeanTween.alpha(target.gameObject, to, time);
	}

	public void RpcMoveGMToTransform(Transform transform, float time)
	{
		LeanTween.move(target.gameObject, transform, time);
	}

	public void RpcMoveGMToVector3Local(Vector3 vector, float time)
	{
		LeanTween.moveLocal(target.gameObject, vector, time);
	}

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

	public void RpcRotateGameObject(Vector3 vector, float time)
	{
		LeanTween.rotate(target.gameObject, vector, time);
	}

	public void RpcScaleGameObject(Vector3 vector, float time)
	{
		LeanTween.scale(target.gameObject, vector, time);
	}

	public void RpcValueFloat(float from, float to, float time)
	{
		LeanTween.value(target.gameObject, from, to, time);
	}

	public void RpcValueVector2(Vector2 from, Vector2 to, float time)
	{
		LeanTween.value(target.gameObject, from, to, time);
	}

	public void RpcValueVector3(Vector2 from, Vector2 to, float time)
	{
		LeanTween.value(target.gameObject, from, to, time);
	}

	public void RpcValueColor(Color from, Color to, float time)
	{
		LeanTween.value(target.gameObject, from, to, time);
	}
}

