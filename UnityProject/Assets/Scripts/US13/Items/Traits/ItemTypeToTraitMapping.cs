using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using US13.ScriptableObjects;
using US13.Systems.Inventory;

namespace US13.Items.Traits
{
	/// <summary>
	/// Only used to map from item types from the imported DMI data
	/// to ItemTrait. Shouldn't be used for anything else
	/// </summary>
	[CreateAssetMenu(fileName = "ItemTypeToTraitMappingSingleton", menuName = "Singleton/Traits/ItemTypeToTraitMapping")]
	public class ItemTypeToTraitMapping : SingletonScriptableObject<ItemTypeToTraitMapping>
	{
		[Serializable]
		public class TypeToTraitEntry
		{
			public ItemType Type;
			public ItemTrait Trait;
		}

		[SerializeField]
		private List<TypeToTraitEntry> Mappings = null;

		public ItemTrait GetTrait(ItemType forType)
		{
			return Mappings.FirstOrDefault(mp => mp.Type == forType)?.Trait;
		}

	}
}
