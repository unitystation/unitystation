using ScriptableObjects.RP;
using UnityEngine;

namespace Player.EmoteScripts
{
	[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/Backflip")]
	public class Backflip : EmoteSO
	{
		public override void Do(GameObject player)
		{
			var manager = player.GetComponent<PlayerEffectsManager>();
			if(manager == null)
			{
				Logger.LogError("[EmoteSO/Backflip] - Could not find a rotate effect on the player!");
				return;
			}
			manager.RotatePlayer(1, 0.2f, 180, false);
			base.Do(player);
		}
	}
}