using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using Items;
using Mirror;
using UnityEngine;
using UI.Objects.Cargo;
using Systems.Cargo;

namespace Objects.Cargo
{
	public class CargoConsole : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		public bool CorrectID;
		public bool Emagged;

		public GUI_Cargo cargoGUI;

		[SerializeField]
		private List<JobType> allowedTypes = null;

		[SerializeField] private AddressableAudioSource creditArrivalSound;
		private bool soundIsOnCooldown = false;

		[SerializeField] private string offlineMessage = "The console flashes red as an error message appears and says that access is denied.";

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side))
				return false;
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Id))
				return true;
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag))
				return true;
			return false;
		}

		/// <summary>
		/// Resets the ID to false
		/// </summary>
		[Server]
		public void ResetID()
		{
			CorrectID = false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if(CargoOfflineCheck()) return;
			if (interaction.HandSlot.Item.TryGetComponent<IDCard>(out var id))
			{

				CheckID(id.JobType, interaction.Performer);
				return;
			}
			Emag mag = interaction.HandSlot.Item.GetComponent<Emag>();
			if (mag == null || Emagged) return;
			if (mag.UseCharge(interaction))
			{
				Emagged = true;
				CorrectID = true;
				if (cargoGUI) cargoGUI.pageCart.UpdateTab();
			}
		}

		[Server]
		private void CheckID(JobType usedID, GameObject playeref)
		{
			if (cargoGUI == null) return;

			foreach (var aJob in allowedTypes.Where(aJob => usedID == aJob))
			{
				CorrectID = true;
				cargoGUI.pageCart.UpdateTab();
				break;
			}

			var denyString = "the console denies your ID";
			if (CorrectID)
			{
				denyString = "the console accepts your ID";
			}
			Chat.AddActionMsgToChat(playeref, $"You swipe your ID through the supply console's ID slot, {denyString}",
				$"{playeref.ExpensiveName()} swiped their ID through the supply console's ID slot");

		}

		public void PlayBudgetUpdateSound()
		{
			_ = SoundManager.PlayNetworkedAtPosAsync(creditArrivalSound, gameObject.WorldPosServer());
			StartCoroutine(SoundCooldown());
		}

		public bool CargoOfflineCheck()
		{
			if(CargoManager.Instance.CargoOffline)
			{
				Chat.AddActionMsgToChat(gameObject, offlineMessage, offlineMessage);
				return true;
			}

			return false;
		}

		private IEnumerator SoundCooldown()
		{
			soundIsOnCooldown = true;
			yield return WaitFor.Seconds(2f);
			soundIsOnCooldown = false;
		}
	}
}
