using UnityEngine;
using US13.Objects.Disposals;

namespace US13.Systems.Disposals
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
