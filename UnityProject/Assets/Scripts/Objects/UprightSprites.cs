
using System;
using UnityEngine;

/// <summary>
/// Client side component. Keeps object's sprites upright no matter the orientation of their parent matrix.
/// Allows defining what should happen to the sprites during a matrix rotation,
/// </summary>
public class UprightSprites : MonoBehaviour, IClientLifecycle, IMatrixRotation
{
	[Tooltip("Defines how this object's sprites should behave during a matrix rotation")]
	public SpriteMatrixRotationBehavior spriteMatrixRotationBehavior =
		SpriteMatrixRotationBehavior.RotateUprightAtEndOfMatrixRotation;

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

	private void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
		spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
		InitOnSpawn();
	}

	private void InitOnSpawn()
	{
		//orient upright
		SetSpritesUpright();
	}

	private void OnEnable()
	{
		InitOnSpawn();
	}

	public void OnSpawnClient(ClientSpawnInfo info)
	{
		InitOnSpawn();
	}


	public void OnDespawnClient(ClientDespawnInfo info)
	{
		UpdateManager.Instance.Remove(SetSpritesUpright);
	}

	//makes sure it's removed from update manager at end of round since currently updatemanager is not
	//reset on round end.
	private void OnDisable()
	{
		UpdateManager.Instance.Remove(SetSpritesUpright);
	}

	private void SetSpritesUpright()
	{
		if (spriteRenderers == null) return;
		foreach (SpriteRenderer renderer in spriteRenderers)
		{
			renderer.transform.rotation = ExtraRotation;
		}
	}

	private void OnMatrixChange(Matrix oldMatrix, Matrix newMatrix)
	{
		//make sure we switch upright when the matrix changes, because
		//we can't always be sure our OnMatrixRotate fired
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
					UpdateManager.Instance.Add(SetSpritesUpright);
				}
			}
			else if (rotationInfo.IsEnding)
			{
				if (spriteMatrixRotationBehavior == SpriteMatrixRotationBehavior.RemainUpright)
				{
					//stop reorienting to face upright
					UpdateManager.Instance.Remove(SetSpritesUpright);
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
