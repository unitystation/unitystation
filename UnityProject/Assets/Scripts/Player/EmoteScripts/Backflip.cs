using UnityEngine;


[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/Backflip")]
public class Backflip : EmoteSO 
{
	public override void Do(GameObject player)
    {
        RotateEffect backflipEffect = player.transform.GetComponent<PlayerScript>().PlayerEffectsManager.GetComponent<RotateEffect>();
		if(backflipEffect == null)
		{
			Logger.LogError("[EmoteSO/Backflip] - Could not find a rotate effect on the player!");
			return;
		}
        backflipEffect.setupEffectvars(1, 0.2f, 180, true);
        backflipEffect.CmdStartAnimation();
        base.Do(player);
    }
}