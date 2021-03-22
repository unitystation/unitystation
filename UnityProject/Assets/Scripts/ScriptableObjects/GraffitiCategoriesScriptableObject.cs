using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects
{
	/// <summary>
	/// Contains a list of all possible ghost roles. This information is available on both server and client.
	/// </summary>
	[CreateAssetMenu(fileName = "GraffitiCategories", menuName = "ScriptableObjects/Systems/Tiles/GraffitiCategories")]
	public class GraffitiCategoriesScriptableObject : ScriptableObject
	{
		[SerializeField]
		private List<GraffitiTileList> graffitiTilesCategories = new List<GraffitiTileList>();
		public List<GraffitiTileList> GraffitiTilesCategories => graffitiTilesCategories;
	}
}
