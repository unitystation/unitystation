using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BloodSplat : CustomNetTransform
{
	[SyncVar(hook = "SetSprite")] public int bloodSprite;
	private Sprite[] bloodSprites;
	public SpriteRenderer spriteRend;

	public override void OnStartClient()
	{
		StartCoroutine(WaitForLoad());
		base.OnStartClient();
	}

	private IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(3f);
		SetSprite(bloodSprite);
	}

	private void SetSprite(int spritenum)
	{
		bloodSprite =
			spritenum; //officially recognized unet problem (feature?), you need to manually update the syncvar int if using with hook
		if (bloodSprites == null)
		{
			bloodSprites = SpriteManager.BloodSprites["blood"];
		}
		spriteRend.sprite = bloodSprites[spritenum];
		spriteRend.enabled = true;
	}

	protected override void OnHit( Vector3Int pos, ThrowInfo info, List<LivingHealthBehaviour> objects, List<TilemapDamage> tiles )
	{
//		base.OnHit( pos, info, objects, tiles );
		//umm todo
	}
}