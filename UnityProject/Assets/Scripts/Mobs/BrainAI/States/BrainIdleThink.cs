namespace Mobs.BrainAI.States
{
	public class BrainIdleThink : BrainMobState
	{
		public override void OnEnterState()
		{
			// No Behavior Required
		}

		public override void OnExitState()
		{
			// No Behavior Required
		}

		public override void OnUpdateTick()
		{
			foreach (var possibleNewState in master.MobStates)
			{
				if(possibleNewState.HasGoal()) master.AddRemoveState(null, possibleNewState);
			}
		}

		public override bool HasGoal()
		{
			return false;
		}
	}
}
