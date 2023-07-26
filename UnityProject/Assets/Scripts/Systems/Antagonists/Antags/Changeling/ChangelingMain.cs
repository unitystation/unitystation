using Mirror;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Systems.Character;
using UnityEngine;
using Util;

namespace Changeling
{
	[DisallowMultipleComponent]
	public class ChangelingMain : NetworkBehaviour, IServerActionGUIMulti
	{
		[Header("Changeling main")]
		[SyncVar(hook = nameof(SyncChemCount))] private float chem = 25;
		public float Chem => chem;
		[SyncVar(hook = nameof(SyncMindID))] private uint changelingMindID = 0;
		[SyncVar] private float chemMax = 75;
		[SyncVar] private float chemAddPerTime = CHEM_ADD_TIME;
		[SyncVar] private float chemAddTime = CHEM_ADD_PER_TIME_BASE;
		public int ExtractedDNA => changelingDNAs.Count;

		[SyncVar(hook = nameof(SyncResetCounts))] private int resetCount = 0;
		[SyncVar(hook = nameof(SyncMaxResetCounts))] private int resetCountMax = 1;

		[SyncVar] private int absorbCount = 0;
		public int AbsorbCount => absorbCount;
		public int ResetsLeft => resetCountMax - resetCount;

		private List<ChangelingDNA> changelingDNAs = new List<ChangelingDNA>();
		private List<ChangelingMemories> changelingMemories = new List<ChangelingMemories>();
		public List<ChangelingMemories> ChangelingMemories => changelingMemories;

		public bool isFakingDeath = false;

		public ChangelingDNA currentDNA;

		[SyncVar(hook = nameof(ChangelingDNASync))]
		private string changelingDNASer = "";
		[SyncVar(hook = nameof(ChangelingMemoriesSync))]
		private string changelingMemoriesSer = "";

		public List<ChangelingDNA> ChangelingDNAs => new List<ChangelingDNA>(changelingDNAs);
		public List<ChangelingDNA> ChangelingLastDNAs
		{
			get
			{
				return ChangelingDNAs.Skip(changelingDNAs.Count - MAX_LAST_EXTRACTED_DNA_FOR_TRANSFORM).ToList();
			}
		}
		[SyncVar(hook = nameof(SyncEPCount))] private int evolutionPoints = 10;
		public int EvolutionPoints => evolutionPoints;

		private ObservableCollection<ChangelingAbility> abilitiesNow => changelingMind.ChangelingAbilities;
		public ObservableCollection<ChangelingAbility> AbilitiesNow => abilitiesNow;
		private List<ChangelingData> AbilitiesNowDataSynced = new List<ChangelingData>();
		public List<ChangelingData> AbilitiesNowData
		{
			get
			{
				return AbilitiesNowDataSynced;
			}
		}
		public List<ChangelingData> AllAbilities => ChangelingAbilityList.Instance.Abilites;

		[SyncVar(hook = nameof(SyncAbilityList))]
		private string abilitesIDSNow = "";

		public static Dictionary<uint, ChangelingMain> ChangelingByMindID = new();
		public static Dictionary<uint, Mind> ChangelingMinds = new();
		private UI_Changeling uiChangeling;
		public UI_Changeling Ui => uiChangeling;
		private Mind changelingMind;
		public Mind ChangelingMind
		{
			get
			{
				if (changelingMind == null)
				{
					Logger.LogError("Changeling can`t find Mind", Category.Changeling);
					changelingMind = gameObject.transform.parent.gameObject.GetComponent<PlayerScript>().Mind;
				}
				return changelingMind;
			}
		}

		public List<ActionData> ActionData => throw new NotImplementedException();

		PlayerScript playerScript;

		private int tickTimer = 0;

		private const float CHEM_ADD_TIME = 1;
		private const float CHEM_ADD_PER_TIME_BASE = 2;
		private const float CHEM_ADD_PER_TIME_BASE_SLOWED = 1;
		private const int MAX_LAST_EXTRACTED_DNA_FOR_TRANSFORM = 7;

