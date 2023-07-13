using Changeling;
using GameModes;
using HealthV2;
using Items;
using Mirror;
using Newtonsoft.Json;
using Objects;
using Player;
using ScriptableObjects.Systems.Spells;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Systems.Character;
using UI.Action;
using UI.Core.Action;
using UnityEngine;
using UnityEngine.TextCore.Text;
using Util;
using static UnityEngine.UIElements.UxmlAttributeDescription;

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
		[SyncVar(hook = nameof(SyncIsToggled))]
		private bool isToggled = false;
		public bool IsToggled => isToggled;

		private static readonly StandardProgressActionConfig injectProgressBar =
		new StandardProgressActionConfig(StandardProgressActionType.CPR);
		private const float maxDistanceToTile = 1.5f;
		private const float extractTime = 2f;

		public void SyncIsToggled(bool oldValue, bool value)
		{
			isToggled = value;
		}

		public virtual void CallActionClient()
		{
			UIAction action = UIActionManager.Instance.DicIActionGUI[this][0];
			//PlayerList.Instance.GetPlayersOnMatrix()
			if (AbilityData.IsLocal)// && ValidateAbility(PlayerManager.LocalPlayerScript.PlayerInfo))
			{
				if (ValidateAbilityClient(PlayerManager.LocalPlayerScript.PlayerInfo))
				{
					UseAbilityLocal(PlayerManager.LocalPlayerScript.PlayerInfo.Mind.Body.GetComponent<ChangelingMain>(), ability);
				}
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
			if (AbilityData.IsLocal) //  && ValidateAbilityClient(PlayerManager.LocalPlayerScript.PlayerInfo)
			{
				UseAbilityToggle(PlayerManager.LocalPlayerScript.PlayerInfo.Mind.Body.GetComponent<ChangelingMain>(), ability, toggled);
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
			if (ValidateAbility(SentByPlayer) &&
				CastAbilityToggleServer(SentByPlayer, clickPosition, toggle))
			{
				isToggled = toggle;
			}
		}

		public void CallActionServerWithParam(PlayerInfo SentByPlayer, Vector3 clickPosition, List<string> param)
		{
			if (ValidateAbility(SentByPlayer) &&
				CastAbilityServerWithParam(SentByPlayer, clickPosition, param))
			{
				//AfterAbility(SentByPlayer);
			}
		}

		private bool CastAbilityServerWithParam(PlayerInfo sentByPlayer, Vector3 clickPosition, List<string> param)
		{
			var changeling = sentByPlayer.Mind.Body.GetComponent<ChangelingMain>();
			UseAbilityWithParam(changeling, AbilityData, param);
			return true;
		}

		private bool CastAbilityToggleServer(PlayerInfo sentByPlayer, Vector3 clickPosition, bool toggle)
		{
			var changeling = sentByPlayer.Mind.Body.GetComponent<ChangelingMain>();
			if (toggle)
			{
				changeling.UseAbility(this);
			}
			else
			{
				AfterAbility(sentByPlayer);
			}
			UseAbilityToggle(changeling, AbilityData, toggle);
			return true;
		}

		private bool CastAbilityServer(PlayerInfo sentByPlayer, Vector3 clickPosition)
		{
			var changeling = sentByPlayer.Mind.Body.GetComponent<ChangelingMain>();
			changeling.UseAbility(this);
			//ability.PerfomAbility(changeling, clickPosition);
			UseAbility(changeling, ability, clickPosition);
			return true;
		}

		private PlayerScript GetPlayerOnClick(ChangelingMain changeling, Vector3 clickPosition, Vector3 rounded)
		{
			MatrixInfo matrixinfo = MatrixManager.AtPoint(rounded, true);
			var tilePosition = matrixinfo.MetaTileMap.Layers[LayerType.Base].Tilemap.WorldToCell(clickPosition);
			matrixinfo = MatrixManager.AtPoint(tilePosition, true);
			var localPosInt = clickPosition.ToLocal(matrixinfo.Matrix);
			if (!changeling.ChangelingMind.Body.IsPositionReachable(tilePosition + new Vector3Int(2, 1, 0), true, maxDistanceToTile))
			{
				return null;
			}

			PlayerScript target = null;
			foreach (PlayerScript integrity in matrixinfo.Matrix.Get<PlayerScript>(Vector3Int.CeilToInt(localPosInt), true))
			{
				// to be sure that player don`t morph into AI or something like that
				if (integrity.PlayerType != PlayerTypes.Normal)
					continue;
				target = integrity;
				break;
			}
			return target;
		}

		private bool UseAbility(ChangelingMain changeling, ChangelingData data, Vector3 clickPosition)
		{
			switch (data.abilityType)
			{
				case ChangelingAbilityType.Sting:
					switch (data.stingType)
					{
						case StingType.ExtractDNASting:
							clickPosition = new Vector3(clickPosition.x - 0.5f, clickPosition.y - 0.5f, 0);
							var rounded = Vector3Int.CeilToInt(clickPosition);
							var target = GetPlayerOnClick(changeling, clickPosition, rounded);
							if (target == null || target == changeling.ChangelingMind.Body)
							{
								return false;
							}

							Chat.AddCombatMsgToChat(changeling.gameObject,
							$"<color=red>You start DNA extraction of {target.playerName}</color>",
							$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.playerName} starts DNA extraction of {target.playerName}</color>");

							var action = StandardProgressAction.Create(injectProgressBar,
								() => PerfomAbilityAfter(changeling, data, clickPosition, target));
							action.ServerStartProgress(changeling.ChangelingMind.Body.AssumedWorldPos, extractTime, changeling.gameObject);
							return true;
						case StingType.Absorb:
							clickPosition = new Vector3(clickPosition.x - 0.5f, clickPosition.y - 0.5f, 0);
							rounded = Vector3Int.CeilToInt(clickPosition);
							target = GetPlayerOnClick(changeling, clickPosition, rounded);
							if (target == null || target == changeling.ChangelingMind.Body)
							{
								return false;
							}

							Chat.AddCombatMsgToChat(changeling.gameObject,
							$"<color=red>You start absorbing of {target.playerName}</color>",
							$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.playerName} starts absorbing of {target.playerName}</color>");

							action = StandardProgressAction.Create(injectProgressBar,
								() => PerfomAbilityAfter(changeling, data, clickPosition, target));
							action.ServerStartProgress(changeling.ChangelingMind.Body.AssumedWorldPos, extractTime, changeling.gameObject);
							return true;
						case StingType.HallucinationSting:
							clickPosition = new Vector3(clickPosition.x - 0.5f, clickPosition.y - 0.5f, 0);
							rounded = Vector3Int.CeilToInt(clickPosition);
							target = GetPlayerOnClick(changeling, clickPosition, rounded);
							if (target == null || target == changeling.ChangelingMind.Body)
							{
								return false;
							}

							action = StandardProgressAction.Create(injectProgressBar,
								() => PerfomAbilityAfter(changeling, data, clickPosition, target));
							action.ServerStartProgress(changeling.ChangelingMind.Body.AssumedWorldPos, extractTime, changeling.gameObject);
							return true;
					}
					break;
				case ChangelingAbilityType.Heal:
					switch (data.healType)
					{
						case ChangelingHealType.Regenerate:
							StartCoroutine(RegenerationProcess(changeling));
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
							var dnaObject = Instantiate(data.DnaPrefab, changeling.gameObject.transform);
							//var spellComponent = spellObject.GetComponent<ChangelingAbility>();
							Chat.AddCombatMsgToChat(changeling.gameObject,
							$"<color=red>You finished DNA extraction of {target.playerName}</color>",
							$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.playerName} finished DNA extraction of {target.playerName}</color>");

							foreach (var x in changeling.ChangelingDNAs)
							{
								if (x.DnaID == target.Mind.Body.GetComponent<PrefabTracker>().ForeverID)
								{
									x.UpdateDNA(target, changeling);
									return true;
								}
							}
							var targetDNA = dnaObject.GetComponent<ChangelingDNA>();
							targetDNA.FormDNA(target, changeling);

							changeling.AddDNA(targetDNA);
							return true;
						case StingType.Absorb:
							//var spellComponent = spellObject.GetComponent<ChangelingAbility>();
							Chat.AddCombatMsgToChat(changeling.gameObject,
							$"<color=red>You finished absorbing of {target.playerName}</color>",
							$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.playerName} finished absorbing of {target.playerName}</color>");

							if (target.PlayerInfo.Mind.IsOfAntag<Changeling>())
							{
								var targetDNAs = new List<ChangelingDNA>();

								targetDNAs.AddRange(target.Mind.Body.GetComponent<ChangelingMain>().ChangelingDNAs);

								foreach (var x in targetDNAs)
								{
									x.transform.SetParent(changeling.transform);
								}

								target.Mind.Body.GetComponent<ChangelingMain>().RemoveDNA(targetDNAs);
								changeling.AddDNA(targetDNAs);
								return true;
							}
							dnaObject = Instantiate(data.DnaPrefab, changeling.gameObject.transform);

							targetDNA = dnaObject.GetComponent<ChangelingDNA>();
							targetDNA.FormDNA(target, changeling);

							changeling.AddDNA(targetDNA);
							return true;
						case StingType.HallucinationSting:
							var randomTimeAfter = UnityEngine.Random.Range(30, 60f);
							//Chat.AddCombatMsgToChat(changeling.gameObject,
							//	$"<color=red>You finished absorbing of {target.playerName}</color>",
							//	$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.playerName} finished absorbing of {target.playerName}</color>");

							if (target.PlayerInfo.Mind.IsOfAntag<Changeling>())
							{
								var targetDNAs = new List<ChangelingDNA>();

								targetDNAs.AddRange(target.Mind.Body.GetComponent<ChangelingMain>().ChangelingDNAs);

								foreach (var x in targetDNAs)
								{
									x.transform.SetParent(changeling.transform);
								}

								target.Mind.Body.GetComponent<ChangelingMain>().RemoveDNA(targetDNAs);
								changeling.AddDNA(targetDNAs);
								return true;
							}
							dnaObject = Instantiate(data.DnaPrefab, changeling.gameObject.transform);

							targetDNA = dnaObject.GetComponent<ChangelingDNA>();
							targetDNA.FormDNA(target, changeling);

							changeling.AddDNA(targetDNA);
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
							break;
						case ChangelingMiscType.AugmentedEyesight:
							break;
					}
					break;
				case ChangelingAbilityType.Transform:
					switch (data.transformType)
					{
						case ChangelingTransformType.TransformMenuOpen:
							UIManager.Display.hudChangeling.OpenTransformUI(changeling, (ChangelingDNA dna) =>
							{
								PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestChangelingAbilitesWithParam(AbilityData.UseAfterChoise.Index, new Vector3(), new List<string>()
								{
									dna.DnaID.ToString()
								});
							});
							return true;
							break;
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
						$"<color=red>Your bones started to cracking</color>",
						$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.playerName} bones started to cracking</color>");
						if (AbilityData.CastSound != null)
							SoundManager.PlayNetworkedAtPos(
							AbilityData.CastSound, changeling.ChangelingMind.CurrentPlayScript.WorldPos, sourceObj: changeling.ChangelingMind.Body.GameObject, global: false);
					}
					part.HealDamage(null, part.MaxHealth / 4, DamageType.Brute);
					part.HealDamage(null, part.MaxHealth / 12, DamageType.Tox);
					part.HealDamage(null, part.MaxHealth / 24, DamageType.Burn);
					part.HealDamage(null, part.MaxHealth / 24, DamageType.Radiation);
					//part.HealDamage(null, part.MaxHealth / 12, DamageType.Oxy);
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
				var spawnedBodypart = Spawn.ServerPrefab(toSpawn).GameObject.GetComponent<BodyPart>();
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

			bloodSystem.BloodPool.RemoveVolume(bloodSystem.BloodPool.Total);
			bloodSystem.AddFreshBlood(bloodSystem.BloodPool, bloodSystem.StartingBlood);

			yield break;
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
					break;
				case ChangelingAbilityType.Transform:
					break;
				case ChangelingAbilityType.Misc:
					switch (data.miscType)
					{
						case ChangelingMiscType.AugmentedEyesight:
							// TODO maybe this need rework
							var lighting = Camera.main.GetComponent<LightingSystem>();
							if (!lighting)
							{
								Logger.LogWarning("Local Player can't find lighting system on Camera.main", Category.Lighting);
							}

							lighting.enabled = !toggle;
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

							var dna = changeling.GetDNAByID(dnaID);

							CharacterSheet characterSheet = dna.CharacterSheet;

							PlayerScript body = changeling.ChangelingMind.Body;

							body.visibleName = characterSheet.Name;
							body.playerName = characterSheet.Name;

							body.characterSettings = characterSheet;

							PlayerHealthData raceBodyparts = characterSheet.GetRaceSoNoValidation();

							ColorUtility.TryParseHtmlString(characterSheet.SkinTone, out var bodyColor);

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

							dataForMutation.BodyPartSearchString = "Torso"; // adding the same thing but with dif name for some species that have torso for every gender

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

							StartCoroutine(body.playerHealth.ProcessDNAPayload(dataForMutations, characterSheet));

							var storage = changeling.ChangelingMind.CurrentPlayScript.DynamicItemStorage;

							foreach (var id in dna.BodyClothesPrefabID)
							{
								var fakeClothes = Spawn.ServerPrefab(CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[id]).GameObject;
								var placed = Inventory.ServerAdd(fakeClothes, storage.GetBestSlotFor(fakeClothes));
								var itemName = fakeClothes.GetComponent<ItemAttributesV2>().InitialName;

								fakeClothes.GetComponent<Pickupable>().OnInventoryMoveServerEvent.AddListener((GameObject item) => // removing item anytime when item was moved or something
								{
									Chat.AddCombatMsgToChat(gameObject,
									$"<color=red>{itemName} was absorbed back into your body.</color>",
									$"<color=red>{itemName} was absorbed into {changeling.ChangelingMind.CurrentPlayScript.playerName} body.</color>");

									_ = Inventory.ServerDespawn(fakeClothes);

									changeling.ChangelingMind.Body.RefreshVisibleName();
								});

								if (!placed)
								{
									_ = Despawn.ServerSingle(fakeClothes);
								}
							}
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
			Cooldowns.TryStartServer(sentByPlayer.Script, AbilityData, CooldownTime);

			UIActionManager.SetCooldown(this, CooldownTime, sentByPlayer.GameObject);
		}

		private bool ValidateAbility(PlayerInfo sentByPlayer)
		{
			if (sentByPlayer.Script.IsDeadOrGhost || sentByPlayer.Script.playerHealth.IsCrit ||
			sentByPlayer.Script.Mind.Body.GetComponent<ChangelingMain>().Chem - AbilityData.AbilityChemCost < 0)
			{
				return false;
			}

			bool isRecharging = Cooldowns.IsOnServer(sentByPlayer.Script, AbilityData);
			if (isRecharging)
			{
				//Chat.AddExamineMsg(sentByPlayer.GameObject, FormatStillRechargingMessage(sentByPlayer));
				return false;
			}
			var changelingMain = sentByPlayer.Mind.Body.GetComponent<ChangelingMain>();
			return changelingMain.HasAbility(ability);
		}

		private bool ValidateDNA(PlayerInfo sentByPlayer, ChangelingDNA dna)
		{
			var changelingMain = sentByPlayer.Mind.Body.GetComponent<ChangelingMain>();
			return changelingMain.HasDna(dna);
		}

		private bool ValidateAbilityClient(PlayerInfo sentByPlayer)
		{
			if (sentByPlayer.Script.IsDeadOrGhost || sentByPlayer.Script.playerHealth.IsCrit ||
			sentByPlayer.Script.Mind.Body.GetComponent<ChangelingMain>().Chem - AbilityData.AbilityChemCost < 0)
			{
				return false;
			}

			var changelingMain = sentByPlayer.Mind.Body.GetComponent<ChangelingMain>();
			return changelingMain.HasAbility(ability);
		}

	}

}