using System;
using System.Linq;
using Systems.Electricity;
using Objects.Research;
using UnityEngine;

namespace Systems.Ai
{
	public class AiUploadConsole : MonoBehaviour, ICheckedInteractable<HandApply>, IExaminable
	{
		[SerializeField]
		private ItemTrait moduleTrait = null;

		private AiPlayer selectedAiPlayer;
		private int lastIndex;

		private APCPoweredDevice apcPoweredDevice;

		private void Awake()
		{
			apcPoweredDevice = GetComponent<APCPoweredDevice>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (Validations.HasItemTrait(interaction.HandObject, moduleTrait)) return true;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (apcPoweredDevice.State == PowerState.Off)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"{gameObject.ExpensiveName()} has no power");
				return;
			}

			//Cycle through AIs
			if (interaction.HandObject == null)
			{
				var aiPlayers = PlayerList.Instance.GetAllByPlayersOfState(PlayerScript.PlayerStates.Ai).Where(
					a => a.GameObject.TryGetComponent<AiPlayer>(out var aiPlayer) && aiPlayer.HasDied == false).ToList();

				if (lastIndex >= aiPlayers.Count)
				{
					lastIndex = 0;
				}

				if (aiPlayers.Count == 0)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "There are no Ai's");
					return;
				}

				selectedAiPlayer = aiPlayers[lastIndex].GameObject.GetComponent<AiPlayer>();

				Chat.AddExamineMsgFromServer(interaction.Performer, $"{selectedAiPlayer.gameObject.ExpensiveName()} selected");

				lastIndex++;
				return;
			}

			//Make sure Ai selected
			if (selectedAiPlayer == null)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Select an Ai to upload laws to first");
				return;
			}

			//Check Ai isn't dead
			if (selectedAiPlayer.HasDied)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"Unable to connect to {selectedAiPlayer.gameObject.ExpensiveName()}");
				return;
			}

			//Must have used module, but do check in case
			if (interaction.HandObject.TryGetComponent<AiLawModule>(out var module) == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Can only use a module on this console");
				return;
			}

			var lawFromModule = module.GetLawsFromModule(interaction.PerformerPlayerScript);

			if (module.AiModuleType == AiModuleType.Purge || module.AiModuleType == AiModuleType.Reset)
			{
				var isPurge = module.AiModuleType == AiModuleType.Purge;
				selectedAiPlayer.ResetLaws(isPurge);
				Chat.AddActionMsgToChat(interaction.Performer, $"You {(isPurge ? "purge" : "reset")} all of {selectedAiPlayer.gameObject.ExpensiveName()}'s laws",
					$"{interaction.Performer.ExpensiveName()} {(isPurge ? "purges" : "resets")} all of {selectedAiPlayer.gameObject.ExpensiveName()}'s laws");
				return;
			}

			if (lawFromModule.Count == 0)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "No laws to upload");
				return;
			}

			//If we are only adding core laws then we must mean to remove old core laws
			//This means we are assuming that the law set must only have core laws if it is to replace the old laws fully
			var notOnlyCoreLaws = false;

			foreach (var law in lawFromModule)
			{
				if (law.Key != AiPlayer.LawOrder.Core)
				{
					notOnlyCoreLaws = true;
					break;
				}
			}

			selectedAiPlayer.SetLaws(lawFromModule, true, notOnlyCoreLaws);

			Chat.AddActionMsgToChat(interaction.Performer, $"You change {selectedAiPlayer.gameObject.ExpensiveName()} laws",
				$"{interaction.Performer.ExpensiveName()} changes {selectedAiPlayer.gameObject.ExpensiveName()} laws");
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			if (selectedAiPlayer != null)
			{
				return $"{selectedAiPlayer.gameObject.ExpensiveName()} selected for law change";
			}

			return "";
		}
	}
}
