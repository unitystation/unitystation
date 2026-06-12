using ScriptableObjects;
using UnityEngine;

namespace US13.Tilemaps.Tiles
{
	/// <summary>
	/// Defines a particular trait that a tile can have (assigned on the tile asset).
	/// </summary>
	[CreateAssetMenu(fileName = "TileTrait", menuName = "Tiles/TileTrait")]
	public class TileTrait : SOTracker
	{
		// Is used in editor, so "unused" warning is ignored.
#pragma warning disable CS0414
		[TextArea]
		[SerializeField] string traitDescription = "Describe me!"; // A short description of the trait and what it does
#pragma warning restore CS0414
	}
}
