using Chemistry;
using Mirror;
using System.Collections;
using UnityEngine;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Systems/ChangelingAbilities/HallucinationSting")]
	public class HallucinationSting: Sting
	{
		[SerializeField] private Reagent reagent;
		public Reagent Reagent => reagent;

		[SerializeField] private float reagentCount = 25;
		public float ReagentCount => reagentCount;

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
			if (target.IsDeadOrGhost)
			{
				Chat.AddExamineMsg(changeling.ChangelingMind.gameObject, "<color=red>You cannot sting a dead body!</color>");
				return false;
			}

			changeling.UseAbility(this);
			var action = StandardProgressAction.Create(stingProgressBar,
				() => PerfomAbilityAfter(changeling, target));
			action.ServerStartProgress(changeling.ChangelingMind.Body.AssumedWorldPos, StingTime, changeling.ChangelingMind.Body.gameObject);

			return true;
		}

		protected override void PerfomAbilityAfter(ChangelingMain changeling, PlayerScript target)
		{
			var randomTimeAfter = UnityEngine.Random.Range(30, 60f);
			var targetDNA = new ChangelingDna();

			targetDNA.FormDna(target);

			changeling.AddDna(targetDNA);

			changeling.StartCoroutine(ReagentAdding(randomTimeAfter, Reagent, ReagentCount, target));
		}

		private IEnumerator ReagentAdding(float time, Reagent reagent, float reagentCount, PlayerScript target)
		{
			yield return WaitFor.SecondsRealtime(time);

			if (target.Mind.IsGhosting == false && target.playerHealth.IsDead == false)
				target.playerHealth.reagentPoolSystem.BloodPool.Add(reagent, reagentCount);
		}
	}
}