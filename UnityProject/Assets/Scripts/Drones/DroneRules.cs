using UnityEngine;
using System.Collections;
namespace Drones
{
	public abstract class DroneRules : ScriptableObject
	{
		public Mind Owner { get; protected set; }
		[SerializeField]
		protected string rulesetName;
		public string RulesetName => rulesetName;
		[SerializeField]
		protected string droneName;
		public string DroneName => droneName;
		[SerializeField]
		protected string rule1;
		public string Rule1 => rule1;
		[SerializeField]
		protected string rule2;
		public string Rule2 => rule2;
		[SerializeField]
		protected string rule3;
		public string Rule3 => rule3;
	}
}