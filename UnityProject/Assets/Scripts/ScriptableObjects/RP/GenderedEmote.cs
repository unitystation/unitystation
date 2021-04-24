using HealthV2;
using UnityEngine;

namespace ScriptableObjects.RP
{
	[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/GenderedEmote")]
	public class GenderedEmote : EmoteSO
	{
		private string viewTextFinal;

		public override void Do(GameObject player)
		{
			HealthCheck(player);
			Chat.AddActionMsgToChat(player, $"{youText}", $"{player.ExpensiveName()} {viewTextFinal}.");
			PlayAudio(GetBodyTypeAudio(player), player);
		}

		private void HealthCheck(GameObject player)
		{
			bool playerCondition = CheckPlayerCritState(player);

			viewTextFinal = playerCondition ? critViewText : viewText;
		}
	}
}
