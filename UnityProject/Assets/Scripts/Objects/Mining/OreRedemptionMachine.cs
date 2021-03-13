using System.Collections;
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
		private MaterialStorageLink materialStorageLink;

		private void Awake()
		{
			registerObject = GetComponent<RegisterObject>();
			materialStorageLink = GetComponent<MaterialStorageLink>();
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
		}

		private void AddOre(GameObject ore)
		{
			foreach (var materialSheet in CraftingManager.MaterialSheetData.Values)
			{
				if (Validations.HasItemTrait(ore, materialSheet.oreTrait))
				{
					var inStackable = ore.GetComponent<Stackable>();
					materialStorageLink.TryAddSheet(materialSheet.materialTrait, inStackable.Amount);
					Despawn.ServerSingle(ore);
				}
			}
		}
	}
}