		#region Hooks

		[Client]
		private void ChangelingDNASync(string oldValue, string newValue)
		{
			if (changelingDNAs == null)
				changelingDNAs = new List<ChangelingDNA>();
			else
				changelingDNAs.Clear();

			foreach (var dnaSer in newValue.Split("\n"))
			{
				dnaSer.Replace("\\", "");
				var dnaDes = JsonConvert.DeserializeObject<ChangelingDNA>(dnaSer);
				if (dnaDes == null)
					continue;

				changelingDNAs.Add(dnaDes);
			}
			changelingDNASer = newValue;

			if (PlayerManager.LocalPlayerScript != null && PlayerManager.LocalPlayerScript.Mind != null && changelingMindID == PlayerManager.LocalPlayerScript.Mind.netId)
			{
				if (changelingMind == null)
					changelingMind = PlayerManager.LocalPlayerScript.Mind;
				SetChangelingUI();
			}
		}

		[Client]
		private void ChangelingMemoriesSync(string oldValue, string newValue)
		{
			if (changelingMemories == null)
				changelingMemories = new List<ChangelingMemories>();
			else
				changelingMemories.Clear();

			foreach (var memSer in newValue.Split("\n"))
			{
				var memDes = JsonConvert.DeserializeObject<ChangelingMemories>(memSer);

				if (memDes == null)
					continue;

				changelingMemories.Add(memDes);
			}

			changelingMemoriesSer = newValue;
		}

		[Client]
		private void SyncAbilityList(string oldValue, string newValue)
		{
			abilitesIDSNow = newValue;

			if (AbilitiesNowDataSynced == null)
				AbilitiesNowDataSynced = new List<ChangelingData>();
			else
				AbilitiesNowDataSynced.Clear();
			foreach (string id in abilitesIDSNow.Split("\n"))
			{
				if (short.TryParse(id, out var idParsed) && !AbilitiesNowDataSynced.Contains(ChangelingAbilityList.Instance.FromIndex(idParsed)))
					AbilitiesNowDataSynced.Add(ChangelingAbilityList.Instance.FromIndex(idParsed));
			}
		}

		[Client]
		private void SyncChemCount(float oldValue, float newValue)
		{
			chem = newValue;

			try
			{
				if (PlayerManager.LocalPlayerScript != null && PlayerManager.LocalPlayerScript.Mind != null && changelingMindID == PlayerManager.LocalPlayerScript.Mind.netId)
				{
					if (changelingMind == null)
						changelingMind = PlayerManager.LocalPlayerScript.Mind;
					SetChangelingUI();
					uiChangeling.UpdateChemText();
				}
			}
			catch
			{
				Logger.LogError("Changeling can`t set up UI when sync chem", Category.Changeling);
			}
		}

		[Client]
		private void SyncMindID(uint oldValue, uint newValue)
		{
			changelingMindID = newValue;
			try
			{
				if (PlayerManager.LocalPlayerScript != null && PlayerManager.LocalPlayerScript.Mind != null && changelingMindID == PlayerManager.LocalPlayerScript.Mind.netId)
				{
					if (changelingMind == null)
						changelingMind = PlayerManager.LocalPlayerScript.Mind;
					SetChangelingUI();
				}
			}
			catch
			{
				Logger.LogError("Changeling can`t set up UI when sync mind", Category.Changeling);
			}
		}

		[Client]
		private void SyncEPCount(int oldValue, int newValue)
		{
			evolutionPoints = newValue;
			try
			{
				if (PlayerManager.LocalPlayerScript != null && PlayerManager.LocalPlayerScript.Mind != null && changelingMindID == PlayerManager.LocalPlayerScript.Mind.netId)
				{
					if (changelingMind == null)
						changelingMind = PlayerManager.LocalPlayerScript.Mind;
					SetChangelingUI();
					uiChangeling.UpdateEPText();
				}
			}
			catch
			{
				Logger.LogError("Changeling can`t set up UI when sync ep", Category.Changeling);
			}
		}

