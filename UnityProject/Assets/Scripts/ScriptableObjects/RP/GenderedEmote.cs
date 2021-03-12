using HealthV2;
using UnityEngine;

namespace ScriptableObjects.RP
{
	[CreateAssetMenu(fileName = "Emote", menuName = "ScriptableObjects/RP/Emotes/GenderedEmote")]
	public class GenderedEmote : EmoteSO
	{
		[SerializeField]
		private string critViewText = "screams in pain!";

		private string viewTextFinal;

		public override void Do(GameObject player)
		{
			HealthCheck(player);
			Chat.AddActionMsgToChat(player, $"{youText}", $"{player.ExpensiveName()} {viewTextFinal}.");
			PlayAudio(GetBodyTypeAudio(player), player);
		}

		private void HealthCheck(GameObject player)
		{
			var health = player.GetComponent<LivingHealthMasterBase>();

			if (health == null || health.IsDead)
			{
				return;
			}

			viewTextFinal = health.IsCrit ? critViewText : viewText;
		}
	}
}
