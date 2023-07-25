using CameraEffects;
using Chemistry;
using GameModes;
using HealthV2;
using HealthV2.Living.PolymorphicSystems;
using Items.Implants.Organs;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Systems.Character;
using TileManagement;
using UI.Action;
using UI.Core.Action;
using UnityEngine;
using Util;

namespace Changeling
{
	[Serializable]
	[DisallowMultipleComponent]
	public class ChangelingAbility : NetworkBehaviour, IActionGUI
	{
		public ChangelingData ability;

		public ChangelingData AbilityData => ability;

		public ActionData ActionData => ability;
		public float CooldownTime { get; set; }
		[SyncVar]
		private bool isToggled = false;
		public bool IsToggled => isToggled;

		private static readonly StandardProgressActionConfig injectProgressBar =
		new StandardProgressActionConfig(StandardProgressActionType.CPR);

		private bool absorbing = true;

		private const float MAX_REMOVING_WHILE_ABSORBING_BODY = 70f;
		private const float MAX_DISTANCE_TO_TILE = 1.6f;

		//public void SyncIsToggled(bool oldValue, bool value)
		//{
		//	isToggled = value;
		//}

		public virtual void CallActionClient()
		{
			UIAction action = UIActionManager.Instance.DicIActionGUI[this][0];
			//PlayerList.Instance.GetPlayersOnMatrix()
			if (AbilityData.IsLocal && ValidateAbilityClient())// && ValidateAbility(PlayerManager.LocalPlayerScript.PlayerInfo))
			{
					//UseAbilityLocal(PlayerManager.LocalPlayerScript.PlayerInfo.Mind.Body.Changeling, ability);
					UseAbilityLocal(UIManager.Instance.displayControl.hudChangeling.ChangelingMain, ability);
					AfterAbility(PlayerManager.LocalPlayerScript);
					//AbilityData.PerfomAbilityClient();
			}
			else
			{
				PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestChangelingAbilites(AbilityData.Index, action.LastClickPosition);
			}
		}

		public void CallToggleActionClient(bool toggled)
		{
			if (ActionData.IsAimable)
				return;
			isToggled = toggled;
			UIAction action = UIActionManager.Instance.DicIActionGUI[this][0];
			if (AbilityData.IsLocal && ValidateAbilityClient()) //  && ValidateAbilityClient(PlayerManager.LocalPlayerScript.PlayerInfo)
			{
				UseAbilityToggle(UIManager.Instance.displayControl.hudChangeling.ChangelingMain, ability, toggled);
			}
			else
			{
				PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestChangelingAbilitesToggle(AbilityData.Index, action.LastClickPosition, toggled);
			}
		}

		public void CallActionServer(PlayerInfo SentByPlayer, Vector3 clickPosition)
		{
			if (ValidateAbility(SentByPlayer) &&
				CastAbilityServer(SentByPlayer, clickPosition))
			{
				AfterAbility(SentByPlayer);
			}
		}

		public void CallActionToggleServer(PlayerInfo SentByPlayer, Vector3 clickPosition, bool toggle)
		{
			var validateAbility = ValidateAbility(SentByPlayer);
			if (validateAbility &&
				CastAbilityToggleServer(SentByPlayer, clickPosition, toggle))
			{
				isToggled = toggle;
			} else if (!validateAbility)
			{
				//Setting ability icon back
				if (isToggled)
				{
					UIActionManager.SetServerSpriteSO(this, ActionData.Sprites[1]);
				}
				else
				{
					UIActionManager.SetServerSpriteSO(this, ActionData.Sprites[0]);
				}
			}
		}

		public void ForceToggleToState(bool toggle)
		{
			isToggled = toggle;

			if (isToggled)
			{
				UIActionManager.SetServerSpriteSO(this, ActionData.Sprites[1]);
			}
			else
			{
				UIActionManager.SetServerSpriteSO(this, ActionData.Sprites[0]);
			}
		}

		public void CallActionServerWithParam(PlayerInfo sentByPlayer, Vector3 clickPosition, string paramString)
		{
			var changeling = ChangelingMain.ChangelingByMindID[sentByPlayer.Mind.netId];
			List<string> param = paramString.Split('\n').ToList();
			if (ValidateAbility(sentByPlayer) &&
				CastAbilityServerWithParam(sentByPlayer, clickPosition, param))
			{
				//AfterAbility(sentByPlayer);
				changeling.UseAbility(this);
			}
		}

