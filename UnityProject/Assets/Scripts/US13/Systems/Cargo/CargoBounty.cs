using UnityEngine;
using UnityEngine.Serialization;
using US13.Items.Traits;

namespace US13.Systems.Cargo
{
	public class CargoBounty : ScriptableObject
	{
		public SerializableDictionary<ItemTrait, int> Demands = new SerializableDictionary<ItemTrait, int>();
		public int Reward;
		public string TooltipDescription;
		[FormerlySerializedAs("Description")] public string Title;
	}
}