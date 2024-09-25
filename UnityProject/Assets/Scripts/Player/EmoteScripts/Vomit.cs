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

		public override void Do(GameObject actor)
		{
			var health = actor.GetComponent<LivingHealthMasterBase>();
			if (health.IsDead) return;

			if (instant == false)
			{
				StandardProgressAction action = StandardProgressAction.Create(
					new StandardProgressActionConfig(StandardProgressActionType.SelfHeal),
					() => CheckAndDo(actor, health));
				Chat.AddActionMsgToChat(actor, $"<color=red>{health.playerScript.visibleName} attempts to make themselves vomit.</color>");
				action.ServerStartProgress(actor.RegisterTile(), 6f, actor);
				return;
			}
			CheckAndDo(actor, health);
		}

		private void CheckAndDo(GameObject player, LivingHealthMasterBase health)
		{
			var bodyParts = health.BodyPartList;
			foreach (var part in bodyParts)
			{
				if (part.TryGetComponent<StomachExpulsion>(out var stomach) == false) continue;
				if (stomach.WillDryHeave()) continue;
				stomach.Vomit();
				base.Do(player);
				return;
			}
			if (instant == false) Chat.AddExamineMsg(player, "You do not have a stomach to do this...");
		}
	}
}