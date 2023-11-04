namespace Mobs.AI.States
{
	public class IdleThink : MobState
	{
		public override void OnEnterState(MobAI master)
		{
			// No Behavior Required
		}

		public override void OnExitState(MobAI master)
		{
			// No Behavior Required
		}

		public override void OnUpdateTick(MobAI master)
		{
			foreach (var possibleNewState in master.MobStates)
			{
				if(possibleNewState.HasGoal(master)) master.SwitchState(null, possibleNewState);
			}
		}

		public override bool HasGoal(MobAI master)
		{
			return false;
		}
	}
}
