using Mirror;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Logs;
using UI.Core.Action;
using UnityEngine;

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
		[SyncVar] private float chemAddPerTime = CHEM_ADD_PER_TIME;
		[SyncVar] private float chemAddTime = CHEM_ADD_TIME_BASE;
		public int ExtractedDna => ChangelingDnas.Count;

		[SyncVar(hook = nameof(SyncResetCounts))] private int resetCount = 0;
		[SyncVar(hook = nameof(SyncMaxResetCounts))] private int resetCountMax = 1;

		[SyncVar] private int absorbCount = 0;
		public int AbsorbCount => absorbCount;
		public int ResetsLeft => resetCountMax - resetCount;
		private List<ChangelingMemories> changelingMemories = new List<ChangelingMemories>();
		public List<ChangelingMemories> ChangelingMemories => new List<ChangelingMemories>(changelingMemories);

		private bool isFakingDeath = false;
		public bool IsFakingDeath => isFakingDeath;

		public ChangelingDna currentDNA;

		private List<ChangelingDna> changelingDnas = new List<ChangelingDna>();
		public List<ChangelingDna> ChangelingDnas => new List<ChangelingDna>(changelingDnas);
		public List<ChangelingDna> ChangelingLastDNAs
		{
			get
			{
				return ChangelingDnas.Skip(ChangelingDnas.Count - MAX_LAST_EXTRACTED_DNA_FOR_TRANSFORM).ToList();
			}
		}
		[SyncVar(hook = nameof(SyncEPCount))]
		private int evolutionPoints = 10;
		public int EvolutionPoints => evolutionPoints;

		private readonly ObservableCollection<ChangelingAbility> changelingAbilities = new ObservableCollection<ChangelingAbility>();
		public ObservableCollection<ChangelingAbility> ChangelingAbilities => new(changelingAbilities);

		public ObservableCollection<ChangelingAbility> AbilitiesNow => changelingAbilities;
		private List<ChangelingBaseAbility> AbilitiesNowDataSynced = new List<ChangelingBaseAbility>();
		public List<ChangelingBaseAbility> AbilitiesNowData
		{
			get
			{
				return AbilitiesNowDataSynced;
			}
		}
		public List<ChangelingBaseAbility> AllAbilities => ChangelingAbilityList.Instance.Abilites;

		[SyncVar(hook = nameof(SyncAbilityList))]
		private string abilitesIDSNow = "";

		private static Dictionary<uint, ChangelingMain> changelingByMindID = new();
		public static Dictionary<uint, ChangelingMain> ChangelingByMindID => new(changelingByMindID);
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

		// Amount of chemical added per time
		private const float CHEM_ADD_PER_TIME = 1;
		// Amount of time needed for add chemical
		private const float CHEM_ADD_TIME_BASE = 2;
		// Amount of time needed for add chemical (Adds to base amount for every activated ability with isSlowingChemRegeneration)
		private const float CHEM_ADD_TIME_BASE_SLOWED = 2;
		private const int MAX_LAST_EXTRACTED_DNA_FOR_TRANSFORM = 7;

		[SyncVar(hook = nameof(SyncChangelingDna))]
		private string changelingDNASer = "";
		[SyncVar(hook = nameof(SyncChangelingMemories))]
		private string changelingMemoriesSer = "";

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
				AbilitiesNowDataSynced = new List<ChangelingBaseAbility>();
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
				Loggy.LogError($"[ChangelingMain/SyncAbilityList]{ChangelingMind.CurrentPlayScript.playerName} can`t refresh abilities", Category.Changeling);
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
				Loggy.LogError($"[ChangelingMain/SyncChemCount]{ChangelingMind.CurrentPlayScript.playerName} can`t set up UI", Category.Changeling);
			}
		}

		private void SyncMindID(uint oldValue, uint newValue)
		{
			changelingMindID = newValue;
			try
			{
				if (PlayerManager.LocalPlayerScript != null && PlayerManager.LocalPlayerScript.Mind != null
				&& changelingMindID == PlayerManager.LocalPlayerScript.Mind.netId)
				{
					if (changelingMind == null)
						changelingMind = PlayerManager.LocalPlayerScript.Mind;
					SetChangelingUI();
				}
			}
			catch
			{
				Loggy.LogError($"[ChangelingMain/SyncMindID]{ChangelingMind.CurrentPlayScript.playerName} can`t set up UI", Category.Changeling);
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
				Loggy.LogError($"[ChangelingMain/SyncEPCount]{ChangelingMind.CurrentPlayScript.playerName} can`t set up UI", Category.Changeling);
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
			if (ResetsLeft <= 0)
				return;
			resetCount++;

			var forRemove = new List<ChangelingAbility>();
			foreach (var abil in AbilitiesNow)
			{
				if (abil.AbilityData.startAbility == false)
				{
					forRemove.Add(abil);
				}
			}
			// If we don't have to remove any ability it's a good choice to save reset
			if (forRemove.Count == 0)
			{
				resetCount--;
				return;
			}

			StringBuilder abilitesIDSNowToSer = new StringBuilder();
			foreach (var abil in forRemove)
			{
				if (changelingAbilities.Contains(abil))
				{
					evolutionPoints += abil.AbilityData.AbilityEPCost;
					if (abil.AbilityData is ChangelingToggleAbility toggleAbility && abil.AbilityData.IsAimable == false && abil.IsToggled == true)
					{
						toggleAbility.UseAbilityToggleClient(this, false);
					}

					changelingAbilities.Remove(abil);
				}
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

		public void HasFakingDeath(bool set)
		{
			isFakingDeath = set;
		}

		public void ResetAbilities()
		{
			// toggle off abilities when player resets them
			foreach (var abil in AbilitiesNow)
			{
				if (abil.AbilityData is ChangelingToggleAbility toggleAbility &&
				abil.AbilityData.startAbility == false && abil.IsToggled && !abil.AbilityData.IsAimable)
				{
					toggleAbility.UseAbilityToggleClient(this, false);
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
			var mindId = changelingMind.netId;
			if (changelingByMindID.ContainsKey(mindId))
				changelingByMindID.Remove(mindId);
			if (changelingMinds.ContainsKey(mindId))
				changelingMinds.Remove(mindId);

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Tick);
		}

		public bool HasDna(ChangelingDna dna)
		{
			return ChangelingDnas.Contains(dna);
		}

		public void AddDna(ChangelingDna dna)
		{
			var stringBuildes = new StringBuilder();
			foreach (var x in ChangelingDnas)
			{
				if (x.DnaID == dna.DnaID)
				{
					RemoveDna(new List<ChangelingDna>() {x});
					break;
				}
			}

			stringBuildes.Append(changelingDNASer);
			if (!HasDna(dna))
			{
				changelingDnas.Add(dna);
				stringBuildes.AppendLine(JsonConvert.SerializeObject(dna));
			}
			SyncChangelingDna(changelingDNASer, stringBuildes.ToString());
		}

		private void OnCrit()
		{
			if (isFakingDeath)
				return;
			foreach (var abil in AbilitiesNow)
			{
				if (abil.AbilityData is ChangelingToggleAbility toggleAbility &&
				toggleAbility.SwithedToOnWhenInCrit)
				{
					abil.ForceToggleToState(true);
				}
			}
		}

		private void OnExitCrit()
		{
			if (isFakingDeath)
				return;
			foreach (var abil in AbilitiesNow)
			{
				if (abil.AbilityData is ChangelingToggleAbility toggleAbility &&
				toggleAbility.SwithedToOffWhenExitCrit)
				{
					abil.ForceToggleToState(false);
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
			SyncChangelingMemories(changelingMemoriesSer, changelingMemoriesToSer.ToString());
		}

		private void AddMemories(ChangelingMemories memories)
		{
			changelingMemories.Add(memories);

			var changelingMemoriesToSer = new StringBuilder();
			changelingMemoriesToSer.Append(changelingMemoriesSer);

			changelingMemoriesToSer.AppendLine(JsonConvert.SerializeObject(memories));

			SyncChangelingMemories(changelingMemoriesSer, changelingMemoriesToSer.ToString());
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

			SyncChangelingMemories(changelingMemoriesSer, "");
		}

		public void AbsorbDna(PlayerScript target, ChangelingMain changelingMain) // That gonna be another changeling
		{
			foreach (var dna in changelingMain.ChangelingDnas)
			{
				AddDna(dna);
			}
			AddMemories(changelingMain.ChangelingMemories);
			changelingMain.RemoveDna(changelingMain.ChangelingDnas);
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
				builder.AppendLine(JsonConvert.SerializeObject(dna));
			}
			SyncChangelingDna(changelingDNASer, builder.ToString());
		}

		private void Tick()
		{
			if (CustomNetworkManager.IsServer)
			{
				tickTimer++;
				if (chem < 0)
					chem = 0;

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
				if (abil.AbilityData is ChangelingToggleAbility toggleAbility && abil.IsToggled)
				{
					if (toggleAbility.IsSlowingChemRegeneration)
						slowingCount++;
					if (toggleAbility.IsStopingChemRegeneration)
					{
						chemAddPerTime = 0;
						return;
					}
				}
			}

			chemAddPerTime = CHEM_ADD_PER_TIME;
			chemAddTime = CHEM_ADD_TIME_BASE + CHEM_ADD_TIME_BASE_SLOWED * slowingCount;
		}

		public void Init(Mind changelingMindUser)
		{
			changelingMind = changelingMindUser;

			var playerScript = changelingMindUser.CurrentPlayScript;


			if (!CustomNetworkManager.IsServer) return;

			changelingAbilities.CollectionChanged += (_, e) =>
			{
				if (e == null)
				{
					return;
				}

				if (e.NewItems != null)
				{
					foreach (ChangelingAbility x in e.NewItems)
					{
						if (x.AbilityData.ShowInActions)
							UIActionManager.ToggleServer(changelingMind.gameObject, x, true);
					}
				}

				if (e.OldItems != null)
				{
					foreach (ChangelingAbility y in e.OldItems)
					{
						UIActionManager.ToggleServer(changelingMind.gameObject, y, false);
					}
				}
			};

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
			AddMemories(dnaObject, changelingMind.Body);
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

		public void AddAbility(ChangelingBaseAbility ability)
		{
			if (evolutionPoints - ability.AbilityEPCost < 0)
				return;

			var abilityInst = ability.AddToPlayer(changelingMind);
			if (changelingAbilities.Contains(abilityInst))
			{
				return;
			}
			evolutionPoints -= ability.AbilityEPCost;
			abilitesIDSNow += $"\n{ability.Index}";

			changelingAbilities.Add(abilityInst);
		}

		public void RemoveAbility(ChangelingBaseAbility ability)
		{
			abilitesIDSNow = "";
			if (CustomNetworkManager.IsServer)
				evolutionPoints += ability.AbilityEPCost;

			var abilityInst = ability.AddToPlayer(changelingMind);
			if (!changelingAbilities.Contains(abilityInst))
			{
				return;
			}
			changelingAbilities.Remove(abilityInst);

			foreach (var x in AbilitiesNow)
			{
				abilitesIDSNow = $"\n{x.AbilityData.Index}";
			}
		}

		public bool HasAbility(ChangelingBaseAbility ability)
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

		public void UseAbility(ChangelingBaseAbility changelingAbility)
		{
			if (HasAbility(changelingAbility))
				chem -= changelingAbility.AbilityChemCost;
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