		private bool CastAbilityServerWithParam(PlayerInfo sentByPlayer, Vector3 clickPosition, List<string> param)
		{
			var changeling = ChangelingMain.ChangelingByMindID[sentByPlayer.Mind.netId];
			UseAbilityWithParam(changeling, AbilityData, param);
			return true;
		}

		private bool CastAbilityToggleServer(PlayerInfo sentByPlayer, Vector3 clickPosition, bool toggle)
		{
			var changeling = ChangelingMain.ChangelingByMindID[sentByPlayer.Mind.netId];
			if (toggle)
			{
				if (AbilityData.CooldownWhenToggled)
				{
					AfterAbility(sentByPlayer);
					if (AbilityData.DrawCostWhenToggledOn)
						changeling.UseAbility(this);
				}
			}
			else
			{
				if (AbilityData.DrawCostWhenToggledOff)
					changeling.UseAbility(this);
				AfterAbility(sentByPlayer);
			}
			UseAbilityToggle(changeling, AbilityData, toggle);
			return true;
		}

		private bool CastAbilityServer(PlayerInfo sentByPlayer, Vector3 clickPosition)
		{
			var changeling = ChangelingMain.ChangelingByMindID[sentByPlayer.Mind.netId];//sentByPlayer.Mind.Body.Changeling;
			if (!AbilityData.IsAimable)
				changeling.UseAbility(this);
			//ability.PerfomAbility(changeling, clickPosition);
			UseAbility(changeling, ability, clickPosition);
			return true;
		}

		private PlayerScript GetPlayerOnClick(ChangelingMain changeling, Vector3 clickPosition, Vector3 rounded)
		{
			MatrixInfo matrixinfo = MatrixManager.AtPoint(rounded, true);
			clickPosition += new Vector3(-0.5f, -0.5f); // shifting point for geting player tile instead of shifted
			var tilePosition = matrixinfo.MetaTileMap.Layers[LayerType.Floors].Tilemap.WorldToCell(clickPosition);
			matrixinfo = MatrixManager.AtPoint(tilePosition, true);

			var localPosInt = clickPosition.ToLocal(matrixinfo.Matrix);
			//if (!changeling.ChangelingMind.Body.IsPositionReachable(tilePosition, true, maxDistanceToTile1))
			//{
			//	return null;
			//}

			PlayerScript target = null;
			foreach (PlayerScript integrity in matrixinfo.Matrix.Get<PlayerScript>(Vector3Int.CeilToInt(localPosInt), true))
			{
				// to be sure that player don`t morph into AI or something like that
				if (integrity.PlayerType != PlayerTypes.Normal)
					continue;
				target = integrity;
				break;
			}
			if (target == null)
				return null;

			if (Vector3.Distance(changeling.ChangelingMind.Body.GameObject.AssumedWorldPosServer(), target.Mind.Body.GameObject.AssumedWorldPosServer()) > MAX_DISTANCE_TO_TILE)
			{
				return null;
			}
			return target;
		}

