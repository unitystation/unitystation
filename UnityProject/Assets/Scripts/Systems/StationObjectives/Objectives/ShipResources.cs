﻿using System.Collections.Generic;
using UnityEngine;
using System.Text;
using Systems.Cargo;
using Managers;
using Antagonists;

namespace StationObjectives
{
	/// <summary>
	/// An objective to ship items through the cargo shuttle
	/// </summary>
	[CreateAssetMenu(menuName = "ScriptableObjects/StationObjectives/ShipResources")]
	public class ShipResources : StationObjective
	{
		/// <summary>
		/// The pool of possible resources to ship
		/// </summary>
		[SerializeField]
		private SerializableDictionary<ItemTrait, int> itemPool;
		public SerializableDictionary<ItemTrait, int> ItemPool => new (itemPool);

		private int requiredAmount;
		private ItemTrait itemTrait;
		private int currentAmount;

		protected override void Setup()
		{
			var itemEntry = itemPool.PickRandom();
			itemTrait = itemEntry.Key;

			// randomizes the amount needed to complete the shipment, with a minimum of 2/3rds of the default value and a maximum of 1 and 1/3rd of the default
			requiredAmount = Random.Range(itemEntry.Value - itemEntry.Value / 3, itemEntry.Value + itemEntry.Value / 3);

			var report = new StringBuilder();
			report.AppendFormat(description, itemTrait.name, MatrixManager.MainStationMatrix.GameObject.scene.name, requiredAmount);
			report.AppendLine("\n\nAsteroid coordinates are as follows:");
			var index = 0;
			foreach (var location in CentComm.asteroidLocations)
			{
				index++;
				if (index != 1)
				{
					report.Append(" - ");
				}
				report.AppendFormat("<size=24>{0}</size>", Vector2Int.RoundToInt(location));
				if (index == 4)
				{
					report.Append("\n");
					index = 0;
				}
			}
			description = report.ToString();
		}

		public override void OnCanceling()
		{
			description = $"The order to locate and mine local {itemTrait.name} deposits in order of {requiredAmount} was <color=red>CANCELED</color>";
			GameManager.Instance.CentComm.MakeCommandReport(Description, false);
		}

		protected override void SetupInGame()
		{
			var itemEntry = itemPool.PickRandom();

			// randomizes the amount needed to complete the shipment, with a minimum of 2/3rds of the default value and a maximum of 1 and 1/3rd of the default
			requiredAmount = Random.Range(itemEntry.Value - itemEntry.Value / 3, itemEntry.Value + itemEntry.Value / 3);

			itemEntry = new KeyValuePair<ItemTrait, int>(CommonTraits.Instance.GetFromIndex(attributes[0].ItemTraitIndex), attributes[1].Number);

			itemTrait = itemEntry.Key;
			requiredAmount = itemEntry.Value;

			var report = new StringBuilder();
			report.AppendFormat(description, itemTrait.name, MatrixManager.MainStationMatrix.GameObject.scene.name, requiredAmount);
			report.AppendLine("\n\nAsteroid coordinates are as follows:");
			var index = 0;
			foreach (var location in CentComm.asteroidLocations)
			{
				index++;
				if (index != 1)
				{
					report.Append(" - ");
				}
				report.AppendFormat("<size=24>{0}</size>", Vector2Int.RoundToInt(location));
				if (index == 4)
				{
					report.Append("\n");
					index = 0;
				}
			}
			description = report.ToString();

			GameManager.Instance.CentComm.MakeCommandReport(Description, false);
		}

		public override string GetDescription()
		{
			var report = new StringBuilder();
			report.AppendFormat(description, itemTrait.name, MatrixManager.MainStationMatrix.GameObject.scene.name, requiredAmount);

			return report.ToString();
		}

		public override string GetShortDescription()
		{
			return $"Mine {itemTrait.name} in order of {requiredAmount}. Current amount - {currentAmount}";
		}

		public override string GetRoundEndReport()
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.AppendFormat(roundEndReport, requiredAmount, itemTrait.name, currentAmount);
			roundEndReport = stringBuilder.ToString();
			return roundEndReport;
		}

		public override bool CheckStationObjectiveCompletion()
		{
			var soldHistory = CargoManager.Instance.SoldHistory;
			if (soldHistory.ContainsKey(itemTrait) && soldHistory[itemTrait] >= requiredAmount)
			{
				currentAmount = soldHistory[itemTrait];
				Complete = true;
			}
			return base.CheckStationObjectiveCompletion();
		}
	}
}