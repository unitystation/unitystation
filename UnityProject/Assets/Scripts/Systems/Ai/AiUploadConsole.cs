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
				//Only allow alive AI's cores, or alive AI's carded which have interactions enabled to have law changes
				var aiPlayers = PlayerList.Instance.GetAllByPlayersOfState(PlayerScript.PlayerStates.Ai).Where(
					a => a.GameObject.TryGetComponent<AiPlayer>(out var aiPlayer) && aiPlayer.HasDied == false &&
					     (aiPlayer.IsCarded == false || aiPlayer.AllowRemoteAction)).ToList();

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

			//Try to upload law module
			selectedAiPlayer.UploadLawModule(interaction, true);
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
