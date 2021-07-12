using System;
using Items;
using UnityEngine;

/// <summary>
/// A pair of values: The ingredient itself as ItemAttributesV2 and its quantity
/// </summary>
[Serializable]
public class IngredientV2
{

	[SerializeField] [Min(1)] [Tooltip("The amount of required items.")]
	private int requiredAmount = 1;

	/// <summary>
	/// The amount of required items.
	/// </summary>
	public int RequiredAmount => requiredAmount;

	[SerializeField] [Tooltip("The required item.")]
	private ItemAttributesV2 requiredItem;

	/// <summary>
	/// The required item.
	/// </summary>
	public ItemAttributesV2 RequiredItem => requiredItem;
}
