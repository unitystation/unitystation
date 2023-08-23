using HealthV2;
using Items.Implants.Organs;
using Mirror;
using System;
using System.Collections;
using UnityEngine;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Systems/ChangelingAbilities/AbsorbAbility")]
	public class AbsorbSting: Sting
	{
		private const float MAX_REMOVING_WHILE_ABSORBING_BODY = 70f;

		public override bool UseAbilityClient(ChangelingMain changeling)
		{
			return false;
		}

		[Server]
		public override bool UseAbilityServer(ChangelingMain changeling, Vector3 clickPosition)
		{
			if (CustomNetworkManager.IsServer == false) return false;
			clickPosition = new Vector3(clickPosition.x, clickPosition.y, 0);
			var rounded = Vector3Int.RoundToInt(clickPosition);
			var target = GetPlayerOnClick(changeling, clickPosition, rounded);
			if (target == null || target == changeling.ChangelingMind.Body)
			{
				return false;
			}

			changeling.UseAbility(this);
			Chat.AddCombatMsgToChat(changeling.gameObject,
			$"<color=red>You start absorbing of {target.visibleName}</color>",
			$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.visibleName} starts absorbing of {target.visibleName}</color>");
			var cor = changeling.StartCoroutine(AbsorbingProgress(stingTime / 10f, target));
			var action = StandardProgressAction.Create(stingProgressBar,
				() =>
				{
					changeling.StopCoroutine(cor);
					PerfomAbilityAfter(changeling, target);
				},
				(_) =>
				{
					changeling.StopCoroutine(cor);
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
				Logger.LogError($"[ChangelingAbility/AfterAbsorbSting] Failed to find target PlayerInfo or mind of {target.visibleName} {ex}", Category.Changeling);
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

		private IEnumerator AbsorbingProgress(float pauseTime, PlayerScript target)
		{
			var absorbing = true;
			var toRemove = target.Mind.Body.playerHealth.reagentPoolSystem.BloodPool.Total / 10f;
			while (absorbing)
			{
				yield return WaitFor.SecondsRealtime(pauseTime);
				Chat.AddExamineMsg(target.gameObject, "<color=red>Your body is absorbing!</color>");

				if (target.playerHealth.reagentPoolSystem.BloodPool.Total > toRemove && target.playerHealth.reagentPoolSystem.BloodPool.Total > MAX_REMOVING_WHILE_ABSORBING_BODY)
					target.playerHealth.reagentPoolSystem.BloodPool.RemoveVolume(toRemove);
			}
		}
	}
}