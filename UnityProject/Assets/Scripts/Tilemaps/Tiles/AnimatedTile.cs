using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tiles
{
	// TODO: obsolete?
	public class AnimatedTile : BasicTile
	{


		public override Sprite PreviewSprite => Sprites.Length > 0 ? Sprites[0] : null;

	}
}

