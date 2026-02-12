using UnityEngine;
using US13.Items;
using US13.Items.Traits;

namespace US13.ScriptableObjects.Research
{
	[CreateAssetMenu(fileName = "DesignProductionData", menuName = "ScriptableObjects/Systems/Techweb/DesignProductionData")]
	public class DesignProductionData : ScriptableObject
	{
		public SerializableDictionary<string, MaterialSheet> MaterialSheets;

		public SerializableDictionary<ItemTrait, string> TraitsToDesignID;
	}
}