using System.Collections.Generic;
using UnityEngine;
using System.Linq;
namespace Drones
{
	[CreateAssetMenu(menuName = "ScriptableObjects/DroneData")]
	public class DroneData : ScriptableObject
	{
		[SerializeField]
		private List<Drone> DroneTypes = new List<Drone>();
		[SerializeField]
		private List<DroneRules> DroneLaws = new List<DroneRules>();
	}
}