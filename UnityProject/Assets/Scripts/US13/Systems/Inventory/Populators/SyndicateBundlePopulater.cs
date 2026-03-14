using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Core.Utils;
using US13.UI.Items.PDA;
using Util;

namespace US13.Systems.Inventory.Populators
{
	[CreateAssetMenu(fileName = "SyndicateBundlePopulater", menuName = "Inventory/Populators/Storage/SyndicateBundlePopulater", order = 5)]
	public class SyndicateBundlePopulater : ItemStoragePopulator
	{

		public int TCMinSpend = 23;

		public UplinkCategoryList ReferenceCategory;

		public bool AllowNukeOps = false;

		public int NumberOfItemsToChoose = 5;

		//TODO Check job restriction?

		public override void PopulateItemStorage(IStoreThings toPopulate,MonoBehaviour component , PopulationContext context, SpawnInfo info)
		{

			List<UplinkItem> Chosen = new List<UplinkItem>();


			List<UplinkItem> availableToChoose = ReferenceCategory.ItemCategoryList.SelectMany(x =>
				x.ItemList.Where(x => (AllowNukeOps || x.IsNukeOps == false) && x.ExcludedRandomPick == false)).Shuffle().ToList();

			int Spent = 0;

			foreach (var item in availableToChoose)
			{
				Chosen.Add(item);
				Spent = Spent + item.Cost;

				if (Spent >= TCMinSpend )
				{
					break;
				}

				if (Chosen.Count >= NumberOfItemsToChoose)
				{
					var ToRemove = Chosen.RemoveRandom<UplinkItem>();
					Spent = Spent - ToRemove.Cost;
				}
			}

			foreach (var Item in Chosen)
			{
				var gobject = Spawn.ServerPrefab(Item.Item,component.gameObject.AssumedWorldPosServer(), PrePickRandom: true).GameObject;
				toPopulate.ServerTryAdd(gobject);
			}
		}
	}
}
