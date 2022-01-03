using HealthV2;
using Core.Threading;

namespace Health.Sickness
{
	public class SicknessThread : ThreadedBehaviour
	{
		public float startedTime;
		public System.Random random;
		public override void ThreadedWork()
		{
			lock (SicknessManager.Instance.sickPlayers)
			{
				SicknessManager.Instance.sickPlayers.RemoveAll(p => p.sicknessAfflictions.Count == 0);

				foreach (var playerSickness in SicknessManager.Instance.sickPlayers)
				{
					// Don't check sickness for unconcious and dead players.
					if ((playerSickness.MobHealth != null) && (playerSickness.MobHealth.ConsciousState < ConsciousState.UNCONSCIOUS))
					{
						playerSickness.sicknessAfflictions.RemoveAll(p => p.IsHealed);

						lock (playerSickness.sicknessAfflictions)
						{
							foreach (var sicknessAffliction in playerSickness.sicknessAfflictions)
							{
								CheckSicknessProgression(sicknessAffliction);
								CheckSymptomOccurence(sicknessAffliction, playerSickness.MobHealth);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Check if we should trigger due symptoms
		/// </summary>
		public void CheckSymptomOccurence(SicknessAffliction sicknessAffliction, LivingHealthMasterBase livingHealth)
		{
			var sickness = sicknessAffliction.Sickness;

			// Loop on each reached stage to see if a symptom should trigger
			for (var stage = 0; stage < sickness.SicknessStages.Count; stage++)
			{
				if (stage < sicknessAffliction.CurrentStage)
				{
					float? stageNextOccurence = sicknessAffliction.GetStageNextOccurence(stage);
					if ((stageNextOccurence != null) && (startedTime > stageNextOccurence))
					{
						SicknessStage sicknessStage = sickness.SicknessStages[stage];

						// Since many symptoms need to be called within the main thread, we invoke it
						SicknessManager.Instance.blockingCollectionSymptoms.Add(new SymptomManifestation(sicknessAffliction, stage, livingHealth));

						if (sicknessStage.RepeatSymptom)
						{
							ScheduleStageSymptom(sicknessAffliction, stage);
						}
					}
				}
			}
		}

		/// <summary>
		/// Check if we should progress the current sickness.  Progress if it we are due.
		/// </summary>
		/// <param name="sicknessAffliction">A sickness of the player</param>
		public void CheckSicknessProgression(SicknessAffliction sicknessAffliction)
		{
			Sickness sickness = sicknessAffliction.Sickness;

			// Check if we are already at the final stage of a sickness
			if (sicknessAffliction.CurrentStage < sickness.SicknessStages.Count)
			{
				// Loop on each stage of the sickness to see if we have now reached the stage
				// Makes the sickness progress
				var totalSecondCount = 0;
				for (var stage = 0; stage < sickness.SicknessStages.Count; stage++)
				{
					// If the stage is isn't reached yet, we check if it's time to progress toward it
					if (stage > (int)sicknessAffliction.CurrentStage - 1)
					{
						// Check if it's time to progress to the new stage
						if ((startedTime - sicknessAffliction.ContractedTime) > totalSecondCount)
						{
							// It's time to progress to the new stage
							ScheduleStageSymptom(sicknessAffliction, stage);
						}
						else
						{
							// It's not time yet, no point checking for further stages
							break;
						}
					}

					totalSecondCount += sickness.SicknessStages[stage].SecondsBeforeNextStage;
				}
			}
		}

		/// <summary>
		/// Schedule the next occurence of the symptom of a particular stage
		/// </summary>
		/// <param name="sicknessAffliction">A sickness afflicting a player</param>
		/// <param name="stage">The stage for which the symptom must be scheduled</param>
		private void ScheduleStageSymptom(SicknessAffliction sicknessAffliction, int stage)
		{
			var sickness = sicknessAffliction.Sickness;
			var sicknessStage = sickness.SicknessStages[stage];

			if (sicknessStage.RepeatMaxDelay < sicknessStage.RepeatMinDelay)
			{
				Logger.LogError($"Sickness: {sickness.SicknessName}. Repeatable sickness symptoms should always have a RepeatMaxDelay ({sicknessStage.RepeatMaxDelay}) >= RepeatMinDelay ({sicknessStage.RepeatMinDelay})", Category.Health);
				return;
			}

			if (sicknessStage.RepeatMinDelay < 5)
			{
				Logger.LogError($"Sickness: {sickness.SicknessName}. Repeatable sickness symptoms should have a RepeatMinDelay ({sicknessStage.RepeatMinDelay}) >= 5.  Think of the server performance.", Category.Health);
				return;
			}

			sicknessAffliction.ScheduleStageNextOccurence(stage, startedTime + random.Next((int)sicknessStage.RepeatMinDelay, (int)sicknessStage.RepeatMaxDelay));
		}
	}
}