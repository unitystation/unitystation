
using UnityEditor;

/* Editor control over the FloorTile component:
 * This is used to add the ambient tiles so the
 * majority are instantiated in editmode
 * rather then on Start() when the game has started
 */
#if UNITY_EDITOR
namespace US13.Objects.Construction.Floors
{
	[CustomEditor(typeof(FloorTile))]
	[CanEditMultipleObjects]
	public class FloorTileEditor : Editor
	{
		private FloorTile floorTile;

		private void OnSceneGUI()
		{
			if (floorTile == null)
			{
				floorTile = target as FloorTile;
			}
			if (floorTile == null)
			{
				return;
			}
			EditorChangedActions();
		}

		private void EditorChangedActions()
		{
			floorTile.CheckAmbientTile();
		}
	}
}
#endif