		[Client]
		private void SyncResetCounts(int oldValue, int newValue)
		{
			resetCount = newValue;
			try
			{
				UIManager.Display.hudChangeling.UpdateResetButton();
			}
			catch
			{
				Logger.LogError("Changeling failes to update reset button", Category.Changeling);
			}
		}

		[Client]
		private void SyncMaxResetCounts(int oldValue, int newValue)
		{
			resetCountMax = newValue;
			try
			{
				UIManager.Display.hudChangeling.UpdateResetButton();
			}
			catch
			{
				Logger.LogError("Changeling failes to update reset button", Category.Changeling);
			}
		}

		#endregion

		#region Commands

		/// <summary>
		/// Resets changeling abilities
		/// </summary>
		[Command(requiresAuthority = false)]
		public void CmdResetAbilities()
		{
			if (resetCount >= resetCountMax)
				return;
			resetCount++;

			var forRemove = new List<ChangelingAbility>();
			foreach (var abil in abilitiesNow)
			{
				if (abil.AbilityData.canBeReseted)
				{
					forRemove.Add(abil);
				}
			}

			abilitesIDSNow = "";
			foreach (var abil in forRemove)
			{
				evolutionPoints += abil.AbilityData.AbilityEPCost;
				abilitiesNow.Remove(abil);
			}
			foreach (var abil in abilitiesNow)
			{
				abilitesIDSNow += $"\n{abil.AbilityData.Index}";
			}
		}


		/// <summary>
		/// Add ability to changeling
		/// </summary>
		/// <param name="index">Ability index</param>
		[Command(requiresAuthority = false)]
		public void CmdBuyAbility(short index)
		{
			//var changeling = changelingMindID[changelingPlayer.netId];
			var abil = ChangelingAbilityList.Instance.FromIndex(index);

			AddAbility(abil);
		}

		#endregion

		private void OnDisable()
		{
			if (UIManager.Display.hudChangeling.ChangelingMain == this)
			{
				UIManager.Display.hudChangeling.SetActive(false);
			}

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Tick);
		}

		public bool HasDna(ChangelingDNA dna)
		{
			return changelingDNAs.Contains(dna);
		}

		public void AddDNA(ChangelingDNA dna)
		{
			if (!HasDna(dna))
			{
				changelingDNAs.Add(dna);
				changelingDNASer += $"{JsonConvert.SerializeObject(dna)}\n";
			}
			//return JsonConvert.DeserializeObject<CharacterSheet>(json);
		}

		private void OnCrit()
		{
			if (isFakingDeath)
				return;
			foreach (var abil in abilitiesNow)
			{
				if (abil.AbilityData.IsToggle && !abil.AbilityData.IsAimable && abil.AbilityData.SwithedToOnWhenInCrit)
				{
					abil.ForceToggleToState(true);
				}
			}
		}

		private void SetChangelingUI()
		{
			if (changelingMindID != 0 && uiChangeling == null)
			{
				uiChangeling = UIManager.Display.hudChangeling;
				uiChangeling.ChangelingMain = this;
				ToggleHUD();
			}
		}

		public void ToggleHUD(bool turnOn = true)
		{
			if (uiChangeling == null)
				return;
			if (turnOn)
			{
				uiChangeling.SetUp(this);
				uiChangeling.gameObject.SetActive(true);
			}
			else
			{
				uiChangeling.TurnOff();
				uiChangeling.gameObject.SetActive(false);
			}
		}

		private void OnExitCrit()
		{
			if (isFakingDeath)
				return;
			foreach (var abil in abilitiesNow)
			{
				if (abil.AbilityData.IsToggle && !abil.AbilityData.IsAimable && abil.AbilityData.SwithedToOffWhenExitCrit)
				{
					abil.ForceToggleToState(false);
				}
			}
		}

