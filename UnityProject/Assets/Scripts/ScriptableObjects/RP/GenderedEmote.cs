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
		public override void Do(GameObject actor)
		{
			if(CheckAllBaseConditions(actor) == false) return;
			HealthCheck(actor);
			RunBehaviors(actor);
			if (string.IsNullOrEmpty(youText))
			{
				Chat.AddActionMsgToChat(actor, $"{actor.ExpensiveName()} {viewTextFinal}.");
			}
			else
			{
				Chat.AddActionMsgToChat(actor, $"{youText}", $"{actor.ExpensiveName()} {viewTextFinal}.");
			}
			if (soundsAreTyped)
			{
				PlayAudio(GetBodyTypeAudio(actor), actor);
				return;
			}
			PlayAudio(defaultSounds, actor);
		}

		private void HealthCheck(GameObject player)
		{
			bool playerCondition = CheckPlayerCritState(player);

			viewTextFinal = playerCondition ? critViewText : viewText;
		}
	}
}
