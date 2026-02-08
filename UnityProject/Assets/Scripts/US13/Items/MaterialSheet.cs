using UnityEngine;
using US13.Items.Traits;

namespace US13.Items
{
	[CreateAssetMenu(fileName = "MaterialsInMachineStorage", menuName = "ScriptableObjects/Mining/MaterialSheet")]
	public class MaterialSheet : ScriptableObject
	{
		public int laborPoint;
		public ItemTrait oreTrait;
		public string displayName;
		public GameObject RefinedPrefab;
		public ItemTrait materialTrait;
	}
}