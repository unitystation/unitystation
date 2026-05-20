using System;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Attributes;
using US13.Core.Chat;
using US13.HealthV2;
using US13.HealthV2.Living.MedicalChemistry;
using US13.HealthV2.Living.PolymorphicSystems;
using US13.Managers;
using US13.Player;
using US13.Systems.Antagonists;

namespace US13.Items.Weapons.Melee
{
	[Serializable]
	public class SanguineMeleeEffect : ICustomMeleeBehaviour
	{
		[SerializeField] private TeamData vampireTeam;

		[SerializeField, Range(0,100)] private int sanguineDrainThreshold = 10;
		private float  SanguineThresholdFraction => sanguineDrainThreshold / 100.0f;
		[SerializeField, Range(0,100)] private int sanguineDrainAmount = 4;
		private float  SanguineAmountFraction => sanguineDrainAmount / 100.0f;
		[SerializeField, Range(0,100)] private int sanguineEfficiency = 30;
		private float SanguineEfficiencyFraction => sanguineEfficiency / 100.0f;

		[SerializeField, Range(0,20)] private int vampireSafetyConcentration = 12;
		private float VampireSafetyConcentration => vampireSafetyConcentration / 100.0f;

		[SerializeReference, SelectImplementation(typeof(IHitRequirement))]
		private List<IHitRequirement> hitRequirements;

		[SerializeField] private AddressableAudioSource useSound = null;

		private bool isEnabled = true;

		bool ICustomMeleeBehaviour.IsEnabled
		{
			get => isEnabled;
			set => isEnabled = value;
		}

		List<IHitRequirement> ICustomMeleeBehaviour.Requirements
		{
			get => hitRequirements;
			set => hitRequirements = value;
		}

		public WeaponNetworkActions.MeleeStats CustomMeleeBehaviour(GameObject attacker,
			GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats)
		{
			return stats;
		}

		public void OnHitBehaviour(GameObject attacker, GameObject target, BodyPartType damageZone,
			WeaponNetworkActions.MeleeStats stats)
		{
			if (attacker.TryGetComponent<PlayerScript>(out var attackerPlayerScript) == false) return;
			if (target.TryGetComponent<PlayerScript>(out var victimPlayerScript) == false) return;

			ReagentPoolSystem victimReagentPool = victimPlayerScript.playerHealth?.reagentPoolSystem;
			ReagentPoolSystem attackerReagentPool = attackerPlayerScript.playerHealth?.reagentPoolSystem;
			if (victimReagentPool == null || attackerReagentPool == null) return;

			TeamData currentTeam = victimPlayerScript.Mind?.AntagPublic?.CurTeam?.Data;
			if (currentTeam == vampireTeam && victimReagentPool.BloodPool[CommonSicknesses.Instance.VampirismReagent] < (VampireSafetyConcentration * victimReagentPool.NormalBlood)) return;
			if (victimReagentPool.BloodPool.Total < victimReagentPool.NormalBlood * SanguineThresholdFraction) return;

			ReagentMix extractedBlood = victimReagentPool.BloodPool.Take(victimReagentPool.NormalBlood * SanguineAmountFraction);
			float gainedBlood = extractedBlood.Total * SanguineEfficiencyFraction;

			attackerReagentPool.BloodPool.Add(CommonSicknesses.Instance.VampirismReagent, gainedBlood);
			Chat.AddExamineMsgFromServer(attacker, $"Syphoned {gainedBlood} corruption from target");
			if(useSound != null) SoundManager.PlayNetworkedAtPos(useSound, target.transform.position, sourceObj: target.gameObject);
		}
		public void OnBlockBehaviour(GameObject attacker, GameObject target, BodyPartType damageZone, WeaponNetworkActions.MeleeStats stats) { }
	}
}