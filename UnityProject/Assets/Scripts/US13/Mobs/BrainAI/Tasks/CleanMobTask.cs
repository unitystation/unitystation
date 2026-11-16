using Chemistry;
using US13.Core;
using US13.Objects.Construction.FloorDecals;
using Util;

namespace US13.Mobs.BrainAI.Tasks
{
	public sealed class CleanMobTask : MobTask
	{
		public override int SearchRadius => 3;
		public override bool FindTarget(MobBehaviorContext context, int radius)
		{
			var matrix = MatrixOf(context);
			foreach (var cell in Cells(context, radius))
			{
				bool eligible = context.AI.IsEmagged ? !matrix.MetaDataLayer.IsSlipperyAt(cell) : matrix.MetaDataLayer.HasReagentSpatter(cell);
				if (eligible && Select(context, cell, matrix)) return true;
			}
			if (context.AI.IsEmagged) return false;
			var decals = ComponentsTracker<FloorDecal>.GetAllNearbyTypesToTarget(context.AI.Body.gameObject, radius);
			if (decals == null) return false;
			foreach (var decal in decals)
				if (decal != null && decal.Cleanable && Select(context, decal.gameObject.AssumedWorldPosServer().ToLocalInt(matrix), matrix, decal.gameObject)) return true;
			return false;
		}
		public override void Complete(MobBehaviorContext context)
		{
			var matrix = context.TargetMatrix;
			var cell = context.TargetCell;
			if (context.AI.IsEmagged)
			{
				if (context.AI.GetSettings<BotTaskSettings>().ReagentToSpill == null) return;
				matrix.MetaDataLayer.ReagentReact(new ReagentMix(context.AI.GetSettings<BotTaskSettings>().ReagentToSpill, 5f, 273.15f), cell.ToWorldInt(matrix), cell);
			}
			else matrix.MetaDataLayer.Clean(cell.ToWorldInt(matrix), cell, false);
		}
	}
}