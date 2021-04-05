using System;
using UnityEngine;

namespace Systems.Cargo
{
	public class CargoBounty : ScriptableObject
	{
		public DemandDictionary Demands = new DemandDictionary();
		public int Reward;
		public string Description;
	}

	[Serializable]
	public class DemandDictionary : SerializableDictionary<ItemTrait, int>
	{
	}
}