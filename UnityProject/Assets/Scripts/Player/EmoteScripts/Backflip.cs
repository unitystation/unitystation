using UnityEngine;

public class Backflip : Emote 
{
	public override void Do(GameObject player)
    {
        RotateEffect backflipEffect = PlayerManager.LocalPlayerScript.PlayerEffectsManager.GetComponent<RotateEffect>();
        backflipEffect.setupEffectvars(1, 0.2f, 180);
        backflipEffect.CmdStartAnimation();
        base.Do(player);
    }
}