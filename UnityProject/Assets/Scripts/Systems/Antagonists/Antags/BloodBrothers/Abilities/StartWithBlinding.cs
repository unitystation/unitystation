using Actions.V2;
using Antagonists;
using Core;
using UnityEngine;

namespace Systems.Antagonists.Antags.BloodBrothers.Abilities
{
	public class StartWithBlinding : IBloodBrotherAbility
	{
		public float ChanceToGiveOnSpawn { get; } = 5f;

		[SerializeField] private ActionButtonData _serverAddAction = new ActionButtonData()
		{
			ID = "BB_BlindingLight",
			DisplayName = "Blinding Light",
			CooldownTime = 225f,
			Description = "Release a bright shining light that temporarily blinds everyone that's near a blood brother.",
			TriggerType = ActionTriggerType.ServerOnly,
			Type = ActionType.Trigger,
			CanUseWhileGhosting = true,
		};

		public void GiveAbility(Mind mind)
		{
			mind.PlayerButtonedActions.ServerAddAction(_serverAddAction, BlindEveryoneNearby);
		}

		private void BlindEveryoneNearby(Vector2 position)
		{
			foreach (var antag in AntagManager.Instance.ActiveAntags)
			{
				if (antag.Antagonist is BloodBrother)
				{
					Chat.AddActionMsgToChat(antag.Owner.Body.gameObject ,$"A blinding light erupts from {antag.Owner.Body.playerName}!");
					var nearbyMobs = ComponentsTracker<PlayerScript>.GetAllNearbyTypesToTarget(antag.Owner.Body.gameObject, 25f, false);
					foreach (var mob in nearbyMobs)
					{
						if (mob == antag.Owner.Body) continue;
						mob.playerHealth.TryFlash(3f);
						mob.playerHealth.TryDeafen(antag.gameObject, 4f);
					}
				}
			}
		}
	}
}