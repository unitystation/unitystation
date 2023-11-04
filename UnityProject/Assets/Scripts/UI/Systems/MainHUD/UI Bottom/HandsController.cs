using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using Items;
using Items.Implants.Organs;
using Logs;
using UnityEngine;
using UnityEngine.Events;

public class HandsController : MonoBehaviour
{
	public static HandsController Instance;

	public DoubleHandController DoubleHandController;

	public ToolCarousel PrefabToolCarousel;

	public HashSet<DoubleHandController> DoubleHandControllers = new HashSet<DoubleHandController>();

	public List<DoubleHandController> AvailableLeftHand = new List<DoubleHandController>();

	public List<DoubleHandController> AvailableRightHand = new List<DoubleHandController>();

	public List<ToolCarousel> AllToolCarousels = new List<ToolCarousel>();

	public Dictionary<BodyPartUISlots.StorageCharacteristics, DoubleHandController> StorageToHands =
		new Dictionary<BodyPartUISlots.StorageCharacteristics, DoubleHandController>();

	public static readonly UnityEvent OnSwapHand = new UnityEvent();

	public IUIHandAreasSelectable activeDoubleHandController;
	public NamedSlot ActiveHand;

	public void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
	}

	//0 - Hide both hands, 1 - hide left hand, 2 - hide right hand, something else - hide none
	public void HideHands(HiddenHandValue Selection)
	{
		foreach (var doubleHand in DoubleHandControllers)
		{
			doubleHand.HideHands(Selection);
		}
	}

	public void AddHand(IDynamicItemSlotS bodyPartUISlots, BodyPartUISlots.StorageCharacteristics StorageCharacteristics)
	{
		if (this == null) return;
		DoubleHandController HandController = null;
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
			case NamedSlot.storage01:
			case NamedSlot.storage02:
			case NamedSlot.storage03:
			case NamedSlot.storage04:
			case NamedSlot.storage05:
			case NamedSlot.storage06:
			case NamedSlot.storage07:
			case NamedSlot.storage08:
			case NamedSlot.storage09:
			case NamedSlot.storage10:
			case NamedSlot.storage11:
			case NamedSlot.storage12:
			case NamedSlot.storage13:
			case NamedSlot.storage14:
			case NamedSlot.storage15:
			case NamedSlot.storage16:
			case NamedSlot.storage17:
			case NamedSlot.storage18:
			case NamedSlot.storage19:
			case NamedSlot.storage20:

				ToolCarousel uesToolCarousel = null;

				foreach (var ToolCarousel in AllToolCarousels)
				{
					if (ToolCarousel.HasFree())
					{
						uesToolCarousel = ToolCarousel;
					}
				}

				if (uesToolCarousel == null)
				{
					uesToolCarousel = Instantiate(PrefabToolCarousel, transform);
					uesToolCarousel.HideAll();
					uesToolCarousel.RelatedHandsController = this;
					AllToolCarousels.Add(uesToolCarousel);
				}

				uesToolCarousel.AddToCarousel(bodyPartUISlots,StorageCharacteristics );
				break;
			default:
				Loggy.LogError("humm Tried to put non-hand into Hand Slot");
				return;
		}

		if (HandController != null)
		{
			StorageToHands[StorageCharacteristics] = HandController;
			HandController.AddHand(bodyPartUISlots, StorageCharacteristics);
			if (PlayerManager.LocalPlayerScript.OrNull()?.PlayerNetworkActions != null)
			{
				if (PlayerManager.LocalPlayerScript.PlayerNetworkActions.activeHand == null)
				{
					HandController.PickActiveHand();
				}
			}
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
		if (PlayerManager.LocalPlayerScript.OrNull()?.PlayerNetworkActions != null)
		{
			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdSetActiveHand(0, NamedSlot.none);
			PlayerManager.LocalPlayerScript.PlayerNetworkActions.activeHand = null;
			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CurrentActiveHand = NamedSlot.none;
		}
	}

	public void RemoveHand(BodyPartUISlots.StorageCharacteristics StorageCharacteristics)
	{
		switch (StorageCharacteristics.namedSlot)
		{
			case NamedSlot.leftHand:
			case NamedSlot.rightHand:
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
							if (PlayerManager.LocalPlayerScript.OrNull()?.PlayerNetworkActions != null)
							{
								PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdSetActiveHand(0, NamedSlot.none);
								PlayerManager.LocalPlayerScript.PlayerNetworkActions.activeHand = null;
								PlayerManager.LocalPlayerScript.PlayerNetworkActions.CurrentActiveHand = NamedSlot.none;
							}
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
				break;
			case NamedSlot.storage01:
			case NamedSlot.storage02:
			case NamedSlot.storage03:
			case NamedSlot.storage04:
			case NamedSlot.storage05:
			case NamedSlot.storage06:
			case NamedSlot.storage07:
			case NamedSlot.storage08:
			case NamedSlot.storage09:
			case NamedSlot.storage10:
			case NamedSlot.storage11:
			case NamedSlot.storage12:
			case NamedSlot.storage13:
			case NamedSlot.storage14:
			case NamedSlot.storage15:
			case NamedSlot.storage16:
			case NamedSlot.storage17:
			case NamedSlot.storage18:
			case NamedSlot.storage19:
			case NamedSlot.storage20:

				ToolCarousel uesToolCarousel = null;

				foreach (var ToolCarousel in AllToolCarousels)
				{
					if (ToolCarousel.HasSlot(StorageCharacteristics) == false) continue;
					uesToolCarousel = ToolCarousel;
					break;
				}

				if (uesToolCarousel == null)
				{
					Loggy.LogError($"Slot wasn't found for  {StorageCharacteristics.namedSlot}");
					return;
				}

				uesToolCarousel.RemoveFromCarousel(StorageCharacteristics );

				if (uesToolCarousel.FilledSlots.Count == 0)
				{
					AllToolCarousels.Remove(uesToolCarousel);
					Destroy(uesToolCarousel.gameObject);

				}

				//TODO Pick new slot

				break;
			default:
				Loggy.LogError("humm Tried to put non-hand into Hand Slot");
				return;
		}




	}

	public static void SwapHand()
	{
		OnSwapHand.Invoke();
		if (Instance.activeDoubleHandController == null) return;
		Instance.activeDoubleHandController.SwapHand();
	}

	public void SetActiveHand(IUIHandAreasSelectable doubleHandController, NamedSlot SetActiv)
	{

		try
		{
			activeDoubleHandController?.DeSelect(ActiveHand);
		}
		catch (Exception e)
		{
			Loggy.LogError(e.ToString());
		}

		activeDoubleHandController = doubleHandController;
		ActiveHand = SetActiv;
		if (activeDoubleHandController.GetHand(SetActiv)?.RelatedBodyPartUISlots?.GameObject == null) return;

		if (PlayerManager.LocalPlayerScript.OrNull()?.PlayerNetworkActions != null)
		{
			PlayerManager.LocalPlayerScript.OrNull()?.PlayerNetworkActions.OrNull()?.CmdSetActiveHand(
				activeDoubleHandController.GetHand(SetActiv).RelatedBodyPartUISlots.GameObject.NetId(), SetActiv);

			PlayerManager.LocalPlayerScript.PlayerNetworkActions.activeHand = activeDoubleHandController.GetHand(SetActiv).RelatedBodyPartUISlots.GameObject;

			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CurrentActiveHand = SetActiv;
		}

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
		if (!PlayerManager.LocalPlayerScript.playerMove.AllowInput ||
		    PlayerManager.LocalPlayerScript.IsGhost)
		{
			Loggy.Log("Invalid player, cannot perform action!", Category.Interaction);
			return false;
		}

		return true;
	}
}
