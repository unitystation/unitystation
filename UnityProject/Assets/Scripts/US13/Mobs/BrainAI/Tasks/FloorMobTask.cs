using UnityEngine;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Utils;

namespace US13.Mobs.BrainAI.Tasks
{
	public sealed class FloorMobTask : MobTask
	{
		public override int SearchRadius => 2;
		private static bool Eligible(MobBehaviorContext context, Vector3Int cell, Matrix matrix) =>
			matrix.MetaTileMap.IsAtmosPassableAt(cell, matrix) && (context.AI.IsEmagged
				? matrix.MetaTileMap.GetTile(cell, LayerType.Floors) != null
				: matrix.MetaTileMap.GetTile(cell, LayerType.Floors) == null && matrix.MetaTileMap.GetTile(cell, LayerType.Base) != null);
		public override bool FindTarget(MobBehaviorContext context, int radius)
		{
			var matrix = MatrixOf(context);
			foreach (var cell in Cells(context, radius))
				if (Eligible(context, cell, matrix) && Select(context, cell, matrix)) return true;
			return false;
		}
		public override bool IsValid(MobBehaviorContext context) => base.IsValid(context) && Eligible(context, context.TargetCell, context.TargetMatrix);
		public override void Complete(MobBehaviorContext context)
		{
			if (context.AI.IsEmagged) context.TargetMatrix.MetaTileMap.RemoveTileWithlayer(context.TargetCell, LayerType.Floors);
			else if (context.AI.Settings.Task.TileToPlace != null) context.TargetMatrix.MetaTileMap.SetTile(context.TargetCell, context.AI.Settings.Task.TileToPlace);
		}
	}
}