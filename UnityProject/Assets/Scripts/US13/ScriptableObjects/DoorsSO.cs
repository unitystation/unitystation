using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace US13.ScriptableObjects
{
	[CreateAssetMenu(fileName = "DoorsSO", menuName = "Doors/DoorsList")]
	public class DoorsSO : ScriptableObject
	{
		[SerializeField, FormerlySerializedAs("Doors")] private List<GameObject> doors;
		public List<GameObject> Doors => doors;
	}
}