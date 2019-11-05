
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Only used to map from item types from the imported DMI data
/// to ItemTrait. Shouldn't be used for anything else
/// </summary>
[CreateAssetMenu(fileName = "ItemTypeToTraitMapping", menuName = "Traits/ItemTypeToTraitMapping")]
public class ItemTypeToTraitMapping : ScriptableObject
{
	[Serializable]
	public class TypeToTraitEntry
	{
		public ItemType Type;
		public ItemTrait Trait;
	}

	public List<TypeToTraitEntry> Mappings;

	public ItemTrait GetTrait(ItemType forType)
	{
		return Mappings.FirstOrDefault(mp => mp.Type == forType)?.Trait;
	}

}
