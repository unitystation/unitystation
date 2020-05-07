using UnityEngine;
using UnityEditor;
namespace Drones
{
	/// <summary>
	/// Defines a drone.
	/// </summary>
	public abstract class Drone : ScriptableObject
	{
		[Tooltip("The name of the drone type")]
		[SerializeField]
		private string droneName = "New Drone";
		/// <summary>
		/// The name of the drone type
		/// </summary>
		public string DroneName => droneName;
		[Tooltip("How many rules this drone has")]
		[SerializeField]
		private int numberOfRules = 3;
		/// <summary>
		/// How many rules this drone has
		/// </summary>
		public int NumberOfRules => numberOfRules;
		[Tooltip("Default drone occupation is Drone, this shouldn't be changed.")]
		[SerializeField]
		private Occupation droneOccupation = null;
		/// <summary>
		/// Occupation that drones should spawn as, shouldn't be left null.
		/// </summary>
		public Occupation DroneOccupation => droneOccupation;
	}
}