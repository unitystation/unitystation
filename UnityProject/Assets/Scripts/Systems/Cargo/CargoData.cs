using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.Cargo
{
	public class CargoData : ScriptableObject
	{
		public List<CargoCategory> Categories = new List<CargoCategory>();
		public List<CargoBounty> CargoBounties = new List<CargoBounty>();

	}
}
