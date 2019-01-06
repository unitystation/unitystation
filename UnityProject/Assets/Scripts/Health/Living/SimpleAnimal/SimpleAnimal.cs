using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class SimpleAnimal : LivingHealthBehaviour
{
	public Sprite aliveSprite;
	
	public Sprite deadSprite;

	//Syncvar hook so that new players can sync state on start
	[SyncVar(hook = "SetAliveState")] public bool deadState;

	public SpriteRenderer spriteRend;

	private void Start()
	{
		//Set it automatically because we are using the SimpleAnimalBehaviour
		isNotPlayer = true;
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		StartCoroutine(WaitForLoad());
	}

	private IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(2f);
		SetAliveState(deadState);
	}

	[Server]
	protected override void OnDeathActions()
	{
		deadState = true;
	}

	private void SetAliveState(bool state)
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