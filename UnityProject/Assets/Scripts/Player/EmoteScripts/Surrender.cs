using ScriptableObjects.RP;
using UnityEngine;

namespace Player.EmoteScripts
{
	[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/Surrender")]
	public class Surrender : EmoteSO
	{
		public override void Do(GameObject player)
		{
			if(player.GetComponent<RegisterPlayer>() == null)
			{
				Logger.LogError("RegisterPlayer could not be found!");
				Chat.AddActionMsgToChat(player, $"{failText}", "");
				return;
			}
			if(CheckHandState(player) == false)
			{
				Chat.AddActionMsgToChat(player, "You surrender in defeat then lay faced down on the ground.", $"{player.ExpensiveName()} surrenders in defeat then lay faced down on the ground.");
			}
			else
			{
				base.Do(player);
			}
			player.GetComponent<RegisterPlayer>().ServerSetIsStanding(false);
		}
	}
}
