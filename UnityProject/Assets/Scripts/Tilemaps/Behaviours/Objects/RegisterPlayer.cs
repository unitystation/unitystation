using UnityEngine;


[ExecuteInEditMode]
public class RegisterPlayer : RegisterTile
{

	/// <summary>
	/// Whether the player should currently be depicted laying on the ground
	/// </summary>
	private bool isDown;

	private UserControlledSprites playerSprites;
	private PlayerScript playerScript;

	public bool IsBlocking => !playerScript.IsGhost && !isDown;

	/// <summary>
	/// True when the player is laying down
	/// </summary>
	public bool IsDown => isDown;
	private void Awake()
	{
		playerSprites = GetComponent<UserControlledSprites>();
		playerScript = GetComponent<PlayerScript>();
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

	protected override void OnRotationStart(RotationOffset fromCurrent, bool isInitialRotation)
	{
		base.OnRotationStart(fromCurrent, isInitialRotation);
		if (!isInitialRotation)
		{
			UpdateManager.Instance.Add(RemainUpright);
		}
	}

	void RemainUpright()
	{
		//stay upright until rotation stops (RegisterTile only updates our rotation at the end of rotation),
		//but players need to stay upright constantly unless they are downed
		foreach (SpriteRenderer renderer in spriteRenderers)
		{
			renderer.transform.rotation = isDown ? Quaternion.Euler(0, 0, -90) : Quaternion.identity;
		}
	}

	protected override void OnRotationEnd(RotationOffset fromCurrent, bool isInitialRotation)
	{
		base.OnRotationEnd(fromCurrent, isInitialRotation);

		if (!isInitialRotation)
		{
			//stop reorienting to face upright
			UpdateManager.Instance.Remove(RemainUpright);
		}

		//add extra rotation to ensure we are sideways
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