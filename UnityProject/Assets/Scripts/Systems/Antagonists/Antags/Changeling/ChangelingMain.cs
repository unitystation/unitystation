using Items.Implants.Organs;
using Mirror;
using ScriptableObjects.Systems.Spells;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Systems.Spells;
using UI;
using UI.Core.Action;
using UnityEngine;

namespace Changeling
{
	//AlienPlayer
	public class ChangelingMain : NetworkBehaviour, IAdminInfo, IOnPlayerPossess,
			IOnPlayerRejoin, IOnPlayerLeaveBody, IServerActionGUIMulti
	{
		// TODO remove SerializeField from all bruh
		[Header("Changeling main")]
		[SyncVar] [SerializeField] private float chem = 0;
		public float Chem => chem;
		[SyncVar] [SerializeField] private float chemMax = 100;
		[SerializeField] private float chemAddPerTime = 1;
		[SerializeField] private float chemAddTime = 5;
		[SerializeField] public int ExtractedDNA => changelingDNAs.Count;
		[SyncVar] [SerializeField] private int maxExtractedDNA = 7;

		// TODO need to be SyncVar I think or not...
		[SerializeField] private List<PlayerHealthData> changelingDNAs = new List<PlayerHealthData>();
		// ep - evolution point
		[SyncVar] [SerializeField] private int epPoints = 10;
		public int EpPoints => epPoints;
		[SerializeField] private List<ChangelingData> abilitiesToBuy = new List<ChangelingData>();
		[SerializeField] public List<ChangelingData> AbilitiesToBuy => abilitiesToBuy;
		[SerializeField] private ObservableCollection<ChangelingAbility> abilitiesNow = new ObservableCollection<ChangelingAbility>();
		[SerializeField] public ObservableCollection<ChangelingAbility> AbilitiesNow => abilitiesNow;
		[SerializeField] private UI_Changeling ui;
		public UI_Changeling Ui => ui;
		[SerializeField] private Mind changelingMind;
		public Mind ChangelingMind => changelingMind;

		public List<ActionData> ActionData => throw new NotImplementedException();

		PlayerScript playerScript;

		[SerializeField] private int tickTimer = 0;

		public List<ChangelingData> GetAbilitesForBuy()
		{
			List<ChangelingData> changelingAbilityBases = new List<ChangelingData>();

			foreach (var x in abilitiesToBuy)
			{
				//if (!abilitiesNow.Contains(x))
				//{
				changelingAbilityBases.Add(x);
				//}
			}

			return changelingAbilityBases;
		}

		public List<ChangelingData> GetAbilitesBuyed()
		{
			List<ChangelingData> changelingAbilityBases = new List<ChangelingData>();

			foreach (var x in abilitiesNow)
			{
				changelingAbilityBases.Add(x.AbilityData);
			}

			return changelingAbilityBases;
		}

		public bool BuyAbility(ChangelingData changelingAbility)
		{
			//abilitiesNow.Add(changelingAbility.AddToPlayer(changelingMind));
			return true;
		}

		void Awake()
		{
			//playerScript = GetComponent<PlayerScript>();

			//changelingMind = playerScript.Mind;
			chem = 0;
		}

		void Tick()
		{
			if (CustomNetworkManager.IsServer)
			{
				tickTimer++;

				if (tickTimer >= chemAddTime)
				{
					if (chem + chemAddPerTime < chemMax)
						chem += chemAddPerTime;
					tickTimer = 0;
				}
			}
		}

		public void Init(Mind changelingMindUser)
		{
			ui = UIManager.Display.hudChangeling.GetComponent<UI_Changeling>();

			UpdateManager.Add(Tick, 1f);

			changelingMind = changelingMindUser;

			playerScript = changelingMindUser.gameObject.GetComponent<PlayerScript>();
			playerScript.OnActionControlPlayer += PlayerEnterBody;

			//abilitiesNow.CollectionChanged += (sender, e) =>
			//{
			//	if (e == null)
			//	{
			//		return;
			//	}

			//	if (e.NewItems != null)
			//	{
			//		foreach (ChangelingAbility x in e.NewItems)
			//		{
			//			UIActionManager.ToggleServer(this.gameObject, x, true);
			//		}
			//	}

			//	if (e.OldItems != null)
			//	{
			//		foreach (ChangelingAbility y in e.OldItems)
			//		{
			//			UIActionManager.ToggleServer(this.gameObject, y, false);
			//		}
			//	}
			//};

			SetUpAbilites();

			if (!CustomNetworkManager.IsServer) return;
		}

		private void SetUpAbilites()
		{
			//abilitiesNow

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
			changelingMind.AddAbility(ability.AddToPlayer(changelingMind));
		}

		public void RemoveAbility(ChangelingData ability)
		{
			changelingMind.RemoveAbility(ability.AddToPlayer(changelingMind));
		}

		public void AddAbilityToStore(ChangelingData ability)
		{
			if (!abilitiesToBuy.Contains(ability))
			{
				abilitiesToBuy.Add(ability);
			}
		}

		[SerializeField] public SpellData spell;
		[ContextMenu("TEST")]
		private void TEST()
		{
			changelingMind.AddSpell(spell.AddToPlayer(changelingMind));
			//abilitiesNow.Add(Instantiate(abilitiesNow[0]));
			//GetComponent<BodyPartSprites>().UpdateData("{\"Name\":\"Evan Sutton\",\"AiName\":\"R.O.B.O.T.\",\"BodyType\":0,\"ClothingStyle\":0,\"BagStyle\":3,\"PlayerPronoun\":3,\"Age\":21,\"Speech\":0,\"SkinTone\":\"#C58C85\",\"SerialisedBodyPartCustom\":[],\"SerialisedExternalCustom\":[{\"Key\":\"PlayerUnderShirt\",\"SerialisedValue\":{\"SelectedName\":\"Sports Bra\",\"Colour\":\"#FFFFFF\"}},{\"Key\":\"PlayerUnderWear\",\"SerialisedValue\":{\"SelectedName\":\"Men's Grey Boxer\",\"Colour\":\"#FFFFFF\"}},{\"Key\":\"PlayerSocks\",\"SerialisedValue\":{\"SelectedName\":\"Knee-high (White)\",\"Colour\":\"#FFFFFF\"}}],\"Species\":\"Cow\",\"JobPreferences\":{},\"AntagPreferences\":{}}");
		}

		[SerializeField] public ChangelingData abil;
		[ContextMenu("TEST2")]
		private void TEST2()
		{
			changelingMind.AddAbility(abil.AddToPlayer(changelingMind));
			//AddAbility(abil.AddToPlayer(changelingMind));
			//abilitiesNow.Add(Instantiate(abilitiesNow[0]));
			//GetComponent<BodyPartSprites>().UpdateData("{\"Name\":\"Evan Sutton\",\"AiName\":\"R.O.B.O.T.\",\"BodyType\":0,\"ClothingStyle\":0,\"BagStyle\":3,\"PlayerPronoun\":3,\"Age\":21,\"Speech\":0,\"SkinTone\":\"#C58C85\",\"SerialisedBodyPartCustom\":[],\"SerialisedExternalCustom\":[{\"Key\":\"PlayerUnderShirt\",\"SerialisedValue\":{\"SelectedName\":\"Sports Bra\",\"Colour\":\"#FFFFFF\"}},{\"Key\":\"PlayerUnderWear\",\"SerialisedValue\":{\"SelectedName\":\"Men's Grey Boxer\",\"Colour\":\"#FFFFFF\"}},{\"Key\":\"PlayerSocks\",\"SerialisedValue\":{\"SelectedName\":\"Knee-high (White)\",\"Colour\":\"#FFFFFF\"}}],\"Species\":\"Cow\",\"JobPreferences\":{},\"AntagPreferences\":{}}");
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

		private void SetUpActions()
		{
			UIActionManager.Instance.MultiIActionGUIToMind.Add(this, this.gameObject);
			foreach (var action in abilitiesNow)
			{
				UIActionManager.ToggleMultiServer(gameObject, this, action.AbilityData, true);
			}
		}

		public string AdminInfoString()
		{
			var adminInfo = new StringBuilder();

			adminInfo.AppendLine($"Chemicals: {chem}");
			adminInfo.AppendLine($"Evolution points: {epPoints}");
			//adminInfo.AppendLine($"Chem: {chem}");
			//adminInfo.AppendLine($"Health: {health}%");
			//adminInfo.AppendLine($"Victory: {numOfNonSpaceBlobTiles / numOfTilesForVictory}%");

			return adminInfo.ToString();
		}

		public void PlayerEnterBody()
		{
			SetUpHUD();
			//SetUpActions();
			//SetUpHUD();
		}

		public void OnServerPlayerPossess(Mind mind)
		{

		}

		public void OnPlayerRejoin(Mind mind)
		{
			//SetUpHUD();
		}

		public void OnPlayerLeaveBody(PlayerInfo account)
		{
			//SetUpHUD(false);
		}

		public void CallActionServer(ActionData data, PlayerInfo playerInfo)
		{
			
		}

		public void CallActionClient(ActionData data)
		{
			
		}

		public bool HasAbility(ChangelingAbility ability)
		{
			return abilitiesNow.Contains(ability);
		}
	}
}