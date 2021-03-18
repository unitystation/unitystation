using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.Cargo
{
	public class CargoData : ScriptableObject
	{
		[ReorderableList]
		[Tooltip("Stores all possible supplies broken into categories")]
		public List<CargoCategory> Categories = new List<CargoCategory>();
	}
}
