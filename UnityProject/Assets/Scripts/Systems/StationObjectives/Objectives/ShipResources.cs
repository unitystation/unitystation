using System.Collections.Generic;
using UnityEngine;
using System.Text;
using Systems.Cargo;

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
		private DemandDictionary itemPool;

		private int requiredAmount;
		private ItemTrait itemTrait;
		private int currentAmount;

		public override void Setup()
		{
			var itemEntry = itemPool.PickRandom();
			itemTrait = itemEntry.Key;

			// randomizes the amount needed to complete the shipment, with a minimum of 2/3rds of the default value and a maximum of 1 and 1/3rd of the default
			requiredAmount = Random.Range(itemEntry.Value - itemEntry.Value / 3, itemEntry.Value + itemEntry.Value / 3);

			var report = new StringBuilder();
			report.AppendFormat(description, itemTrait.name, MatrixManager.MainStationMatrix.GameObject.scene.name, requiredAmount);
			description = report.ToString();
		}

		public override string GetRoundEndReport()
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.AppendFormat(roundEndReport, requiredAmount, itemTrait.name, currentAmount);
			roundEndReport = stringBuilder.ToString();
			return roundEndReport;
		}

		public override bool CheckCompletion()
		{
			var soldHistory = CargoManager.Instance.SoldHistory;
			if (soldHistory.ContainsKey(itemTrait) && soldHistory[itemTrait] >= requiredAmount)
			{
				currentAmount = soldHistory[itemTrait];
				Complete = true;
			}
			return base.CheckCompletion();
		}
	}
}