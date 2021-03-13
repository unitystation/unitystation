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
	public class OreRedemptionMachine : MonoBehaviour, IInteractable<HandApply>
	{
		private RegisterObject registerObject;
		private MaterialStorageLink materialStorageLink;

		private void Awake()
		{
			registerObject = GetComponent<RegisterObject>();
			materialStorageLink = GetComponent<MaterialStorageLink>();
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			var localPosInt = MatrixManager.Instance.WorldToLocalInt(registerObject.WorldPositionServer, registerObject.Matrix);
			var OreItems = registerObject.Matrix.Get<ItemAttributesV2>(localPosInt + Vector3Int.up, true);

			foreach (var Ore in OreItems)
			{
				foreach (var materialSheet in CraftingManager.MaterialSheetData.Values)
				{
					if (Ore.HasTrait(materialSheet.oreTrait))
					{
						var inStackable = Ore.gameObject.GetComponent<Stackable>();
						materialStorageLink.TryAddSheet(materialSheet.materialTrait, inStackable.Amount);
						Despawn.ServerSingle(Ore.transform.gameObject);
					}
				}
			}
		}
	}
}
