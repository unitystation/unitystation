using Clothing;
using GameModes;
using HealthV2;
using Items;
using Items.Implants.Organs;
using Mirror;
using Newtonsoft.Json;
using Shared.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using Systems.Character;
using Systems.Clothing;
using UI.CharacterCreator;
using UI.Core.Action;
using UnityEngine;
using Util;

namespace Changeling
{
	public class ChangelingMain : NetworkBehaviour, IOnPlayerPossess,
			IOnPlayerRejoin, IOnPlayerLeaveBody, IServerActionGUIMulti
	{
		[Header("Changeling main")]
		[SerializeField] [SyncVar] private float chem = 25;
		public float Chem => chem;
		[SyncVar] private float chemMax = 75;
		private const float chemAddPerTimeConst = 1;
		[SyncVar] private float chemAddPerTime = chemAddPerTimeConst;
		private const float chemAddTimeBase = 2;
		private const float chemAddTimeBaseSlowedAdd = 1;
		[SyncVar] private float chemAddTime = chemAddTimeBase;
		public int ExtractedDNA => changelingDNAs.Count;
		private const int maxLastExtractedDNA = 7;

		[SyncVar] private int resetCount = 0;
		[SyncVar] private int resetCountMax = 1;
		public int ResetsLeft => resetCountMax - resetCount;

		[SerializeField] [SyncVar] private List<ChangelingDNA> changelingDNAs = new List<ChangelingDNA>();
		public List<ChangelingDNA> ChangelingDNAs => new List<ChangelingDNA>(changelingDNAs);
		public List<ChangelingDNA> ChangelingLastDNAs
		{
			get
			{
				return ChangelingDNAs.Skip(changelingDNAs.Count - maxLastExtractedDNA).ToList();
			}
		}
		public List<string> ChangelingDNAsID
		{
			get
			{
				var dNAsIDs = new List<string>();
				foreach (var x in changelingDNAs)
				{
					dNAsIDs.Add(x.DnaID);
				}
				return dNAsIDs;
			}
		}
		public List<ChangelingAbility> ChangelingAbilitesForReset
		{
			get
			{
				var forRemove = new List<ChangelingAbility>();
				foreach (var x in abilitiesNow)
				{
					if (x.AbilityData.canBeReseted)
						forRemove.Add(x);
				}
				return forRemove;
			}
		}
		[SyncVar] private int evolutionPoints = 10;
		public int EvolutionPoints => evolutionPoints;

		private List<ChangelingData> abilitiesToBuy = new List<ChangelingData>();
		public List<ChangelingData> AbilitiesToBuy => abilitiesToBuy;
		private ObservableCollection<ChangelingAbility> abilitiesNow => changelingMind.ChangelingAbilities;
		public ObservableCollection<ChangelingAbility> AbilitiesNow => abilitiesNow;
		public List<ChangelingData> AbilitiesNowData
		{
			get
			{
				var data = new List<ChangelingData>();

				foreach (var x in abilitiesNow)
				{
					data.Add(x.AbilityData);
				}
				return data;
			}
		}
		public List<ChangelingData> AllAbilities => ChangelingAbilityList.Instance.Abilites;

		private UI_Changeling ui;
		public UI_Changeling Ui => ui;
		private Mind changelingMind;
		public Mind ChangelingMind => changelingMind;

		public List<ActionData> ActionData => throw new NotImplementedException();

		PlayerScript playerScript;

		private int tickTimer = 0;

		public string newNameTest;
		public string charSetTest;

		[ContextMenu("TESTRENAME")]
		private void TESTRENAME()
		{
			PlayerScript body = changelingMind.Body;
			CharacterSheet characterSheet = JsonConvert.DeserializeObject<CharacterSheet>(charSetTest);

			body.visibleName = characterSheet.Name;
			body.playerName = characterSheet.Name;

			body.playerSprites.ThisCharacter = characterSheet;
			body.GetComponent<PlayerScript>().characterSettings = characterSheet;
			body.characterSettings = characterSheet;
			body.PlayerInfo.Name = characterSheet.Name;
			body.PlayerInfo.RequestedCharacterSettings = characterSheet;
			body.Mind.CurrentCharacterSettings = characterSheet;
			body.Mind.name = characterSheet.Name;
		}

		[ContextMenu("TESTTRANSFORMATIONTOSHEET")]
		private void TESTTRANSFORMATIONTOSHEET()
		{
			PlayerScript body = changelingMind.Body;

			CharacterSheet characterSheet = JsonConvert.DeserializeObject<CharacterSheet>(charSetTest);
			body.visibleName = characterSheet.Name;
			body.playerName = characterSheet.Name;

			body.characterSettings = characterSheet;

			var playerSprites = body.playerSprites;

			PlayerHealthData raceBodyparts = characterSheet.GetRaceSoNoValidation();

			ColorUtility.TryParseHtmlString(characterSheet.SkinTone, out var bodyColor);

			foreach (var part in body.playerHealth.SurfaceBodyParts)
			{
				
			}

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

			//dataForMutation = new DNAMutationData();
			//payload = new DNAMutationData.DNAPayload
			//{
			//	SpeciesMutateTo = raceBodyparts,
			//	MutateToBodyPart = raceBodyparts.Base.ArmRight.Elements[0]
			//};

			//dataForMutation.Payload.Add(payload);

			//dataForMutation.BodyPartSearchString = "Hair";

			//dataForMutations.Add(dataForMutation);


			body.visibleName = characterSheet.Name;
			body.playerName = characterSheet.Name;

			body.playerSprites.ThisCharacter = characterSheet;
			body.GetComponent<PlayerScript>().characterSettings = characterSheet;
			body.characterSettings = characterSheet;
			body.PlayerInfo.Name = characterSheet.Name;
			body.PlayerInfo.RequestedCharacterSettings = characterSheet;
			body.Mind.CurrentCharacterSettings = characterSheet;
			body.Mind.name = characterSheet.Name;

			//foreach (var bodyPart in changelingMind.Body.playerSprites.Addedbodypart)
			//{
			//	if (bodyPart.name.ToLower().Contains("eyes"))
			//	{
			//		var color = Color.red;
			//		foreach (var bodyPartSheet in characterSheet.SerialisedBodyPartCustom)
			//		{
			//			if (bodyPartSheet.path.ToLower().Contains("eyes"))
			//			{
			//				ColorUtility.TryParseHtmlString(bodyPartSheet.Data, out color);
			//			}
			//			break;
			//		}
			//		bodyPart.spriteRenderer.color = color;
			//		Debug.Log("EYES!");
			//		break;
			//	}
			//}

			StartCoroutine(body.playerHealth.ProcessDNAPayload(dataForMutations, characterSheet));

			//changelingMind.Body.playerSprites.SetSurfaceColour();

			//foreach (var x in changelingMind.Body.playerSprites.OpenSprites)
			//{
			//	x.spriteRenderer.color = bodyColor;
			//}


			//playerSprites.RootBodyPartsLoaded = false;
			//foreach (var x in playerSprites.Addedbodypart)
			//{
			//	Destroy(x.gameObject);
			//}
			//playerSprites.Addedbodypart.Clear();

			//playerSprites.OnCharacterSettingsChange(characterSheet);
		}

		public List<GameObject> clothes = new List<GameObject>();

		[ContextMenu("TESTTRANSFORMCLOTHES")]
		private void TESTTRANSFORMCLOTHES()
		{
			if (!CustomNetworkManager.IsServer) return;
			DynamicItemStorage storage = ChangelingMind.CurrentPlayScript.DynamicItemStorage;


			foreach (var clth in clothes)
			{
				var id = clth.GetComponent<PrefabTracker>().ForeverID;
				var fakeClothes = Spawn.ServerPrefab(CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[id]).GameObject;
				var placed = Inventory.ServerAdd(fakeClothes, storage.GetBestSlotFor(fakeClothes));
				var itemName = fakeClothes.GetComponent<ItemAttributesV2>().InitialName;

				fakeClothes.GetComponent<Pickupable>().OnInventoryMoveServerEvent.AddListener((GameObject item) => // removing item anytime when item was moved or something
				{
					Chat.AddCombatMsgToChat(gameObject,
					$"<color=red>{itemName} was absorbed back into your body.</color>",
					$"<color=red>{itemName} was absorbed into {ChangelingMind.CurrentPlayScript.playerName} body.</color>");

					_ = Inventory.ServerDespawn(fakeClothes);

					changelingMind.Body.RefreshVisibleName();
				});

				if (!placed)
				{
					_ = Despawn.ServerSingle(fakeClothes);
				}
			}
		}

		private void OnDisable()
		{
			UIManager.Display.hudChangeling.SetActive(false);
			abilitiesNow.Clear();
		}

		public bool HasDna(ChangelingDNA dna)
		{
			return changelingDNAs.Contains(dna);
		}

		public void AddDNA(ChangelingDNA dna)
		{
			if (!HasDna(dna))
				changelingDNAs.Add(dna);
		}

		public void AddDNA(List<ChangelingDNA> dnas) // that gonna be another changeling
		{
			foreach (var dna in dnas)
			{
				if (!HasDna(dna))
					changelingDNAs.Add(dna);
			}
			resetCountMax++;
			chemMax += 50;
			chem += 50;
			evolutionPoints += 5;
		}

		public void RemoveDNA(List<ChangelingDNA> targetDNAs)
		{
			foreach (var dna in targetDNAs)
			{
				changelingDNAs.Remove(dna);
			}
		}

		private void Tick()
		{
			if (CustomNetworkManager.IsServer)
			{
				tickTimer++;

				if (tickTimer >= chemAddTime)
				{
					RefreshChemRegeneration();
					if (chem + chemAddPerTime <= chemMax)
						chem += chemAddPerTime;
					tickTimer = 0;
				}
			}
		}

		private void RefreshChemRegeneration()
		{
			int slowingCount = 0;
			foreach (ChangelingAbility abil in abilitiesNow)
			{
				if (abil.IsToggled)
				{
					if (abil.AbilityData.IsSlowingChemRegeneration)
						slowingCount++;
					if (abil.AbilityData.IsStopingChemRegeneration)
					{
						chemAddPerTime = 0;
						return;
					}
				}
			}

			chemAddPerTime = chemAddPerTimeConst;
			chemAddTime = chemAddTimeBase + chemAddTimeBaseSlowedAdd * slowingCount;
		}

		public void Init(Mind changelingMindUser)
		{
			ui = UIManager.Display.hudChangeling;

			changelingMind = changelingMindUser;

			playerScript = changelingMindUser.CurrentPlayScript;
			playerScript.OnActionControlPlayer += PlayerEnterBody;

			SetUpAbilites();

			if (!CustomNetworkManager.IsServer) return;

			UpdateManager.Add(Tick, 1f);
			UpdateManager.Add(LateInit, 2f);
		}

		public void LateInit() // need to be done after spawn because not all player systems was loaded
		{
			var dnaObject = Spawn.ServerPrefab(abilitiesNow[0].AbilityData.DnaPrefab, parent: gameObject.transform).GameObject.GetComponent<ChangelingDNA>();
			dnaObject.FormDNA(changelingMind.Body.PlayerInfo.Script, this);

			AddDNA(dnaObject);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, LateInit);
		}

