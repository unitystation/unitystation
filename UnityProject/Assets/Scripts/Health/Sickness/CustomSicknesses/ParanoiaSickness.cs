using System.Collections;
using System.Collections.Generic;
using Core.Chat;
using HealthV2;
using ScriptableObjects.RP;
using UnityEngine;

namespace Health.Sickness
{
	public class ParanoiaSickness : Sickness
	{
		[SerializeField] private List<string> theThoughtsOfSomeoneAboutToRunOverSomeGreenGlowies = new List<string>();

		public override void SicknessBehavior(LivingHealthMasterBase health)
		{
			if (isOnCooldown) return;
			Chat.AddExamineMsg(health.gameObject, theThoughtsOfSomeoneAboutToRunOverSomeGreenGlowies.PickRandom());
			if (CurrentStage >= 4) health.CannotRecognizeNames = DMMath.Prob(50);
			if(CurrentStage >= 2) health.playerScript.playerNetworkActions.CmdSetCurrentIntent(Intent.Harm);
			base.SicknessBehavior(health);
			//TODO : ALLOW PLAYERS TO SEE VISUAL HALLUCINATIONS AS WELL
		}

		public override void SymptompFeedback(LivingHealthMasterBase health)
		{
			if(CurrentStage >= 3) EmoteActionManager.DoEmote(emoteFeedback, health.gameObject);
		}
	}
}