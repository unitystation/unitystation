using UnityEngine;


[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/Backflip")]
public class Backflip : EmoteSO 
{
	public override void Do(GameObject player)
    {
        RotateEffect backflipEffect = PlayerManager.LocalPlayerScript.PlayerEffectsManager.GetComponent<RotateEffect>();
		Debug.Log(backflipEffect);
        backflipEffect.setupEffectvars(1, 0.2f, 180);
        backflipEffect.CmdStartAnimation();
        base.Do(player);
    }
}