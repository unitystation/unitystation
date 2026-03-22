using System.Collections.Generic;
using Logs;
using NaughtyAttributes;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Addressables.Types;
using US13.Core.Lifecycle;
using US13.HealthV2.Living;
using US13.Managers;
using US13.ScriptableObjects.RP;
using Util;

namespace US13.Items.Implants.Organs.Vomit
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
			var vomitReagent = vomitSplat.GameObject.GetCachedComponent<ReagentContainer>(includeDisabled: false);
			if (vomitReagent == null)
			{
				Loggy.Error($"Vomit prefab does not have a reagent container!!!");
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

		public bool WillDryHeave()
		{
			//If the entity can never dry heave, or the stomach isn't near or at empty, we are vomiting instead
			if (dryHeavingEmote == null || stomach.StomachContents.IsEmpty == false ||
				stomach.StomachContents.CurrentReagentMix.Total > 0.09f) return false;

			// Simulate the reflex where it sometimes takes a few dry heaves before they stop
			if (Random.value > 0.5f)
				dryHeavingEmote.Do(RelatedPart.HealthMaster.gameObject); //Get the last of it out

			return true;
		}
	}
}