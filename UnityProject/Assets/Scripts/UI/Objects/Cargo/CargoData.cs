
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using Objects;

namespace Systems.Cargo
{
	public class CargoData : ScriptableObject
	{
		//Stores all possible supplies broken into categories
		public List<CargoOrderCategory> Supplies = new List<CargoOrderCategory>();

		//TO-DO - comeup with clever idea for bounties
		public int GetBounty(ObjectBehaviour item)
		{
			return 50;
		}



		[Button("Auto Calculate Order Price")]
		public void AutoCalculateOrderPrice()
		{
			foreach (var category in Supplies)
			{
				foreach (var order in category.Supplies)
				{
					if (order.CreditsCost != 0) continue;

					int newValue = 0;
					foreach (var item in order.Items)
					{
						if (item.TryGetComponent<Attributes>(out var attributes) == false)
						{
							continue;
						}

						newValue += attributes.ExportCost;
					}

					if (order.Crate.TryGetComponent<Attributes>(out var crateValue))
					{
						newValue += crateValue.ExportCost;
					}

					order.CreditsCost = (int) (newValue * 1.8f);
				}
			}
		}

	}
}
