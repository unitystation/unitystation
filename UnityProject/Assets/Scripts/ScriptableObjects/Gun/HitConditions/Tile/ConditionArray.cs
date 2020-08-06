using UnityEngine;

namespace ScriptableObjects.Gun.HitConditions.Tile
{
	[CreateAssetMenu(fileName = "ConditionArray", menuName = "ScriptableObjects/Gun/HitConditions/Tile/ConditionArray", order = 0)]
	public class ConditionArray : ScriptableObject
	{
		private HitInteractTileCondition[] conditions;
		public HitInteractTileCondition[] Conditions => conditions;
	}
}