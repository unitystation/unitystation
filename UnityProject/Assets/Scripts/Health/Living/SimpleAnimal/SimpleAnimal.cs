using System.Collections;
using UnityEngine;
using Mirror;

public class SimpleAnimal : LivingHealthBehaviour
{
	public Sprite aliveSprite;

	public Sprite deadSprite;

	//Syncvar hook so that new players can sync state on start
	[SyncVar(hook = nameof(SetAliveState))] public bool deadState;

	public SpriteRenderer spriteRend;

	public RegisterObject registerObject;

	void Awake()
	{
		registerObject = GetComponent<RegisterObject>();
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		StartCoroutine(WaitForLoad());
	}

	private IEnumerator WaitForLoad()
	{
		yield return WaitFor.Seconds(2f);
		SetAliveState(deadState, deadState);
	}

	[Server]
	protected override void OnDeathActions()
	{
		deadState = true;
	}

	private void SetAliveState(bool oldState, bool state)
	{
		deadState = state;
		registerObject.Passable = state;
		if (state)
		{
			spriteRend.sprite = deadSprite;
			SetToBodyLayer();
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