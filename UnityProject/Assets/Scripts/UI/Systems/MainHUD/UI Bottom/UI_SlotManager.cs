using System;
using System.Collections.Generic;
using Items.Implants.Organs;
using UnityEngine;

public class UI_SlotManager : MonoBehaviour
{
	public List<UI_DynamicItemSlot> OpenSlots = new List<UI_DynamicItemSlot>();

	public Dictionary<int, List<GameObject>> BodyPartToSlot =
		new Dictionary<int, List<GameObject>>();

	public GameObject Pockets;
	public GameObject SuitStorage;
	public GameObject BeltPDABackpack;
	public GameObject Clothing;
	public GameObject SlotPrefab;

	public HandsController HandsController;

	private static readonly Dictionary<NamedSlot, int> NamedSlotOrder = new Dictionary<NamedSlot, int>
	{
		{ NamedSlot.head, 0 },
		{ NamedSlot.ear, 1 },
		{ NamedSlot.eyes, 2 },
		{ NamedSlot.mask, 3 },
		{ NamedSlot.neck, 4 },
		{ NamedSlot.uniform, 5 },
		{ NamedSlot.outerwear, 6 },
		{ NamedSlot.hands, 7 },
		{ NamedSlot.belt, 8 },
		{ NamedSlot.feet, 9 },
		{ NamedSlot.leftHand, 10 },
		{ NamedSlot.rightHand, 11 },
		{ NamedSlot.back, 12 },
		{ NamedSlot.id, 13 },
		{ NamedSlot.handcuffs, 14 },
		{ NamedSlot.suitStorage, 15 },
		{ NamedSlot.storage01, 16 },
		{ NamedSlot.storage02, 17 },
		{ NamedSlot.storage03, 18 },
		{ NamedSlot.storage04, 19 },
		{ NamedSlot.storage05, 20 },
		{ NamedSlot.storage06, 21 },
		{ NamedSlot.storage07, 22 },
		{ NamedSlot.storage08, 23 },
		{ NamedSlot.storage09, 24 },
		{ NamedSlot.storage10, 25 },
		{ NamedSlot.storage11, 26 },
		{ NamedSlot.storage12, 27 },
		{ NamedSlot.storage13, 28 },
		{ NamedSlot.storage14, 29 },
		{ NamedSlot.storage15, 30 },
		{ NamedSlot.storage16, 31 },
		{ NamedSlot.storage17, 32 },
		{ NamedSlot.storage18, 33 },
		{ NamedSlot.storage19, 34 },
		{ NamedSlot.storage20, 35 },
		{ NamedSlot.ghostStorage01, 36 },
		{ NamedSlot.ghostStorage02, 37 },
		{ NamedSlot.ghostStorage03, 38 },
		{ NamedSlot.none, int.MaxValue }
	}; // (Max): Hacky workaround to how the NamedSlots enum cannot be re-ordered.


	//Instance ID of class that implements it
	public List<Tuple<int, BodyPartUISlots.StorageCharacteristics>> ContainSlots = new List<Tuple<int, BodyPartUISlots.StorageCharacteristics>>();
	public void Start()
	{
		EventManager.AddHandler(Event.ServerLoggedOut, CompleteClean);
		EventManager.AddHandler(Event.PlayerSpawned, UpdateUI);
		EventManager.AddHandler(Event.RoundEnded, UpdateUI);
		EventManager.AddHandler(Event.PreRoundStarted, CompleteClean);
	}

	private void OnDestroy()
	{
		EventManager.RemoveHandler(Event.ServerLoggedOut, CompleteClean);
		EventManager.RemoveHandler(Event.PlayerSpawned, UpdateUI);
		EventManager.RemoveHandler(Event.RoundEnded, UpdateUI);
		EventManager.RemoveHandler(Event.PreRoundStarted, CompleteClean);
		OpenSlots.Clear();
	}
	public void CompleteClean()
	{
		foreach (var contained in ContainSlots)
		{
			RemoveSpecifyedUISlot(contained.Item1, contained.Item2);
		}
		ContainSlots.Clear();
	}

	public void UpdateUI()
	{
		if (PlayerManager.LocalPlayerScript.OrNull()?.DynamicItemStorage != null)
		{
			SyncUISlots();
		}
		else
		{
			foreach (var contained in ContainSlots)
			{
				RemoveSpecifyedUISlot(contained.Item1, contained.Item2);
			}
			ContainSlots.Clear();
		}
		SortSlots(Clothing.transform);
	}

