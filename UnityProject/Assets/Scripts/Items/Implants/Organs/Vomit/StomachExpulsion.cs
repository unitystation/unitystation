using System.Collections.Generic;
using AddressableReferences;
using Chemistry.Components;
using HealthV2;
using Logs;
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
		[SerializeField] private float divisbleVomitAmount = 4;
		[SerializeField] private AddressableAudioSource vomitSound;
		private readonly List<IVomitExtension> vomitLogicExtensions = new List<IVomitExtension>();

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
				LivingHealthMaster.gameObject.AssumedWorldPosServer());
			var vomitReagent = vomitSplat.GameObject.GetComponent<ReagentContainer>();
			if (vomitReagent == null)
			{
				Loggy.LogError($"Vomit prefab does not have a reagent container!!!");
				return;
			}
			stomach.StomachContents.TransferTo(amountToVomit, vomitReagent);
			foreach (var logic in vomitLogicExtensions)
			{
				logic?.OnVomit(amountToVomit, LivingHealthMaster, stomach);
			}
			if(vomitSound != null) _ = SoundManager.PlayNetworkedAtPosAsync(
				vomitSound, LivingHealthMaster.gameObject.AssumedWorldPosServer());
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