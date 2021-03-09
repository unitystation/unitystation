using UnityEngine;
using Mirror;

/// <summary>
/// Handles syncing common variables used in animations.
/// </summary>

public class NetworkedLeanTween : NetworkBehaviour
{
	[SyncVar]
	public bool isAnim = false;
	[SyncVar]
	public Transform target;
	[SyncVar]
	public AnimType animType = AnimType.MOVEMENT;

	[SyncVar]
	private Vector3 pos;
	[SyncVar]
	private Vector3 scale;
	[SyncVar]
	private Quaternion rotation;

	[SyncVar]
	private Color color;

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
		if(target == null){ target = this.transform;}
	}

	private void FixedUpdate() {
		if(isAnim)
		{
			updateBasedOnState();
		}
	}

	private void updateBasedOnState()
	{
		switch (animType)
		{
			case (AnimType.MOVEMENT):
				pos = target.position;
				rotation = target.rotation;
				scale = target.localScale;
				target.position = pos;
				target.rotation = rotation;
				target.localScale = scale;
				break;
			case (AnimType.COLOR):
				var mat = target.GetComponent<Material>();
				if(mat == null)
				{
					Debug.LogError("[NetworkedLeanTween] -> No material detected!");
					break;
				}
				color = mat.color;
				mat.color = color;
				break;
		}
	}

	public void setTarget(Transform t)
	{
		target = t;
	}

	public void setAnimType(AnimType type)
	{
		animType = type;
	}

	//The functions below just handle playing animations on target.
	//Mainly used to help people not worry about getting the target to animate and make cleaner code.

	public void StopAll(bool state)
	{
		LeanTween.cancelAll(state);
	}

	public void CancelObject(GameObject gameObject, bool callOnComplete)
	{
		LeanTween.cancel(gameObject, callOnComplete);
	}

	public void AlphaGameObject(float to, float time)
	{
		LeanTween.alpha(target.gameObject, to, time);
	}

	public void MoveGMToTransform(Transform transform, float time)
	{
		LeanTween.move(target.gameObject, transform, time);
	}

	public void MoveGMToVector3Local(Vector3 vector, float time)
	{
		LeanTween.moveLocal(target.gameObject, vector, time);
	}

	public void Move(Axis axis, Vector3 vector, float time)
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
	public void LocalMove(Axis axis, Vector3 vector, float time)
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

	public void RotateGameObject(Vector3 vector, float time)
	{
		LeanTween.rotate(target.gameObject, vector, time);
	}

	public void ScaleGameObject(Vector3 vector, float time)
	{
		LeanTween.scale(target.gameObject, vector, time);
	}

	public void ValueFloat(float from, float to, float time)
	{
		LeanTween.value(target.gameObject, from, to, time);
	}

	public void ValueVector2(Vector2 from, Vector2 to, float time)
	{
		LeanTween.value(target.gameObject, from, to, time);
	}

	public void ValueVector3(Vector2 from, Vector2 to, float time)
	{
		LeanTween.value(target.gameObject, from, to, time);
	}

	public void ValueColor(Color from, Color to, float time)
	{
		LeanTween.value(target.gameObject, from, to, time);
	}
}

