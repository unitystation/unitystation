using System;
using System.Collections.Generic;
using UnityEngine;
using Items;
using Objects.Machines;
using Systems.Score;
using UI.Objects.Cargo;

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
			var nearbyObjects = MatrixManager.GetAdjacent<UniversalObjectPhysics>(registerObject.WorldPosition, true);
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

			UpdateLaborPointsUI();
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
			var localPosInt = MatrixManager.WorldToLocalInt(registerObject.WorldPositionServer, registerObject.Matrix);
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

			UpdateLaborPointsUI();
		}

		private void UpdateLaborPointsUI()
		{
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
					ore.GetComponent<Stackable>().ServerConsume(inStackable.Amount);
				}
			}
		}

		public void ClaimLaborPoints(GameObject player)
		{
			var playerStorage = player.GetComponent<DynamicItemStorage>();
			var itemSlotList = playerStorage.GetNamedItemSlots(NamedSlot.id);
			foreach (var itemSlot in itemSlotList)
			{
				if (itemSlot.ItemObject)
				{
					var idCard = AccessRestrictions.GetIDCard(itemSlot.ItemObject);
					ScoreMachine.AddToScoreInt(laborPoints, RoundEndScoreBuilder.COMMON_SCORE_LABORPOINTS);
					idCard.currencies[(int)CurrencyType.LaborPoints] += laborPoints;
					laborPoints = 0;
					oreRedemptiomMachineGUI.UpdateLaborPoints(laborPoints);
					return;
				}
			}
		}
	}
}
