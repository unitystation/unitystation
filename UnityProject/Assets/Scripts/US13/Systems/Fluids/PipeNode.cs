using UnityEngine;
using US13.Objects.Pipes;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Behaviours.Meta;

namespace US13.Systems.Fluids
{
	public class PipeNode
	{
		public MetaDataNode IsOn;
		public Vector3Int NodeLocation;
		public Matrix LocatedOn;
		public PipeTile RelatedTile;
		public PipeData pipeData;
		//Stuff for pipes to go in here

		public void Initialise(PipeTile DataToTake, MetaDataNode metaDataNode, Vector3Int searchVec, Matrix locatedon, int RotationOffset)
		{
			RelatedTile = DataToTake;
			IsOn = metaDataNode;
			pipeData = new PipeData();
			pipeData.SetUp( DataToTake, RotationOffset);
			pipeData.pipeNode = this;
			NodeLocation = searchVec;
			LocatedOn = locatedon;
			pipeData.OnEnable();
		}

	}
}
