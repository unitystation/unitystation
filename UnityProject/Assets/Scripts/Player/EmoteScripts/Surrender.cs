using Logs;
using ScriptableObjects.RP;
using UnityEngine;

namespace Player.EmoteScripts
{
	[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/Surrender")]
	public class Surrender : EmoteSO
	{
		private RegisterPlayer registerPlayer;

		public override void Do(GameObject actor)
		{
			registerPlayer = actor.GetComponent<RegisterPlayer>();

			if (registerPlayer == null)
			{
				Loggy.LogError("RegisterPlayer could not be found!");
				Chat.AddActionMsgToChat(actor, $"{failText}", "");
				return;
			}
			SurrenderLogic(actor);
		}

		private void SurrenderLogic(GameObject player)
		{
			bool isCrawling = CheckIfPlayerIsCrawling(player);
			if (CheckPlayerCritState(player) == false && isCrawling == false)
			{
				LayDown(player);
			}
			if(CheckPlayerCritState(player) == true)
			{
				FailText(player, FailType.Critical);
			}
			if(isCrawling == true)
			{
				base.Do(player);
			}
		}
		private void LayDown(GameObject player)
		{
			if (CheckHandState(player) == false)
			{
				Chat.AddActionMsgToChat(player, "You surrender in defeat then lay faced down on the ground.", $"{player.ExpensiveName()} surrenders in defeat then lay faced down on the ground.");
			}
			else
			{
				base.Do(player);
			}

			registerPlayer.ServerSetIsStanding(false);
		}
	}
}
