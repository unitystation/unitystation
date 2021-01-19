
using System;
using System.Collections.Generic;
using System.Linq;
using ScriptableObjects;
using UnityEngine;

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
