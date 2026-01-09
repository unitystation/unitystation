using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using NUnit.Framework;
using ScriptableObjects.Atmospherics;
using UnityEngine;

namespace Chemistry.Effects
{
	[CreateAssetMenu(fileName = "PlagueBomb",
		menuName = "ScriptableObjects/Chemistry/Effects/PlagueBomb")]
	public class PlagueBomb : Chemistry.Effect
	{
		[SerializeField] private ItemTrait blockingTrait = null;

		[SerializeField] private Reagent reagentToSpread = null;
		[SerializeField] private float spreadRange = 1;
		[SerializeField] private float spreadCount = 1;

		[SerializeField] private GasSO gasToEmit = null;
		[SerializeField] private float amountOfGas = 1;

		[SerializeField] ContactFilter2D filter = new ContactFilter2D();
		RaycastHit2D[] hits = new RaycastHit2D[1];


		public override void Apply(MonoBehaviour sender, ReagentMix ReagentMix, Vector3 WorldPosition, float amount)
		{
			if (sender == null) return;
			if (sender is MetabolismComponent metabolismComponent == false) return;
			if (metabolismComponent.RelatedPart.HealthMaster == false) return;

			LivingHealthMasterBase healthMaster = metabolismComponent.RelatedPart.HealthMaster;
			Vector3 actorPos = healthMaster.gameObject.AssumedWorldPosServer();

			//This might trigger multiple times otherwise!
			if (healthMaster.IsDead) return;

			//Afflict nearby players with pathogen
			if (reagentToSpread != null && spreadRange > 0 && spreadCount > 0.01f)
			{
				foreach (var player in PlayerList.Instance.InGamePlayers)
				{
					AttemptToAfflictPlayer(actorPos, player);
				}
			}

			//Add gas to area (Typically Miasma)
			if (gasToEmit != null && amountOfGas > 0)
			{
				MetaDataNode node = MatrixManager.GetMetaDataAt(actorPos.CutToInt());
				node.GasMixLocal.AddGasWithTemperature(gasToEmit, amountOfGas, node.GasMixLocal.Temperature);
			}

			//Gib the victim
			healthMaster.OnGib();
		}

		private void AttemptToAfflictPlayer(Vector3 actorPosition, PlayerInfo victim)
		{
			if (victim.Mind.isGhosting) return;
			PlayerScript player = victim.Mind.CurrentPlayScript;

			Vector3 victimPos = player.GameObject.AssumedWorldPosServer();
			float victimDistance = Vector3.Distance(actorPosition, victimPos);
			if (victimDistance > spreadRange) return;

			victimPos -= actorPosition;
			int hitCount = Physics2D.Raycast(actorPosition, victimPos, filter, hits, victimDistance);
			if (hitCount > 0) return;

			if (HasBlockingSuit(player) == true) return;

			float amountToAfflict = spreadCount * (1 - (victimDistance / spreadRange));
			victim.Script.playerHealth.reagentPoolSystem.BloodPool.Add(reagentToSpread, amountToAfflict);
		}

		private bool HasBlockingSuit(PlayerScript playerToCheck)
		{
			if(CheckHeadForProtection(playerToCheck) == false) return false;
			if(CheckChestForProtection(playerToCheck) == false) return false;

			return true;
		}


		private bool CheckHeadForProtection(PlayerScript playerToCheck)
		{
			if (playerToCheck.Equipment.IsInternalsEnabled) return true;

			if (playerToCheck.Equipment.ItemStorage.ServerContents.TryGetValue(NamedSlot.head, out var headSlots) ==
			    false) return true;
			if (playerToCheck.Equipment.ItemStorage.ServerContents.TryGetValue(NamedSlot.mask, out var maskSlots) ==
			    false) return true;

			foreach (var slot in headSlots)
			{
				if(slot.ItemAttributes == null) continue;
				if (slot.ItemAttributes.HasTrait(blockingTrait)) return true;
			}
			foreach (var slot in maskSlots)
			{
				if(slot.ItemAttributes == null) continue;
				if (slot.ItemAttributes.HasTrait(blockingTrait)) return true;
			}

			return false;
		}

		private bool CheckChestForProtection(PlayerScript playerToCheck)
		{
			if (playerToCheck.Equipment.ItemStorage.ServerContents.TryGetValue(NamedSlot.outerwear, out var outerWearSlots) ==
			    false) return true;
			if (playerToCheck.Equipment.ItemStorage.ServerContents.TryGetValue(NamedSlot.uniform, out var uniformSlots) ==
			    false) return true;

			foreach (var slot in outerWearSlots)
			{
				if(slot.ItemAttributes == null) continue;
				if (slot.ItemAttributes.HasTrait(blockingTrait)) return true;
			}
			foreach (var slot in uniformSlots)
			{
				if(slot.ItemAttributes == null) continue;
				if (slot.ItemAttributes.HasTrait(blockingTrait)) return true;
			}

			return false;
		}
	}
}
