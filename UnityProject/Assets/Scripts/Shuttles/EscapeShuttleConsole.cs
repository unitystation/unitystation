using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Objects
{
	/// <summary>
	/// Escape shuttle logic
	/// </summary>
	public class EscapeShuttleConsole : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		[SerializeField]
		private float timeToHack = 20f;

		[SerializeField]
		private float chanceToFailHack = 25f;

		private bool beenEmagged;

		private RegisterTile registerTile;

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			TryEmagConsole(interaction);
		}

		private void TryEmagConsole(HandApply interaction)
		{
			if (beenEmagged)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "The shuttle has already been Emagged!");
				return;
			}

			Chat.AddActionMsgToChat(interaction.Performer, $"You attempt to hack the shuttle console, this will take around {timeToHack} seconds",
				$"{interaction.Performer.ExpensiveName()} starts hacking the shuttle console");

			var cfg = new StandardProgressActionConfig(StandardProgressActionType.Restrain);

			StandardProgressAction.Create(
				cfg,
				() => FinishHack(interaction)
			).ServerStartProgress(ActionTarget.Object(registerTile), timeToHack, interaction.Performer);

		}

		private void FinishHack(HandApply interaction)
		{
			if (DMMath.Prob(chanceToFailHack))
			{
				Chat.AddActionMsgToChat(interaction.Performer, "Your attempt to hack the shuttle console failed",
					$"{interaction.Performer.ExpensiveName()} failed to hack the shuttle console");
				return;
			}

			Chat.AddActionMsgToChat(interaction.Performer, "You hack the shuttle console",
				$"{interaction.Performer.ExpensiveName()} hacked the shuttle console");

			beenEmagged = true;

			if (GameManager.Instance.ShuttleSent) return;

			Chat.AddSystemMsgToChat("\n\n<color=#FF151F><size=40><b>Escape Shuttle Emergency Launch Triggered!</b></size></color>\n\n",
				MatrixManager.MainStationMatrix);

			Chat.AddSystemMsgToChat("\n\n<color=#FF151F><size=40><b>Escape Shuttle Emergency Launch Triggered!</b></size></color>\n\n",
				GameManager.Instance.PrimaryEscapeShuttle.MatrixInfo);

			_ = SoundManager.PlayNetworked(CommonSounds.Instance.Notice1);

			GameManager.Instance.ForceSendEscapeShuttleFromStation(10);
		}
	}
}
