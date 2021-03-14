
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.Cargo
{
	public class CargoData : ScriptableObject
	{
		//Stores all possible supplies broken into categories
		public List<CargoOrderCategory> Supplies = new List<CargoOrderCategory>();
	}
}
