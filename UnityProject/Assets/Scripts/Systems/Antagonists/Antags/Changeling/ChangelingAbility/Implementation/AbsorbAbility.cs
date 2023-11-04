using HealthV2;
using Items.Implants.Organs;
using System;
using System.Collections;
using System.Security.Policy;
using Logs;
using UnityEngine;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Systems/ChangelingAbilities/AbsorbAbility")]
	public class AbsorbAbility : Sting
	{

		public override bool UseAbilityClient(ChangelingMain changeling)
		{
			return true;
		}


		public override bool UseAbilityServer(ChangelingMain changeling, Vector3 clickPosition)
		{
			clickPosition = new Vector3(clickPosition.x, clickPosition.y, 0);
			var rounded = Vector3Int.RoundToInt(clickPosition);
			var target = GetPlayerOnClick(changeling, clickPosition, rounded);
			if (target == null || target == changeling.ChangelingMind.Body)
			{
				return false;
			}
			if (target.IsDeadOrGhost)
			{
				Chat.AddExamineMsg(changeling.ChangelingMind.gameObject, "<color=red>You cannot absorb a dead body!</color>");
				return false;
			}

			Chat.AddCombatMsgToChat(changeling.gameObject,
			$"<color=red>You start absorbing of {target.visibleName}</color>",
			$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.visibleName} starts absorbing of {target.visibleName}</color>");
			Chat.AddExamineMsg(target.gameObject, "<color=red>Your body is being absorbed!</color>");
			var action = StandardProgressAction.Create(stingProgressBar,
				() =>
				{
					PerfomAbilityAfter(changeling, target);
				},
				(_) =>
				{
					Chat.AddExamineMsg(target.gameObject, "<color=red>Your body is no longer being absorbed!</color>");
				});


			action.ServerStartProgress(changeling.ChangelingMind.Body.AssumedWorldPos, StingTime, changeling.ChangelingMind.Body.gameObject);

			return true;
		}

		protected override void PerfomAbilityAfter(ChangelingMain changeling, PlayerScript target)
		{
			Chat.AddCombatMsgToChat(changeling.gameObject,
			$"<color=red>You finished absorbing of {target.visibleName}</color>",
			$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.visibleName} finished absorbing of {target.visibleName}</color>");

			bool targetIsChangeling = false;
			try
			{
				targetIsChangeling = target.PlayerInfo.Mind.IsOfAntag<Changeling>();
			}
			catch (Exception ex)
			{
				Loggy.LogError($"[ChangelingAbility/AfterAbsorbSting] Failed to find target PlayerInfo or mind of {target.visibleName} {ex}", Category.Changeling);
			}

			if (targetIsChangeling)
			{
				changeling.AbsorbDna(target, target.Changeling);
			}
			else
			{
				var targetDNA = new ChangelingDna();
				targetDNA.FormDna(target);
				changeling.AbsorbDna(targetDNA, target);
			}

			// fatal damage
			target.Mind.Body.playerHealth.ApplyDamageAll(null, 999, AttackType.Internal, DamageType.Oxy);
			target.Mind.Body.playerHealth.ApplyDamageAll(null, 999, AttackType.Internal, DamageType.Brute);
			target.Mind.Body.playerHealth.ApplyDamageAll(null, 999, AttackType.Internal, DamageType.Clone);
			target.Mind.Body.playerHealth.reagentPoolSystem.BloodPool.RemoveVolume(target.Mind.Body.playerHealth.reagentPoolSystem.BloodPool.Total);
			var breakLoop = false;
			foreach (var bodyPart in target.Mind.Body.playerHealth.BodyPartList)
			{
				foreach (BodyPartFunctionality organ in bodyPart.OrganList)
				{
					if (organ is Brain brain)
					{
						brain.SyncDrunkenness(brain.DrunkAmount, 0);
						_ = Despawn.ServerSingle(organ.gameObject);
						breakLoop = true;
						break;
					}
				}
				if (breakLoop == true)
				{
					break;
				}
			}
		}
	}
}