		private bool UseAbility(ChangelingMain changeling, ChangelingData data, Vector3 clickPosition)
		{
			switch (data.abilityType)
			{
				case ChangelingAbilityType.Sting:
					clickPosition = new Vector3(clickPosition.x, clickPosition.y, 0);
					var rounded = Vector3Int.RoundToInt(clickPosition);
					var target = GetPlayerOnClick(changeling, clickPosition, rounded);
					if (target == null || target == changeling.ChangelingMind.Body)
					{
						return false;
					}
					changeling.UseAbility(this);
					switch (data.stingType)
					{
						case StingType.ExtractDNASting:
							Chat.AddCombatMsgToChat(changeling.gameObject,
							$"<color=red>You start stings of {target.playerName}</color>",
							$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.playerName} starts stings of {target.playerName}</color>");

							var action = StandardProgressAction.Create(injectProgressBar,
								() => PerfomAbilityAfter(changeling, data, clickPosition, target));
							action.ServerStartProgress(changeling.ChangelingMind.Body.AssumedWorldPos, AbilityData.StingTime, changeling.ChangelingMind.Body.gameObject);
							return true;
						case StingType.Absorb:

							Chat.AddCombatMsgToChat(changeling.gameObject,
							$"<color=red>You start absorbing of {target.playerName}</color>",
							$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.playerName} starts absorbing of {target.playerName}</color>");
							var cor = StartCoroutine(AbsorbingProgress(ability.StingTime / 10f, target));
							action = StandardProgressAction.Create(injectProgressBar,
								() =>
								{
									StopCoroutine(cor);
									PerfomAbilityAfter(changeling, data, clickPosition, target);
								},
								(interruptionType) =>
								{
									StopCoroutine(cor);
								});


							action.ServerStartProgress(changeling.ChangelingMind.Body.AssumedWorldPos, AbilityData.StingTime, changeling.ChangelingMind.Body.gameObject);
							return true;
						case StingType.HallucinationSting:
							action = StandardProgressAction.Create(injectProgressBar,
								() => PerfomAbilityAfter(changeling, data, clickPosition, target));
							action.ServerStartProgress(changeling.ChangelingMind.Body.AssumedWorldPos, AbilityData.StingTime, changeling.ChangelingMind.Body.gameObject);
							return true;
					}
					break;
				case ChangelingAbilityType.Heal:
					switch (data.healType)
					{
						case ChangelingHealType.Regenerate:
							StartCoroutine(RegenerationProcess(changeling));
							return true;
						case ChangelingHealType.RevivingStasis:
							return true;
					}
					break;
				case ChangelingAbilityType.Transform:
					break;
				case ChangelingAbilityType.Misc:
					break;
			}
			return false;
		}

		public bool PerfomAbilityAfter(ChangelingMain changeling, ChangelingData data, Vector3 clickPos, PlayerScript target)
		{
			switch (data.abilityType)
			{
				case ChangelingAbilityType.Sting:
					switch (data.stingType)
					{
						case StingType.ExtractDNASting:
							var targetDNA = new ChangelingDNA();// Spawn.ServerPrefab(ChangelingAbilityList.Instance.DNAPrefab).GameObject;
							//var spellComponent = spellObject.GetComponent<ChangelingAbility>();
							Chat.AddCombatMsgToChat(changeling.gameObject,
							$"<color=red>You finished sting of {target.playerName}</color>",
							$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.playerName} finished sting of {target.playerName}</color>");

							foreach (var x in changeling.ChangelingDNAs)
							{
								if (x.DnaID == target.Mind.bodyMobID)
								{
									x.UpdateDNA(target);
									return true;
								}
							}
							//var targetDNA = dnaObject.GetComponent<ChangelingDNA>();
							targetDNA.FormDNA(target);

							changeling.AddDNA(targetDNA);
							return true;
						case StingType.Absorb:
							//var spellComponent = spellObject.GetComponent<ChangelingAbility>();
							Chat.AddCombatMsgToChat(changeling.gameObject,
							$"<color=red>You finished absorbing of {target.playerName}</color>",
							$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.playerName} finished absorbing of {target.playerName}</color>");

							try
							{
								if (target.PlayerInfo.Mind.IsOfAntag<Changeling>())
								{
									var targetDNAs = new List<ChangelingDNA>();

									targetDNAs.AddRange(target.Mind.Body.Changeling.ChangelingDNAs);

									//foreach (var x in targetDNAs)
									//{
									//	x.transform.SetParent(changeling.transform);
									//}

									target.Mind.Body.Changeling.RemoveDNA(targetDNAs);
									changeling.AbsorbDNA(targetDNAs, target, target.Changeling);
									return true;
								}
							} catch
							{
								Logger.LogWarning("Can`t get player is antag", Category.Changeling);
							}
							targetDNA = new ChangelingDNA();

							// fatal damage
							target.Mind.Body.playerHealth.ApplyDamageAll(null, 999, AttackType.Internal, DamageType.Oxy);
							target.Mind.Body.playerHealth.ApplyDamageAll(null, 999, AttackType.Internal, DamageType.Brute);
							target.Mind.Body.playerHealth.ApplyDamageAll(null, 999, AttackType.Internal, DamageType.Clone);
							target.Mind.Body.playerHealth.reagentPoolSystem.BloodPool.RemoveVolume(target.Mind.Body.playerHealth.reagentPoolSystem.BloodPool.Total);
							foreach (var bodyPart in target.Mind.Body.playerHealth.BodyPartList)
							{
								foreach (BodyPartFunctionality organ in bodyPart.OrganList)
								{
									if (organ is Brain brain)
									{
										Destroy(brain);
										goto SkipingFor;
									}
								}
							}

							SkipingFor:
							//targetDNA = dnaObject.GetComponent<ChangelingDNA>();
							targetDNA.FormDNA(target);

							changeling.AbsorbDNA(targetDNA, target);
							return true;
						case StingType.HallucinationSting:
							var randomTimeAfter = UnityEngine.Random.Range(30, 60f);
							targetDNA = new ChangelingDNA(); // Instantiate(ChangelingAbilityList.Instance.DNAPrefab, changeling.gameObject.transform);

							//targetDNA = dnaObject.GetComponent<ChangelingDNA>();
							targetDNA.FormDNA(target);

							changeling.AddDNA(targetDNA);

							StartCoroutine(ReagentAdding(randomTimeAfter, AbilityData.Reagent, AbilityData.ReagentCount, target));
							return true;
					}
					break;
			}
			return false;
		}

