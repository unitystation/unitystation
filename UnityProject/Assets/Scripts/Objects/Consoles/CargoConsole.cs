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
using Systems.Clearance;

namespace Objects.Cargo
{
	public class CargoConsole : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		public bool CorrectID;
		public bool Emagged;

		public GUI_Cargo cargoGUI;

		private ClearanceRestricted clearanceRestricted;

		[SerializeField] private AddressableAudioSource creditArrivalSound;
		private bool soundIsOnCooldown = false;

		[SerializeField] private string offlineMessage = "The console flashes red as an error message appears and says that access is denied.";

		private void Awake()
		{
			clearanceRestricted = GetComponent<ClearanceRestricted>();
		}

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

				CheckID(interaction);
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
		private void CheckID(HandApply interaction)
		{
			if (cargoGUI == null) return;

			if (interaction.HandObject != null && clearanceRestricted.HasClearance(interaction.HandObject))
			{
				CorrectID = true;
				cargoGUI.pageCart.UpdateTab();
			}

			var denyString = "the console denies your ID";
			if (CorrectID)
			{
				denyString = "the console accepts your ID";
			}
			Chat.AddActionMsgToChat(interaction.Performer, $"You swipe your ID through the supply console's ID slot, {denyString}",
				$"{interaction.Performer.ExpensiveName()} swiped their ID through the supply console's ID slot");

		}

		public void PlayBudgetUpdateSound()
		{
			if(soundIsOnCooldown) return;
			_ = SoundManager.PlayNetworkedAtPosAsync(creditArrivalSound, gameObject.AssumedWorldPosServer());
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
