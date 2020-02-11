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
		if (state)
		{
			spriteRend.sprite = deadSprite;
		}
		else
		{
			spriteRend.sprite = aliveSprite;
		}
	}
}