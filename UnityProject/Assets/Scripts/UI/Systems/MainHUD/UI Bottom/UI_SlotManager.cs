using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using UnityEngine;

public class UI_SlotManager : MonoBehaviour
{
	public List<UI_DynamicItemSlot> OpenSlots = new List<UI_DynamicItemSlot>();

	public Dictionary<IDynamicItemSlotS, List<GameObject>> BodyPartToSlot =
		new Dictionary<IDynamicItemSlotS, List<GameObject>>();

	public GameObject Pockets;
	public GameObject SuitStorage;

	public GameObject Hands;

	public GameObject BeltPDABackpack;

	public GameObject Clothing;


	public GameObject SlotPrefab;

	public HandsController HandsController;


	public List<Tuple<IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>> ContainSlots = new List<Tuple<IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>>();
	public void Start()
	{
		EventManager.AddHandler(Event.LoggedOut, CompleteClean);
		EventManager.AddHandler(Event.PlayerSpawned, UpdateUI);
		EventManager.AddHandler(Event.RoundEnded, UpdateUI);
		EventManager.AddHandler(Event.PreRoundStarted, CompleteClean);
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
			List<Tuple<IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>> Inremove = new List<Tuple<IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>>();

			foreach (var slot in Newstored)
			{
				if (slot.Value.NotPresentOnUI) continue;

				bool NotPresent = true;
				foreach (var Oldslot in ContainSlots)
				{
					if (Oldslot.Item1 == slot.Value.RelatedIDynamicItemSlotS && Oldslot.Item2 == slot.Value)
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
					if (OLDslot.Item1 == Newslot.Value.RelatedIDynamicItemSlotS && OLDslot.Item2 == Newslot.Value)
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
				ContainSlots.Add(Adding);
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

	public void AddContainer(IDynamicItemSlotS bodyPartUISlots)
	{
		foreach (var storageCharacteristicse in bodyPartUISlots.Storage)
		{
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
				if (BodyPartToSlot.ContainsKey(bodyPartUISlots) == false)
					BodyPartToSlot[bodyPartUISlots] = new List<GameObject>();
				BodyPartToSlot[bodyPartUISlots].Add(gameobjt);

				OpenSlots.Add(NewSlot);
			}
		}
	}

	public void RemoveContainer(BodyPartUISlots bodyPartUISlots)
	{
		if (BodyPartToSlot.ContainsKey(bodyPartUISlots) == false)
			BodyPartToSlot[bodyPartUISlots] = new List<GameObject>();
		foreach (var uiDynamicItemSlot in BodyPartToSlot[bodyPartUISlots])
		{
			OpenSlots.Remove(uiDynamicItemSlot.GetComponentInChildren<UI_DynamicItemSlot>());
			uiDynamicItemSlot.GetComponentInChildren<UI_DynamicItemSlot>().ReSetSlot();
			Destroy(uiDynamicItemSlot);
		}

		BodyPartToSlot.Remove(bodyPartUISlots);
		foreach (var storageCharacteristicse in bodyPartUISlots.Storage)
		{
			if (storageCharacteristicse.SlotArea == SlotArea.Hands)
			{
				HandsController.RemoveHand( storageCharacteristicse);
			}
		}
	}


	public void RemoveSpecifyedUISlot(IDynamicItemSlotS bodyPartUISlots,
		BodyPartUISlots.StorageCharacteristics StorageCharacteristics)
	{
		if (BodyPartToSlot.ContainsKey(bodyPartUISlots) == false)
			BodyPartToSlot[bodyPartUISlots] = new List<GameObject>();
		var namedItemSlot = bodyPartUISlots.RelatedStorage.GetNamedItemSlot(StorageCharacteristics.namedSlot);
		for (int i = 0; i < BodyPartToSlot[bodyPartUISlots].Count; i++)
		{
			var slot = BodyPartToSlot[bodyPartUISlots][i].OrNull()?.GetComponentInChildren<UI_DynamicItemSlot>();

			if (slot == null)
			{
				Logger.LogError($"{bodyPartUISlots.RelatedStorage.OrNull()?.gameObject.ExpensiveName()} has null UI_DynamicItemSlot, slot: {StorageCharacteristics.namedSlot}");
				continue;
			}

			if (slot.ItemSlot == namedItemSlot)
			{
				OpenSlots.Remove(BodyPartToSlot[bodyPartUISlots][i].GetComponentInChildren<UI_DynamicItemSlot>());
				BodyPartToSlot[bodyPartUISlots][i].GetComponentInChildren<UI_DynamicItemSlot>().ReSetSlot();
				Destroy(BodyPartToSlot[bodyPartUISlots][i]);
				BodyPartToSlot[bodyPartUISlots].RemoveAt(i);
			}
		}


		if (BodyPartToSlot[bodyPartUISlots].Count == 0)
		{
			BodyPartToSlot.Remove(bodyPartUISlots);
		}

		if (StorageCharacteristics.SlotArea == SlotArea.Hands)
		{
			HandsController.RemoveHand(StorageCharacteristics);
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
			if (BodyPartToSlot.ContainsKey(bodyPartUISlots) == false)
				BodyPartToSlot[bodyPartUISlots] = new List<GameObject>();
			BodyPartToSlot[bodyPartUISlots].Add(gameobjt);

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