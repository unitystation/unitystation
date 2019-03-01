using UnityEngine;


[ExecuteInEditMode]
public class RegisterPlayer : RegisterTile
{



	/// <summary>
	/// Whether the player should currently be depicted laying on the ground
	/// </summary>
	private bool isDown;

	private PlayerSprites playerSprites;

	public bool IsBlocking { get; set; } = true;
	/// <summary>
	/// True when the player is laying down
	/// </summary>
	public bool IsDown => isDown;
	private void Awake()
	{
		playerSprites = GetComponent<PlayerSprites>();
		//initially we are upright and don't rotate with the matrix
		rotateWithMatrix = false;
	}

	public override bool IsPassable()
	{
		return !IsBlocking;
	}

	public override bool IsPassable(Vector3Int from)
	{
		return IsPassable();
	}

	protected override void OnRotationEnd(RotationOffset fromCurrent, bool isInitialRotation)
	{
		base.OnRotationEnd(fromCurrent, isInitialRotation);

		//add additional rotation to remain sideways if we are down
		if (isDown)
		{
			foreach (SpriteRenderer spriteRenderer in spriteRenderers)
			{
				spriteRenderer.transform.Rotate(0, 0, -90);
			}
		}
	}

	/// <summary>
	/// Cause the player sprite to appear as laying down, which also causes them to become
	/// passable
	/// </summary>
	public void LayDown()
	{
		if (!isDown)
		{
			isDown = true;
			IsBlocking = false;
			//make sure sprite is in sync with server regardless of local prediction
			playerSprites.SyncWithServer();
			//rotate the sprites and change their layer
			foreach (SpriteRenderer spriteRenderer in spriteRenderers)
			{
				spriteRenderer.transform.rotation = Quaternion.identity;
				spriteRenderer.transform.Rotate(0, 0, -90);
				spriteRenderer.sortingLayerName = "Blood";
			}

		}

	}

	/// <summary>
	/// Causes the player sprite to stand upright, causing them to become
	/// blocking again.
	/// </summary>
	public void GetUp()
	{
		if (isDown)
		{
			isDown = false;
			IsBlocking = true;
			//make sure sprite is in sync with server regardless of local prediction
			playerSprites.SyncWithServer();
			//change sprites to be upright
			foreach (SpriteRenderer spriteRenderer in spriteRenderers)
			{
				spriteRenderer.transform.rotation = Quaternion.identity;
				spriteRenderer.sortingLayerName = "Players";
			}
		}
	}
}