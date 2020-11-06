using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Player;
using UnityEngine;
using UnityEngine.UI;

public class PlayerExaminationWindowUI : WindowDrag
{
	[SerializeField] private Text playerName;
	[SerializeField] private Text playerRace;
	[SerializeField] private Text playerJob;
	[SerializeField] private Text playerStatus;
	[Space]
	[SerializeField] private GameObject expandedView;
	[SerializeField] private Text additionalInformationsText;

	private PlayerExaminationWindowSlot[] examinationSlotsUI;

	/// <summary>
	/// Storage that is currenly displayed
	/// </summary>
	public ItemStorage CurrentOpenStorage { get; private set; }
	private Equipment currentEquipment;

	private void Awake()
	{
		// find all slots
		examinationSlotsUI = GetComponentsInChildren<PlayerExaminationWindowSlot>();
		gameObject.SetActive(false);
	}

	/// <summary>
	/// Reset all components and disable window
	/// </summary>
	private void Reset()
	{
		playerName.text = string.Empty;
		playerRace.text = string.Empty;
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
		SoundManager.Play("Click01");

		Reset();
	}

	/// <summary>
	/// Called when expand/collapse button is clicked
	/// </summary>
	public void OnClickExpandCollapse()
	{
		SoundManager.Play("Click01");

		expandedView.SetActive(!expandedView.activeSelf);
	}

	/// <summary>
	/// Enable window and start listening for inventory updates
	/// </summary>
	/// <param name="itemStorage">reference to player item storage</param>
	/// <param name="visibleName">player's visible name</param>
	/// <param name="race">player's race</param>
	/// <param name="job">player's job</param>
	/// <param name="status">player's status</param>
	public void ExaminePlayer(ItemStorage itemStorage, string visibleName, string race, string job, string status, string additionalInformations)
	{
		Reset();

		CurrentOpenStorage = itemStorage;
		currentEquipment = itemStorage.gameObject.GetComponent<Equipment>();

		UpdateStorageUI();
		
		playerName.text = visibleName;
		playerRace.text = race;
		playerJob.text = job;
		playerStatus.text = status;

		// display additional informations
		if (additionalInformations.Length > 0)
		{
			additionalInformationsText.text = additionalInformations.Replace(";", "\n");
		}

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

			// if slot is obscured - enable overlay
			if (currentEquipment.IsSlotObscured(slotUI.UI_ItemSlot.NamedSlot))
			{
				slotUI.SetObscuredOverlayActive(true);
			}
			// else link slot
			else
			{
				ItemSlot playerSlot = CurrentOpenStorage.GetNamedItemSlot(slotUI.UI_ItemSlot.NamedSlot);
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
