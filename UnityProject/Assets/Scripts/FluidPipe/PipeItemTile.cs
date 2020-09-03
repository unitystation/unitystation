using System.Linq;
using UnityEngine;

namespace Pipes
{
	public class PipeItemTile : PipeItem
	{
		public PipeTile pipeTile;

		public override void BuildPipe()
		{
			var searchVec = registerItem.LocalPosition;
			var Tile = (GetPipeTile());
			if (Tile != null)
			{
				int Offset = PipeFunctions.GetOffsetAngle(transform.localEulerAngles.z);
				Quaternion rot = Quaternion.Euler(0.0f, 0.0f,Offset );
				var Matrix = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one);
				registerItem.Matrix.AddUnderFloorTile(searchVec, Tile,Matrix,Colour);
				Tile.InitialiseNodeNew(searchVec,registerItem.Matrix,Matrix );
				Despawn.ServerSingle(this.gameObject);
			}

		}

		public virtual void Setsprite()
		{
		}


		public virtual PipeTile GetPipeTile()
		{
			return pipeTile;
		}

		public override  Connections GetConnections()
		{
			if (pipeTile != null)
			{
				return (pipeTile.Connections.Copy());
			}
			return null;
		}
	}
}
