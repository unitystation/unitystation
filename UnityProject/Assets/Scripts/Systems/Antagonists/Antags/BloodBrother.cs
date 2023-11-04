using System.Collections.Generic;
using Antagonists;
using GameModes;
using Health.Sickness;
using UnityEngine;
using Task = System.Threading.Tasks.Task;
using Wizard = Antagonists.Wizard;

namespace Systems.Antagonists.Antags
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/BloodBrother")]
	public class BloodBrother : Antagonist
	{
		[SerializeField] private float extraHealthForBrothers = 350f;
		[SerializeField] private Sickness paranoiaSickness;

		[Tooltip("For use in Syndicate Uplinks")]
		[SerializeField]
		private int initialTC = 12;


		public override void AfterSpawn(Mind SpawnMind)
		{
			Chat.AddExamineMsg(SpawnMind.Body.gameObject,
				"<color=red>You're a convicted prisoner and test subject who was given " +
				"a new chance for freedom by the syndicate.\n You and your blood brothers <b>must all succeed</b> to earn your freedom, or die trying.</color>");
			_ = CheckForOtherBloodBrothers(SpawnMind.Body.gameObject);
			SpawnMind.Body.playerHealth.OnDeath += BloodBrothers.OnBrotherDeath;
			SpawnMind.Body.playerHealth.SetMaxHealth(SpawnMind.Body.playerHealth.MaxHealth + extraHealthForBrothers);
			AntagManager.TryInstallPDAUplink(SpawnMind, initialTC, false);
			if (DMMath.Prob(25))
			{
				Wizard.AddSpellToPlayer(Wizard.GetRandomWizardSpell(), SpawnMind);
				Chat.AddExamineMsg(SpawnMind.Body.gameObject,
					"Due to your past in prison.. You've gained magical ability.");
			}
			if (DMMath.Prob(15))
			{
				SpawnMind.Body.playerHealth.AddSickness(paranoiaSickness);
				Chat.AddExamineMsg(SpawnMind.Body.gameObject,
					"Due to your past in prison.. You've gained paranoia from the experiments they've done on you.");
				return;
			}
			Chat.AddExamineMsg(SpawnMind.Body.gameObject,
				"You feel much more resilient.");
		}

		private async Task CheckForOtherBloodBrothers(GameObject spawnMind)
		{
			await Task.Delay(1250);
			if (AntagManager.Instance.AntagCount < 2)
			{
				NoBrothersFound(spawnMind);
				return;
			}

			var listOfBrotherNames = new List<string>();
			foreach (var brother in AntagManager.Instance.ActiveAntags)
			{
				if (brother.Antagonist is not BloodBrother) continue;
				listOfBrotherNames.Add(brother.Owner.CurrentPlayScript.characterSettings.Name);
			}

			if (listOfBrotherNames.Count < 2)
			{
				NoBrothersFound(spawnMind);
				return;
			}

			Chat.AddExamineMsg(spawnMind,"<color=red>Your blood brothers are:</color>");
			foreach (var brotherName in listOfBrotherNames)
			{
				Chat.AddExamineMsg(spawnMind,$"- {brotherName}");
			}
		}

		private void NoBrothersFound(GameObject spawnMind)
		{
			Chat.AddExamineMsg(spawnMind,"<color=red>Your blood brother has not arrived with you..</color>");
		}
	}
}