		private bool UseAbilityLocal(ChangelingMain changeling, ChangelingData data)
		{
			if (!data.IsLocal)
				return false;

			switch (data.abilityType)
			{
				case ChangelingAbilityType.Misc:
					switch (data.miscType)
					{
						case ChangelingMiscType.OpenStore:
							UIManager.Display.hudChangeling.OpenStoreUI();
							return true;
						case ChangelingMiscType.AugmentedEyesight:
							break;
						case ChangelingMiscType.OpenMemories:
							UIManager.Display.hudChangeling.OpenMemoriesUI();
							return true;
					}
					break;
				case ChangelingAbilityType.Transform:
					switch (data.transformType)
					{
						case ChangelingTransformType.TransformMenuOpen:
							UIManager.Display.hudChangeling.OpenTransformUI(changeling, (ChangelingDNA dna) =>
							{
								PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestChangelingAbilitesWithParam(AbilityData.UseAfterChoise.Index, new Vector3(), $"{dna.DnaID}");
							});
							return true;
					}
					break;
			}

			return false;
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
						$"<color=red>Your bones starts to cracking</color>",
						$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.playerName} bones starts to cracking</color>");
						if (AbilityData.CastSound != null)
							SoundManager.PlayNetworkedAtPos(
							AbilityData.CastSound, changeling.ChangelingMind.CurrentPlayScript.WorldPos, sourceObj: changeling.ChangelingMind.Body.GameObject, global: false);
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
			var ContainedIn = changeling.ChangelingMind.Body.playerHealth;

			//Regeneration of body parts
			bool noLeftArm = true, noRightArm = true, noLeftLeg = true, noRightLeg = true;
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
			ColorUtility.TryParseHtmlString(characterSheet.SkinTone, out var bodyColor);

			void SpawnPart(GameObject toSpawn)
			{
				var spawnedBodypart = Spawn.ServerPrefab(toSpawn).GameObject.GetComponent<HealthV2.BodyPart>();
				spawnedBodypart.ChangeBodyPartColor(bodyColor);

				Inventory.ServerAdd(spawnedBodypart.gameObject,
					ContainedIn.BodyPartStorage.GetBestSlotFor(spawnedBodypart.gameObject));
			}

			if (noLeftArm)
			{
				SpawnPart(changeling.ChangelingMind.Body.characterSettings.GetRaceSoNoValidation().Base.ArmLeft.Elements[0]);
			}
			if (noRightArm)
			{
				SpawnPart(changeling.ChangelingMind.Body.characterSettings.GetRaceSoNoValidation().Base.ArmRight.Elements[0]);
			}
			if (noLeftLeg)
			{
				SpawnPart(changeling.ChangelingMind.Body.characterSettings.GetRaceSoNoValidation().Base.LegLeft.Elements[0]);
			}
			if (noRightLeg)
			{
				SpawnPart(changeling.ChangelingMind.Body.characterSettings.GetRaceSoNoValidation().Base.LegRight.Elements[0]);
			}

