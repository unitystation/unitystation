using System.Collections;
using UnityEngine;


[ExecuteInEditMode]
public class RegisterPlayer : RegisterTile
{
	private float stunTime;
	private bool isStunned;
	public float StunDuration { get; private set; } = 0;
	private float tickRate = 1f;
	private float tick = 0;

	/// <summary>
	/// Whether the player should currently be depicted laying on the ground
	/// </summary>
	private bool isDown;

	private UserControlledSprites playerSprites;
	private PlayerScript playerScript;
	private MetaDataLayer metaDataLayer;

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
		metaDataLayer = transform.GetComponentInParent<MetaDataLayer>();
	}

	void OnEnable()
	{
		UpdateManager.Instance.Add(UpdateMe);
	}

	void OnDisable()
	{
		if (UpdateManager.Instance != null)
			UpdateManager.Instance.Remove(UpdateMe);
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

	//Handled via UpdateManager
	void UpdateMe()
	{
		//Server Only:
		if (CustomNetworkManager.Instance._isServer)
		{
			tick += Time.deltaTime;
			if (tick > tickRate)
			{
				tick = 0f;
				CalculateStun();
			}
		}
	}

	public void CheckTileSlip()
	{
		if (metaDataLayer.IsSlipperyAt(transform.position.CutToInt()))
		{
			Stun();
		}
	}

	public void Stun(float stunDuration = 4f, bool dropItem = true)
	{
		isStunned = true;
		LayDown();
		if (dropItem)
		{
			playerScript.playerNetworkActions.DropItem("leftHand");
			playerScript.playerNetworkActions.DropItem("rightHand");
		}

		playerScript.playerMove.allowInput = false;
		TryChangeStunDuration(stunDuration);
	}

	public void RemoveStun()
	{
		isStunned = false;
		UpdateCanMove();
	}

	private void CalculateStun()
	{
		if (StunDuration > 0)
		{
			StunDuration -= 1;
			if (StunDuration <= 0)
			{
				RemoveStun();
			}
		}
	}

	public void TryChangeStunDuration(float stunDuration)
	{
		if (stunDuration > StunDuration)
		{
			StunDuration = stunDuration;
		}
	}

	private void UpdateCanMove()
	{
		if (playerScript.playerHealth.IsCrit || playerScript.playerHealth.IsSoftCrit ||
		    playerScript.playerHealth.IsDead || isStunned)
		{
			return;
		}

		GetUp();
		playerScript.playerMove.allowInput = true;
	}
}