using UnityEngine;
using Systems.Pipes;
using Objects.Atmospherics;


namespace Items.Atmospherics
{
	public class PipeItemTile : PipeItem
	{
		public PipeTile pipeTile;

		public override void BuildPipe()
		{
			var searchVec = registerItem.LocalPosition;
			var tile = GetPipeTile();
			if (tile == null) return;

			int Offset = PipeFunctions.GetOffsetAngle(transform.localEulerAngles.z);
			Quaternion rot = Quaternion.Euler(0.0f, 0.0f, Offset);
			var Matrix = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one);
			searchVec = registerItem.Matrix.TileChangeManager.UpdateTile(searchVec, tile, Matrix, Colour);
			tile.InitialiseNodeNew(searchVec, registerItem.Matrix, Matrix);
			_ = Despawn.ServerSingle(this.gameObject);

		}

		public virtual void Setsprite() { }

		public virtual PipeTile GetPipeTile()
		{
			return pipeTile;
		}

		public override Connections GetConnections()
		{
			if (pipeTile != null)
			{
				return pipeTile.Connections.Copy();
			}

			return null;
		}
	}
}
