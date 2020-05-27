using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pipes
{
	[CreateAssetMenu(fileName = "PipeTile_Tile", menuName = "Tiles/PipeTile")]
	public class PipeTile : BasicTile
	{
		//Remember this is all static
		public CorePipeType PipeType;
		public  Connections Connections = new Connections();
		public Sprite sprite;
		public override Sprite PreviewSprite => sprite;
	}
}