		private void SetUpAbilites()
		{
			foreach (var ability in ChangelingAbilityList.Instance.Abilites)
			{
				if (ability.startAbility)
				{
					AddAbility(ability);
				} else
				{
					AddAbilityToStore(ability);
				}
			}
		}

		public void AddAbility(ChangelingData ability)
		{
			if (CustomNetworkManager.IsServer)
				evolutionPoints -= ability.AbilityEPCost;
			changelingMind.AddAbility(ability.AddToPlayer(changelingMind));
		}

		public void RemoveAbility(ChangelingData ability)
		{
			if (CustomNetworkManager.IsServer)
				evolutionPoints += ability.AbilityEPCost;
			changelingMind.RemoveAbility(ability.AddToPlayer(changelingMind));
		}

		public void AddAbilityToStore(ChangelingData ability)
		{
			if (!abilitiesToBuy.Contains(ability))
			{
				abilitiesToBuy.Add(ability);
			}
		}

		private void SetUpHUD(bool turnOn = true)
		{
			if (turnOn)
			{
				ui.SetUp(this);
				ui.gameObject.SetActive(true);
			} else
			{
				ui.TurnOff();
				ui.gameObject.SetActive(false);
			}
		}

		public void PlayerEnterBody()
		{
			SetUpHUD();
		}

