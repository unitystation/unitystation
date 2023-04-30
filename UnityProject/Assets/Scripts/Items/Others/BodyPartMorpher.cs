using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UI.Core;
using UnityEngine;

public class BodyPartMorpher : MonoBehaviour, IInteractable<HandActivate>
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
		var nPickupable = Pickupable.ItemSlot.ItemStorage.GetComponent<Pickupable>();
		var ItemNotRemovableCash = nPickupable.ItemSlot.ItemNotRemovable;
		var SlotCash = nPickupable.ItemSlot;
		nPickupable.ItemSlot.ItemNotRemovable = false;
		nPickupable.GetComponent<BodyPart>().TryRemoveFromBody(false, false, false, true);

		var NewItemInstance = Spawn.ServerPrefab(NewItem).GameObject;
		Inventory.ServerAdd(NewItemInstance, SlotCash, ReplacementStrategy.DespawnOther);

		SlotCash.ItemNotRemovable = ItemNotRemovableCash;


		Destroy(gameObject);
		Destroy(nPickupable.gameObject);
	}
}
