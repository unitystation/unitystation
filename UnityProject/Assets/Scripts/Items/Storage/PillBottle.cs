using System;
using HealthV2;
using Items.Food;
using UnityEngine;

public class PillBottle : MonoBehaviour, ICheckedInteractable<HandApply>
{

	private ItemStorage ItemStorage;

	private void Awake()
	{
		ItemStorage = this.GetComponentCustom<ItemStorage>();
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (interaction.IsAltClick) return false;
		if (Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject) == false) return false;

		if (side == NetworkSide.Server)
		{
			if (ItemStorage.HasAnyOccupied() == false) return false;
		}

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		var pill = ItemStorage.GetFirstOccupiedSlot();
		var PillEdible = pill.Item.GetComponentCustom<Edible>();
		PillEdible.ServerPerformInteraction(interaction);
	}



}
