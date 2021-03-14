using System.Text;
using Systems.Cargo;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.Balance
{
	public class CargoTest
	{
		[Test]
		public void StonksTest()
		{
			var report = new StringBuilder();

			if (Utils.TryGetScriptableObjectGUID(typeof(CargoData), report, out string guid) == false)
			{
				Assert.Fail(report.ToString());
				return;
			}

			var cargoData = AssetDatabase.LoadAssetAtPath<CargoData>(AssetDatabase.GUIDToAssetPath(guid));

			foreach (var cargoOrderCategory in cargoData.Supplies)
			{
				foreach (var items in cargoOrderCategory.Supplies)
				{
					var value = CalculateOrderValue(items);

					if (value <= items.CreditsCost) continue;

					report.AppendLine("Found possible cargo order exploit in: ");
					report.AppendFormat("{0}/{1}.", cargoOrderCategory.CategoryName, items.OrderName);
					report.AppendFormat("\nBuy price: {0} Sell price: {1}\n\n", items.CreditsCost, value);
				}
			}

			Logger.Log(report.ToString(), Category.Tests);
			Assert.IsEmpty(report.ToString());
		}

		private static int CalculateOrderValue(CargoOrder order)
		{
			int value = 0;
			foreach (var item in order.Items)
			{
				if (item == null) continue;
				value += CalculateItemValue(item);
			}

			value += CalculateCrateValue(order);

			return value;
		}

		private static int CalculateItemValue(GameObject item)
		{
			int value = 0;
			var attributes = item.GetComponent<Attributes>();

			if (attributes != null)
			{
				value += attributes.ExportCost;
			}

			return value;
		}

		private static int CalculateCrateValue(CargoOrder order)
		{
			int value = 0;
			var crate = order.Crate.GetComponent<Attributes>();

			if (crate != null)
			{
				value += crate.ExportCost;
			}

			return value;
		}

	}
}