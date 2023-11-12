using HealthV2.Living.PolymorphicSystems;
using HealthV2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Util;
using Chemistry;
using Mirror;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Systems/ChangelingAbilities/Regenerate")]
	public class Regenerate: ChangelingBaseAbility
	{
		[Server]
		public override bool UseAbilityServer(ChangelingMain changeling, Vector3 _)
		{
			if (CustomNetworkManager.IsServer == false) return false;
			changeling.UseAbility(this);
			changeling.StartCoroutine(RegenerationProcess(changeling));
			return true;
		}

		private IEnumerator RegenerationProcess(ChangelingMain changeling)
		{
			for (int i = 1; i < 4; i++)
			{
				foreach (var part in changeling.ChangelingMind.Body.playerHealth.BodyPartList)
				{
					if (part.Health == part.MaxHealth)
						continue;
					if (part.name.ToLower().Contains("bones") || part.Health < part.MaxHealth / 3)
					{
						Chat.AddCombatMsgToChat(changeling.gameObject,
						$"<color=red>Your bones makes cracking noise.</color>",
						$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.visibleName}'s bones make cracking noises.</color>");
					}
					part.HealDamage(null, part.MaxHealth / 4, DamageType.Brute);
					part.HealDamage(null, part.MaxHealth / 12, DamageType.Tox);
					part.HealDamage(null, part.MaxHealth / 24, DamageType.Burn);
					part.HealDamage(null, part.MaxHealth / 24, DamageType.Radiation);
					part.HealDamage(null, part.MaxHealth / 12, DamageType.Oxy);
				}
				yield return WaitFor.SecondsRealtime(1f);
			}
			changeling.ChangelingMind.Body.playerHealth.RestartHeart();

			var bloodSystem = changeling.ChangelingMind.Body.playerHealth.reagentPoolSystem;

			var characterSheet = changeling.ChangelingMind.Body.characterSettings;
			var bodyParts = characterSheet.GetRaceSoNoValidation();

			ColorUtility.TryParseHtmlString(characterSheet.SkinTone, out var bodyColor);
			RespawnMissedBodyparts(changeling, bodyColor, changeling.ChangelingMind.Body.playerHealth, bodyParts);

			RegenerationOfBodyOrgans(changeling);

			UpdateBloodPool(bloodSystem, changeling);
		}

		private void SpawnPart(GameObject toSpawn, Color bodyColor, PlayerHealthV2 containedIn)
		{
			var spawnedBodypart = Spawn.ServerPrefab(toSpawn).GameObject.GetComponent<HealthV2.BodyPart>();
			spawnedBodypart.ChangeBodyPartColor(bodyColor);

			Inventory.ServerAdd(spawnedBodypart.gameObject,
				containedIn.BodyPartStorage.GetBestSlotFor(spawnedBodypart.gameObject));
		}

		private void RespawnMissedBodyparts(ChangelingMain changeling, Color bodyColor, PlayerHealthV2 containedIn, PlayerHealthData bodyParts)
		{
			bool noLeftArm = true;
			bool noRightArm = true;
			bool noLeftLeg = true;
			bool noRightLeg = true;
			foreach (var RelatedPart in changeling.ChangelingMind.Body.playerHealth.SurfaceBodyParts)
			{
				switch (RelatedPart.BodyPartType)
				{
					case BodyPartType.LeftArm:
						noLeftArm = false;
						break;
					case BodyPartType.RightArm:
						noRightArm = false;
						break;
					case BodyPartType.LeftLeg:
						noLeftLeg = false;
						break;
					case BodyPartType.RightLeg:
						noRightLeg = false;
						break;
				}
			}

			if (noLeftArm)
			{
				SpawnPart(bodyParts.Base.ArmLeft.Elements[0], bodyColor, containedIn);
			}
			if (noRightArm)
			{
				SpawnPart(bodyParts.Base.ArmRight.Elements[0], bodyColor, containedIn);
			}
			if (noLeftLeg)
			{
				SpawnPart(bodyParts.Base.LegLeft.Elements[0], bodyColor, containedIn);
			}
			if (noRightLeg)
			{
				SpawnPart(bodyParts.Base.LegRight.Elements[0], bodyColor, containedIn);
			}

			if (noLeftArm || noLeftLeg || noRightArm || noRightLeg)
			{
				Chat.AddCombatMsgToChat(changeling.gameObject,
				$"<color=red>Your body starts to reconstruct and grow missing parts.</color>",
				$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.visibleName}'s body starts to grow missing parts, reconstructing it self in the process.</color>");
			}
		}

		private void RegenerationOfBodyOrgans(ChangelingMain changeling)
		{
			foreach (var RelatedPart in changeling.ChangelingMind.Body.playerHealth.SurfaceBodyParts)
			{
				var usedOrgansInSpawnedPart = new List<GameObject>();

				foreach (var itemSlot in RelatedPart.OrganStorage.GetItemSlots())
				{
					if (itemSlot.Item != null)
					{
						foreach (var organ in usedOrgansInSpawnedPart)
						{
							if (organ.GetComponent<PrefabTracker>().ForeverID == itemSlot.Item.gameObject.GetComponent<PrefabTracker>().ForeverID)
							{
								usedOrgansInSpawnedPart.Remove(organ);
								break;
							}
						}
					}
				}

				foreach (var toSpawn in usedOrgansInSpawnedPart)
				{
					var bodyPartObject = Spawn.ServerPrefab(toSpawn, spawnManualContents: true).GameObject;
					RelatedPart.OrganStorage.ServerTryAdd(bodyPartObject);
				}
			}
		}

		private void UpdateBloodPool(ReagentPoolSystem bloodSystem, ChangelingMain changeling)
		{
			// just saving food yum yum
			SerializableDictionary<Reagent, float> blood = new(bloodSystem.BloodPool.reagents);

			bloodSystem.BloodPool.RemoveVolume(bloodSystem.BloodPool.Total);
			bloodSystem.AddFreshBlood(bloodSystem.BloodPool, bloodSystem.StartingBlood);

			var foodComps = changeling.ChangelingMind.Body.playerHealth.GetSystem<HungerSystem>().NutrimentToConsume;

			foreach (var x in foodComps)
			{
				if (blood.ContainsKey(x.Key))
					bloodSystem.BloodPool.Add(x.Key, blood[x.Key]);
				else
					bloodSystem.BloodPool.Add(x.Key, 25);
			}
		}

	}
}