			if (noLeftArm || noLeftLeg || noRightArm || noRightLeg)
			{
				Chat.AddCombatMsgToChat(changeling.gameObject,
				$"<color=red>Your body starts to grow missed parts and reconstruct.</color>",
				$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.playerName} body starts to grow missed parts and reconstruct.</color>");
			}

			//Regeneration of body organs
			foreach (var RelatedPart in changeling.ChangelingMind.Body.playerHealth.SurfaceBodyParts)
			{
				var bodyPartExample = bodyParts.Base.Head.Elements[0];

				switch (RelatedPart.BodyPartType)
				{
					case BodyPartType.Head:
						bodyPartExample = bodyParts.Base.Head.Elements[0];
						break;
					case BodyPartType.Chest:
						bodyPartExample = bodyParts.Base.Torso.Elements[0];
						break;
					case BodyPartType.LeftArm:
						bodyPartExample = bodyParts.Base.ArmLeft.Elements[0];
						break;
					case BodyPartType.RightArm:
						bodyPartExample = bodyParts.Base.ArmRight.Elements[0];
						break;
					case BodyPartType.LeftLeg:
						bodyPartExample = bodyParts.Base.LegLeft.Elements[0];
						break;
					case BodyPartType.RightLeg:
						bodyPartExample = bodyParts.Base.LegRight.Elements[0];
						break;
				}

				var bodyPartExampleStorage = bodyPartExample.GetComponent<ItemStorage>();
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

			yield break;
		}

		private IEnumerator ReagentAdding(float time, Reagent reagent, float reagentCount, PlayerScript target)
		{
			yield return WaitFor.SecondsRealtime(time);

			target.playerHealth.reagentPoolSystem.BloodPool.Add(reagent, reagentCount);
		}

		private IEnumerator AbsorbingProgress(float pauseTime, PlayerScript target)
		{
			absorbing = true;
			var toRemove = target.Mind.Body.playerHealth.reagentPoolSystem.BloodPool.Total / 10f;
			while (absorbing)
			{
				yield return WaitFor.SecondsRealtime(pauseTime);
				Chat.AddExamineMsg(target.gameObject, "<color=red>Your body is absobing!</color>");

				if (target.playerHealth.reagentPoolSystem.BloodPool.Total > toRemove && target.playerHealth.reagentPoolSystem.BloodPool.Total > MAX_REMOVING_WHILE_ABSORBING_BODY)
					target.playerHealth.reagentPoolSystem.BloodPool.RemoveVolume(toRemove);
			}
		}

		private bool UseAbilityToggle(ChangelingMain changeling, ChangelingData data, bool toggle)
		{
			if (!data.IsToggle)
				return false;
			switch (data.abilityType)
			{
				case ChangelingAbilityType.Sting:
					break;
				case ChangelingAbilityType.Heal:
					switch (data.healType)
					{
						case ChangelingHealType.RevivingStasis:
							if (toggle)
							{
								UIActionManager.SetServerSpriteSO(this, ActionData.Sprites[1]);
								changeling.isFakingDeath = true;

								changeling.ChangelingMind.Body.playerHealth.StopMetabolismAndHeart();
								changeling.ChangelingMind.Body.playerHealth.StopOverralCalculation();
								changeling.ChangelingMind.Body.playerHealth.SetConsciousState(ConsciousState.UNCONSCIOUS);
							} else
							{
								UIActionManager.SetServerSpriteSO(this, ActionData.Sprites[0]);
								changeling.UseAbility(this);
								// healing
								changeling.ChangelingMind.Body.playerHealth.FullyHeal();
								changeling.ChangelingMind.Body.playerHealth.UnstopOverallCalculation();
								changeling.ChangelingMind.Body.playerHealth.UnstopMetabolismAndRestartHeart();
								changeling.isFakingDeath = false;
							}
							return true;
					}
					break;
				case ChangelingAbilityType.Misc:
					switch (data.miscType)
					{
						case ChangelingMiscType.AugmentedEyesight:
							//var lighting = Camera.main.GetComponent<LightingSystem>();
							//if (!lighting)
							//{
							//	Logger.LogWarning("Local Player can't find lighting system on Camera.main", Category.Lighting);
							//}
							//lighting.enabled = !toggle;
							if (Camera.main == null ||
								Camera.main.TryGetComponent<CameraEffectControlScript>(out var effects) == false) return false;

							//foreach (var bodyPart in changeling.ChangelingMind.Body.playerHealth.BodyPartList)
							//{
							//	foreach (BodyPartFunctionality organ in bodyPart.OrganList)
							//	{
							//		if (organ is Eye eye)
							//		{
							//			eye.HasXray = toggle;
							//			//eye.ApplyChangesXrayState(toggle);
							//			goto SkipingFor;
							//		}
							//	}
							//}

							//SkipingFor:

							effects.AdjustPlayerVisibility(
								toggle ? AbilityData.ExpandedNightVisionVisibility : effects.MinimalVisibilityScale,
								toggle ? AbilityData.DefaultvisibilityAnimationSpeed : AbilityData.RevertvisibilityAnimationSpeed);
							effects.ToggleNightVisionEffectState(toggle);
							effects.SetNightVisionMaxLensRadius(true);

							foreach (var x in changeling.AbilitiesNow)
							{
								if (x.AbilityData == AbilityData)
								{
									x.isToggled = toggle;
								}
							}
							return true;
					}
					break;
			}
			return false;
		}