		public void OnServerPlayerPossess(Mind mind)
		{

		}

		public void OnPlayerRejoin(Mind mind)
		{
			SetUpHUD(true);
		}

		public void OnPlayerLeaveBody(PlayerInfo account)
		{
			SetUpHUD(false);
		}

		public void CallActionServer(ActionData data, PlayerInfo playerInfo)
		{
			
		}

		public void CallActionClient(ActionData data)
		{
			
		}

		public bool HasAbility(ChangelingData ability)
		{
			return AbilitiesNowData.Contains(ability);
		}

		public void RemoveAbilityFromStore(ChangelingData abilityToAdd)
		{
			abilitiesToBuy.Remove(abilityToAdd);
		}

		public void UseAbility(ChangelingAbility changelingAbility)
		{
			if (HasAbility(changelingAbility.AbilityData) && CustomNetworkManager.IsServer)
			{
				//TODO uncomment this
				//chem -= changelingAbility.AbilityData.AbilityChemCost;
			}
		}

		public void CallToggleActionServer(ActionData data, PlayerInfo playerInfo, bool toggle)
		{

		}

		public void CallToggleActionClient(ActionData data, bool toggle)
		{

		}

		public void ResetAbilites()
		{
			resetCount++;

			if (resetCount > resetCountMax)
				return;

			var forRemove = new List<ChangelingAbility>();
			foreach (var abil in abilitiesNow)
			{
				if (abil.AbilityData.canBeReseted)
				{
					forRemove.Add(abil);
				}
			}

			foreach (var abil in forRemove)
			{
				evolutionPoints += abil.AbilityData.AbilityEPCost;
				abilitiesNow.Remove(abil);
			}
		}

		public ChangelingDNA GetDNAByID(string dnaID)
		{
			foreach (var x in ChangelingLastDNAs)
			{
				if (x.DnaID == dnaID)
				{
					return x;
				}
			}

			return null;
		}
	}
}