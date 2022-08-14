using System.Collections.Generic;
using UnityEngine;


namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "DoorsSO", menuName = "Doors/DoorsList")]
	public class DoorsSO : ScriptableObject
	{
		public List<GameObject> Doors;
	}
}