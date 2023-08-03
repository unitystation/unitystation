using Mirror;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Systems.Character;
using UnityEngine;
using Util;

namespace Changeling
{
	[DisallowMultipleComponent]
	public class ChangelingMain : NetworkBehaviour
	{
		[Header("Changeling main")]
		[SyncVar(hook = nameof(SyncChemCount))]
		private float chem = 25;
		public float Chem => chem;
		[SyncVar(hook = nameof(SyncMindID))] private uint changelingMindID = 0;
		[SyncVar] private float chemMax = 75;
		[SyncVar] private float chemAddPerTime = CHEM_ADD_TIME;
		[SyncVar] private float chemAddTime = CHEM_ADD_PER_TIME_BASE;
		public int ExtractedDna => changelingDnas.Count;

		[SyncVar(hook = nameof(SyncResetCounts))] private int resetCount = 0;
		[SyncVar(hook = nameof(SyncMaxResetCounts))] private int resetCountMax = 1;

		[SyncVar] private int absorbCount = 0;
		public int AbsorbCount => absorbCount;
		public int ResetsLeft => resetCountMax - resetCount;

		private List<ChangelingDna> changelingDnas = new List<ChangelingDna>();
		private List<ChangelingMemories> changelingMemories = new List<ChangelingMemories>();
		public List<ChangelingMemories> ChangelingMemories => changelingMemories;

		public bool isFakingDeath = false;

		public ChangelingDna currentDNA;

		[SyncVar(hook = nameof(SyncChangelingDna))]
		private string changelingDNASer = "";
		[SyncVar(hook = nameof(SyncChangelingMemories))]
		private string changelingMemoriesSer = "";

		public List<ChangelingDna> ChangelingDNAs => new List<ChangelingDna>(changelingDnas);
		public List<ChangelingDna> ChangelingLastDNAs
		{
			get
			{
				return ChangelingDNAs.Skip(changelingDnas.Count - MAX_LAST_EXTRACTED_DNA_FOR_TRANSFORM).ToList();
			}
		}
		[SyncVar(hook = nameof(SyncEPCount))]
		private int evolutionPoints = 10;
		public int EvolutionPoints => evolutionPoints;

		public ObservableCollection<ChangelingAbility> AbilitiesNow => changelingMind.ChangelingAbilities;
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

		private static Dictionary<uint, ChangelingMain> changelingByMindID = new();
		public static Dictionary<uint, ChangelingMain> ChangelingByMindID => changelingByMindID;
		private static Dictionary<uint, Mind> changelingMinds = new();
		private UiChangeling uiChangeling;
		public UiChangeling Ui => uiChangeling;
		private Mind changelingMind;
		public Mind ChangelingMind
		{
			get
			{
				if (changelingMind == null)
				{
					changelingMind = gameObject.transform.parent.gameObject.GetComponent<PlayerScript>().Mind;
				}
				return changelingMind;
			}
		}

		public List<ActionData> ActionData => throw new NotImplementedException();

		private int tickTimer = 0;

		private const float CHEM_ADD_TIME = 1;
		private const float CHEM_ADD_PER_TIME_BASE = 2;
		private const float CHEM_ADD_PER_TIME_BASE_SLOWED = 2;
		private const int MAX_LAST_EXTRACTED_DNA_FOR_TRANSFORM = 7;

		#region Hooks

		private void SyncChangelingDna(string oldValue, string newValue)
		{
			if (changelingDnas == null)
				changelingDnas = new List<ChangelingDna>();
			else
				changelingDnas.Clear();

			foreach (var dnaSer in newValue.Split("\n"))
			{
				var dnaDes = JsonConvert.DeserializeObject<ChangelingDna>(dnaSer);
				if (dnaDes == null)
					continue;

				changelingDnas.Add(dnaDes);
			}
			changelingDNASer = newValue;

			if (PlayerManager.LocalPlayerScript != null && PlayerManager.LocalPlayerScript.Mind != null && changelingMindID == PlayerManager.LocalPlayerScript.Mind.netId)
			{
				if (changelingMind == null)
					changelingMind = PlayerManager.LocalPlayerScript.Mind;
				SetChangelingUI();
			}
		}

