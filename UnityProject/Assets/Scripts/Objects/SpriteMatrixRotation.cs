
using System;
using UnityEngine;

/// <summary>
/// Client side. Defines how sprites of an object should rotate based on their parent matrix
///  and matrix rotations.
/// </summary>
[RequireComponent(typeof(RegisterTile))]
public class SpriteMatrixRotation : MonoBehaviour
{
	/// <summary>
	/// Defines how this object's sprites should rotate when its parent matrix rotates.
	/// </summary>
	[Tooltip("Defines how this object's sprites should rotate when its parent matrix rotates.")]
	public SpriteMatrixRotationBehavior spriteMatrixRotationBehavior;

	/// <summary>
	/// Client side only. Extra rotation to apply to the sprites after setting their rotation based on
	/// MatrixSpriteRotationBehavior, can be used to give the object an appearance
	/// of being knocked down by, for example, setting this to Quaternion.Euler(0,0,-90).
	/// </summary>
	[Tooltip("Extra rotation to apply to the sprites, after setting their rotation based on MatrixSpriteRotationBehavior" +
	         ", can be used to give the object an appearance" +
	         " of being knocked down.")]
	public Quaternion ExtraRotation = Quaternion.identity;

	//cached spriteRenderers of this gameobject
	protected SpriteRenderer[] spriteRenderers;
	// cached registertile
	private RegisterTile registerTile;

	private void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
		//cache the sprite renderers
		if (spriteRenderers == null)
		{
			spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
			//orient upright
			//set sprites
			if (spriteRenderers != null)
			{
				//set upright
				if ((spriteMatrixRotationBehavior == SpriteMatrixRotationBehavior.RemainUpright
				     || spriteMatrixRotationBehavior == SpriteMatrixRotationBehavior.RotateUprightAtEndOfMatrixRotation)
				)
				{
					foreach (SpriteRenderer renderer in spriteRenderers)
					{
						renderer.transform.rotation = ExtraRotation;
					}
				}
				//set upright in matrix
				else if (spriteMatrixRotationBehavior == SpriteMatrixRotationBehavior.RotateWithMatrix)
				{
					foreach (SpriteRenderer renderer in spriteRenderers)
					{
						renderer.transform.localRotation = ExtraRotation;
					}
				}

			}
		}

		//subscribe to matrix rotations
		registerTile.OnMatrixWillChange.AddListener(OnMatrixWillChange);
		OnMatrixWillChange(registerTile.Matrix);
	}

	private void OnDisable()
	{
		if (registerTile.Matrix != null)
		{
			var move = registerTile.Matrix.GetComponentInChildren<MatrixMove>();
			if (move != null)
			{
				move.OnRotateStart.RemoveListener(OnMatrixRotationStart);
				move.OnRotateEnd.RemoveListener(OnMatrixRotationEnd);
			}
		}
	}

	//invoked when our parent matrix is being changed or initially set
	private void OnMatrixWillChange(Matrix newMatrix)
	{
		//add our listeners
		//unsub from old matrix
		if (registerTile.Matrix != null)
		{
			var move = registerTile.Matrix.GetComponentInChildren<MatrixMove>();
			if (move != null)
			{
				move.OnRotateStart.RemoveListener(OnMatrixRotationStart);
				move.OnRotateEnd.RemoveListener(OnMatrixRotationEnd);
			}
		}

		//sub to new matrix
		var newMove = newMatrix.GetComponentInChildren<MatrixMove>();
		if (newMove != null)
		{
			newMove.OnRotateStart.AddListener(OnMatrixRotationStart);
			newMove.OnRotateEnd.AddListener(OnMatrixRotationEnd);
		}
	}

	private void OnMatrixRotationStart(RotationOffset fromCurrent, bool isInitialRotation)
	{
		if (!isInitialRotation && spriteMatrixRotationBehavior == SpriteMatrixRotationBehavior.RemainUpright)
		{
			UpdateManager.Instance.Add(RemainUpright);
		}
	}

	private void RemainUpright()
	{
		//stay upright until rotation stops
		foreach (SpriteRenderer renderer in spriteRenderers)
		{
			renderer.transform.rotation = ExtraRotation;
		}
	}

	/// <summary>
	/// Invoked when receiving rotation event from our current matrix's matrixmove
	/// </summary>
	//invoked when matrix rotation is ending
	private void OnMatrixRotationEnd(RotationOffset fromCurrent, bool isInitialRotation)
	{
		if (!isInitialRotation && spriteMatrixRotationBehavior == SpriteMatrixRotationBehavior.RemainUpright)
		{
			//stop reorienting to face upright
			UpdateManager.Instance.Remove(RemainUpright);
		}

		// reorient to stay upright if we are configured to do so
		if (spriteMatrixRotationBehavior == SpriteMatrixRotationBehavior.RotateUprightAtEndOfMatrixRotation
		    || spriteMatrixRotationBehavior == SpriteMatrixRotationBehavior.RemainUpright)
		{
			foreach (SpriteRenderer renderer in spriteRenderers)
			{
				renderer.transform.rotation = Quaternion.identity;
			}
		}
	}

}
