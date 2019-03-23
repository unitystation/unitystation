using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public partial class FloorDecal : CustomNetTransform
{
	[SyncVar(hook = "SetSprite")] public int sprite;
	protected Sprite[] Sprites;
	public SpriteRenderer spriteRend;

	public override void OnStartClient()
	{
		StartCoroutine(WaitForLoad());
		base.OnStartClient();
	}

	private IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(3f);
		SetSprite(sprite);
		Splash();
	}

	protected virtual void Splash()
	{

	}

	private void SetSprite(int spritenum)
	{
		sprite = spritenum; //officially recognized unet problem (feature?), you need to manually update the syncvar int if using with hook
		if (Sprites == null)
		{
			GetSprites();
		}

		spriteRend.sprite = Sprites[spritenum];
		spriteRend.enabled = true;
	}

	protected virtual void GetSprites()
	{

	}

	protected override void OnHit(Vector3Int pos, ThrowInfo info, List<LivingHealthBehaviour> objects,
		List<TilemapDamage> tiles)
	{
		//base.OnHit( pos, info, objects, tiles );
		//umm todo
	}
}