using System.Collections.Generic;
using System.Linq;
using Antagonists;
using Core.Editor.Attributes;
using Cysharp.Threading.Tasks;
using Systems.Antagonists.Antags.BloodBrothers;
using UnityEngine;

namespace Systems.Antagonists.Antags
{
	/// <summary>
	/// Blood brothers is a **TEAM** based antagonist role where multiple players
	/// are spawned to try and complete objectives together. The death of one player is the death of all players.
	/// Blood Brothers *can* spawn outside their usual game-mode, but it's not the main intention, and should be considered as a game breaking bug
	/// if Bod ever shoves blood brothers where they shouldn't be again.
	/// </summary>
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/BloodBrother")]
	public class BloodBrother : Antagonist
	{
		[SerializeField] private float extraHealthForBrothers = 350f;

		[Tooltip("For use in Syndicate Uplinks")]
		[SerializeField]
		private int initialTC = 12;

		[SerializeReference, SelectImplementation(typeof(IBloodBrotherAbility))]
		public List<IBloodBrotherAbility> BloodBrotherAbilities = new();


		public override void AfterSpawn(Mind SpawnMind)
		{
			Chat.AddExamineMsg(SpawnMind.Body.gameObject,
				"<color=red>You're a convicted prisoner and test subject who was given " +
				"a new chance for freedom by the syndicate.\n You and your blood brothers <b>must all succeed</b> to earn your freedom, or die trying.</color>");
			_ = CheckForOtherBloodBrothers(SpawnMind.Body.gameObject);

			SpawnMind.Body.playerHealth.SetMaxHealth(SpawnMind.Body.playerHealth.MaxHealth + extraHealthForBrothers);
			AntagManager.TryInstallPDAUplink(SpawnMind, initialTC, false);
			SetupPowers(SpawnMind);
			SpawnMind.Body.playerHealth.OnDeath += GameModes.BloodBrothers.OnBrotherDeath;
		}

		private async UniTask CheckForOtherBloodBrothers(GameObject spawnMind)
		{
			await UniTask.WaitForSeconds(5f);
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

		private void SetupPowers(Mind spawnMind)
		{
			foreach (var ability in BloodBrotherAbilities)
			{
				if (DMMath.Prob(ability.ChanceToGiveOnSpawn))
				{
					ability.GiveAbility(spawnMind);
				}
			}
			Chat.AddExamineMsg(spawnMind.Body.gameObject,
				"You feel much more resilient.");
		}
	}
}