using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pipes
{
	[CreateAssetMenu(fileName = "PipeTile_Tile", menuName = "Tiles/PipeTile")]
	public class PipeTile : BasicTile
	{
		//Remember this is all static
		public PipeLayer PipeLayer = PipeLayer.Second;
		public CorePipeType PipeType;

		public CustomLogic CustomLogic;
		public  Connections Connections = new Connections();
		public bool NetCompatible = true;
		public Sprite sprite;
		public override Sprite PreviewSprite => sprite;

		public void InitialiseNode(Vector3Int Location,Matrix matrix )
		{
			var ZeroedLocation = new Vector3Int(x:Location.x, y:Location.y,0);
			var metaData = matrix.MetaDataLayer.Get(ZeroedLocation);
			var pipeNode = new PipeNode();
			var rotation = matrix.UnderFloorLayer.GetMatrix4x4(Location, this);
			int Offset = PipeFunctions.GetOffsetAngle(rotation.rotation.eulerAngles.z);
			pipeNode.Initialise(this, metaData, ZeroedLocation, matrix,Offset);
			metaData.PipeData.Add(pipeNode);
		}
	}
}
