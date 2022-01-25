using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Mirror;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Rotatable : NetworkBehaviour, IMatrixRotation
{
	public enum RotationMethod
	{
		None,
		Parent,
		Sprites
	}

	public RotationMethod MethodRotation = RotationMethod.None;

	public bool ChangeSprites = false;


	[ShowIf(nameof(ChangeSprites))] public bool isChangingSO;

	[FormerlySerializedAs("InitialDirection")]
	public OrientationEnum CurrentDirection;

	[SyncVar(hook = nameof(SyncServerDirection))]
	private OrientationEnum SynchroniseCurrentDirection;

	[SyncVar(hook = nameof(SyncServerLockAndDirection))]
	private LockAndDirection SynchroniseCurrentLockAndDirection;

	private SpriteRenderer[] spriteRenderers;
	private SpriteHandler[] spriteHandlers;

	/// <summary>
	/// Invoked when this object's sprites should be updated to indicate it is facing the
	/// specified direction. Components listening for this event don't need to worry about
	/// client prediction or server sync, just update sprites and assume this is the correct direction.
	/// </summary>
	public RotationChangeEvent OnRotationChange = new RotationChangeEvent();




	private void SetDirection(OrientationEnum dir)
	{
		if (SynchroniseCurrentLockAndDirection.Locked)
		{
			SyncServerDirection(SynchroniseCurrentDirection, SynchroniseCurrentLockAndDirection.LockedTo);
		}
		else
		{
			SyncServerDirection(SynchroniseCurrentDirection, dir);
		}
	}

	public void LockDirectionTo(bool Lock, OrientationEnum Dir)
	{
		SyncServerLockAndDirection(SynchroniseCurrentLockAndDirection,
			new LockAndDirection() {Locked = Lock, LockedTo = Dir});
	}

	private void SyncServerLockAndDirection(LockAndDirection oldDir, LockAndDirection dir)
	{
		SynchroniseCurrentLockAndDirection = dir;
		SetDirection(SynchroniseCurrentLockAndDirection.LockedTo);
	}

	private void SyncServerDirection(OrientationEnum oldDir, OrientationEnum dir)
	{
		CurrentDirection = dir;
		SynchroniseCurrentDirection = dir;
		RotateObject(dir);
		if (oldDir != dir)
		{
			OnRotationChange.Invoke(dir);
		}
	}

	public void SetFaceDirectionLocalVictor(Vector2Int direction)
	{
		var newDir = OrientationEnum.Down_By180;
		if (direction == Vector2Int.down)
		{
			newDir = OrientationEnum.Down_By180;
		}
		else if (direction == Vector2Int.left)
		{
			newDir = OrientationEnum.Left_By270;
		}
		else if (direction == Vector2Int.up)
		{
			newDir = OrientationEnum.Up_By0;
		}
		else if (direction == Vector2Int.right)
		{
			newDir = OrientationEnum.Right_By90;
		}
		else if (direction.y == -1)
		{
			newDir = OrientationEnum.Down_By180;
		}
		else if (direction.y == 1)
		{
			newDir = OrientationEnum.Up_By0;
		}

		SetDirection(newDir);
	}

	public void FaceDirection(OrientationEnum newDir)
	{
		SetDirection(newDir);
	}

	public void RotateBy(byte inInt)
	{
		var SetInt = inInt + (int)CurrentDirection;
		while (SetInt > 3)
		{
			SetInt = SetInt - 4;
		}

		FaceDirection((OrientationEnum)SetInt);

	}

	[NaughtyAttributes.Button()]
	public void Refresh()
	{
		SetDirection(CurrentDirection);
		ResitOthers();
	}

	public void Start()
	{
		Refresh();
	}

	private void Awake()
	{
		if (spriteRenderers == null)
		{
			spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
		}

		if (spriteHandlers == null)
		{
			spriteHandlers = GetComponentsInChildren<SpriteHandler>();
		}
	}

	public Quaternion ByDegreesToQuaternion(OrientationEnum dir)
	{
		var OutQuaternion = new Quaternion();
		switch (dir)
		{
			case OrientationEnum.Up_By0:
				OutQuaternion.eulerAngles = new Vector3(0, 0, 0f);
				break;
			case OrientationEnum.Right_By90:
				OutQuaternion.eulerAngles = new  Vector3(0, 0, -90f);
				break;
			case OrientationEnum.Down_By180:
				OutQuaternion.eulerAngles = new  Vector3(0, 0, -180f);
				break;
			case OrientationEnum.Left_By270:
				OutQuaternion.eulerAngles = new  Vector3(0, 0, -270f);
				break;
		}
		return OutQuaternion;
	}

	public void RotateObject(OrientationEnum dir)
	{
		if (MethodRotation == RotationMethod.Parent)
		{
			transform.localRotation = ByDegreesToQuaternion(dir);
		}
		else if (MethodRotation == RotationMethod.Sprites)
		{
			var Quaternion = ByDegreesToQuaternion(dir);

			foreach (var spriteRenderer in spriteRenderers)
			{
				spriteRenderer.transform.localRotation = Quaternion;
			}
		}

		if (ChangeSprites == false) return;

		int spriteVariant = 0;
		switch (dir)
		{
			case OrientationEnum.Up_By0:
				spriteVariant = 1;
				break;
			case OrientationEnum.Right_By90:
				spriteVariant = 2;
				break;
			case OrientationEnum.Down_By180:
				spriteVariant = 0;
				break;
			case OrientationEnum.Left_By270:
				spriteVariant = 3;
				break;
		}

		foreach (var spriteHandler in spriteHandlers)
		{
			if (isChangingSO)
			{
				spriteHandler.ChangeSprite(spriteVariant, false);
			}
			else
			{
				spriteHandler.ChangeSpriteVariant(spriteVariant, false);
			}
		}
	}

	public struct LockAndDirection
	{
		public bool Locked;
		public OrientationEnum LockedTo;
	}

	public void OnValidate()
	{
		Awake();
		RotateObject(CurrentDirection);
	}

	public void ResitOthers()
	{
		if (MethodRotation != RotationMethod.Parent)
		{
			transform.localRotation = ByDegreesToQuaternion(OrientationEnum.Up_By0);
		}

		if (MethodRotation != RotationMethod.Sprites)
		{
			var Quaternion = ByDegreesToQuaternion(OrientationEnum.Up_By0);

			foreach (var spriteRenderer in spriteRenderers)
			{
				spriteRenderer.transform.localRotation = Quaternion;
			}
		}

		if (ChangeSprites == false)
		{
			int SpriteVariant = 0;
			foreach (var spriteHandler in spriteHandlers)
			{
				if (isChangingSO)
				{
					spriteHandler.ChangeSprite(0, false);
				}
				else
				{
					spriteHandler.ChangeSpriteVariant(0, false);
				}

				// PrefabUtility.RecordPrefabInstancePropertyModifications(spriteHandler);
			}
		}
		// PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
	}

	public void OnMatrixRotate(MatrixRotationInfo rotationInfo)
	{

	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;

		if (Application.isEditor && !Application.isPlaying)
		{
			DebugGizmoUtils.DrawArrow(transform.position, CurrentDirection.ToLocalVector3());
		}
		else
		{
			DebugGizmoUtils.DrawArrow(transform.position, CurrentDirection.ToLocalVector3());
		}
	}
	/// <summary>
	/// Event which indicates a direction change has occurred.
	/// </summary>
	public class RotationChangeEvent : UnityEvent<OrientationEnum>{}

	public interface IOnRotationChangeEditor
	{
		public void OnRotationChangeEditor(OrientationEnum newDir);
	}
}
