using System;
using System.Collections;
using System.Collections.Generic;
using AdminCommands;
using Managers;
using UnityEngine;
using Strings;

namespace Objects
{
	/// <summary>
	/// Escape shuttle logic
	/// </summary>
	public class EscapeShuttleConsole : MonoBehaviour, ICheckedInteractable<HandApply>, IRightClickable
	{
		[SerializeField]
		private float timeToHack = 20f;

		[SerializeField]
		private float chanceToFailHack = 25f;

		private bool beenEmagged;

		private RegisterTile registerTile;

		[SerializeField] private List<Access> validAccess = new List<Access>();

		private HashSet<IDCard> registeredIDs = new HashSet<IDCard>();

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag) || Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Id);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject.TryGetComponent<IDCard>(out var card))
			{
				if(card.HasAccess(validAccess)) RegisterEarlyShuttleLaunch(card, interaction.PerformerPlayerScript);
				return;
			}
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

			DepartShuttle();
		}

		private void RegisterEarlyShuttleLaunch(IDCard card, PlayerScript performer)
		{
			if (GameManager.Instance.ShuttleSent)
			{
				Chat.AddExamineMsg(performer.gameObject, "The shuttle is already moving!");
				return;
			}
			if (registeredIDs.Contains(card))
			{
				Chat.AddExamineMsg(performer.gameObject, "You've already done this!");
				return;
			}
			registeredIDs.Add(card);

			if (GameManager.Instance.CentComm.CurrentAlertLevel is CentComm.AlertLevel.Red or CentComm.AlertLevel.Delta && registeredIDs.Count >= 2)
			{
				DepartShuttle();
				return;
			}

			if (registeredIDs.Count >= 4)
			{
				DepartShuttle();
				return;
			}

			AnnounceRemainingSwipesRequired();
		}

		private void AnnounceRemainingSwipesRequired()
		{
			var swpiesRequired = GameManager.Instance.CentComm.CurrentAlertLevel is CentComm.AlertLevel.Red or CentComm.AlertLevel.Delta ? 2 : 4;
			var remainingSwipes = swpiesRequired - registeredIDs.Count;
			string announcemnt = $"\n\n<color=#FF151F><size={ChatTemplates.LargeText}><b>Escape Shuttle Emergency Launch has been request! need {remainingSwipes} more votes.</b></size></color>\n\n"
			Chat.AddSystemMsgToChat(announcemnt, MatrixManager.MainStationMatrix);

			Chat.AddSystemMsgToChat($"\n\n<color=#FF151F><size={ChatTemplates.LargeText}><b>Escape Shuttle Emergency Launch has been request! need {remainingSwipes} more votes.</b></size></color>\n\n",
				GameManager.Instance.PrimaryEscapeShuttle.MatrixInfo);
			_ = SoundManager.PlayNetworked(CommonSounds.Instance.Notice1);
		}

		private void DepartShuttle()
		{
			if (GameManager.Instance.ShuttleSent) return;
			var departTime = beenEmagged ? 5 : 10;
			string announcement =
				$"\n\n<color=#FF151F><size={ChatTemplates.LargeText}><b>Escape Shuttle Emergency Launch Triggered! Launching in {departTime} seconds..</b></size></color>\n\n";
			Chat.AddSystemMsgToChat(announcement, MatrixManager.MainStationMatrix);

			Chat.AddSystemMsgToChat(announcement, GameManager.Instance.PrimaryEscapeShuttle.MatrixInfo);

			_ = SoundManager.PlayNetworked(CommonSounds.Instance.Notice1);
			GameManager.Instance.ForceSendEscapeShuttleFromStation(departTime);
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			if (CustomNetworkManager.IsServer == false) return null;
			return RightClickableResult.Create().AddElement("Launch Early", DepartShuttle, Color.red);
		}
	}
}
