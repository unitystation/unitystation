using UnityEngine;

namespace ScriptableObjects.Gun.HitConditions.Tile
{
	[CreateAssetMenu(fileName = "ConditionArray", menuName = "ScriptableObjects/Gun/HitConditions/Tile/ConditionArray", order = 0)]
	public class ConditionsTileArray : ScriptableObject
	{
		[SerializeField] private HitInteractTileCondition[] conditions = default;

		public HitInteractTileCondition[] Conditions => conditions;
	}
}
