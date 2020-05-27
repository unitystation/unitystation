using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pipes
{
	public class PipeNode
	{
		public MetaDataNode IsOn;
		public Vector3Int NodeLocation;
		public Matrix LocatedOn;
		public PipeTile RelatedTile;
		public PipeData pipeData;
		public PipeActions PipeAction;

		//Stuff for pipes to go in here

		public void Initialise(PipeTile DataToTake, MetaDataNode metaDataNode, Vector3Int searchVec, Matrix locatedon, PipeItem pipeItem,int RotationOffset )
		{
			RelatedTile = DataToTake;
			IsOn = metaDataNode;
			pipeData = new PipeData();
			pipeData.SetUp(pipeItem, RotationOffset);
			pipeData.pipeNode = this;
			NodeLocation = searchVec;
			LocatedOn = locatedon;
			pipeData.OnEnable();
		}

	}
}


