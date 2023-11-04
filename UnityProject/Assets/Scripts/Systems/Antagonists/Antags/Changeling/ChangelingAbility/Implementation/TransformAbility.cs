using Clothing;
using HealthV2;
using Items;
using Mirror;
using Player;
using System.Collections;
using System.Collections.Generic;
using Systems.Character;
using UI.Action;
using UnityEngine;
using UnityEngine.Rendering;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Systems/ChangelingAbilities/TransformAbility")]
	public class TransformAbility: ChangelingParamAbility
	{
		private const float TIME_FOR_COMPLETION_TRANSFORM = 2f;
		private static readonly StandardProgressActionConfig transformProgressBar =
		new StandardProgressActionConfig(StandardProgressActionType.CPR, false, false, true, true, true);

		public override bool UseAbilityParamClient(ChangelingMain changeling)
		{
			return true;
		}

		[Server]
		public override bool UseAbilityParamServer(ChangelingMain changeling, List<string> param)
		{
			if (CustomNetworkManager.IsServer == false) return false;
			string dnaID = param[0];
			var dna = changeling.GetDnaById(int.Parse(dnaID));
			CharacterSheet characterSheet = dna.CharacterSheet;
			PlayerScript body = changeling.ChangelingMind.Body;
			Chat.AddExamineMsgFromServer(body.gameObject, $"Your body starts morph into a new form.");
			changeling.UseAbility(this);
			changeling.StartCoroutine(ChangelingStartTransformAction(body, characterSheet, dna, changeling));

			return true;
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
						if (item.TryGetPlayer(out var pl))
							Chat.AddCombatMsgToChat(changeling.gameObject,
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
	}
}