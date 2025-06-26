using Chemistry;
using HealthV2.Sickness;
using UnityEngine;

namespace ScriptableObjects.RP
{
	/// <summary>
	/// Controls the spreading of diseases through coughing / sneezing
	/// </summary>
	[CreateAssetMenu(fileName = "newDiseaseSpreadingEmote", menuName = "ScriptableObjects/RP/Emotes/DiseaseSpreadEmote")]
	public class SpreadDiseaseEmote : GenderedEmote
	{
		[SerializeField] private float maxSpreadRange = 3;
		[SerializeField] private float maxSpreadAngle = 45;
		[SerializeField] private float maxReagentCount = 0.25f;
		[SerializeField] private ItemTrait blockingTrait;

		private const float MinSpreadRange = 0.2f;

		public override void Do(GameObject actor)
		{
			base.Do(actor);
			if (wasEmoteSuccessful == false) return;

			if(actor.TryGetComponent<PlayerScript>(out var actorPlayerScript) == false) return;
			if(actor.TryGetComponent<Rotatable>(out var rotatable) == false) return;

			Vector3 actorPos = actor.AssumedWorldPosServer();
			ReagentMix coughMix = GetDiseaseFromBlood(actorPlayerScript);

			//Coughing and/or sneezing will be common. Iterating through player list should be quicker than a physics overlap
			foreach (var player in PlayerList.Instance.InGamePlayers)
			{
				AttemptToAfflictPlayer(actorPos, rotatable.CurrentDirection, player, coughMix);
			}
		}

		private void AttemptToAfflictPlayer(Vector3 actorPosition, OrientationEnum actorRotation, PlayerInfo victim, ReagentMix coughMix)
		{
			if(coughMix.Total < 0.1f) return;
			if (victim.Mind.isGhosting) return;

			PlayerScript player = victim.Mind.CurrentPlayScript;

			Vector3 victimPos = player.GameObject.AssumedWorldPosServer();
			float victimDistance = Vector3.Distance(actorPosition, victimPos);
			if (victimDistance < MinSpreadRange || victimDistance > maxSpreadRange) return;

			victimPos -= actorPosition; //Victim world pos to Victim relative pos to the actor

			if (Mathf.Abs(Mathf.Atan2(victimPos.y, victimPos.x) * Mathf.Rad2Deg -
			              ((int)actorRotation * 90)) //Is the victim within a cone of effect from the actor
			    > maxSpreadAngle) return;

			if (HasBlockingItem(player)) return;

			float amountToAfflict = maxReagentCount * (1 - (victimDistance / maxSpreadRange)); //Players further away will get less reagent from the emote
			victim.Script.playerHealth.reagentPoolSystem.BloodPool.Add(coughMix.Take(amountToAfflict));
		}

		private ReagentMix GetDiseaseFromBlood(PlayerScript actor)
		{
			ReagentMix mix = new ReagentMix(CommonReagents.Instance.Water, 5);
			ReagentMix sample = actor.playerHealth.reagentPoolSystem.BloodPool.Take(5); //Take blood sample

			foreach (Reagent reagent in sample.reagentKeys) //Find all the diseases in the blood
			{
				if (CureManager.InitialisedSicknesses.ContainsKey(reagent)) //Add those diseases to the cough/sneeze mix
				{
					float amount = sample.reagents[reagent];
					mix.Add(reagent, sample.Remove(reagent, amount * 0.05f) + 0.05f);
				}
			}
			actor.playerHealth.reagentPoolSystem.BloodPool.Add(sample);
			return mix;
		}

		private bool HasBlockingItem(PlayerScript playerToCheck)
		{
			if (playerToCheck.Equipment.IsInternalsEnabled) return true;
			if (playerToCheck.Equipment.ItemStorage.ServerContents.TryGetValue(NamedSlot.head, out var headSlots) ==
			    false) return true;
			if (playerToCheck.Equipment.ItemStorage.ServerContents.TryGetValue(NamedSlot.mask, out var maskSlots) ==
			    false) return true;
			//If the player has no head or mask slots, they have no mouth i.e cannot catch oral diseases

			foreach (var slot in headSlots)
			{
				if(slot.ItemAttributes.HasTrait(blockingTrait)) return true;
			}
			foreach (var slot in maskSlots)
			{
				if(slot.ItemAttributes.HasTrait(blockingTrait)) return true;
			}

			return false; //Has a mouth, but no internals, bio blocking mask & or helmet
		}
	}
}
