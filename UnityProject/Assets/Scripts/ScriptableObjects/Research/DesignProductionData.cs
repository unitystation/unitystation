using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects.Systems.Research
{
	[CreateAssetMenu(fileName = "DesignProductionData", menuName = "ScriptableObjects/Systems/Techweb/DesignProductionData")]
	public class DesignProductionData : ScriptableObject
	{
		public SerializableDictionary<string, MaterialSheet> MaterialSheets;
	}
}