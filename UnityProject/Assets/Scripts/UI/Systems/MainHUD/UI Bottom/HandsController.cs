using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using UnityEngine;

public class HandsController : MonoBehaviour
{
	public static HandsController Instance;

	public DoubleHandController DoubleHandController;

	public HashSet<DoubleHandController> DoubleHandControllers = new HashSet<DoubleHandController>();

	public List<DoubleHandController> AvailableLeftHand = new List<DoubleHandController>();

	public List<DoubleHandController> AvailableRightHand = new List<DoubleHandController>();


	public Dictionary<BodyPartUISlots.StorageCharacteristics, DoubleHandController> StorageToHands =
		new Dictionary<BodyPartUISlots.StorageCharacteristics, DoubleHandController>();

	public DoubleHandController activeDoubleHandController;
	public NamedSlot ActiveHand;


	public void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
	}


	public void HideHands(bool HideState)
	{
		foreach (var doubleHand in DoubleHandControllers)
		{
			doubleHand.HideHands(HideState);
		}
	}

	public void AddHand(IDynamicItemSlotS bodyPartUISlots, BodyPartUISlots.StorageCharacteristics StorageCharacteristics)
	{
		DoubleHandController HandController;
		switch (StorageCharacteristics.namedSlot)
		{
			case NamedSlot.leftHand:
				if (AvailableLeftHand.Count > 0)
				{
					HandController = AvailableLeftHand[0];
					AvailableLeftHand.RemoveAt(0);
				}
				else
				{
					HandController = Instantiate(DoubleHandController, transform);
					HandController.HideAll();
					HandController.RelatedHandsController = this;
					AvailableRightHand.Add(HandController);
					DoubleHandControllers.Add(HandController);
				}

				break;
			case NamedSlot.rightHand:
				if (AvailableRightHand.Count > 0)
				{
					HandController = AvailableRightHand[0];
					AvailableRightHand.RemoveAt(0);
				}
				else
				{
					HandController = Instantiate(DoubleHandController, transform);
					HandController.HideAll();
					HandController.RelatedHandsController = this;
					AvailableLeftHand.Add(HandController);
					DoubleHandControllers.Add(HandController);
				}

				break;
			default:
				Logger.LogError("humm Tried to put non-hand into Hand Slot");
				return;
		}

		StorageToHands[StorageCharacteristics] = HandController;
		HandController.AddHand(bodyPartUISlots, StorageCharacteristics);
		if (PlayerManager.LocalPlayerScript.playerNetworkActions.activeHand == null)
		{
			HandController.PickActiveHand();
		}
	}

	public void RemoveAllHands()
	{
		foreach (var HandStorage in DoubleHandControllers.ToArray())
		{
			Destroy(HandStorage.gameObject);

		}
		StorageToHands.Clear();
		AvailableLeftHand.Clear();
		AvailableRightHand.Clear();
		DoubleHandControllers.Clear();
		activeDoubleHandController = null;
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand(0, NamedSlot.none);
		PlayerManager.LocalPlayerScript.playerNetworkActions.activeHand = null;
		PlayerManager.LocalPlayerScript.playerNetworkActions.CurrentActiveHand = NamedSlot.none;
	}

	public void RemoveHand(
		BodyPartUISlots.StorageCharacteristics StorageCharacteristics)
	{
		if (StorageToHands.ContainsKey(StorageCharacteristics) == false) return;
		if (StorageToHands[StorageCharacteristics].RemoveHand( StorageCharacteristics))
		{
			var DoubleHand = StorageToHands[StorageCharacteristics];
			DoubleHandControllers.Remove(DoubleHand);
			if (AvailableLeftHand.Contains(DoubleHand)) AvailableLeftHand.Remove(DoubleHand);
			if (AvailableRightHand.Contains(DoubleHand)) AvailableRightHand.Remove(DoubleHand);
			List<BodyPartUISlots.StorageCharacteristics> Toremove = new List<BodyPartUISlots.StorageCharacteristics>();
			foreach (var KHandController in StorageToHands)
			{
				if (KHandController.Value == DoubleHand)
				{
					Toremove.Add(KHandController.Key);
				}
			}

			foreach (var storageCharacteristicse in Toremove)
			{
				StorageToHands.Remove(storageCharacteristicse);
			}

			if (activeDoubleHandController == DoubleHand)
			{
				if (DoubleHandControllers.Count > 0)
				{
					foreach (var DHC in DoubleHandControllers)
					{
						DHC.PickActiveHand();
						break;
					}
				}
				else
				{
					PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand(0, NamedSlot.none);
					PlayerManager.LocalPlayerScript.playerNetworkActions.activeHand = null;
					PlayerManager.LocalPlayerScript.playerNetworkActions.CurrentActiveHand = NamedSlot.none;
				}
			}

			DoubleHandControllers.Remove(DoubleHand);
			Destroy(DoubleHand.gameObject);
		}
		else
		{
			switch (StorageCharacteristics.namedSlot)
			{
				case NamedSlot.leftHand:
					AvailableLeftHand.Add(StorageToHands[StorageCharacteristics]);
					break;
				case NamedSlot.rightHand:
					AvailableRightHand.Add(StorageToHands[StorageCharacteristics]);
					break;
			}
		}

		StorageToHands.Remove(StorageCharacteristics);
	}

	public static void SwapHand()
	{
		if (Instance.activeDoubleHandController == null) return;
		Instance.activeDoubleHandController?.Deactivate(Instance.ActiveHand);
		if (Instance.ActiveHand == NamedSlot.leftHand &&
		    Instance.activeDoubleHandController.GetHand(NamedSlot.rightHand) != null)
		{
			Instance.activeDoubleHandController.ActivateRightHand();
		}
		else if (Instance.ActiveHand == NamedSlot.rightHand &&
		         Instance.activeDoubleHandController.GetHand(NamedSlot.leftHand) != null)
		{
			Instance.activeDoubleHandController.ActivateLeftHand();
		}
	}

	public void SetActiveHand(DoubleHandController doubleHandController, NamedSlot SetActiv)
	{
		activeDoubleHandController.OrNull()?.Deactivate(ActiveHand);
		activeDoubleHandController = doubleHandController;
		ActiveHand = SetActiv;

		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetActiveHand(
			activeDoubleHandController.GetHand(SetActiv).RelatedBodyPartUISlots.GameObject.NetId(), SetActiv);
		PlayerManager.LocalPlayerScript.playerNetworkActions.activeHand =
			activeDoubleHandController.GetHand(SetActiv).RelatedBodyPartUISlots.GameObject;
		PlayerManager.LocalPlayerScript.playerNetworkActions.CurrentActiveHand = SetActiv;
	}

	/// <summary>
	/// General function to activate the item's UIInteract
	/// This is the same as clicking the item with the same item's hand
	/// </summary>
	public static void Activate()
	{
		if (!isValidPlayer())
		{
			return;
		}

		var CurrentSlot = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot();
		// Is there an item in the active hand?
		if (CurrentSlot.Item == null)
		{
			return;
		}

		CurrentSlot.LocalUISlot.TryItemInteract();
	}

	/// <summary>
	/// General function to try to equip the item in the active hand
	/// </summary>
	public static void Equip()
	{
		// Is the player allowed to interact? (not a ghost)
		if (!isValidPlayer())
		{
			return;
		}

		// Is there an item to equip?
		var CurrentSlot = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot();
		if (CurrentSlot.Item == null)
		{
			return;
		}

		//This checks which UI slot the item can be equiped to and swaps it there
		//Try to equip the item into the appropriate slot
		var bestSlot =
			BestSlotForTrait.Instance.GetBestSlot(CurrentSlot.Item, PlayerManager.LocalPlayerScript.DynamicItemStorage);
		if (bestSlot == null)
		{
			Chat.AddExamineMsg(PlayerManager.LocalPlayerScript.gameObject, "There is no available slot for that");
			return;
		}

		SwapItem(bestSlot.LocalUISlot);
	}

	public static bool SwapItem(UI_ItemSlot itemSlot)
	{
		var CurrentSlot = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot();
		if (isValidPlayer())
		{
			if (CurrentSlot != itemSlot.ItemSlot)
			{
				if (CurrentSlot.Item == null)
				{
					if (itemSlot.Item != null)
					{
						Inventory.ClientRequestTransfer(itemSlot.ItemSlot, CurrentSlot);
						return true;
					}
				}
				else
				{
					if (itemSlot.Item == null)
					{
						Inventory.ClientRequestTransfer(CurrentSlot, itemSlot.ItemSlot);
						return true;
					}
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Check if the player is allowed to interact with objects
	/// </summary>
	/// <returns>True if they can, false if they cannot</returns>
	public static bool isValidPlayer()
	{
		if (PlayerManager.LocalPlayerScript == null) return false;

		// TODO tidy up this if statement once it's working correctly
		if (!PlayerManager.LocalPlayerScript.playerMove.allowInput ||
		    PlayerManager.LocalPlayerScript.IsGhost)
		{
			Logger.Log("Invalid player, cannot perform action!", Category.Interaction);
			return false;
		}

		return true;
	}
}
