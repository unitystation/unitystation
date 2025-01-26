using System;
using System.Linq;
using Core;
using UnityEngine;
using Core.Editor.Attributes;
using Logs;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;


/// <summary>
/// Client side component. Keeps object's sprites upright no matter the orientation of their parent matrix.
/// Allows defining what should happen to the sprites during a matrix rotation,
/// </summary>
public class UprightSprites : MonoBehaviour, IMatrixRotation
{
	[Tooltip("Defines how this object's sprites should behave during a matrix rotation")]
	public SpriteMatrixRotationBehavior spriteMatrixRotationBehavior =
		SpriteMatrixRotationBehavior.RotateUprightAtEndOfMatrixRotation;


	[Tooltip("Ignore additional rotation (for example, when object is knocked down)")]
	public SpriteRenderer[] ignoreExtraRotation = new SpriteRenderer[0];


	public GameObject RotateParent = null;


	private Quaternion extraRotation = Quaternion.identity;

	private SpriteRenderer[] spriteRenderers;
	private RegisterTile registerTile;
	private UniversalObjectPhysics uop;

	private void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
		if (RotateParent == null)
		{
			spriteRenderers = GetComponentsInChildren<SpriteRenderer>().Except(ignoreExtraRotation).ToArray();
		}

		uop = GetComponent<UniversalObjectPhysics>();
		registerTile.OnParentChangeComplete.AddListener(OnAppearOrChangeMatrix);
		registerTile.OnAppearClient.AddListener(OnAppearOrChangeMatrix);
		SetSpritesUpright();
	}

	private void OnAppearOrChangeMatrix()
	{
		//if our parent changed, our local rotation might've changed so make sure our sprites are still upright
		SetSpritesUpright();
	}

	private void OnEnable()
	{
		SetSpritesUpright();
	}

	private void OnDestroy()
	{
		UpdateManager.Remove(CallbackType.UPDATE, SetSpritesUpright);
	}

	//makes sure it's removed from update manager at end of round since currently updatemanager is not
	//reset on round end.
	private void OnDisable()
	{
		// Make sure we're in play mode if running in editor.
#if UNITY_EDITOR
		if (Application.isPlaying)
#endif
			UpdateManager.Remove(CallbackType.UPDATE, SetSpritesUpright);
	}

	[NaughtyAttributes.Button]
	private void SetSpritesUpright()
	{
		if (Manager3D.Is3D) return;
		var Rotation = transform.rotation.eulerAngles;
		Rotation.z = 0;
		transform.rotation = Quaternion.Euler(Rotation);
	}

	public void OnMatrixRotate()
	{
		if (CustomNetworkManager.IsHeadless) return;
		if (spriteMatrixRotationBehavior == SpriteMatrixRotationBehavior.RotateUprightAtEndOfMatrixRotation)
		{
			var up = new Vector3(0, 1, 0).DirectionLocalToWorld(registerTile.Matrix).ToOrientationEnum();

			var zSet = 0f;
			switch (up)
			{
				case OrientationEnum.Up_By0:
					zSet = 0;
					break;
				case OrientationEnum.Right_By270:
					zSet = -270f;
					break;
				case OrientationEnum.Down_By180:
					zSet = -180f;
					break;
				case OrientationEnum.Left_By90:
					zSet = -90f;
					break;
			}


			var elu = transform.localRotation.eulerAngles;
			elu.z = zSet;
			transform.localRotation = Quaternion.Euler(elu);
			return;
		}
		else
		{
			SetSpritesUpright();
		}
	}
}


/// <summary>
/// Enum describing how an object's sprites should rotate when matrix rotations happen
/// </summary>
public enum SpriteMatrixRotationBehavior
{
	/// <summary>
	/// Object always remains upright, top of the sprite pointing at the top of the screen
	/// </summary>
	RemainUpright = 0,

	/// <summary>
	/// Object rotates with matrix until the end of a matrix rotation, at which point
	/// it rotates so its top is pointing at the top of the screen (this is how most objects in the game behave).
	/// </summary>
	RotateUprightAtEndOfMatrixRotation = 1
}