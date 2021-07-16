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

	public void Start()
	{
		EventManager.AddHandler(Event.LoggedOut, RemoveAll);
		EventManager.AddHandler(Event.PlayerSpawned, RemoveAll);
		EventManager.AddHandler(Event.RoundEnded, RemoveAll);
		EventManager.AddHandler(Event.PreRoundStarted, RemoveAll);
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


	public void RemoveAll()
	{
		if (this == null) return;
		foreach (var Inslots in BodyPartToSlot.Keys.ToArray())
		{
			foreach (var Characteristics in Inslots.Storage)
			{
				RemoveSpecifyedUISlot(Inslots, Characteristics);
			}

		}
		BodyPartToSlot.Clear();

		HandsController.RemoveAllHands();
	}

	public void AddIndividual(IDynamicItemSlotS bodyPartUISlots,
		BodyPartUISlots.StorageCharacteristics storageCharacteristicse)
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

	public enum SlotArea
	{
		Pockets,
		SuitStorage,
		Hands,
		BeltPDABackpack,
		Clothing
	}
}