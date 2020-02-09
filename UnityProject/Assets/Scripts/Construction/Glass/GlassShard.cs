using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

public class GlassShard : NetworkBehaviour
{
	public Sprite[] glassSprites;

	[SyncVar(hook = nameof(SpriteChange))]
	public int spriteIndex;

	private SpriteRenderer spriteRenderer;

	void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
	}

	public override void OnStartClient()
	{
		EnsureInit();
		base.OnStartClient();
		SpriteChange(spriteIndex, spriteIndex);
	}

	[Server]
	public void SetSpriteAndScatter(int index){
		//Set the syncvar and update to all clientS:
		SpriteChange(spriteIndex, index);

		var netTransform = GetComponent<CustomNetTransform>();

		netTransform?.SetPosition(netTransform.ServerState.WorldPosition + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f)));
	}

	[Server]
	public void SetRandomSprite()
	{
		SpriteChange(spriteIndex, Random.Range(0, glassSprites.Length));
	}

	public void SpriteChange(int oldValue, int index)
	{
		EnsureInit();
		spriteIndex = index;
		spriteRenderer.sprite = glassSprites[spriteIndex];



		//Scatter them around (just for visual candy, no need to network sync as they are on the same grid co-ord anyway):
		// spriteRenderer.transform.localPosition = new Vector3(
		// 	Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f), 0f);

		//Also add a bit of rotation variance to the sprite obj:
		var axis = new Vector3(0,0,1);
		spriteRenderer.transform.localRotation = Quaternion.AngleAxis(Random.Range(-180f, 180f), axis);


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
			SoundManager.PlayNetworkedAtPos("GlassStep", coll.transform.position, Random.Range(0.8f, 1.2f));
		}
	}
}