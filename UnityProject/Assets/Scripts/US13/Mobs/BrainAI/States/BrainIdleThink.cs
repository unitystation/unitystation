namespace US13.Mobs.BrainAI.States
{
	[System.Serializable]
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
				if(possibleNewState.HasGoal()) master.RemoveAddState(null, possibleNewState);
			}
		}

		public override bool HasGoal()
		{
			return false;
		}
	}
}
