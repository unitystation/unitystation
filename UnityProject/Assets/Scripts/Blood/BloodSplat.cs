using System.Collections;
using Sprites;
using UnityEngine;
using UnityEngine.Networking;

public class BloodSplat : NetworkBehaviour
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
}