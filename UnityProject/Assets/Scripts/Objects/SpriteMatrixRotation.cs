
using System;
using UnityEngine;

/// <summary>
/// Client side. Defines how sprites of an object should rotate based on their parent matrix
///  and matrix rotations. If you want something to always stay rotated along with the matrix (such as shuttle thrusters),
/// simply omit this component from the object.
/// </summary>
public class SpriteMatrixRotation : MonoBehaviour, IClientLifecycle, IMatrixRotation
{
	/// <summary>
	/// Defines how this object's sprites should rotate when its parent matrix rotates.
	/// </summary>
	[Tooltip("Defines how this object's sprites should rotate when its parent matrix rotates.")]
	public SpriteMatrixRotationBehavior spriteMatrixRotationBehavior =
		SpriteMatrixRotationBehavior.RotateUprightAtEndOfMatrixRotation;

	/// <summary>
	/// Client side only. Extra rotation to apply to the sprites after setting their rotation based on
	/// MatrixSpriteRotationBehavior, can be used to give the object an appearance
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
		Logger.LogTraceFormat("Setting sprites upright for {0}", Category.Matrix, this);
		if (spriteRenderers == null) return;
		foreach (SpriteRenderer renderer in spriteRenderers)
		{
			renderer.transform.rotation = ExtraRotation;
		}
	}

	public void OnMatrixRotate(MatrixRotationInfo rotationInfo)
	{
		//this component is clientside only
		if (rotationInfo.IsClientside)
		{
			if (rotationInfo.IsStart)
			{
				if (spriteMatrixRotationBehavior == SpriteMatrixRotationBehavior.RemainUpright)
				{
					Logger.LogTraceFormat("{0} matrix rotation starting on {1}, forcing upright", Category.Matrix, this, registerTile.Matrix);
					UpdateManager.Instance.Add(SetSpritesUpright);
				}
			}
			else
			{
				if (spriteMatrixRotationBehavior == SpriteMatrixRotationBehavior.RemainUpright)
				{
					//stop reorienting to face upright
					Logger.LogTraceFormat("{0} matrix rotation ending on {1}, stop forcing upright", Category.Matrix, this, registerTile.Matrix);
					UpdateManager.Instance.Remove(SetSpritesUpright);
				}

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
