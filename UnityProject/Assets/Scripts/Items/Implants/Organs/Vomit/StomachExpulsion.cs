using System.Collections.Generic;
using Castle.Core.Internal;
using Chemistry.Components;
using HealthV2;
using NaughtyAttributes;
using ScriptableObjects.RP;
using UnityEngine;

namespace Items.Implants.Organs.Vomit
{
	[RequireComponent(typeof(Stomach))]
	public class StomachExpulsion : BodyPartFunctionality
	{
		[SerializeField] private Stomach stomach;
		[SerializeField] private EmoteSO dryHeavingEmote;
		[SerializeField] private GameObject vomitReagentPrefab;
		[SerializeField] private float divisbleVomitAmount = 6;
		private List<IVomitExtension> vomitLogicExtensions = new List<IVomitExtension>();

		public override void Awake()
		{
			base.Awake();
			stomach ??= GetComponent<Stomach>();
			vomitLogicExtensions.AddRange(GetComponentsInChildren<IVomitExtension>());
		}


		[Button()]
		public void Vomit()
		{
			if (WillDryHeave()) return;
			var amountToVomit = Random.Range(0.1f, stomach.StomachContents.CurrentReagentMix.Total / divisbleVomitAmount);
			var vomitSplat = Spawn.ServerPrefab(
				vomitReagentPrefab,
				livingHealthMaster.gameObject.AssumedWorldPosServer());
			var vomitReagent = vomitSplat.GameObject.GetComponent<ReagentContainer>();
			if (vomitReagent == null)
			{
				Logger.LogError($"Vomit prefab does not have a reagent container!!!");
				return;
			}
			stomach.StomachContents.TransferTo(amountToVomit, vomitReagent);
			//add sound here
			//add chat message here
			foreach (var logic in vomitLogicExtensions)
			{
				logic?.OnVomit(amountToVomit, livingHealthMaster);
			}
		}

		private bool WillDryHeave()
		{
			if (dryHeavingEmote == null) return false;
			if (stomach.StomachContents.IsEmpty == false || stomach.StomachContents.CurrentReagentMix.Total > 0.09f) return false;
			dryHeavingEmote.Do(RelatedPart.HealthMaster.gameObject);
			return true;
		}
	}
}