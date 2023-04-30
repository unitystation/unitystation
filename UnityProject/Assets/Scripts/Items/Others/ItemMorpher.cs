using System;
using System.Collections;
using System.Collections.Generic;
using Systems.Ai;
using UI.Core;
using UnityEngine;

public class ItemMorpher : MonoBehaviour, IInteractable<HandActivate>
{
	private Pickupable Pickupable;

	public List<GameObject> PossibleMorph = new List<GameObject>();

	public void Awake()
	{
		Pickupable = this.GetComponent<Pickupable>();
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		var Choosing = new List<DynamicUIChoiceEntryData>();


		foreach (var Morph in PossibleMorph)
		{
			Choosing.Add(new DynamicUIChoiceEntryData()
				{
					ChoiceAction = () => { MorphTo(Morph); },
					Text = $"Morph Item to {Morph.name}"
				}
			);
		}

		DynamicChoiceUI.ClientDisplayChoicesNotNetworked("Choose linked AI for Brain ",
			" Choose whichever you would like to link this brain to ", Choosing);
	}

	public void MorphTo(GameObject NewItem)
	{
		var ItemNotRemovableCash = Pickupable.ItemSlot.ItemNotRemovable;
		var SlotCash = Pickupable.ItemSlot;
		Pickupable.ItemSlot.ItemNotRemovable = false;

		var NewItemInstance = Instantiate(NewItem, transform.position, transform.rotation);
		Inventory.ServerAdd(NewItemInstance, Pickupable.ItemSlot, ReplacementStrategy.DropOther);
		SlotCash.ItemNotRemovable = ItemNotRemovableCash;
		Destroy(gameObject);
	}
}