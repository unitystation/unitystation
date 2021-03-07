using UnityEngine;


[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/Backflip")]
public class Backflip : EmoteSO 
{
	public override void Do(GameObject player)
    {
        RotateEffect backflipEffect = PlayerManager.LocalPlayerScript.PlayerEffectsManager.GetComponent<RotateEffect>();
        backflipEffect.setupEffectvars(1, 0.2f, 180, true);
        backflipEffect.CmdStartAnimation();
        base.Do(player);
    }
}