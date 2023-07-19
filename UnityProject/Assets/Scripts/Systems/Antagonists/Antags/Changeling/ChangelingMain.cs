using Chemistry.Components;
using Clothing;
using GameModes;
using HealthV2;
using Items;
using Items.Implants.Organs;
using Items.Others;
using Items.PDA;
using Mirror;
using Newtonsoft.Json;
using Objects.Atmospherics;
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
using UI;
using UI.Action;
using UI.CharacterCreator;
using UI.Core.Action;
using UnityEngine;
using UnityEngine.Events;
using Util;

namespace Changeling
{
	[DisallowMultipleComponent]
	public class ChangelingMain : NetworkBehaviour, IServerActionGUIMulti
	{
		[Header("Changeling main")]
		[SyncVar (hook = nameof(SyncChemCount))] private float chem = 25;
		public float Chem => chem;
		[SyncVar(hook = nameof(SyncMindID))] private uint changelingMindID = 0;
		[SyncVar] private float chemMax = 75;
		[SyncVar] private float chemAddPerTime = CHEM_ADD_PER_TIME;
		[SyncVar] private float chemAddTime = CHEM_ADD_TIME_BASE;
		public int ExtractedDNA => changelingDNAs.Count;

		[SyncVar] private int resetCount = 0;
		[SyncVar] private int resetCountMax = 1;

		[SyncVar] private int absorbCount = 0;
		public int AbsorbCount => absorbCount;
		public int ResetsLeft => resetCountMax - resetCount;

		[SyncVar] private List<ChangelingDNA> changelingDNAs = new List<ChangelingDNA>();
		[SyncVar] private List<ChangelingMemories> changelingMemories = new List<ChangelingMemories>();
		public List<ChangelingMemories> ChangelingMemories => changelingMemories;

		public bool isFakingDeath = false;

		public ChangelingDNA currentDNA;

