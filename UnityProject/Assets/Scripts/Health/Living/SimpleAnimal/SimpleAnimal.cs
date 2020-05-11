using System.Collections;
using UnityEngine;
using Mirror;

public class SimpleAnimal : LivingHealthBehaviour
{
	public Sprite aliveSprite;

	public Sprite deadSprite;

	//Syncvar hook so that new players can sync state on start
	[SyncVar(hook = nameof(SyncAliveState))] public bool deadState;

	public SpriteRenderer spriteRend;

	public RegisterObject registerObject;

	[Server]
	public void SetDeadState(bool isDead)
	{
		deadState = isDead;
	}

	public override void Awake()
	{
		registerObject = GetComponent<RegisterObject>();
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		SyncAliveState(deadState, deadState);
	}

	[Server]
	protected override void OnDeathActions()
	{
		deadState = true;
	}

	private void SyncAliveState(bool oldState, bool state)
	{
		deadState = state;

		if (state)
		{
			spriteRend.sprite = deadSprite;
			SetToBodyLayer();
			registerObject.Passable = state;
		}
		else
		{
			spriteRend.sprite = aliveSprite;
			SetToNPCLayer();
		}
	}

	/// <summary>
	/// Set the sprite renderer to bodies when the mob has died
	/// </summary>
	public void SetToBodyLayer()
	{
		spriteRend.sortingLayerName = "Bodies";
	}

	/// <summary>
	/// Set the mobs sprite renderer to NPC layer
	/// </summary>
	public void SetToNPCLayer()
	{
		spriteRend.sortingLayerName = "NPCs";
	}
}