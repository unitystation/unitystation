using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Objects.Machines
{
	public class MaterialStorage : MonoBehaviour
	{
		public Dictionary<ItemTrait, int> MaterialList = new Dictionary<ItemTrait, int>();
		private int currentResources;

		public bool infiniteStorage;
		//wont appear to be edited if the storage is infinite
		[ConditionalField(nameof(infiniteStorage), false)]
		public int maximumResources = 1000000;

		//2000cm per sheet is standard for SS13
		public static readonly int Cm3PerSheet = 2000;

		public UnityEvent UpdateGUIs;
		private void Awake()
		{
			foreach (var material in CraftingManager.MaterialSheetData.Keys)
			{
				MaterialList.Add(material, 0);
			}
		}

		private void AddMaterial(ItemTrait material, int quantity)
		{
			MaterialList[material] += quantity;
			currentResources += quantity;
		}

		private void ConsumeMaterial(ItemTrait material, int quantity)
		{
			MaterialList[material] -= quantity;
			currentResources -= quantity;
		}

		public bool TryAddSheet(ItemTrait material, int quantity)
		{
			quantity *= Cm3PerSheet;
			var totalSum = currentResources + quantity;

			if (infiniteStorage || totalSum <= maximumResources)
			{
				AddMaterial(material, quantity);
				UpdateGUIs.Invoke();
				return true;
			}
			return false;
		}

		public int TryRemoveSheet(ItemTrait material, int quantity)
		{
			if (MaterialList[material] < Cm3PerSheet)
			{
				return 0;
			}
			var Cm3Used = Cm3PerSheet * quantity;
			if (MaterialList[material] < Cm3Used)
			{
				quantity = MaterialList[material] / Cm3PerSheet;
				Cm3Used = Cm3PerSheet * quantity;
			}
			ConsumeMaterial(material, Cm3Used);
			UpdateGUIs.Invoke();
			return quantity;
		}

		/// <summary>
		/// Attempt to remove an amount of materials from a Dictionary of materials
		/// </summary>
		public bool TryConsumeList(DictionaryMaterialToIntAmount consume)
		{
			foreach (var materialSheet in consume.Keys)
			{
				if (MaterialList[materialSheet.materialTrait] < consume[materialSheet])
				{
					return false;
				}
			}

			//Removes all the materials and their amount from the storage.
			foreach (var materialSheet in consume.Keys)
			{
				ConsumeMaterial(materialSheet.materialTrait, consume[materialSheet]);
			}

			UpdateGUIs.Invoke();
			return true;
		}

		public void DispenseSheet(int amountOfSheets, ItemTrait material, Vector3 worldPos)
		{
			var removedSheets = TryRemoveSheet(material, amountOfSheets);
			if (removedSheets != 0)
			{
				var materialToSpawn = CraftingManager.MaterialSheetData[material].RefinedPrefab;
				Spawn.ServerPrefab(materialToSpawn, worldPos, transform.parent, count: removedSheets);
			}
		}

		public void DropAllMaterials()
		{
			foreach (var material in MaterialList.Keys)
			{
				var materialToSpawn = CraftingManager.MaterialSheetData[material].RefinedPrefab;
				var amountToSpawn = MaterialList[material] / Cm3PerSheet;
				if (amountToSpawn > 0)
				{
					Spawn.ServerPrefab(materialToSpawn, gameObject.WorldPosServer(), transform.parent, count: amountToSpawn);
				}
			}
		}

		public ItemTrait FindMaterial(GameObject handObject)
		{
			foreach (var material in MaterialList.Keys)
			{
				if (Validations.HasItemTrait(handObject, material))
				{
					return material;
				}
			}
			return null;
		}
	}
}