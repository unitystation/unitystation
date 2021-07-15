using System;
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
	public int RequiredAmount { get; set; }

	[SerializeField] [Tooltip("The required item. Includes all of its children (prefab variants).")]
	private GameObject requiredItem;

	/// <summary>
	/// The required item. Includes all of its children (prefab variants).
	/// </summary>
	public GameObject RequiredItem => requiredItem;
}