		public List<ChangelingDNA> ChangelingDNAs => new List<ChangelingDNA>(changelingDNAs);
		public List<ChangelingDNA> ChangelingLastDNAs
		{
			get
			{
				return ChangelingDNAs.Skip(changelingDNAs.Count - MAX_LAST_EXTRACTED_DNA_FOR_TRANSFORM).ToList();
			}
		}
		public List<int> ChangelingDNAsID
		{
			get
			{
				var dNAsIDs = new List<int>();
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
		[SyncVar(hook = nameof(SyncEPCount))] private int evolutionPoints = 10;
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

		private UI_Changeling uiChangeling;
		public UI_Changeling Ui => uiChangeling;
		private Mind changelingMind;
		public Mind ChangelingMind => changelingMind;

		public List<ActionData> ActionData => throw new NotImplementedException();

		PlayerScript playerScript;

		private int tickTimer = 0;

		private const float CHEM_ADD_PER_TIME = 1;
		private const float CHEM_ADD_TIME_BASE = 2;
		private const float CHEM_ADD_TIME_BASE_SLOWED_ADD = 1;
		private const int MAX_LAST_EXTRACTED_DNA_FOR_TRANSFORM = 7;

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
				changelingDNAs.Add(dna);
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

		[Client]
		private void SyncChemCount(float oldValue, float newValue)
		{
			chem = newValue;

			if (changelingMindID == PlayerManager.LocalPlayerScript.Mind.netId)
			{
				SetChangelingUI();
				uiChangeling.UpdateChemText();
			}
		}

		[Client]
		private void SyncMindID(uint oldValue, uint newValue)
		{
			changelingMindID = newValue;

			if (PlayerManager.LocalPlayerScript != null && changelingMindID == PlayerManager.LocalPlayerScript.Mind.netId)
			{
				SetChangelingUI();
				changelingMind = gameObject.transform.parent.gameObject.GetComponent<PlayerScript>().Mind;
			}
		}

		[Client]
		private void SyncEPCount(int oldValue, int newValue)
		{
			evolutionPoints = newValue;

			if (changelingMindID == PlayerManager.LocalPlayerScript.Mind.netId)
			{
				SetChangelingUI();
				uiChangeling.UpdateChemText();
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
			if (!HasDna(dna))
				changelingDNAs.Add(dna);
			if (chem + 50 <= chemMax)
				chem += 50;
			else
				chem += chemMax - chem;

			absorbCount++;
			AddMemories(dna, target);
		}

		private void AddMemories(ChangelingMemories memories)
		{
			changelingMemories.Add(memories);
		}

		private void AddMemories(ChangelingDNA dna, PlayerScript target)
		{
			var targetName = target.playerName;
			var mem = new ChangelingMemories();
			var information = target.Mind.GetObjectives();
			var species = target.Mind.CurrentCharacterSettings.Species;
			var gender = target.Mind.CurrentCharacterSettings.GetGender();
			mem.Form(dna.Job, targetName, information, species, gender);
			changelingMemories.Add(mem);
		}

		private void AddMemories(PlayerScript target)
		{
			var targetName = target.playerName;
			var mem = new ChangelingMemories();
			var information = target.Mind.GetObjectives();
			var species = target.Mind.CurrentCharacterSettings.Species;
			var gender = target.Mind.CurrentCharacterSettings.GetGender();
			mem.Form(target.PlayerInfo.Job, targetName, information, species, gender);
			changelingMemories.Add(mem);
		}

		//public void AddDNA(List<ChangelingDNA> dnas) // that gonna be another changeling
		//{
		//	foreach (var dna in dnas)
		//	{
		//		if (!HasDna(dna))
		//			changelingDNAs.Add(dna);
		//	}
		//	resetCountMax++;
		//	chemMax += 50;
		//	chem += 50;
		//	evolutionPoints += 5;
		//}

		public void AbsorbDNA(List<ChangelingDNA> dnas, PlayerScript target, ChangelingMain changelingMain) // That gonna be another changeling
		{
			foreach (var dna in dnas)
			{
				if (!HasDna(dna))
					changelingDNAs.Add(dna);
			}
			changelingMemories.AddRange(changelingMain.changelingMemories);
			changelingMain.changelingMemories.Clear();
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

			chemAddPerTime = CHEM_ADD_PER_TIME;
			chemAddTime = CHEM_ADD_TIME_BASE + CHEM_ADD_TIME_BASE_SLOWED_ADD * slowingCount;
		}

		public void Init(Mind changelingMindUser)
		{
			changelingMind = changelingMindUser;

			playerScript = changelingMindUser.CurrentPlayScript;
			playerScript.OnActionControlPlayer += PlayerEnterBody;

			SetUpAbilites();

			if (!CustomNetworkManager.IsServer) return;

			UpdateManager.Add(Tick, 1f);
			StartCoroutine(LateInit());

			playerScript.playerHealth.OnCrit.AddListener(OnCrit);
			playerScript.playerHealth.OnCritExit.AddListener(OnExitCrit);
		}

		//private void Start()
		//{
		//	StartCoroutine(LateUIInit());
		//}

		private IEnumerator LateInit() // need to be done after spawn because not all player systems was loaded at init moment
		{
			var inited = false;
			while (!inited)
			{
				yield return WaitFor.SecondsRealtime(1f);
				try
				{
					inited = true;
					changelingMindID = changelingMind.netId;
					var dnaObject = Spawn.ServerPrefab(ChangelingAbilityList.Instance.DNAPrefab, parent: gameObject.transform).GameObject.GetComponent<ChangelingDNA>();
					dnaObject.FormDNA(changelingMind.Body.PlayerInfo.Script, this);

					AddDNA(dnaObject);
					currentDNA = dnaObject;
				}
				catch
				{

				}
			}
		}

		//private IEnumerator LateUIInit() // need to be done after spawn because not all player systems was loaded at init moment
		//{
		//	yield return WaitFor.SecondsRealtime(1f);

		//	if (PlayerManager.LocalMindScript.Body.TryGetComponent<ChangelingMain>(out _))
		//	{
		//		ui = UIManager.Display.hudChangeling;
		//		ToggleHUD();
		//	}
		//}

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

		public void PlayerEnterBody()
		{
			//StartCoroutine(LateUIInit());
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
				chem -= changelingAbility.AbilityData.AbilityChemCost;
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
}