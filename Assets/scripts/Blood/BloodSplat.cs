using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Sprites;

public class BloodSplat : NetworkBehaviour
{

    public SpriteRenderer spriteRend;
    private Sprite[] bloodSprites;

    [SyncVar(hook = "SetSprite")]
    public int bloodSprite;

    public override void OnStartClient()
    {
        StartCoroutine(WaitForLoad());
        base.OnStartClient();
    }

    IEnumerator WaitForLoad()
    {
        yield return new WaitForSeconds(3f);
        SetSprite(bloodSprite);
    }

    void SetSprite(int spritenum)
    {
        bloodSprite = spritenum; //officially recognized unet problem (feature?), you need to manually update the syncvar int if using with hook
        if (bloodSprites == null)
        {
            bloodSprites = SpriteManager.BloodSprites["blood"];
        }
        spriteRend.sprite = bloodSprites[spritenum];
        spriteRend.enabled = true;
    }

}
