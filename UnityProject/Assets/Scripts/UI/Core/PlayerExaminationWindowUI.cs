using System.Collections;
using System.Collections.Generic;
using Managers;
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

		public List<GameObject> UISlots = new List<GameObject>();


		public GameObject SlotPrefab;


		public GameObject AreaGameObject;

		/// <summary>
		/// Storage that is currenly displayed
		/// </summary>
		public DynamicItemStorage CurrentOpenStorage { get; private set; }
		private Equipment currentEquipment;

		private void Start()
		{

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
				foreach (var slotUI in UISlots)
				{
					// remove event listener
					ItemSlot playerSlot = slotUI.GetComponent<PlayerExaminationWindowSlot>().UI_ItemSlot.ItemSlot;
					playerSlot.OnSlotContentsChangeClient.RemoveListener(OnSlotContentsChangeClient);
					Destroy(slotUI);
				}
			}
			UISlots.Clear();
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
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			Reset();
		}

		/// <summary>
		/// Called when expand/collapse button is clicked
		/// </summary>
		public void OnClickExpandCollapse()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			expandedView.SetActive(!expandedView.activeSelf);
		}

		/// <summary>
		/// Called when clicking other player's slot
		/// </summary>
		/// <param name="slot">clicket slot</param>
		public void TryInteract(PlayerExaminationWindowSlot slot)
		{
			var targetSlot = slot.UI_ItemSlot.ItemSlot;

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
				playerSlot = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot();
			}
			OtherPlayerSlotTransferMessage.Send(playerSlot, targetSlot, isGhost);
		}

		/// <summary>
		/// Enable window and start listening for inventory updates
		/// </summary>
		/// <param name="itemStorage">reference to player's DynamicItemStorage</param>
		/// <param name="visibleName">player's visible name</param>
		/// <param name="species">player's species</param>
		/// <param name="job">player's job</param>
		/// <param name="status">player's status</param>
		/// <param name="additionalInformation">extra information characters can see about this character</param>
		public void ExaminePlayer(DynamicItemStorage itemStorage, string visibleName, string species, string job, string status, string additionalInformation)
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
			foreach (var slotUI in CurrentOpenStorage.ClientSlotCharacteristic)
			{
				ItemSlot playerSlot = slotUI.Key;
				playerSlot.OnSlotContentsChangeClient.AddListener(OnSlotContentsChangeClient);
				var slot = Instantiate(SlotPrefab, Vector3.zero, Quaternion.identity, AreaGameObject.transform);
				slot.transform.localScale = Vector3.one;
				slot.GetComponent<PlayerExaminationWindowSlot>().SetUp(slotUI.Value, slotUI.Key, this);
				UISlots.Add(slot);
			}

			gameObject.SetActive(true);
			OnSlotContentsChangeClient();
		}

		/// <summary>
		/// Update ui inventory slots
		/// </summary>
		public void UpdateStorageUI()
		{
			foreach (var UISlot in UISlots)
			{
				var slotUI = UISlot.GetComponent<PlayerExaminationWindowSlot>();

				// reset inventory slot
				slotUI.RefreshImage();

				// if slot is obscured - enable overlay
				if (currentEquipment.IsSlotObscured(slotUI.UI_ItemSlot.ItemSlot.NamedSlot.GetValueOrDefault()))
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
				}
			}
		}

		/// <summary>
		/// Called on player inventory update
		/// </summary>
		private void OnSlotContentsChangeClient()
		{
			gameObject.SetActive(true);
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
