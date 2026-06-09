using UnityEngine;
using US13.Tilemaps.Tiles;

namespace US13.ScriptableObjects.Gun.TileAnimations
{
	[CreateAssetMenu(fileName = "AnimationTile", menuName = "ScriptableObjects/Gun/TileAnimations/AnimationTile", order = 0)]
	public class AnimationTile : ScriptableObject
	{
		[SerializeField] private AnimatedOverlayTile tile = null;
		public AnimatedOverlayTile Tile => tile;

		[SerializeField] private float time = 0;
		public float Time => time;
	}
}