using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ClearAllTilesUtil : MonoBehaviour
{
	[SerializeField] private Tilemap map;

	[Button("Erase all tiles")]
	private void EraseAllTimes()
	{
		if (map == null)
		{
			Debug.LogError("Assign a tilemap to clear first!");
			return;
		}
		map.ClearAllTiles();
	}
}
