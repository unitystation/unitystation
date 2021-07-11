using System;
using Items;
using UnityEngine;

[Serializable]
public class IngredientV2
{
	[SerializeField] [Min(1)] private int requiredAmount = 1;

	public int RequiredAmount => requiredAmount;

	[SerializeField] private ItemAttributesV2 requiredItem;

	public ItemAttributesV2 RequiredItem => requiredItem;
}
