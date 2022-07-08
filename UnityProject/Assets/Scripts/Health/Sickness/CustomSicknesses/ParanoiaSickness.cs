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
		private bool isOnCooldown = false;
		[SerializeField, Range(10f,60f)] private float cooldownTime = 10f;

		[SerializeField] private List<string> theThoughtsOfSomeoneAboutToRunOverSomeGreenGlowies = new List<string>();

		public override void SicknessBehavior(LivingHealthMasterBase health)
		{
			if (isOnCooldown) return;
			base.SicknessBehavior(health);
			Chat.AddExamineMsg(health.gameObject, theThoughtsOfSomeoneAboutToRunOverSomeGreenGlowies.PickRandom());
			if (CurrentStage > 4) health.CannotRecognizeNames = DMMath.Prob(50);
			StartCoroutine(Cooldown());
			//TODO : ALLOW PLAYERS TO SEE VISUAL HALLUCINATIONS AS WELL
		}

		private IEnumerator Cooldown()
		{
			isOnCooldown = true;
			yield return WaitFor.Seconds(cooldownTime);
			isOnCooldown = false;
		}
	}
}