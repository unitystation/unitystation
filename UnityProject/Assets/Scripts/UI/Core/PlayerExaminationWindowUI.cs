using System.Collections;
using Messages.Client;
using Messages.Client.Interaction;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

namespace UI.Core
{
	public class PlayerExaminationWindowUI : MonoBehaviour
	{
		[SerializeField] private Text playerName = default;
		[SerializeField] private Text playerSpecies = default;
		[SerializeField] private Text playerJob = default;
		[SerializeField] private Text playerStatus = default;
		[Space]
		[SerializeField] private GameObject expandedView = default;
		[SerializeField] private Text additionalInformationsText = default;

		private PlayerExaminationWindowSlot[] examinationSlotsUI;

		/// <summary>
		/// Storage that is currenly displayed
		/// </summary>
		public ItemStorage CurrentOpenStorage { get; private set; }
		private Equipment currentEquipment;

		private void Start()
		{
			// find all slots
			examinationSlotsUI = GetComponentsInChildren<PlayerExaminationWindowSlot>();

			// set parent to this
			foreach (var slotUI in examinationSlotsUI)
			{
				slotUI.parent = this;
			}

			gameObject.SetActive(false);
		}

		/// <summary>
		/// Reset all components and disable window
		/// </summary>
		private void Reset()
		{
			playerName.text = string.Empty;
			playerSpecies.text = string.Empty;
			playerJob.text = string.Empty;
			playerStatus.text = string.Empty;

			additionalInformationsText.text = string.Empty;

			if (CurrentOpenStorage != null)
			{
				foreach (var slotUI in examinationSlotsUI)
				{
					// remove event listener
					ItemSlot playerSlot = CurrentOpenStorage.GetNamedItemSlot(slotUI.UI_ItemSlot.NamedSlot);
					playerSlot.OnSlotContentsChangeClient.RemoveListener(OnSlotContentsChangeClient);

					// reset inventory slot
					slotUI.Reset();
				}
			}

			CurrentOpenStorage = null;
			currentEquipment = null;

			gameObject.SetActive(false);
			expandedView.SetActive(false);
		}

		/// <summary>
		/// Called when X button is clicked
		/// </summary>
		public void OnClickExit()
		{
			_ = SoundManager.Play(SingletonSOSounds.Instance.Click01);

			Reset();
		}

		/// <summary>
		/// Called when expand/collapse button is clicked
		/// </summary>
		public void OnClickExpandCollapse()
		{
			_ = SoundManager.Play(SingletonSOSounds.Instance.Click01);

			expandedView.SetActive(!expandedView.activeSelf);
		}

		/// <summary>
		/// Called when clicking other player's slot
		/// </summary>
		/// <param name="slot">clicket slot</param>
		public void TryInteract(PlayerExaminationWindowSlot slot)
		{
			var targetSlot = CurrentOpenStorage.GetNamedItemSlot(slot.UI_ItemSlot.NamedSlot);

			if (slot.IsObscured || slot.IsPocket)
			{
				// when player clicks on obscured slot/pocket second time
				if (slot.IsQuestionMarkActive)
				{
					InteractWithOtherPlayersSlot(targetSlot);
				}
				// when player clicks on obscured slot/pocket first time
				else
				{
					slot.SetQuestionMarkActive(targetSlot.IsOccupied);
				}
			}
			else
			{
				if (KeyboardInputManager.IsShiftPressed() && targetSlot.Item != null)
				{
					RequestExamineMessage.Send(targetSlot.Item.GetComponent<NetworkIdentity>().netId);
					return;
				}
				InteractWithOtherPlayersSlot(targetSlot);
			}
		}

		private void InteractWithOtherPlayersSlot(ItemSlot targetSlot)
		{
			ItemSlot playerSlot;
			var isGhost = PlayerManager.LocalPlayerScript.IsGhost;
			if (isGhost)
			{
				if (PlayerList.Instance.IsClientAdmin)
				{
					playerSlot = AdminManager.Instance.LocalAdminGhostStorage.GetNamedItemSlot(NamedSlot.ghostStorage01);
				}
				else
				{
					return;
				}
			}
			else
			{
				playerSlot = UIManager.Hands.CurrentSlot.ItemSlot;
			}
			OtherPlayerSlotTransferMessage.Send(playerSlot, targetSlot, isGhost);
		}

		/// <summary>
		/// Enable window and start listening for inventory updates
		/// </summary>
		/// <param name="itemStorage">reference to player item storage</param>
		/// <param name="visibleName">player's visible name</param>
		/// <param name="species">player's species</param>
		/// <param name="job">player's job</param>
		/// <param name="status">player's status</param>
		/// <param name="additionalInformation">extra information characters can see about this character</param>
		public void ExaminePlayer(ItemStorage itemStorage, string visibleName, string species, string job, string status, string additionalInformation)
		{
			Reset();

			CurrentOpenStorage = itemStorage;
			currentEquipment = itemStorage.gameObject.GetComponent<Equipment>();

			UpdateStorageUI();

			// display info
			playerName.text = visibleName;
			playerSpecies.text = species;
			playerJob.text = job;
			playerStatus.text = status;

			// display additional informations
			additionalInformationsText.text = additionalInformation;

			// add listeners
			foreach (var slotUI in examinationSlotsUI)
			{
				ItemSlot playerSlot = CurrentOpenStorage.GetNamedItemSlot(slotUI.UI_ItemSlot.NamedSlot);
				playerSlot.OnSlotContentsChangeClient.AddListener(OnSlotContentsChangeClient);
			}

			gameObject.SetActive(true);
		}

		/// <summary>
		/// Update ui inventory slots
		/// </summary>
		public void UpdateStorageUI(bool test = false)
		{
			foreach (var slotUI in examinationSlotsUI)
			{
				// reset inventory slot
				slotUI.Reset();

				var namedSlot = slotUI.UI_ItemSlot.NamedSlot;

				// if slot is obscured - enable overlay
				if (currentEquipment.IsSlotObscured(namedSlot))
				{
					slotUI.SetObscuredOverlayActive(true);
				}
				// else link slot
				else
				{
					if (slotUI.IsPocket)
					{
						continue;
					}

					var playerSlot = CurrentOpenStorage.GetNamedItemSlot(namedSlot);
					slotUI.UI_ItemSlot.LinkSlot(playerSlot);
				}
			}
		}

		/// <summary>
		/// Called on player inventory update
		/// </summary>
		private void OnSlotContentsChangeClient()
		{
			// need to wait one frame because item needs to refresh before updating UI
			StartCoroutine(WaitOneFrameForUpdate());
		}

		private IEnumerator WaitOneFrameForUpdate()
		{
			yield return null;

			UpdateStorageUI();
		}

		/// <summary>
		/// Disable and reset window
		/// </summary>
		public void CloseWindow()
		{
			Reset();
		}
	}
}