		private void SyncChangelingMemories(string oldValue, string newValue)
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

			try
			{
				if (PlayerManager.LocalPlayerScript != null && PlayerManager.LocalPlayerScript.Mind != null && changelingMindID == PlayerManager.LocalPlayerScript.Mind.netId)
				{
					uiChangeling.RefreshAbilites();
				}
			}
			catch
			{
				Logger.LogError($"[ChangelingMain/SyncAbilityList]{ChangelingMind.CurrentPlayScript.playerName} can`t refresh abilities", Category.Changeling);
			}
		}

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
				Logger.LogError($"[ChangelingMain/SyncChemCount]{ChangelingMind.CurrentPlayScript.playerName} can`t set up UI", Category.Changeling);
			}
		}

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
				Logger.LogError($"[ChangelingMain/SyncMindID]{ChangelingMind.CurrentPlayScript.playerName} can`t set up UI", Category.Changeling);
			}
		}

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
				Logger.LogError($"[ChangelingMain/SyncEPCount]{ChangelingMind.CurrentPlayScript.playerName} can`t set up UI", Category.Changeling);
			}
		}

		private void SyncResetCounts(int oldValue, int newValue)
		{
			resetCount = newValue;
			UIManager.Display.hudChangeling.UpdateResetButton();
		}

		private void SyncMaxResetCounts(int oldValue, int newValue)
		{
			resetCountMax = newValue;
			UIManager.Display.hudChangeling.UpdateResetButton();
		}

		#endregion

		#region Commands

		/// <summary>
		/// Resets changeling abilities
		/// </summary>
		[Command(requiresAuthority = true)]
		public void CmdResetAbilities()
		{
			if (resetCount >= resetCountMax)
				return;
			resetCount++;

			var forRemove = new List<ChangelingAbility>();
			foreach (var abil in AbilitiesNow)
			{
				if (abil.AbilityData.canBeReseted)
				{
					forRemove.Add(abil);
				}
			}

			StringBuilder abilitesIDSNowToSer = new StringBuilder();
			foreach (var abil in forRemove)
			{
				evolutionPoints += abil.AbilityData.AbilityEPCost;
				AbilitiesNow.Remove(abil);
			}
			foreach (var abil in AbilitiesNow)
			{
				abilitesIDSNowToSer.AppendLine(abil.AbilityData.Index.ToString());
			}
			abilitesIDSNow = abilitesIDSNowToSer.ToString();
		}


		/// <summary>
		/// Add ability to changeling
		/// </summary>
		/// <param name="index">Ability index</param>
		[Command(requiresAuthority = true)]
		public void CmdBuyAbility(short index)
		{
			var abil = ChangelingAbilityList.Instance.FromIndex(index);

			AddAbility(abil);
		}

		#endregion

		public void ResetAbilities()
		{
			// toggle off abilities when player resets them
			foreach (var abil in AbilitiesNow)
			{
				if (abil.AbilityData.canBeReseted && abil.IsToggled && !abil.AbilityData.IsAimable)
				{
					abil.CallToggleActionClient(false);
				}
			}
			CmdResetAbilities();
		}

		private void OnDisable()
		{
			if (UIManager.Display.hudChangeling.ChangelingMain == this)
			{
				UIManager.Display.hudChangeling.SetActive(false);
			}

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Tick);
		}

		public bool HasDna(ChangelingDna dna)
		{
			return changelingDnas.Contains(dna);
		}

		public void AddDna(ChangelingDna dna)
		{
			var stringBuildes = new StringBuilder();
			stringBuildes.Append(changelingDNASer);
			if (!HasDna(dna))
			{
				changelingDnas.Add(dna);
				stringBuildes.AppendLine(JsonConvert.SerializeObject(dna));
			}
			changelingDNASer = stringBuildes.ToString();
		}

		private void OnCrit()
		{
			if (isFakingDeath)
				return;
			foreach (var abil in AbilitiesNow)
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
				ToggleHud();
			}
		}

		public void ToggleHud(bool turnOn = true)
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
			foreach (var abil in AbilitiesNow)
			{
				if (abil.AbilityData.IsToggle && !abil.AbilityData.IsAimable && abil.AbilityData.SwithedToOffWhenExitCrit)
				{
					abil.ForceToggleToState(false);
				}
			}
		}

		public void AbsorbDna(ChangelingDna dna, PlayerScript target)
		{
			AddDna(dna);
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

			var changelingMemoriesToSer = new StringBuilder();
			changelingMemoriesToSer.Append(changelingMemoriesSer);

			foreach (var memToSer in changelingMemories)
			{
				changelingMemoriesToSer.AppendLine(JsonConvert.SerializeObject(memToSer));
			}
			changelingMemoriesSer = changelingMemoriesToSer.ToString();
		}

		private void AddMemories(ChangelingMemories memories)
		{
			changelingMemories.Add(memories);
			var changelingMemoriesToSer = new StringBuilder();
			changelingMemoriesToSer.Append(changelingMemoriesSer);
			changelingMemoriesToSer.AppendLine($"{JsonConvert.SerializeObject(memories)}");
			changelingMemoriesSer = changelingMemoriesToSer.ToString();
		}

		private void AddMemories(ChangelingDna dna, PlayerScript target)
		{
			var targetName = target.playerName;
			var mem = new ChangelingMemories();
			var information = target.Mind.GetObjectives();
			var species = target.Mind.CurrentCharacterSettings.Species;
			var gender = target.Mind.CurrentCharacterSettings.GetGender();
			var pronoun = target.Mind.CurrentCharacterSettings.PlayerPronoun;
			mem.Form(dna.Job, targetName, information, species, gender, pronoun);
			AddMemories(mem);
		}

		private void AddMemories(PlayerScript target)
		{
			var targetName = target.playerName;
			var mem = new ChangelingMemories();
			var information = target.Mind.GetObjectives();
			var species = target.Mind.CurrentCharacterSettings.Species;
			var gender = target.Mind.CurrentCharacterSettings.GetGender();
			var pronoun = target.Mind.CurrentCharacterSettings.PlayerPronoun;
			mem.Form(target.PlayerInfo.Job, targetName, information, species, gender, pronoun);
			AddMemories(mem);
		}

		public void ClearMemories()
		{
			changelingMemories.Clear();
			changelingMemoriesSer = "";
		}
		
		public void AbsorbDna(PlayerScript target, ChangelingMain changelingMain) // That gonna be another changeling
		{
			foreach (var dna in changelingMain.changelingDnas)
			{
				AddDna(dna);
			}
			AddMemories(changelingMain.changelingMemories);
			changelingMain.RemoveDna(changelingMain.changelingDnas);
			foreach (var mem in changelingMain.ChangelingMemories)
			{
				AddMemories(mem);
			}
			changelingMain.ClearMemories();
			AddMemories(target);
			resetCountMax++;
			chemMax += 50;
			chem += 50;
			evolutionPoints += 5;
		}

		public void RemoveDna(List<ChangelingDna> targetDNAs)
		{
			foreach (var dna in targetDNAs)
			{
				changelingDnas.Remove(dna);
			}

			var builder = new StringBuilder();
			foreach (var dna in changelingDnas)
			{
				changelingDnas.Remove(dna);
				builder.AppendLine(JsonConvert.SerializeObject(dna));
			}
			changelingDNASer = builder.ToString();
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
			foreach (ChangelingAbility abil in AbilitiesNow)
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

			var playerScript = changelingMindUser.CurrentPlayScript;


			if (!CustomNetworkManager.IsServer) return;

			if (changelingByMindID.ContainsKey(changelingMind.netId))
			{
				changelingByMindID.Remove(changelingMind.netId);
			}
			changelingByMindID.Add(changelingMind.netId, this);

			if (changelingMinds.ContainsKey(changelingMind.netId))
			{
				changelingMinds.Remove(changelingMind.netId);
			}
			changelingMinds.Add(changelingMind.netId, changelingMind);

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
			var dnaObject = new ChangelingDna();

			dnaObject.FormDna(changelingMind.Body.PlayerInfo.Script);

			AddDna(dnaObject);
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

			foreach (var x in AbilitiesNow)
			{
				abilitesIDSNow = $"\n{x.AbilityData.Index}";
			}
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

		public ChangelingDna GetDnaById(int dnaID)
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