using CameraEffects;
using Chemistry;
using Clothing;
using HealthV2;
using HealthV2.Living.PolymorphicSystems;
using Items;
using Items.Implants.Organs;
using Mirror;
using Player;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Systems.Character;
using UI.Action;
using UI.Core.Action;
using UnityEngine;
using UnityEngine.Rendering;
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

		private static readonly StandardProgressActionConfig stingProgressBar =
		new StandardProgressActionConfig(StandardProgressActionType.CPR);

		private static readonly StandardProgressActionConfig transformProgressBar =
		new StandardProgressActionConfig(StandardProgressActionType.CPR, false, false, true, true, true);

		private const float MAX_REMOVING_WHILE_ABSORBING_BODY = 70f;
		private const float MAX_DISTANCE_TO_TILE = 1.6f;
		private const float TIME_FOR_COMPLETION_TRANSFORM = 2f;

		public virtual void CallActionClient()
		{
			UIAction action = UIActionManager.Instance.DicIActionGUI[this][0];
			if (AbilityData.IsLocal && ValidateAbilityClient())
			{
					UseAbilityLocal(UIManager.Instance.displayControl.hudChangeling.ChangelingMain, ability);
					AfterAbility(PlayerManager.LocalPlayerScript);
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

			if (AbilityData.IsLocal && ValidateAbilityClient())
			{
				isToggled = toggled;
				UseAbilityToggleLocal(UIManager.Instance.displayControl.hudChangeling.ChangelingMain, ability, toggled);
			}
			else
			{
				PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestChangelingAbilitesToggle(AbilityData.Index, toggled);
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

		public void CallToggleActionServer(PlayerInfo SentByPlayer, bool toggle)
		{
			var validateAbility = ValidateAbility(SentByPlayer);
			if (validateAbility &&
				CastAbilityToggleServer(SentByPlayer, toggle))
			{
				isToggled = toggle;
			} else if (validateAbility == false)
			{
				//Set ability icon back
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

		public void CallActionServerWithParam(PlayerInfo sentByPlayer, string paramString)
		{
			var changeling = ChangelingMain.ChangelingByMindID[sentByPlayer.Mind.netId];
			List<string> param = paramString.Split('\n').ToList();
			if (ValidateAbility(sentByPlayer) &&
				CastAbilityServerWithParam(sentByPlayer, param))
			{
				changeling.UseAbility(this);
			}
		}

		private bool CastAbilityServerWithParam(PlayerInfo sentByPlayer, List<string> param)
		{
			var changeling = ChangelingMain.ChangelingByMindID[sentByPlayer.Mind.netId];
			UseAbilityWithParam(changeling, AbilityData, param);
			return true;
		}

		private bool CastAbilityToggleServer(PlayerInfo sentByPlayer, bool toggle)
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
			var changeling = ChangelingMain.ChangelingByMindID[sentByPlayer.Mind.netId];
			if (AbilityData.IsAimable == false)
				changeling.UseAbility(this);
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

			PlayerScript target = null;
			foreach (PlayerScript integrity in matrixinfo.Matrix.Get<PlayerScript>(Vector3Int.CeilToInt(localPosInt), true))
			{
				// to be sure that player don`t morph into AI or something like that
				if (integrity.PlayerType != PlayerTypes.Normal)
					continue;
				target = integrity;
				break;
			}
			if (target == null || target.Mind == null)
				return null;

			var brainIsFounded = false;
			foreach (var bodyPart in target.Mind.Body.playerHealth.BodyPartList)
			{
				foreach (BodyPartFunctionality organ in bodyPart.OrganList)
				{
					if (organ is Brain brain)
					{
						brainIsFounded = true;
						break;
					}
				}
				if (brainIsFounded == true)
				{
					break;
				}
			}

			if (Vector3.Distance(changeling.ChangelingMind.Body.GameObject.AssumedWorldPosServer(), target.Mind.Body.GameObject.AssumedWorldPosServer()) > MAX_DISTANCE_TO_TILE
				|| target.IsDeadOrGhost || brainIsFounded == false)
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
					return StingAbilities(changeling, data, target);
				case ChangelingAbilityType.Heal:
					return RegenerateAbilities(changeling, data);
			}
			return false;
		}

		public bool PerfomAbilityAfter(ChangelingMain changeling, ChangelingData data, PlayerScript target)
		{
			switch (data.abilityType)
			{
				case ChangelingAbilityType.Sting:
					return PerfomAbilityAfterSting(changeling, target, data);
			}
			return false;
		}

		private bool UseAbilityLocal(ChangelingMain changeling, ChangelingData data)
		{
			if (data.IsLocal == false)
				return false;

			switch (data.abilityType)
			{
				case ChangelingAbilityType.Misc:
					return MiscAbilitiesLocal(changeling, data);
			}

			return false;
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

		private IEnumerator ReagentAdding(float time, Reagent reagent, float reagentCount, PlayerScript target)
		{
			yield return WaitFor.SecondsRealtime(time);

			if (target.Mind.IsGhosting == false && target.playerHealth.IsDead == false)
				target.playerHealth.reagentPoolSystem.BloodPool.Add(reagent, reagentCount);
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

		private bool UseAbilityToggle(ChangelingMain changeling, ChangelingData data, bool toggle)
		{
			if (data.IsToggle == false)
				return false;
			switch (data.abilityType)
			{
				case ChangelingAbilityType.Heal:
					return RegenerateAbilitiesToggle(changeling, data, toggle);
				case ChangelingAbilityType.Misc:
					return MiscAbilitiesToggle(changeling, data, toggle);
			}
			return false;
		}

		private bool UseAbilityToggleLocal(ChangelingMain changeling, ChangelingData data, bool toggle)
		{
			if (data.IsToggle == false)
				return false;
			switch (data.abilityType)
			{
				case ChangelingAbilityType.Misc:
					return MiscAbilitiesToggleLocal(changeling, data, toggle);
			}
			return false;
		}

		private bool UseAbilityWithParam(ChangelingMain changeling, ChangelingData data, List<string> param)
		{
			switch (data.abilityType)
			{
				case ChangelingAbilityType.Transform:
					return TransformAbilityWithParam(changeling, data, param);
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
			if (CooldownTime < 0.01f)
				return;
			Cooldowns.TryStartClient(sentByPlayer, AbilityData, CooldownTime);

			UIActionManager.SetCooldown(this, CooldownTime, sentByPlayer.GameObject);
		}

		private bool ValidateAbility(PlayerInfo sentByPlayer)
		{
			var changelingMain = ChangelingMain.ChangelingByMindID[sentByPlayer.Mind.netId];
			if (sentByPlayer.Script.IsDeadOrGhost || (sentByPlayer.Script.playerHealth.IsCrit && !AbilityData.CanBeUsedWhileInCrit))
			{
				return false;
			}

			if (changelingMain.Chem - AbilityData.AbilityChemCost < 0)
			{
				Chat.AddExamineMsg(changelingMain.gameObject, "Not enough chemicals for ability!");
				return false;
			}

			bool isRecharging = Cooldowns.IsOnClient(sentByPlayer.Script, AbilityData) || Cooldowns.IsOnServer(sentByPlayer.Script, AbilityData);
			if (isRecharging)
			{
				Chat.AddExamineMsg(changelingMain.gameObject, $"Ability {AbilityData.Name} is recharging!");
				return false;
			}
			return changelingMain.HasAbility(ability);
		}

		private bool ValidateAbilityClient()
		{
			var changelingMain = UIManager.Display.hudChangeling.ChangelingMain;
			if (changelingMain.Chem - AbilityData.AbilityChemCost < 0)
			{
				Chat.AddExamineMsg(changelingMain.gameObject, "Not enough chemicals for ability!");
				return false;
			}
			if (changelingMain.ChangelingMind.Body.IsDeadOrGhost || changelingMain.ChangelingMind.Body.playerHealth.IsCrit)
			{
				return false;
			}

			bool isRecharging = Cooldowns.IsOnClient(changelingMain.ChangelingMind.Body, AbilityData) ||
			Cooldowns.IsOnServer(changelingMain.ChangelingMind.Body, AbilityData);
			if (isRecharging)
			{
				Chat.AddExamineMsg(changelingMain.gameObject, $"Ability {AbilityData.Name} is recharging!");
				return false;
			}

			return changelingMain.HasAbility(ability);
		}

		#region Abilities

		private bool MiscAbilitiesLocal(ChangelingMain changeling, ChangelingData data)
		{
			switch (data.miscType)
			{
				case ChangelingMiscType.OpenStore:
					UIManager.Display.hudChangeling.OpenStoreUI();
					return true;
				case ChangelingMiscType.OpenMemories:
					UIManager.Display.hudChangeling.OpenMemoriesUI();
					return true;
				case ChangelingMiscType.OpenTransform:
					UIManager.Display.hudChangeling.OpenTransformUI(changeling, (ChangelingDna dna) =>
					{
						PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestChangelingAbilitesWithParam(AbilityData.UseAfterChoise.Index, $"{dna.DnaID}");
					});
					return true;
			}
			return false;
		}

		private List<DNAMutationData> SettingUpDnaList(PlayerHealthData raceBodyparts)
		{
			List<DNAMutationData> dataForMutations = new();

			DNAMutationData dataForMutation = new();

			DNAMutationData.DNAPayload payload = new();

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

			return dataForMutations;
		}

		private void UpdatePlayerSettings(PlayerScript body, ChangelingMain changeling, CharacterSheet characterSheet, ChangelingDna dna)
		{
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
		}

		private List<(Pickupable, NamedSlot)> GetCurrentItems(PlayerScript playerScript)
		{
			var itemsBeforeTransform = new List<(Pickupable, NamedSlot)>();
			DynamicItemStorage storage = playerScript.DynamicItemStorage;
			var allItemsStorage = storage.GetItemSlotTree();

			foreach (ItemSlot itemSlot in allItemsStorage)
			{
				var item = InventoryMove.Drop(itemSlot).MovedObject;
				if (item != null && itemSlot.NamedSlot != null)
					itemsBeforeTransform.Add((item, (NamedSlot)itemSlot.NamedSlot));
			}

			return itemsBeforeTransform;
		}

		private IEnumerator ChangelingStartTransformAction(PlayerScript body, CharacterSheet characterSheet, ChangelingDna dna, ChangelingMain changeling)
		{
			var action = StandardProgressAction.Create(transformProgressBar,
						() =>
						{
						});
			action.ServerStartProgress(changeling.ChangelingMind.Body.AssumedWorldPos, TIME_FOR_COMPLETION_TRANSFORM, changeling.ChangelingMind.Body.gameObject);

			yield return WaitFor.SecondsRealtime(TIME_FOR_COMPLETION_TRANSFORM);

			body.visibleName = characterSheet.Name;
			body.playerName = characterSheet.Name;

			body.characterSettings = characterSheet;

			PlayerHealthData raceBodyparts = characterSheet.GetRaceSoNoValidation();

			var dataForMutations = SettingUpDnaList(raceBodyparts);

			var itemsBeforeTransform = GetCurrentItems(body);

			UpdatePlayerSettings(body, changeling, characterSheet, dna);

			foreach (var item in itemsBeforeTransform)
			{
				if (item.Item1.gameObject.GetComponent<ItemAttributesV2>().IsFakeItem)
				{
					// deleting prev fake items
					if (item.Item1.gameObject.GetComponent<ItemAttributesV2>().IsFakeItem)
						_ = Despawn.ServerSingle(item.Item1.gameObject);
				}
			}

			body.playerHealth.InternalNetIDs.Clear();
			yield return body.playerHealth.InjectDna(dataForMutations, true, characterSheet);

			body.playerHealth.RefreshPumps();
			body.playerHealth.UpdateBloodPool(true);
			var bodyParts = GetBodyParts(body);
			SetUpItems(itemsBeforeTransform, bodyParts);
			SetUpFakeItems(dna, changeling, body.playerHealth);
			yield return WaitFor.SecondsRealtime(2f);
			UpdateSprites(body.playerHealth.playerSprites, characterSheet);
			body.playerHealth.UpdateMeatAndSkinProduce();

			// set new hand because we deleted prev
			body.playerHealth.playerScript.PlayerNetworkActions.CmdSetActiveHand(bodyParts[NamedSlot.leftHand].ItemStorageNetID, NamedSlot.leftHand);
		}

		private void UpdateSprites(PlayerSprites playerSprites, CharacterSheet characterSheet)
		{
			foreach (var x in playerSprites.OpenSprites)
			{
				SpriteHandlerManager.UnRegisterHandler(x.gameObject.GetComponent<SpriteHandler>().GetMasterNetID(), x.gameObject.GetComponent<SpriteHandler>());
				_ = Despawn.ServerSingle(x.gameObject);
			}
			foreach (Transform x in playerSprites.CustomisationSprites.transform)
			{
				if (x.gameObject.name.ToLower().Contains("undershirt") || x.gameObject.name.ToLower().Contains("underwear") ||
				x.gameObject.name.ToLower().Contains("socks"))
				{
					SpriteHandlerManager.UnRegisterHandler(x.gameObject.GetComponent<SpriteHandler>().GetMasterNetID(), x.gameObject.GetComponent<SpriteHandler>());
					_ = Despawn.ServerSingle(x.gameObject);
				}
			}
			playerSprites.OpenSprites.Clear();

			playerSprites.ThisCharacter = characterSheet;
			playerSprites.SetupSprites();
			playerSprites.gameObject.GetComponent<RootBodyPartController>().UpdateClients();

			ColorUtility.TryParseHtmlString(characterSheet.SkinTone, out Color CurrentSurfaceColour);
			playerSprites.SetSurfaceColour(CurrentSurfaceColour);
		}

		private Dictionary<NamedSlot, ItemSlot> GetBodyParts(PlayerScript body)
		{
			var bodyParts = new Dictionary<NamedSlot, ItemSlot>();
			foreach (var x in body.DynamicItemStorage.GetItemSlotTree())
			{
				if (x.NamedSlot != null && !bodyParts.ContainsKey((NamedSlot)x.NamedSlot))
					bodyParts.Add((NamedSlot)x.NamedSlot, x);
			}
			return bodyParts;
		}

		private void SetUpItems(List<(Pickupable, NamedSlot)> itemsBeforeTransform, Dictionary<NamedSlot, ItemSlot> bodyParts)
		{

			foreach (var itemForPlace in itemsBeforeTransform)
			{
				if (itemForPlace.Item1 != null && bodyParts.ContainsKey(itemForPlace.Item2))
				{
					Inventory.ServerAdd(itemForPlace.Item1.gameObject, bodyParts[itemForPlace.Item2]);
				}
			}
		}

		private void SetUpFakeItems(ChangelingDna dna, ChangelingMain changeling, LivingHealthMasterBase health)
		{
			if (dna != null && changeling != null)
			{
				var storage = changeling.ChangelingMind.CurrentPlayScript.DynamicItemStorage;
				// need to firstly create fake uniform for placing id card into fake slot
				foreach (var id in dna.BodyClothesPrefabID)
				{
					var fakeItem = Spawn.ServerPrefab(CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[id]).GameObject;
					ItemSlot bestSlot = storage.GetBestSlotFor(fakeItem);
					if (bestSlot == null)
					{
						_ = Despawn.ServerSingle(fakeItem);
						continue;
					}
					var placed = Inventory.ServerAdd(fakeItem, bestSlot);
					// better don`t put fake items into storages
					if (placed == false || (((int?)bestSlot.NamedSlot) > 15) || bestSlot.NamedSlot == NamedSlot.handcuffs)
					{
						_ = Despawn.ServerSingle(fakeItem);
						continue;
					}
					fakeItem.GetComponent<ItemAttributesV2>().IsFakeItem = true;
					// making items absolutely useless
					RemoveItemsInsideFakeItem(fakeItem);
					ItemStorage itemStrg = MakeFakeItemUseless(fakeItem, health);


					var itemName = fakeItem.GetComponent<ItemAttributesV2>().InitialName;
					// removing item anytime when item was moved or something
					fakeItem.GetComponent<Pickupable>().OnInventoryMoveServerEvent.AddListener((GameObject item) =>
					{
						item.TryGetPlayer(out var pl);
						Chat.AddCombatMsgToChat(gameObject,
						$"<color=red>{itemName} was absorbed back into your body.</color>",
						$"<color=red>{itemName} was absorbed into {pl.Username} body.</color>");

						if (itemStrg != null)
							itemStrg.ServerDropAll();

						_ = Inventory.ServerDespawn(item);

						changeling.ChangelingMind.Body.RefreshVisibleName();
					});
				}
			}
		}

		private ItemStorage MakeFakeItemUseless(GameObject fakeItem, LivingHealthMasterBase health)
		{
			ItemStorage itemStrg = null;
			foreach (var comp in fakeItem.GetComponents(typeof(Component)))
			{
				if (comp is WearableArmor armor)
				{
					foreach (var bodyArmr in armor.ArmoredBodyParts)
					{
						bodyArmr.Armor.Melee = 0;
						bodyArmr.Armor.Bullet = 0;
						bodyArmr.Armor.Laser = 0;
						bodyArmr.Armor.Energy = 0;
						bodyArmr.Armor.Bomb = 0;
						bodyArmr.Armor.Rad = 0;
						bodyArmr.Armor.Fire = 0;
						bodyArmr.Armor.Acid = 0;
						bodyArmr.Armor.Magic = 0;
						bodyArmr.Armor.Bio = 0;
						bodyArmr.Armor.Anomaly = 0;
						bodyArmr.Armor.DismembermentProtectionChance = 0;
						bodyArmr.Armor.StunImmunity = false;
						bodyArmr.Armor.TemperatureProtectionInK = new Vector2(283.15f, 283.15f + 20);
						bodyArmr.Armor.PressureProtectionInKpa = new Vector2(30f, 300f);
					}
					continue;
				}
				if (comp is IDCard card)
				{
					var cardJobName = card.GetJobTitle();
					var cardRegName = card.RegisteredName;
					card.ServerChangeOccupation(OccupationList.Instance.Get(JobType.ASSISTANT), true, true);

					card.ServerSetRegisteredName(cardRegName);
					card.ServerSetJobTitle(cardJobName);
					for (int i = 0; i < card.currencies.Length; i++)
					{
						card.currencies[i] = 0; // removing all currencies on card to be sure
					}
					continue;
				}
				if (comp is ItemStorage itemStorage)
				{
					itemStrg = itemStorage;
					continue;
				}
				if (comp is ItemActionButton actionButton)
				{
					actionButton.OnRemovedFromBody(health);
					continue;
				}
				if (comp is not Pickupable && comp is not UprightSprites && comp is not UniversalObjectPhysics
				 && comp is not SortingGroup && comp is MonoBehaviour mono)
				{
					mono.NetDisable();
					continue;
				}
			}
			return itemStrg;
		}

		private void RemoveItemsInsideFakeItem(GameObject fakeItem)
		{
			if (fakeItem.TryGetComponent<InteractableStorage>(out var iS))
			{
				var items = iS.ItemStorage.GetItemSlots();

				foreach (var x in items)
				{
					if (x.ItemObject != null)
						_ = Despawn.ServerSingle(x.ItemObject);
				}
			}
		}

		private bool TransformAbilityWithParam(ChangelingMain changeling, ChangelingData data, List<string> param)
		{
			switch (data.transformType)
			{
				case ChangelingTransformType.Transform:
					string dnaID = param[0];
					var dna = changeling.GetDnaById(int.Parse(dnaID));
					CharacterSheet characterSheet = dna.CharacterSheet;
					PlayerScript body = changeling.ChangelingMind.Body;
					Chat.AddExamineMsgFromServer(body.gameObject, $"Your body starts morph into a new form.");
					StartCoroutine(ChangelingStartTransformAction(body, characterSheet, dna, changeling));
					return true;
			}
			return false;
		}

		private void ExtractSting(ChangelingMain changeling, ChangelingData data, PlayerScript target)
		{
			Chat.AddCombatMsgToChat(changeling.gameObject,
			$"<color=red>You start sting of {target.playerName}</color>",
			$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.playerName} starts sting of {target.playerName}</color>");

			var action = StandardProgressAction.Create(stingProgressBar,
				() => PerfomAbilityAfter(changeling, data, target));
			action.ServerStartProgress(changeling.ChangelingMind.Body.AssumedWorldPos, AbilityData.StingTime, changeling.ChangelingMind.Body.gameObject);
		}

		private void HallucinationSting(ChangelingMain changeling, ChangelingData data, PlayerScript target)
		{
			var action = StandardProgressAction.Create(stingProgressBar,
				() => PerfomAbilityAfter(changeling, data, target));
			action.ServerStartProgress(changeling.ChangelingMind.Body.AssumedWorldPos, AbilityData.StingTime, changeling.ChangelingMind.Body.gameObject);
		}

		private void AbsorbSting(ChangelingMain changeling, ChangelingData data, PlayerScript target)
		{

			Chat.AddCombatMsgToChat(changeling.gameObject,
			$"<color=red>You start absorbing of {target.playerName}</color>",
			$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.playerName} starts absorbing of {target.playerName}</color>");
			var cor = StartCoroutine(AbsorbingProgress(ability.StingTime / 10f, target));
			var action = StandardProgressAction.Create(stingProgressBar,
				() =>
				{
					StopCoroutine(cor);
					PerfomAbilityAfter(changeling, data, target);
				},
				(_) =>
				{
					StopCoroutine(cor);
				});


			action.ServerStartProgress(changeling.ChangelingMind.Body.AssumedWorldPos, AbilityData.StingTime, changeling.ChangelingMind.Body.gameObject);
		}

		private bool StingAbilities(ChangelingMain changeling, ChangelingData data, PlayerScript target)
		{
			switch (data.stingType)
			{
				case StingType.ExtractDNASting:
					ExtractSting(changeling, data, target);
					return true;
				case StingType.Absorb:
					AbsorbSting(changeling, data, target);
					return true;
				case StingType.HallucinationSting:
					HallucinationSting(changeling, data, target);
					return true;
			}
			return false;
		}

		private bool RegenerateAbilities(ChangelingMain changeling, ChangelingData data)
		{
			switch (data.healType)
			{
				case ChangelingHealType.Regenerate:
					StartCoroutine(RegenerationProcess(changeling));
					return true;
			}
			return false;
		}

		private void RevivingStasis(ChangelingMain changeling, bool toggle)
		{
			if (toggle == false || changeling.IsFakingDeath)
			{
				if (changeling.IsFakingDeath)
				{
					isToggled = false;
				}
				UIActionManager.SetServerSpriteSO(this, ActionData.Sprites[0]);
				changeling.UseAbility(this);
				// healing
				changeling.ChangelingMind.Body.playerHealth.FullyHeal();
				changeling.ChangelingMind.Body.playerHealth.UnstopOverallCalculation();
				changeling.ChangelingMind.Body.playerHealth.UnstopHealthSystemsAndRestartHeart();
				changeling.HasFakingDeath(false);
			}
			else
			{
				UIActionManager.SetServerSpriteSO(this, ActionData.Sprites[1]);
				changeling.HasFakingDeath(true);

				changeling.ChangelingMind.Body.playerHealth.StopHealthSystemsAndHeart();
				changeling.ChangelingMind.Body.playerHealth.StopOverralCalculation();
				changeling.ChangelingMind.Body.playerHealth.SetConsciousState(ConsciousState.UNCONSCIOUS);
			}
		}

		private bool RegenerateAbilitiesToggle(ChangelingMain changeling, ChangelingData data, bool toggle)
		{
			switch (data.healType)
			{
				case ChangelingHealType.RevivingStasis:
					RevivingStasis(changeling, toggle);
					return true;
			}
			return false;
		}

		private void AugmentedEyesightLocal(ChangelingMain changeling, bool toggle)
		{
			if (Camera.main == null ||
				Camera.main.TryGetComponent<CameraEffectControlScript>(out var effects) == false) return;

			effects.AdjustPlayerVisibility(
				toggle ? AbilityData.ExpandedNightVisionVisibility : effects.MinimalVisibilityScale,
				toggle ? AbilityData.DefaultvisibilityAnimationSpeed : AbilityData.RevertvisibilityAnimationSpeed);
			effects.ToggleNightVisionEffectState(toggle);
			effects.NvgHasMaxedLensRadius(true);

			foreach (var x in changeling.AbilitiesNow)
			{
				if (x.AbilityData == AbilityData)
				{
					x.isToggled = toggle;
				}
			}
			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestChangelingAbilitesToggle(AbilityData.Index, toggle);
		}

		private void AugmentedEyesight(ChangelingMain changeling, bool toggle)
		{
			foreach (var bodyPart in changeling.ChangelingMind.Body.playerHealth.BodyPartList)
			{
				foreach (BodyPartFunctionality organ in bodyPart.OrganList)
				{
					if (organ is Eye eye)
					{
						eye.SyncXrayState(eye.HasXray, toggle);
					}
				}
			}
		}

		private bool MiscAbilitiesToggleLocal(ChangelingMain changeling, ChangelingData data, bool toggle)
		{
			switch (data.miscType)
			{
				case ChangelingMiscType.AugmentedEyesight:
					AugmentedEyesightLocal(changeling, toggle);
					return true;
			}
			return false;
		}
		

		private bool MiscAbilitiesToggle(ChangelingMain changeling, ChangelingData data, bool toggle)
		{
			switch (data.miscType)
			{
				case ChangelingMiscType.AugmentedEyesight:
					AugmentedEyesight(changeling, toggle);
					return true;
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
						$"<color=red>Your bones makes cracking noise.</color>",
						$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.visibleName}'s bones make cracking noises.</color>");
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

			ColorUtility.TryParseHtmlString(characterSheet.SkinTone, out var bodyColor);
			RespawnMissedBodyparts(changeling, bodyColor, changeling.ChangelingMind.Body.playerHealth, bodyParts);

			RegenerationOfBodyOrgans(changeling);

			UpdateBloodPool(bloodSystem, changeling);
		}

		private void AfterExtractSting(ChangelingMain changeling, PlayerScript target)
		{
			var targetDNA = new ChangelingDna();
			Chat.AddCombatMsgToChat(changeling.gameObject,
			$"<color=red>You finished sting of {target.playerName}</color>",
			$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.playerName} finished sting of {target.playerName}</color>");

			targetDNA.FormDna(target);

			changeling.AddDna(targetDNA);
		}

		private void AfterAbsorbSting(ChangelingMain changeling, PlayerScript target)
		{
			Chat.AddCombatMsgToChat(changeling.gameObject,
			$"<color=red>You finished absorbing of {target.playerName}</color>",
			$"<color=red>{changeling.ChangelingMind.CurrentPlayScript.playerName} finished absorbing of {target.playerName}</color>");

			try
			{
				if (target.PlayerInfo.Mind.IsOfAntag<Changeling>())
				{
					changeling.AbsorbDna(target, target.Changeling);
				}
				else
				{
					var targetDNA = new ChangelingDna();
					targetDNA.FormDna(target);
					changeling.AbsorbDna(targetDNA, target);
				}
			}
			catch (Exception ex)
			{
				Logger.LogError($"[ChangelingAbility/AfterAbsorbSting] Failed to create DNA of absorbed body ({target.PlayerInfo.Mind.Body.playerName}) {ex}", Category.Changeling);
				return;
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

		private void AfterHallucinationSting(ChangelingMain changeling, PlayerScript target)
		{
			var randomTimeAfter = UnityEngine.Random.Range(30, 60f);
			var targetDNA = new ChangelingDna();

			targetDNA.FormDna(target);

			changeling.AddDna(targetDNA);

			StartCoroutine(ReagentAdding(randomTimeAfter, AbilityData.Reagent, AbilityData.ReagentCount, target));
		}

		private bool PerfomAbilityAfterSting(ChangelingMain changeling, PlayerScript target, ChangelingData data)
		{
			switch (data.stingType)
			{
				case StingType.ExtractDNASting:
					AfterExtractSting(changeling, target);
					return true;
				case StingType.Absorb:
					AfterAbsorbSting(changeling, target);
					return true;
				case StingType.HallucinationSting:
					AfterHallucinationSting(changeling, target);
					return true;
			}
			return false;
		}

		#endregion
	}
}