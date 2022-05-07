using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.Cargo
{
	[CreateAssetMenu(menuName="ScriptableObjects/Cargo/CargoOrderSO")]
	public class CargoOrderSO : ScriptableObject
	{
		[Tooltip("Name of this order, will appear in console")]
		public string OrderName = "Crate with beer and steak";

		[Tooltip("Cost of this order. You can also auto assign a price using the button bellow.")]
		public int CreditCost = 1000;

		[Tooltip("The game object to spawn as crate (or locker)")]
		public GameObject Crate = null;

		[Tooltip("All the items that this order contains.")]
		public List<GameObject> Items = new List<GameObject>();

		[ReadOnly]
		[Tooltip("Making some quick maffs, this is what the order content might be worth.")]
		public int ContentSellPrice = 0;

		[Tooltip("This will only appear on emagged consoles.")]
		public bool EmagOnly;

		private void OnValidate()
		{
			var value = 0;
			foreach (var item in Items)
			{
				if (item == null)
				{
					return;
				}
				if (item.TryGetComponent<Attributes>(out var attributes))
				{
					value += attributes.ExportCost;
				}
			}

			if (Crate != null)
			{
				if (Crate.TryGetComponent<Attributes>(out var crateAtt))
				{
					value += crateAtt.ExportCost;
				}
			}


			ContentSellPrice = value;
		}
	}
}