using UnityEngine;
using Mirror;


[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/Backflip")]
public class Backflip : EmoteSO 
{
	public override void Do(GameObject player)
    {
		PlayerEffectsManager manager = player.transform.GetComponent<PlayerScript>().PlayerEffectsManager;
		if(manager == null)
		{
			Logger.LogError("[EmoteSO/Backflip] - Could not find a rotate effect on the player!");
			return;
		}
        manager.RotatePlayer(1, 0.2f, 180, true);
        base.Do(player);
    }
}