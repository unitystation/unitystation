using UnityEngine;
using US13.ScriptableObjects.RP;

namespace US13.Player.EmoteScripts
{
	[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/Backflip")]
	public class Backflip : EmoteSO
	{
		public override void Do(GameObject actor)
		{
			if (CheckPlayerCritState(actor) == false && CheckIfPlayerIsCrawling(actor) == false)
			{
				var manager = actor.GetComponent<PlayerEffectsManager>();
				manager.RotatePlayer(2, 0.176f, 179, true, true);
				base.Do(actor);
			}
			else
			{
				base.Do(actor);
			}
		}
	}
}