		public void AbsorbDNA(ChangelingDNA dna, PlayerScript target)
		{
			AddDNA(dna);
			if (chem + 50 <= chemMax)
				chem += 50;
			else
				chem += chemMax - chem;

			absorbCount++;
			AddMemories(dna, target);
		}

		private void AddMemories(List<ChangelingMemories> mem)
		{
			changelingMemories.AddRange(mem);

			changelingMemoriesSer = "";

			foreach (var memToSer in changelingMemories)
			{
				changelingMemoriesSer += $"{JsonConvert.SerializeObject(memToSer)}\n";
			}
		}

		private void AddMemories(ChangelingMemories memories)
		{
			changelingMemories.Add(memories);
			changelingMemoriesSer += $"{JsonConvert.SerializeObject(memories)}\n";
		}

		private void AddMemories(ChangelingDNA dna, PlayerScript target)
		{
			var targetName = target.playerName;
			var mem = new ChangelingMemories();
			var information = target.Mind.GetObjectives();
			var species = target.Mind.CurrentCharacterSettings.Species;
			var gender = target.Mind.CurrentCharacterSettings.GetGender();
			mem.Form(dna.Job, targetName, information, species, gender);
			AddMemories(mem);
		}

		private void AddMemories(PlayerScript target)
		{
			var targetName = target.playerName;
			var mem = new ChangelingMemories();
			var information = target.Mind.GetObjectives();
			var species = target.Mind.CurrentCharacterSettings.Species;
			var gender = target.Mind.CurrentCharacterSettings.GetGender();
			mem.Form(target.PlayerInfo.Job, targetName, information, species, gender);
			AddMemories(mem);
		}

		private void ClearMemories()
		{
			changelingMemories.Clear();
			changelingMemoriesSer = "";
		}
		
