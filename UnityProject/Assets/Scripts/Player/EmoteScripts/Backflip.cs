using ScriptableObjects.RP;
using UnityEngine;

namespace Player.EmoteScripts
{
	[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/Backflip")]
	public class Backflip : EmoteSO
	{
		public override void Do(GameObject actor)
		{
			if (CheckPlayerCritState(actor) == false && CheckIfPlayerIsCrawling(actor) == false)
			{
				var manager = actor.GetComponent<PlayerEffectsManager>();
				manager.RotatePlayer(1, 0.2f, 180, false);
				base.Do(actor);
			}
			else
			{
				base.Do(actor);
			}
		}
	}
}