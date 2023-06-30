using HealthV2;
using Items.Implants.Organs.Vomit;
using ScriptableObjects.RP;
using UnityEngine;

namespace Player.EmoteScripts
{
	[CreateAssetMenu(fileName = "Vomit", menuName = "ScriptableObjects/RP/Emotes/Vomit")]
	public class Vomit : GenderedEmote
	{

		[SerializeField] private bool instant = false;

		public override void Do(GameObject player)
		{
			var health = player.GetComponent<LivingHealthMasterBase>();
			if (health.IsDead) return;

			if (instant == false)
			{
				StandardProgressAction action = StandardProgressAction.Create(
					new StandardProgressActionConfig(StandardProgressActionType.SelfHeal),
					() => CheckAndDo(player, health));
				Chat.AddActionMsgToChat(player, $"<color=red>{health.playerScript.visibleName} attempts to make themselves vomit.</color>");
				action.ServerStartProgress(player.RegisterTile(), 6f, player);
				return;
			}
			CheckAndDo(player, health);
		}

		private void CheckAndDo(GameObject player, LivingHealthMasterBase health)
		{
			var bodyParts = health.BodyPartList;
			foreach (var part in bodyParts)
			{
				if (part.TryGetComponent<StomachExpulsion>(out var stomach) == false) continue;
				stomach.Vomit();
				base.Do(player);
				return;
			}
			if(instant == false) Chat.AddExamineMsg(player, "You do not have a stomach to do this...");
		}
	}
}