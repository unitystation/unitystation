
using System;
using UnityEngine;

/// <summary>
/// Client side. Defines how sprites of an object should rotate based on their parent matrix
///  and matrix rotations. If you want something to always stay rotated along with the matrix (such as shuttle thrusters),
/// simply omit this component from the object.
/// </summary>
[RequireComponent(typeof(RegisterTile))]
public class SpriteMatrixRotation : MonoBehaviour, IClientLifecycle
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

		//subscribe to matrix rotations
		registerTile.OnMatrixWillChange.AddListener(OnMatrixWillChange);
		OnMatrixWillChange(registerTile.Matrix);
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

		if (registerTile.MatrixIsMovable)
		{
			registerTile.Matrix.MatrixMove.OnRotateStart.RemoveListener(OnMatrixRotationStart);
			registerTile.Matrix.MatrixMove.OnRotateEnd.RemoveListener(OnMatrixRotationEnd);
		}
	}

	//makes sure it's removed from update manager at end of round since currently updatemanager is not
	//reset on round end.
	private void OnDisable()
	{
		UpdateManager.Instance.Remove(SetSpritesUpright);
		if (registerTile.MatrixIsMovable)
		{
			registerTile.Matrix.MatrixMove.OnRotateStart.RemoveListener(OnMatrixRotationStart);
			registerTile.Matrix.MatrixMove.OnRotateEnd.RemoveListener(OnMatrixRotationEnd);
		}
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


	//invoked when our parent matrix is being changed or initially set
	private void OnMatrixWillChange(Matrix newMatrix)
	{
		//add our listeners
		//unsub from old matrix
		if (registerTile.MatrixIsMovable)
		{
			Logger.LogTraceFormat("{0} unsubbed from matrix rotations for {1}", Category.Matrix, this, registerTile.Matrix);
			registerTile.Matrix.MatrixMove.OnRotateStart.RemoveListener(OnMatrixRotationStart);
			registerTile.Matrix.MatrixMove.OnRotateEnd.RemoveListener(OnMatrixRotationEnd);
		}

		//sub to new matrix
		if (newMatrix != null)
		{
			if (newMatrix.MatrixMove != null)
			{
				Logger.LogTraceFormat("{0} subbed to matrix rotations for {1}", Category.Matrix, this, newMatrix);
				newMatrix.MatrixMove.OnRotateStart.AddListener(OnMatrixRotationStart);
				newMatrix.MatrixMove.OnRotateEnd.AddListener(OnMatrixRotationEnd);
			}
			//changed matrices, so we have to re-orient as well (esp if this is the first matrix we subscribed to)
			SetSpritesUpright();
		}
	}

	private void OnMatrixRotationStart(RotationOffset fromCurrent, bool isInitialRotation)
	{
		if (spriteMatrixRotationBehavior == SpriteMatrixRotationBehavior.RemainUpright)
		{
			Logger.LogTraceFormat("{0} matrix rotation starting on {1}, forcing upright", Category.Matrix, this, registerTile.Matrix);
			UpdateManager.Instance.Add(SetSpritesUpright);
		}
	}

	/// <summary>
	/// Invoked when receiving rotation event from our current matrix's matrixmove
	/// </summary>
	//invoked when matrix rotation is ending
	private void OnMatrixRotationEnd(RotationOffset fromCurrent, bool isInitialRotation)
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
