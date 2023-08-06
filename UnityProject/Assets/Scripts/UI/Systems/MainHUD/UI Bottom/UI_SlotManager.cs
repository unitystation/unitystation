using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using Items.Implants.Organs;
using UnityEngine;

public class UI_SlotManager : MonoBehaviour
{
	public List<UI_DynamicItemSlot> OpenSlots = new List<UI_DynamicItemSlot>();

	public Dictionary<int, List<GameObject>> BodyPartToSlot =
		new Dictionary<int, List<GameObject>>();

	public GameObject Pockets;
	public GameObject SuitStorage;

	public GameObject Hands;

	public GameObject BeltPDABackpack;

	public GameObject Clothing;


	public GameObject SlotPrefab;

	public HandsController HandsController;


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
			var DynamicItemStorage = PlayerManager.LocalPlayerScript.DynamicItemStorage;
			var Newstored = DynamicItemStorage.ClientSlotCharacteristic;
			List<Tuple<IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>> Inadd = new List<Tuple<IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>>();
			List<Tuple<int, BodyPartUISlots.StorageCharacteristics>> Inremove = new List<Tuple<int, BodyPartUISlots.StorageCharacteristics>>();

			foreach (var slot in Newstored)
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
				foreach (var Newslot in Newstored)
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
		else
		{

			foreach (var contained in ContainSlots)
			{
				RemoveSpecifyedUISlot(contained.Item1, contained.Item2);
			}
			ContainSlots.Clear();
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

			// if (ClientContents.ContainsKey(storageCharacteristicse.SlotArea) == false) ClientContents[storageCharacteristicse.SlotArea] = new List<UI_DynamicItemSlot>();
			// ClientContents[storageCharacteristicse.SlotArea].Add(NewSlot);
			gameobjt.transform.localScale = Vector3.one;
			if (BodyPartToSlot.ContainsKey(bodyPartUISlots.InterfaceGetInstanceID) == false)
				BodyPartToSlot[bodyPartUISlots.InterfaceGetInstanceID] = new List<GameObject>();
			BodyPartToSlot[bodyPartUISlots.InterfaceGetInstanceID].Add(gameobjt);

			OpenSlots.Add(NewSlot);
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