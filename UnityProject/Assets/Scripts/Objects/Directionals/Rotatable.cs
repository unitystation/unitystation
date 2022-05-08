using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Mirror;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif
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

	[SerializeField]
	[Tooltip("If active will Make it so only If this gameobject is Local player It won't get updates")]
	private bool IgnoreServerUpdatesIfLocalPlayer= false;

	private SpriteRenderer[] spriteRenderers;
	private SpriteHandler[] spriteHandlers;


	public bool IsAtmosphericDevice = false;
	public bool doNotResetOtherSpriteOptions = false;

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
			SetDirectionInternal(SynchroniseCurrentDirection, SynchroniseCurrentLockAndDirection.LockedTo);
		}
		else
		{
			SetDirectionInternal(SynchroniseCurrentDirection, dir);
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

	private void SetDirectionInternal(OrientationEnum oldDir, OrientationEnum dir)
	{
		CurrentDirection = dir;
		SynchroniseCurrentDirection = dir;
		RotateObject(dir);

		if (oldDir != dir)
		{
			if (
#if UNITY_EDITOR
				Application.isPlaying &&
#endif
				isServer == false && isLocalPlayer)
			{
				CmdChangeDirection(dir);
			}

			OnRotationChange.Invoke(dir);
		}
	}

	public void OnValidate()
	{
		if (Application.isPlaying) return;
#if UNITY_EDITOR
		EditorApplication.delayCall += ValidateLate;
#endif

	}

	public void ValidateLate()
	{
		if (Application.isPlaying) return;
		Awake();
		CurrentDirection = CurrentDirection;
		RotateObject(CurrentDirection);
		ResitOthers();
	}

	private void SyncServerDirection(OrientationEnum oldDir, OrientationEnum dir)
	{
		if (IgnoreServerUpdatesIfLocalPlayer && isLocalPlayer)
		{
			return;
		}
		//Seems like headless is running the hook when it shouldn't be
		//(Mirror bug or our custom code broke something?)
		if (CustomNetworkManager.IsHeadless) return;


		SetDirectionInternal(oldDir, dir);
	}

	public void SetFaceDirectionRotationZ(float direction)
	{
		if (45f > direction)
		{
			SetDirection(OrientationEnum.Up_By0);
		}
		else if (135f > direction)
		{
			SetDirection(OrientationEnum.Left_By90);
		}
		else if (225f > direction)
		{
			SetDirection(OrientationEnum.Down_By180);
		}
		else if (315f > direction)
		{
			SetDirection(OrientationEnum.Right_By270);
		}
		else //Wrapped around
		{
			SetDirection(OrientationEnum.Up_By0);
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
			newDir = OrientationEnum.Left_By90;
		}
		else if (direction == Vector2Int.up)
		{
			newDir = OrientationEnum.Up_By0;
		}
		else if (direction == Vector2Int.right)
		{
			newDir = OrientationEnum.Right_By270;
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
		var SetInt = inInt + (int) CurrentDirection;
		while (SetInt > 3)
		{
			SetInt = SetInt - 4;
		}

		FaceDirection((OrientationEnum) SetInt);
	}

	[NaughtyAttributes.Button()]
	public void Refresh()
	{
		Awake();
		SetDirection(CurrentDirection);
		ResitOthers();
	}

	public override void OnStartClient()
	{
		SyncServerDirection(SynchroniseCurrentDirection, SynchroniseCurrentDirection);
	}

	private void Start()
	{
		Refresh();
	}

	private void Awake()
	{
		if (spriteRenderers == null || spriteRenderers.Length == 0 )
		{
			spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
		}

		if (spriteHandlers == null || spriteHandlers.Length == 0)
		{
			spriteHandlers = GetComponentsInChildren<SpriteHandler>();
		}
	}

	public Quaternion ByDegreesToQuaternion(OrientationEnum dir)
	{
		var outQuaternion = new Quaternion();
		switch (dir)
		{
			case OrientationEnum.Up_By0:
				outQuaternion.eulerAngles = new Vector3(0, 0, 0f);
				break;
			case OrientationEnum.Right_By270:
				outQuaternion.eulerAngles = new Vector3(0, 0, -90f);
				break;
			case OrientationEnum.Down_By180:
				outQuaternion.eulerAngles = new Vector3(0, 0, -180f);
				break;
			case OrientationEnum.Left_By90:
				outQuaternion.eulerAngles = new Vector3(0, 0, -270f);
				break;
		}

		return outQuaternion;
	}

	public void RotateObject(OrientationEnum dir)
	{
		if (MethodRotation == RotationMethod.Parent)
		{
			transform.localRotation = ByDegreesToQuaternion(dir);
		}
		else if (MethodRotation == RotationMethod.Sprites)
		{
			var toQuaternion = ByDegreesToQuaternion(dir);

			foreach (var spriteRenderer in spriteRenderers)
			{
				spriteRenderer.transform.localRotation = toQuaternion;
			}
		}

		if (ChangeSprites == false) return;

		int spriteVariant = 0;
		switch (dir)
		{
			case OrientationEnum.Up_By0:
				spriteVariant = 1;
				break;
			case OrientationEnum.Right_By270:
				spriteVariant = 2;
				break;
			case OrientationEnum.Down_By180:
				spriteVariant = 0;
				break;
			case OrientationEnum.Left_By90:
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

	//client requests the server to change serverDirection
	[Command]
	private void CmdChangeDirection(OrientationEnum direction)
	{
		SetDirection(direction);
	}


	public struct LockAndDirection
	{
		public bool Locked;
		public OrientationEnum LockedTo;
	}

	public void ResitOthers()
	{
		if (doNotResetOtherSpriteOptions) return;

		var DIR = OrientationEnum.Up_By0;
		if (IsAtmosphericDevice)
		{
			DIR = OrientationEnum.Down_By180;
		}


		if (MethodRotation != RotationMethod.Parent)
		{
			transform.localRotation = ByDegreesToQuaternion(DIR);
		}

		if (MethodRotation != RotationMethod.Sprites)
		{
			var Quaternion = ByDegreesToQuaternion(DIR);

			foreach (var spriteRenderer in spriteRenderers)
			{
				spriteRenderer.transform.localRotation = Quaternion;
			}
		}

		if (ChangeSprites == false && IsAtmosphericDevice == false)
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
			}
		}

#if UNITY_EDITOR
		else if (Application.isPlaying == false &&
		         (this.gameObject.scene.path == null || this.gameObject.scene.path.Contains("Scenes") == false) ==
		         false)
		{
			if (MethodRotation != RotationMethod.Parent)
			{
				SerializedObject serializedObject = new UnityEditor.SerializedObject(transform);


				var localRotation = serializedObject.FindProperty("m_LocalRotation");
				PrefabUtility.RevertPropertyOverride(localRotation, InteractionMode.AutomatedAction);
				EditorUtility.SetDirty(this);
			}

			if (MethodRotation != RotationMethod.Sprites)
			{
				foreach (var spriteRenderer in spriteRenderers)
				{
					SerializedObject serializedObject = new UnityEditor.SerializedObject(spriteRenderer.transform);
					var localRotation = serializedObject.FindProperty("m_LocalRotation");
					PrefabUtility.RevertPropertyOverride(localRotation, InteractionMode.AutomatedAction);
					EditorUtility.SetDirty(this);
				}
			}

			if (ChangeSprites == false)
			{
				foreach (var spriteHandler in spriteHandlers)
				{
					SerializedObject serializedspriteHandler = new UnityEditor.SerializedObject(spriteHandler);
					var initialVariantIndex = serializedspriteHandler.FindProperty("initialVariantIndex");
					PrefabUtility.RevertPropertyOverride(initialVariantIndex, InteractionMode.AutomatedAction);

					var SpriteRenderer = spriteHandler.GetComponent<SpriteRenderer>();

					if (SpriteRenderer == null) continue;

					SerializedObject serializedSpriteRenderer = new UnityEditor.SerializedObject(SpriteRenderer);
					var sprite = serializedSpriteRenderer.FindProperty("m_Sprite");
					PrefabUtility.RevertPropertyOverride(sprite, InteractionMode.AutomatedAction);
					EditorUtility.SetDirty(this);
				}
			}
		}
#endif
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
	public class RotationChangeEvent : UnityEvent<OrientationEnum>
	{
	}

	public interface IOnRotationChangeEditor
	{
		public void OnRotationChangeEditor(OrientationEnum newDir);
	}
}