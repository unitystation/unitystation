using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Objects.Disposals;

namespace Systems.DisposalPipes
{
	public class DisposalPipeNode
	{
		public Vector3Int NodeLocation;
		public DisposalPipe DisposalPipeTile;

		public void Initialise(DisposalPipe TileToTake, Vector3Int position)
		{
			DisposalPipeTile = TileToTake;
			NodeLocation = position;
		}
	}
}
