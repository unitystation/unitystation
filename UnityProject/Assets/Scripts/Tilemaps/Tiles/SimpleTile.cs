using UnityEngine;

namespace Tiles
{
	public class SimpleTile : BasicTile
	{
		public Sprite sprite;

		public override Sprite PreviewSprite => sprite;
	}
}
