using UnityEngine;
using US13.Core.Chat;
using US13.HealthV2.Living;
using US13.Items.Implants.Organs.Vomit;
using US13.ScriptableObjects.RP;
using US13.UI.Core.ProgressBar;
using Util;

namespace US13.Player.EmoteScripts
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
			bool FailedDryHeave = false;
			var bodyParts = health.BodyPartList;
			foreach (var part in bodyParts)
			{
				if (part == null) continue;
				if (part.TryGetComponent<StomachExpulsion>(out var stomach) == false) continue;
				if (stomach.WillDryHeave())
				{
					FailedDryHeave = true;
					continue;
				}
				stomach.Vomit();
				base.Do(player);
				return;
			}

			if (FailedDryHeave)
			{
				Chat.AddExamineMsg(player, "Your stomach is empty");
				return;
			}

			if (instant == false)
			{
				Chat.AddExamineMsg(player, "You do not have a stomach to do this...");
				return;
			}
		}
	}
}