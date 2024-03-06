using System.Linq;
using Items;
using Logs;
using NUnit.Framework;
using Systems.Cargo;
using UnityEngine;

namespace Tests.Balance
{
	internal static class CargoReportExtensions
	{
		public static TestReport ExploitHeader(this TestReport report, CargoCategory category, CargoOrderSO order) =>
			report.AppendLine($"Found possible cargo order exploit in: {category.CategoryName}/{order.OrderName}.");

		public static TestReport ExploitFooter(this TestReport report, CargoOrderSO order, int sellPrice) =>
			report.AppendLine($"Buy price: {order.CreditCost} Sell price: {sellPrice}")
				.AppendLine();
	}

	[Category(nameof(Balance))]
	public class CargoTest
	{
		[Test]
		public void CargoOrdersHaveACrate()
		{
			var report = new TestReport();

			foreach (var order in Utils.FindAssetsByType<CargoOrderSO>().Where(order => order.Crate == null))
			{
				report.Fail().AppendLine($"Cargo order {order} is missing a crate to put items in, please fix.");
			}

			report.AssertPassed();
		}

		[Test]
		public void StonksTest()
		{
			var report = new TestReport();

			foreach (var category in Utils.GetSingleScriptableObject<CargoData>(report).Categories)
			{
				foreach (var order in category.Orders)
				{
					var value = order.ContentSellPrice;

					report.Clean()
						.FailIf(value >= order.CreditCost * 0.9f)
						.ExploitHeader(category, order)
						.AppendLine("The export cost is within 10% of the sell price, the items might be too cheap!")
						.ExploitFooter(order, value)
						.MarkDirtyIfFailed()
						.FailIf(value >= order.CreditCost)
						.ExploitHeader(category, order)
						.ExploitFooter(order, value);
				}
			}

			report.Log().AssertPassed();
		}

		[Test]
		public void NoCostTest()
		{
			var report = new TestReport();
			var items = Resources.FindObjectsOfTypeAll<ItemAttributesV2>();
			report.Clean();

			foreach (var item in items)
			{
				if (item.IsFakeItem || item.CanBeSoldInCargo == false) continue;
				if (item.name.StartsWith("_") || item.name.Contains("Item") ||
				    item.name.Contains("Base") || item.name.Contains("Trash") ||
				    item.name.Contains("Random") || item.name.Contains("spawner") || item.name.Contains("Admin")) continue;
				report.FailIf(item.ExportCost == 0).AppendLine($"{item} has no export cost despite it being marked as an object that can be sold, please fix.");
			}

			report.Log().AssertPassed();
		}
	}
}