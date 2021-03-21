using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects
{
	/// <summary>
	/// Contains a list of all the tile categories, used in dev tile changer
	/// </summary>
	[CreateAssetMenu(fileName = "TileCategories", menuName = "ScriptableObjects/Systems/Tiles/TileCategories")]
	public class TileCategorySO : SingletonScriptableObject<TileCategorySO>
	{
		[SerializeField]
		private List<TileListSO> tileCategories = new List<TileListSO>();
		public List<TileListSO> TileCategories => tileCategories;
	}
}
