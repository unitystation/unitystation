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

			foreach (var category in cargoData.Categories)
			{
				foreach (var order in category.Orders)
				{
					var value = order.ContentSellPrice;
					if (value < order.CreditCost)
					{
						//Check for within 10 percent
						if (value >= order.CreditCost * 0.9f)
						{
							report.AppendLine("Found possible cargo order exploit in: ");
							report.AppendFormat("{0}/{1}.", category.CategoryName, order.OrderName);
							report.AppendLine("The export cost is within 10% of the sell price, the items might be too cheap!");
							report.AppendFormat("\nBuy price: {0} Sell price: {1}\n\n", order.CreditCost, value);
						}

						continue;
					}

					report.AppendLine("Found possible cargo order exploit in: ");
					report.AppendFormat("{0}/{1}.", category.CategoryName, order.OrderName);
					report.AppendFormat("\nBuy price: {0} Sell price: {1}\n\n", order.CreditCost, value);
				}
			}

			Logger.Log(report.ToString(), Category.Tests);
			Assert.IsEmpty(report.ToString());
		}
	}
}