		private bool UseAbilityWithParam(ChangelingMain changeling, ChangelingData data, List<string> param)
		{
			switch (data.abilityType)
			{
				case ChangelingAbilityType.Sting:
					break;
				case ChangelingAbilityType.Heal:
					break;
				case ChangelingAbilityType.Transform:
					switch (data.transformType)
					{
						case ChangelingTransformType.Transform:
							string dnaID = param[0];

							var dna = changeling.GetDNAByID(int.Parse(dnaID));

							//if (dna == changeling.currentDNA)
							//	return false;

							CharacterSheet characterSheet = dna.CharacterSheet;

							PlayerScript body = changeling.ChangelingMind.Body;

							body.visibleName = characterSheet.Name;
							body.playerName = characterSheet.Name;

							body.characterSettings = characterSheet;

							PlayerHealthData raceBodyparts = characterSheet.GetRaceSoNoValidation();

							//ColorUtility.TryParseHtmlString(characterSheet.SkinTone, out var bodyColor);

							List<DNAMutationData> dataForMutations = new List<DNAMutationData>();

							DNAMutationData dataForMutation = new DNAMutationData();

							DNAMutationData.DNAPayload payload = new DNAMutationData.DNAPayload();

							payload.SpeciesMutateTo = raceBodyparts;
							payload.MutateToBodyPart = raceBodyparts.Base.Head.Elements[0];

							dataForMutation.Payload.Add(payload);
							dataForMutation.BodyPartSearchString = "Head";

							dataForMutations.Add(dataForMutation);

							dataForMutation = new DNAMutationData();
							payload = new DNAMutationData.DNAPayload
							{
								SpeciesMutateTo = raceBodyparts,
								MutateToBodyPart = raceBodyparts.Base.Torso.Elements[0]
							};

							dataForMutation.Payload.Add(payload);

							dataForMutation.BodyPartSearchString = "Chest";

							dataForMutations.Add(dataForMutation);

							dataForMutation = new DNAMutationData();
							payload = new DNAMutationData.DNAPayload
							{
								SpeciesMutateTo = raceBodyparts,
								MutateToBodyPart = raceBodyparts.Base.Torso.Elements[0]
							};

							dataForMutation.Payload.Add(payload);

							// adding the same thing but with dif name just because main body is named as tosrso or as chest is some cases
							dataForMutation.BodyPartSearchString = "Torso"; 

							dataForMutations.Add(dataForMutation);

							dataForMutation = new DNAMutationData();
							payload = new DNAMutationData.DNAPayload
							{
								SpeciesMutateTo = raceBodyparts,
								MutateToBodyPart = raceBodyparts.Base.LegRight.Elements[0]
							};

							dataForMutation.Payload.Add(payload);
							dataForMutation.BodyPartSearchString = "RightLeg";

							dataForMutations.Add(dataForMutation);

							dataForMutation = new DNAMutationData();
							payload = new DNAMutationData.DNAPayload
							{
								SpeciesMutateTo = raceBodyparts,
								MutateToBodyPart = raceBodyparts.Base.ArmLeft.Elements[0]
							};

							dataForMutation.Payload.Add(payload);

							dataForMutation.BodyPartSearchString = "LeftArm";

							dataForMutations.Add(dataForMutation);

							dataForMutation = new DNAMutationData();
							payload = new DNAMutationData.DNAPayload
							{
								SpeciesMutateTo = raceBodyparts,
								MutateToBodyPart = raceBodyparts.Base.LegLeft.Elements[0]
							};

							dataForMutation.Payload.Add(payload);
							dataForMutation.BodyPartSearchString = "LeftLeg";

							dataForMutations.Add(dataForMutation);

							dataForMutation = new DNAMutationData();
							payload = new DNAMutationData.DNAPayload
							{
								SpeciesMutateTo = raceBodyparts,
								MutateToBodyPart = raceBodyparts.Base.ArmRight.Elements[0]
							};

							dataForMutation.Payload.Add(payload);

							dataForMutation.BodyPartSearchString = "RightArm";

							dataForMutations.Add(dataForMutation);


							body.visibleName = characterSheet.Name;
							body.playerName = characterSheet.Name;

							body.playerSprites.ThisCharacter = characterSheet;
							body.GetComponent<PlayerScript>().characterSettings = characterSheet;
							body.characterSettings = characterSheet;
							body.PlayerInfo.Name = characterSheet.Name;
							body.PlayerInfo.RequestedCharacterSettings = characterSheet;
							body.Mind.CurrentCharacterSettings = characterSheet;
							body.Mind.name = characterSheet.Name;
							changeling.currentDNA = dna;

							StartCoroutine(body.playerHealth.ProcessDNAPayload(dataForMutations, characterSheet, dna, changeling));
							return true;
					}
					break;
				case ChangelingAbilityType.Misc:
					break;
			}
			return false;
		}