		public void AbsorbDNA(List<ChangelingDNA> dnas, PlayerScript target, ChangelingMain changelingMain) // That gonna be another changeling
		{
			foreach (var dna in dnas)
			{
				AddDNA(dna);
				//if (!HasDna(dna))
				//	changelingDNAs.Add(dna);
			}
			AddMemories(changelingMain.changelingMemories);
			changelingMain.RemoveDNA(changelingMain.changelingDNAs);
			changelingMain.ClearMemories();// changelingMain.changelingMemories.Clear();
			AddMemories(target);
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

			changelingDNASer = "";
			foreach (var dna in changelingDNAs)
			{
				changelingDNAs.Remove(dna);
				changelingDNASer += $"{JsonConvert.SerializeObject(dna)}\n";
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

			chemAddPerTime = CHEM_ADD_TIME;
			chemAddTime = CHEM_ADD_PER_TIME_BASE + CHEM_ADD_PER_TIME_BASE_SLOWED * slowingCount;
		}

		public void Init(Mind changelingMindUser)
		{
			changelingMind = changelingMindUser;

			playerScript = changelingMindUser.CurrentPlayScript;
			playerScript.OnActionControlPlayer += PlayerEnterBody;
			//ChangelingByNetPlayerID.Add(playerScript.netId, this);


			if (!CustomNetworkManager.IsServer) return;

			if (ChangelingByMindID.ContainsKey(changelingMind.netId))
			{
				ChangelingByMindID.Remove(changelingMind.netId);
			}
			ChangelingByMindID.Add(changelingMind.netId, this);

			if (ChangelingMinds.ContainsKey(changelingMind.netId))
			{
				ChangelingMinds.Remove(changelingMind.netId);
			}
			ChangelingMinds.Add(changelingMind.netId, changelingMind);

			SetUpAbilites();

			UpdateManager.Add(Tick, 1f);
			StartCoroutine(LateInit());

			playerScript.playerHealth.OnCrit.AddListener(OnCrit);
			playerScript.playerHealth.OnCritExit.AddListener(OnExitCrit);
		}

		private IEnumerator LateInit() // need to be done after spawn because not all player systems was loaded at init moment
		{
			yield return WaitFor.SecondsRealtime(1.5f);
			changelingMindID = changelingMind.netId;
			var dnaObject = new ChangelingDNA();

			dnaObject.FormDNA(changelingMind.Body.PlayerInfo.Script);

			AddDNA(dnaObject);
			currentDNA = dnaObject;
		}

		private void SetUpAbilites()
		{
			foreach (var ability in ChangelingAbilityList.Instance.Abilites)
			{
				if (ability.startAbility)
				{
					AddAbility(ability);
				}
			}
		}

		public void AddAbility(ChangelingData ability)
		{
			if (evolutionPoints - ability.AbilityEPCost < 0)
				return;
			evolutionPoints -= ability.AbilityEPCost;
			abilitesIDSNow += $"\n{ability.Index}";
			changelingMind.AddAbility(ability.AddToPlayer(changelingMind));
		}

		public void RemoveAbility(ChangelingData ability)
		{
			abilitesIDSNow = "";
			if (CustomNetworkManager.IsServer)
				evolutionPoints += ability.AbilityEPCost;
			changelingMind.RemoveAbility(ability.AddToPlayer(changelingMind));

			foreach (var x in abilitiesNow)
			{
				abilitesIDSNow = $"\n{x.AbilityData.Index}";
			}
		}

		public void PlayerEnterBody()
		{
			
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

		public void UseAbility(ChangelingAbility changelingAbility)
		{
			if (HasAbility(changelingAbility.AbilityData) && CustomNetworkManager.IsServer)
			{
				chem -= changelingAbility.AbilityData.AbilityChemCost;
			}
		}

		public void CallToggleActionServer(ActionData data, PlayerInfo playerInfo, bool toggle)
		{

		}

		public void CallToggleActionClient(ActionData data, bool toggle)
		{

		}

		public ChangelingDNA GetDNAByID(int dnaID)
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

	public class ChangelingMemories
	{
		public JobType MemoriesJob;
		public string MemoriesName;
		public string MemoriesObjectives;
		public string MemoriesSpecies;
		public Gender MemoriesGender;

		public void Form(JobType job, string playerName, string objectives, string species, Gender gender)
		{
			MemoriesJob = job;
			MemoriesName = playerName;
			MemoriesObjectives = objectives;
			MemoriesSpecies = species;
			MemoriesGender = gender;
		}
	}

	public class ChangelingDNA
	{
		public int DnaID;
		public string PlayerName = "";
		public string Objectives;
		public CharacterSheet CharacterSheet;
		public JobType Job;
		public List<string> BodyClothesPrefabID = new();

		public void FormDNA(PlayerScript playerDataForDNA)
		{
			foreach (var clothe in playerDataForDNA.Mind.Body.playerSprites.clothes)
			{
				if (clothe.Value.GameObjectReference != null)
				{
					BodyClothesPrefabID.Add(clothe.Value.GameObjectReference.GetComponent<PrefabTracker>().ForeverID);
				}
			}

			PlayerName = playerDataForDNA.playerName;
			DnaID = playerDataForDNA.Mind.bodyMobID;
			CharacterSheet = (CharacterSheet)playerDataForDNA.Mind.CurrentCharacterSettings.Clone();
			try
			{
				Job = playerDataForDNA.PlayerInfo.Job;
			}
			catch
			{
				Job = JobType.ASSISTANT;
				Logger.LogError("When creating DNA can`t find target job", Category.Changeling);
			}
		}

		public void UpdateDNA(PlayerScript playerDataForDNA)
		{
			BodyClothesPrefabID.Clear();

			foreach (var clothe in playerDataForDNA.Mind.Body.playerSprites.clothes)
			{
				if (clothe.Value.GameObjectReference != null)
					BodyClothesPrefabID.Add(clothe.Value.GameObjectReference.GetComponent<PrefabTracker>().ForeverID);
			}

			CharacterSheet = (CharacterSheet)playerDataForDNA.characterSettings.Clone();
		}
	}
}