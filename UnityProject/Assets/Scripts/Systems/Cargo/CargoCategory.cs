using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.Cargo
{
	[CreateAssetMenu(menuName="ScriptableObjects/Cargo/Category")]
	public class CargoCategory: ScriptableObject
	{
		[Tooltip("Name of the category. Will appear in Cargo Console.")]
		public string CategoryName = "";

		[Tooltip("All orders from this category.")]
		[ReorderableList]
		public List<CargoOrderSO> Orders = new List<CargoOrderSO>();
	}
}