		private void AfterAbility(PlayerInfo sentByPlayer)
		{
			Cooldowns.TryStartClient(sentByPlayer.Script, AbilityData, CooldownTime);

			UIActionManager.SetCooldown(this, CooldownTime, sentByPlayer.GameObject);
		}

		private void AfterAbility(PlayerScript sentByPlayer)
		{
			Cooldowns.TryStartClient(sentByPlayer, AbilityData, CooldownTime);

			UIActionManager.SetCooldown(this, CooldownTime, sentByPlayer.GameObject);
		}

		private bool ValidateAbility(PlayerInfo sentByPlayer)
		{
			var changelingMain = ChangelingMain.ChangelingByMindID[sentByPlayer.Mind.netId];//sentByPlayer.Mind.Body.Changeling;
			//var changelingMain = ChangelingMain.ChangelingByNetPlayerID[sentByPlayer.PosssingID];
			if (sentByPlayer.Script.IsDeadOrGhost || (sentByPlayer.Script.playerHealth.IsCrit && !AbilityData.CanBeUsedWhileInCrit))
			{
				return false;
			}

			if (changelingMain.Chem - AbilityData.AbilityChemCost < 0)
			{
				Chat.AddExamineMsg(changelingMain.gameObject, "Not enough chemicals for ability!");
				return false;
			}

			bool isRecharging = Cooldowns.IsOnServer(sentByPlayer.Script, AbilityData);
			if (isRecharging)
			{
				return false;
			}
			return changelingMain.HasAbility(ability);
		}

		//private bool ValidateDNA(PlayerInfo sentByPlayer, ChangelingDNA dna)
		//{
		//	var changelingMain = ChangelingMain.ChangelingByMindID[sentByPlayer.Mind.netId];//sentByPlayer.Mind.Body.Changeling;
		//	return changelingMain.HasDna(dna);
		//}

		private bool ValidateAbilityClient()
		{
			var changelingMain = UIManager.Display.hudChangeling.ChangelingMain;
			if (changelingMain.ChangelingMind.Body.IsDeadOrGhost || changelingMain.ChangelingMind.Body.playerHealth.IsCrit ||
			changelingMain.Chem - AbilityData.AbilityChemCost < 0)
			{
				return false;
			}

			return changelingMain.HasAbility(ability);
		}

	}
}