	private void SyncUISlots()
	{
		var dynamicItemStorage = PlayerManager.LocalPlayerScript.DynamicItemStorage;
		var newstored = dynamicItemStorage.ClientSlotCharacteristic;
		List<Tuple<IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>> Inadd = new List<Tuple<IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>>();
		List<Tuple<int, BodyPartUISlots.StorageCharacteristics>> Inremove = new List<Tuple<int, BodyPartUISlots.StorageCharacteristics>>();

		foreach (var slot in newstored)
		{
			if (slot.Value.NotPresentOnUI) continue;

			bool NotPresent = true;
			foreach (var Oldslot in ContainSlots)
			{
				if (Oldslot.Item1 == slot.Value.RelatedIDynamicItemSlotS.InterfaceGetInstanceID && Oldslot.Item2 == slot.Value)
				{
					NotPresent = false;
				}
			}

			if (NotPresent)
			{
				Inadd.Add(new Tuple<IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>(slot.Value.RelatedIDynamicItemSlotS, slot.Value));
			}
		}

		foreach (var OLDslot in ContainSlots)
		{
			bool NotPresent = true;
			foreach (var Newslot in newstored)
			{
				if (OLDslot.Item1 == Newslot.Value.RelatedIDynamicItemSlotS.InterfaceGetInstanceID && OLDslot.Item2 == Newslot.Value)
				{
					NotPresent = false;
				}
			}

			if (NotPresent)
			{
				Inremove.Add(OLDslot);
			}
		}

		foreach (var removeing in Inremove)
		{
			RemoveSpecifyedUISlot(removeing.Item1, removeing.Item2);
			ContainSlots.Remove(removeing);

		}

		foreach (var Adding in Inadd)
		{
			AddIndividual(Adding.Item1, Adding.Item2);
			ContainSlots.Add(new Tuple<int, BodyPartUISlots.StorageCharacteristics>(Adding.Item1.InterfaceGetInstanceID,Adding.Item2 ));
		}
	}

	public void RemoveSpecifyedUISlot(int bodyPartUISlots,
		BodyPartUISlots.StorageCharacteristics StorageCharacteristics)
	{
		if (StorageCharacteristics.SlotArea == SlotArea.Hands)
		{
			HandsController.RemoveHand(StorageCharacteristics);
		}

		if (BodyPartToSlot.TryGetValue(bodyPartUISlots, out var uiSlots) == false) return;

		for (int i = uiSlots.Count - 1; i >= 0; i--)
		{
			var go = uiSlots[i];
			var slot = go.OrNull()?.GetComponentInChildren<UI_DynamicItemSlot>();

			if (slot == null)
			{
				uiSlots.RemoveAt(i);
				continue;
			}

			if (slot._storageCharacteristics != StorageCharacteristics) continue;

			OpenSlots.Remove(slot);
			slot.ReSetSlot();
			Destroy(go);
			uiSlots.RemoveAt(i);
		}

		if (uiSlots.Count == 0)
		{
			BodyPartToSlot.Remove(bodyPartUISlots);
		}
	}


	public void AddIndividual(IDynamicItemSlotS bodyPartUISlots,
		BodyPartUISlots.StorageCharacteristics storageCharacteristicse)
	{
		if (this == null) return;
		if (storageCharacteristicse.SlotArea == SlotArea.Hands)
		{
			HandsController.AddHand(bodyPartUISlots, storageCharacteristicse);
		}
		else
		{
			var gameobjt = Instantiate(SlotPrefab);
			var NewSlot = gameobjt.GetComponentInChildren<UI_DynamicItemSlot>();
			NewSlot.SetupSlot(bodyPartUISlots, storageCharacteristicse);
			switch (storageCharacteristicse.SlotArea)
			{
				case SlotArea.Pockets:
					gameobjt.transform.SetParent(Pockets.transform);
					break;
				case SlotArea.SuitStorage:
					gameobjt.transform.SetParent(SuitStorage.transform);
					break;
				case SlotArea.BeltPDABackpack:
					gameobjt.transform.SetParent(BeltPDABackpack.transform);
					break;
				case SlotArea.Clothing:
					gameobjt.transform.SetParent(Clothing.transform);
					break;
			}

			gameobjt.transform.localScale = Vector3.one;
			if (BodyPartToSlot.ContainsKey(bodyPartUISlots.InterfaceGetInstanceID) == false)
				BodyPartToSlot[bodyPartUISlots.InterfaceGetInstanceID] = new List<GameObject>();
			BodyPartToSlot[bodyPartUISlots.InterfaceGetInstanceID].Add(gameobjt);
			OpenSlots.Add(NewSlot);
		}
	}

	private void SortSlots(Transform parentTransform)
	{
		if (parentTransform == null) return;
		var slots = parentTransform.GetComponentsInChildren<UI_DynamicItemSlot>();
		Array.Sort(slots, (slot1, slot2) =>
		{
			int order1 = NamedSlotOrder.ContainsKey(slot1.NamedSlot) ? -NamedSlotOrder[slot1.NamedSlot] : int.MaxValue;
			int order2 = NamedSlotOrder.ContainsKey(slot2.NamedSlot) ? -NamedSlotOrder[slot2.NamedSlot] : int.MaxValue;
			return order1.CompareTo(order2);
		});

		for (int i = 0; i < slots.Length; i++)
		{
			slots[i].transform.parent.SetSiblingIndex(i);
		}
	}

	public enum SlotArea
	{
		Pockets,
		SuitStorage,
		Hands,
		BeltPDABackpack,
		Clothing
	}
}