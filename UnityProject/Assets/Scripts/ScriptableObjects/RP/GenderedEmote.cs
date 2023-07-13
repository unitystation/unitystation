using HealthV2;
using UnityEngine;

namespace ScriptableObjects.RP
{
	[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/GenderedEmote")]
	public class GenderedEmote : EmoteSO
	{
		private string viewTextFinal;

		/// <summary>
		/// Gendered Emote is designed for Players only and any NPC that uses HealthV2
		/// </summary>
		public override void Do(GameObject player)
		{
			if(CheckAllBaseConditions(player) == false) return;
			HealthCheck(player);
			if (string.IsNullOrEmpty(youText))
			{
				Chat.AddActionMsgToChat(player, $"{player.ExpensiveName()} {viewTextFinal}.");
			}
			else
			{
				Chat.AddActionMsgToChat(player, $"{youText}", $"{player.ExpensiveName()} {viewTextFinal}.");
			}
			if (soundsAreTyped)
			{
				PlayAudio(GetBodyTypeAudio(player), player);
				return;
			}
			PlayAudio(defaultSounds, player);
		}

		private void HealthCheck(GameObject player)
		{
			bool playerCondition = CheckPlayerCritState(player);

			viewTextFinal = playerCondition ? critViewText : viewText;
		}
	}
}
