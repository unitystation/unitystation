﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Items;
using Objects.Machines;

namespace Objects.Mining
{
	/// <summary>
	/// Causes object to consume ore on the tile above it and produce materials on the tile below it. Temporary
	/// until ORM UI is implemented.
	/// </summary>
	public class OreRedemptionMachine : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		private RegisterObject registerObject;
		public MaterialStorageLink materialStorageLink;
		public GUI_OreRedemptionMachine oreRedemptiomMachineGUI;
		public int laborPoints;

		private void Awake()
		{
			registerObject = GetComponent<RegisterObject>();
			materialStorageLink = GetComponent<MaterialStorageLink>();
		}

		public void LoadNearbyOres()
		{
			var nearbyObjects = MatrixManager.GetAdjacent<ObjectBehaviour>(registerObject.WorldPosition, true);
			foreach (var objectBehaviour in nearbyObjects)
			{
				var item = objectBehaviour.gameObject;
				if (Validations.HasItemTrait(item, CommonTraits.Instance.OreGeneral))
				{
					AddOre(item);
				}
				else
				{
					var oreBox = item.GetComponent<OreBox>();
					if (oreBox != null)
					{
						var itemStorage = oreBox.GetComponent<ItemStorage>();
						var itemSlotList = itemStorage.GetItemSlots();
						foreach (var itemSlot in itemSlotList)
						{
							if (itemSlot.IsEmpty)
							{
								continue;
							}
							AddOre(itemSlot.ItemObject);
						}
					}
				}
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side))
				return false;
			if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.OreGeneral))
				return false;
			return true;
		}
		public void ServerPerformInteraction(HandApply interaction)
		{
			var localPosInt = MatrixManager.Instance.WorldToLocalInt(registerObject.WorldPositionServer, registerObject.Matrix);
			var itemsOnFloor = registerObject.Matrix.Get<ItemAttributesV2>(localPosInt + Vector3Int.up, true);

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.OreGeneral))
				AddOre(interaction.HandObject);

			foreach (var item in itemsOnFloor)
			{
				if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.OreGeneral))
				{
					AddOre(item.gameObject);
				}
			}

			if (oreRedemptiomMachineGUI)
			{
				oreRedemptiomMachineGUI.UpdateLaborPoints(laborPoints);
			}
		}

		private void AddOre(GameObject ore)
		{
			foreach (var materialSheet in CraftingManager.MaterialSheetData.Values)
			{
				if (Validations.HasItemTrait(ore, materialSheet.oreTrait))
				{
					var inStackable = ore.GetComponent<Stackable>();
					laborPoints += inStackable.Amount * materialSheet.laborPoint;
					materialStorageLink.TryAddSheet(materialSheet.materialTrait, inStackable.Amount);
					Despawn.ServerSingle(ore);
				}
			}
		}

		public void ClaimLaborPoints(GameObject player)
		{
			var playerStorage = player.GetComponent<ItemStorage>();
			var idCardObj = playerStorage.GetNamedItemSlot(NamedSlot.id).ItemObject;
			var idCard = AccessRestrictions.GetIDCard(idCardObj);
			idCard.currencies[(int)CurrencyType.LaborPoints] += laborPoints;
			laborPoints = 0;
			oreRedemptiomMachineGUI.UpdateLaborPoints(laborPoints);
		}
	}
}
