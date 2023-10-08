using System;
using System.Linq;
using UnityEngine;
using Core.Editor.Attributes;


/// <summary>
/// Client side component. Keeps object's sprites upright no matter the orientation of their parent matrix.
/// Allows defining what should happen to the sprites during a matrix rotation,
/// </summary>
[ExecuteInEditMode]
public class UprightSprites : MonoBehaviour, IMatrixRotation
{

	[Tooltip("Defines how this object's sprites should behave during a matrix rotation")]
	public SpriteMatrixRotationBehavior spriteMatrixRotationBehavior =
		SpriteMatrixRotationBehavior.RotateUprightAtEndOfMatrixRotation;


	[Tooltip("Ignore additional rotation (for example, when object is knocked down)")]
	public SpriteRenderer[] ignoreExtraRotation = new SpriteRenderer[0];


	public GameObject RotateParent = null;

	/// <summary>
	/// Client side only! additional rotation to apply to the sprites. Can be used to give the object an appearance
	/// of being knocked down by, for example, setting this to Quaternion.Euler(0,0,-90).
	/// </summary>
	public Quaternion ExtraRotation
	{
		get => extraRotation;
		set
		{
			extraRotation = value;
			//need to update sprite the moment this is set
			SetSpritesUpright();
		}
	}

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
		if (Application.isPlaying == false) return;
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

	private void SetSpritesUpright()
	{
		if (RotateParent == null)
		{
			if (spriteRenderers == null) return;
		}

		//if the object has rotation (due to spinning), don't set sprites upright, this
		//avoids it suddenly flicking upright when it crosses a matrix or matrix rotates
		//note only uopS can have spin rotation
		if (uop != null && Quaternion.Angle(transform.localRotation, Quaternion.identity) > 5) return;

		if (RotateParent != null)
		{
			RotateParent.transform.rotation = ExtraRotation;
			return;
		}

		foreach (var rend in spriteRenderers)
		{
			if (rend == null) continue;
			rend.transform.rotation = ExtraRotation;
		}

		foreach (var rend in ignoreExtraRotation)
		{
			if (rend == null) continue;

			rend.transform.rotation = Quaternion.identity;
		}
	}

	public void OnMatrixRotate(MatrixRotationInfo rotationInfo)
	{
		//this component is clientside only
		if (rotationInfo.IsClientside)
		{
			if (rotationInfo.IsStarting)
			{
				if (spriteMatrixRotationBehavior == SpriteMatrixRotationBehavior.RemainUpright)
				{
					UpdateManager.Add(CallbackType.UPDATE, SetSpritesUpright);
				}
			}
			else if (rotationInfo.IsEnding)
			{
				if (spriteMatrixRotationBehavior == SpriteMatrixRotationBehavior.RemainUpright)
				{
					//stop reorienting to face upright
					UpdateManager.Remove(CallbackType.UPDATE, SetSpritesUpright);
				}

				SetSpritesUpright();
			}
			else if (rotationInfo.IsObjectBeingRegistered)
			{
				//failsafe to ensure we go upright regardless of what happened during init.
				SetSpritesUpright();
			}
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
