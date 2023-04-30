using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Mirror;
using UI.Core;
using UnityEngine;

public class BodyPartMorpher : NetworkBehaviour, IClientInteractable<HandActivate>
{
	private Pickupable Pickupable;

	public List<GameObject> PossibleMorph = new List<GameObject>();

	public void Awake()
	{
		Pickupable = this.GetComponent<Pickupable>();
	}


	public bool Interact(HandActivate interaction)
	{
		var Choosing = new List<DynamicUIChoiceEntryData>();

		for (var index = 0; index < PossibleMorph.Count; index++)
		{
			var Thisindex = index;
			var Morph = PossibleMorph[index];
			Choosing.Add(new DynamicUIChoiceEntryData()
				{
					ChoiceAction = () =>
					{

						CommandMorphTo(Thisindex);
					},
					Text = $"Morph Item to {Morph.name}"
				}
			);
		}

		DynamicChoiceUI.ClientDisplayChoicesNotNetworked("Choose linked AI for Brain ",
			" Choose whichever you would like to link this brain to ", Choosing);
		return true;
	}



	[Command(requiresAuthority = false)]
	public void CommandMorphTo(int ItemIndex, NetworkConnectionToClient sender = null)
	{
		if (sender == null) return;
		if (Validations.CanApply(PlayerList.Instance.Get(sender).Script, this.gameObject, NetworkSide.Server, false, ReachRange.Standard) == false) return;

		if (PossibleMorph.Count > ItemIndex)
		{
			MorphTo(PossibleMorph[ItemIndex]);
		}
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


		_ = Despawn.ServerSingle(gameObject);
		_ = Despawn.ServerSingle(nPickupable.gameObject);
	}
}
