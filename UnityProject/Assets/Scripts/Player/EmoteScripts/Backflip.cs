using ScriptableObjects.RP;
using UnityEngine;

namespace Player.EmoteScripts
{
	[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/Backflip")]
	public class Backflip : EmoteSO
	{
		public override void Do(GameObject player)
		{
			if (CheckPlayerCritState(player) == false && CheckIfPlayerIsCrawling(player) == false)
			{
				var manager = player.GetComponent<PlayerEffectsManager>();
				manager.RotatePlayer(1, 0.2f, 180, false);
				base.Do(player);
			}
			else
			{
				base.Do(player);
			}
		}
	}
}