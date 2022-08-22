using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace StationObjectives
{
	/// <summary>
	/// Stores all station objectives. Use the public methods to get since they must be instantiated
	/// </summary>
	[CreateAssetMenu(menuName = "ScriptableObjects/StationObjectiveData")]
	public class StationObjectiveData : ScriptableObject
	{
		/// <summary>
		/// All possible station objectives.
		/// </summary>
		[SerializeField]
		private List<StationObjective> Objectives = new List<StationObjective>();

		public StationObjective GetRandomObjective()
		{
			var objective = Objectives[Random.Range(0, Objectives.Count)];
			return Instantiate(objective);
		}
	}
}