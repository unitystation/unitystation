using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;
using Strings;
using Systems.Cargo;

namespace StationObjectives
{
	/// <summary>
	/// An objective to ship items to nanotrasen
	/// </summary>
	[CreateAssetMenu(menuName = "ScriptableObjects/StationObjectives/ShipResources")]
	public class ShipResources : StationObjective
	{
		/// <summary>
		/// The pool of possible resources to ship
		/// </summary>
		[SerializeField]
		private ItemDictionary itemPool;

		private List<Vector2> asteroidLocations = new List<Vector2>();
		private ResourceTracker tracker;

		protected override bool CheckCompletion()
		{
			if (tracker == null)
			{
				Logger.LogError($"Error, tracker is null!");
				return Complete;
			}
			var finalReport = new StringBuilder();
			finalReport.Append(victoryDescription);
			finalReport.Replace("SHIPPEDVAL", $"{tracker.CurrentAmount}");
			victoryDescription = finalReport.ToString();
			return Complete;
		}

		private void OnEnable()
		{
			CargoManager inst = CargoManager.Instance;
			inst.OnObjectSold += CheckItemSold;
		}
		private void OnDisable()
		{
			CargoManager inst = CargoManager.Instance;
			inst.OnObjectSold -= CheckItemSold;
		}

		private class ResourceTracker
		{
			public int RequiredAmount;
			public string ItemName;
			public int CurrentAmount;

			public ResourceTracker(int requiredAmount, string itemName)
			{
				RequiredAmount = requiredAmount;
				ItemName = itemName;
				CurrentAmount = 0;
			}

			public void AddToTracker(int amount = 1)
			{
				CurrentAmount = CurrentAmount + amount;
			}
		}

		protected override void Setup()
		{
			foreach (var body in GameManager.Instance.SpaceBodies)
			{
				if (body.TryGetComponent<Asteroid>(out _))
				{
					asteroidLocations.Add(body.ServerState.Position);
				}
			}

			int randomPosCount = Random.Range(1, 5);
			for (int i = 0; i <= randomPosCount; i++)
			{
				asteroidLocations.Add(GameManager.Instance.RandomPositionInSolarSystem());
			}
			asteroidLocations = asteroidLocations.OrderBy(x => Random.value).ToList();

			var possibleItems = itemPool.ToList();

			if (possibleItems.Count == 0)
			{
				Logger.LogWarning("Unable to find any items to ship. This shouldn't happen!");
			}

			var itemEntry = possibleItems.PickRandom();
			if (itemEntry.Key == null)
			{
				Logger.LogError($"Objective failed because the item type chosen is somehow destroyed." +
				                " Definitely a programming bug. ", Category.Round);
				return;
			}

			var itemName = itemEntry.Key.Item().InitialName;

			if (string.IsNullOrEmpty(itemName))
			{
				Logger.LogError($"Objective failed because the InitialName has not been" +
				                " set on this objects ItemAttributes. " +
				                $"Item: {itemEntry.Key.Item().gameObject.name}", Category.Round);
				return;
			}

			// randomizes the amount needed to complete the shipment, with a minimum of 2/3rds of the default value and a maximum of 1 and 1/3rd of the default
			var amount = Random.Range(itemEntry.Value - itemEntry.Value / 3, itemEntry.Value + itemEntry.Value / 3);
			if (amount <= 0)
			{
				amount = 1;
			}

			tracker = new ResourceTracker(amount, itemName);
			if (string.IsNullOrEmpty(tracker.ItemName))
			{
				Logger.LogError($"Station objective failed because the tracker is busted." +
								$" Should have gotten {itemName}, got null instead.", Category.Round);
				return;
			}

			var report = new StringBuilder();
			report.AppendFormat(ReportTemplates.DeliveryStationObjective, amount);
			report.Replace("MATERIAL", itemName);

			foreach (var location in asteroidLocations)
			{
				report.AppendFormat(" <size=24>{0}</size> ", Vector2Int.RoundToInt(location));
			}

			description = report.ToString();
			Complete = false;

			var vicReport = new StringBuilder();
			vicReport.AppendFormat(ReportTemplates.DeliveryStationObjectiveEnd, amount);
			vicReport.Replace("MATERIAL", itemName);
			victoryDescription = vicReport.ToString();
		}
		private void CheckItemSold(GameObject soldObject)
		{
			if (soldObject == null){
				Logger.LogError("SoldObject is null!");
				return;
			}
			var attributes = soldObject.GetComponent<Attributes>();
			string exportName;
			if (attributes)
			{
				if (string.IsNullOrEmpty(attributes.InitialName))
				{
					exportName = attributes.ArticleName;
				}
				else
				{
					exportName = attributes.InitialName;
				}
			}
			else
			{
				exportName = soldObject.ExpensiveName();
			}

			if (tracker == null)
			{
				Logger.LogError($"Error, tracker is null!");
				return;
			}
			if (exportName == tracker.ItemName)
			{
				var stackable = soldObject.GetComponent<Stackable>();
				if (stackable)
				{
					tracker.AddToTracker(stackable.Amount);
				}
				else
				{
					tracker.AddToTracker();
				}
			}
			else
			{
				return;
			}
			if (tracker.CurrentAmount >= tracker.RequiredAmount)
			{
				Complete = true;
			}
		}
	}
}