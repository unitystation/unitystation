using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

public class GlassShard : NetworkBehaviour, IServerSpawn
{
	public Sprite[] glassSprites;

	[SyncVar(hook = nameof(SyncSpriteIndex))]
	private int spriteIndex;

	private SpriteRenderer spriteRenderer;
	private CustomNetTransform netTransform;

	void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		netTransform = GetComponent<CustomNetTransform>();
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		SyncSpriteIndex(spriteIndex, spriteIndex);
	}

	[Server]
	public void SetSpriteAndScatter(int index)
	{
		SyncSpriteIndex(spriteIndex, index);
		netTransform?.SetPosition(netTransform.ServerState.WorldPosition + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f)));
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		EnsureInit();
		SetSpriteAndScatter(Random.Range(0, glassSprites.Length));
	}

	private void SyncSpriteIndex(int oldValue, int index)
	{
		EnsureInit();

		spriteIndex = index;

		if (spriteRenderer != null)
		{
			spriteRenderer.sprite = glassSprites[spriteIndex];

			//Add a bit of rotation variance to the sprite obj:
			var axis = new Vector3(0, 0, 1);
			spriteRenderer.transform.localRotation = Quaternion.AngleAxis(Random.Range(-180f, 180f), axis);
		}
	}

	//Serverside only
	public void OnTriggerEnter2D(Collider2D coll)
	{
		if (!isServer)
		{
			return;
		}

		//8 = Players layer
		if (coll.gameObject.layer == 8)
		{
			SoundManager.PlayNetworkedAtPos("GlassStep", coll.transform.position, Random.Range(0.8f, 1.2f), sourceObj: gameObject);
		}
	}
}