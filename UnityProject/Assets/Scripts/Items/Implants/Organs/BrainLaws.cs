using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mirror;
using Newtonsoft.Json;
using Objects;
using Objects.Research;
using Systems.Ai;
using UI.Core;
using UI.Core.Action;
using UI.Systems.MainHUD.UI_Bottom;
using UnityEngine;

public class BrainLaws : NetworkBehaviour, IActionGUI, IClientInteractable<HandActivate>
{


	public AiPlayer AiPlayer; //TODO refactor at some point

	public ActionData ActionDataLawDisplay;

	public ActionData ActionData => ActionDataLawDisplay;

	// 	Law priority order is this:
	//	0: Traitor/Malf/Onehuman-board Law
	//  ##?$-##: HACKED LAW ##!£//#
	//  ##!£//#: Ion Storm Law ##?$-##
	//	Law 1: First Law
	//	Law 2: Second Law
	//	Law 3: Third Law
	//	Law 4: Freeform
	//	Higher laws (the law above each one) override all lower ones. Whether numbered or not, how they appear (in order) is the order of priority.

	//Is sync'd manually to owner client so is accurate on owner client
	//Tried to use sync dictionary from mirror but didnt work correctly, wouldnt sync the values correctly
	private Dictionary<AiPlayer.LawOrder, List<string>> aiLaws = new Dictionary<AiPlayer.LawOrder, List<string>>();
	public Dictionary<AiPlayer.LawOrder, List<string>> AiLaws => aiLaws;

	private LightingSystem lightingSystem;

	[SyncVar(hook = nameof(SynchronisedUpdate))]
	private string SynchronisedLaws;

	//Clientside only
	private UI_Ai aiUi;

	[SerializeField] private List<AiLawSet> defaultLawSets = new List<AiLawSet>();

	private RegisterTile RegisterTile;

	private void Awake()
	{
		RegisterTile = this.GetComponent<RegisterTile>();
		if (aiUi == null)
		{
			aiUi = UIManager.Instance.displayControl.hudBottomAi.GetComponent<UI_Ai>();
		}

		if (lightingSystem == null)
		{
			lightingSystem = Camera.main.GetComponent<LightingSystem>();
		}
	}

	#region AILinking

	public bool Interact(HandActivate interaction)
	{
		if (interaction.IsAltClick == false) return false;
		var Choosing = 	new List<DynamicUIChoiceEntryData>()
		{
			new DynamicUIChoiceEntryData()
			{
				ChoiceAction = () =>
				{
					CommandLinkToAI(-1);
				},
				Text = " No AI. "
			}
		};

		for (var index = 0; index < RegisterTile.Matrix.PresentPlayers.Count; index++)
		{
			var Player = RegisterTile.Matrix.PresentPlayers[index];
			if (Player.TryGetComponent<AiPlayer>(
				    out var AiPlayer)) //TODO Think of some way to network this Since it's not always going to be AiPlayer
			{
				var thisindex = index;
				Choosing.Add(new DynamicUIChoiceEntryData()
					{
						ChoiceAction = () =>
						{

							CommandLinkToAI(thisindex);
						},
						Text = AiPlayer.name
					}
				);
			}
		}

		DynamicChoiceUI.ClientDisplayChoicesNotNetworked("Choose linked AI for Brain ",
			" Choose whichever you would like to link this brain to ", Choosing);
		return true;

	}


	[Command(requiresAuthority = false)]
	public void CommandLinkToAI(int AIIndex, NetworkConnectionToClient sender = null)
	{
		if (sender == null) return;
		if (Validations.CanApply(PlayerList.Instance.Get(sender).Script, this.gameObject, NetworkSide.Server, false, ReachRange.Standard) == false) return;

		if (AIIndex == -1)
		{
			LinkToAI(null);
		}

		if (RegisterTile.Matrix.PresentPlayers.Count > AIIndex)
		{
			var AI = RegisterTile.Matrix.PresentPlayers[AIIndex].GetComponent<AiPlayer>();
			if (AI != null)
			{
				LinkToAI(AI);
			}


		}
	}


	#endregion


	public void LinkToAI(AiPlayer _AiPlayer)
	{
		if (AiPlayer != null)
		{
			if (AiPlayer.LinkedCyborgs.Contains(this))
			{
				AiPlayer.LinkedCyborgs.Remove(this);
			}
		}

		AiPlayer = _AiPlayer;
		AiPlayer.OrNull()?.LinkedCyborgs?.Add(this);
	}


	private void Start()
	{
		if (CustomNetworkManager.IsServer == false) return;

		//TODO beam new AI message, play sound too?

		//Set up laws
		SetRandomDefaultLawSet();
		UIActionManager.ToggleServer(this.gameObject,this, true);
	}

	public void CallActionClient()
	{
		AILawsTabUI.Instance.OpenLaws();
	}


	public void OnDestroy()
	{
		if (AiPlayer != null)
		{
			if (AiPlayer.LinkedCyborgs.Contains(this))
			{
				AiPlayer.LinkedCyborgs.Remove(this);
			}
		}
	}

	[Server]
	public void UploadLawModule(HandApply interaction, bool isUploadConsole = false)
	{
		//Must have used module, but do check in case
		if (interaction.HandObject.TryGetComponent<AiLawModule>(out var module) == false)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer,
				$"Can only use a module on this {(isUploadConsole ? "console" : "core")}");
			return;
		}

		Dictionary<AiPlayer.LawOrder, List<string>> lawFromModule =
			module.GetLawsFromModule(interaction.PerformerPlayerScript);

		if (module.AiModuleType == AiModuleType.Purge || module.AiModuleType == AiModuleType.Reset)
		{
			var isPurge = module.AiModuleType == AiModuleType.Purge;
			ResetLaws(isPurge);
			Chat.AddActionMsgToChat(interaction.Performer,
				$"You {(isPurge ? "purge" : "reset")} all of {gameObject.ExpensiveName()}'s laws",
				$"{interaction.Performer.ExpensiveName()} {(isPurge ? "purges" : "resets")} all of {gameObject.ExpensiveName()}'s laws");
			return;
		}

		if (lawFromModule.Count == 0)
		{
			Chat.AddExamineMsgFromServer(interaction.Performer, "No laws to upload");
			return;
		}

		//If we are only adding core laws then we must mean to remove old core laws
		//This means we are assuming that the law set must only have core laws if it is to replace the old laws fully
		var notOnlyCoreLaws = false;

		foreach (var law in lawFromModule)
		{
			if (law.Key == AiPlayer.LawOrder.Core) continue;
			notOnlyCoreLaws = true;
			break;
		}

		SetLaws(lawFromModule, true, notOnlyCoreLaws);

		Chat.AddActionMsgToChat(interaction.Performer, $"You change {gameObject.ExpensiveName()} laws",
			$"{interaction.Performer.ExpensiveName()} changes {gameObject.ExpensiveName()} laws");
	}

	//Add one law
	//Wont allow more than one traitor law
	[Server]
	public void AddLaw(string newLaw, AiPlayer.LawOrder order, bool init = false)
	{
		if (aiLaws.ContainsKey(order) == false)
		{
			aiLaws.Add(order, new List<string>() {newLaw});
		}
		else
		{
			if (order == AiPlayer.LawOrder.Traitor && aiLaws[order].Count > 0)
			{
				//Can only have one traitor law
				return;
			}

			if (aiLaws[order].Contains(newLaw))
			{
				//Cant add the same law with the same string more than once
				return;
			}

			aiLaws[order].Add(newLaw);
		}

		ServerUpdateClientLaws();
	}

	//Set a new list of laws, used mainly to set new core laws, can remove core laws if parameter set true
	//Wont replace hacked. ion laws and freeform can be replaced if parameters set to false
	[Server]
	public void SetLaws(Dictionary<AiPlayer.LawOrder, List<string>> newLaws, bool keepIonLaws = true,
		bool keepCoreLaws = false,
		bool keepFreeform = true)
	{
		foreach (var lawGroups in aiLaws)
		{
			if (lawGroups.Key == AiPlayer.LawOrder.Traitor)
			{
				TryAddLaw(AiPlayer.LawOrder.Traitor);
			}

			if (keepIonLaws && lawGroups.Key == AiPlayer.LawOrder.Hacked)
			{
				TryAddLaw(AiPlayer.LawOrder.Hacked);
			}

			if (keepIonLaws && lawGroups.Key == AiPlayer.LawOrder.IonStorm)
			{
				TryAddLaw(AiPlayer.LawOrder.IonStorm);
			}

			if (keepCoreLaws && lawGroups.Key == AiPlayer.LawOrder.Core)
			{
				TryAddLaw(AiPlayer.LawOrder.Core);
			}

			if (keepFreeform && lawGroups.Key == AiPlayer.LawOrder.Freeform)
			{
				TryAddLaw(AiPlayer.LawOrder.Freeform);
			}

			void TryAddLaw(AiPlayer.LawOrder order)
			{
				if (newLaws.ContainsKey(order) == false)
				{
					newLaws.Add(order, lawGroups.Value);
				}
				else
				{
					if (order == AiPlayer.LawOrder.Traitor && newLaws[order].Count > 0)
					{
						//Can only have one traitor law
						return;
					}

					var lawsInNewGroup = lawGroups.Value;

					//Only allow unique laws, dont allow multiple of the same law
					foreach (var lawToAdd in lawGroups.Value)
					{
						if (newLaws[order].Contains(lawToAdd))
						{
							lawsInNewGroup.Remove(lawToAdd);
						}
					}

					newLaws[order].AddRange(lawsInNewGroup);
				}
			}
		}

		aiLaws = newLaws;

		Chat.AddExamineMsgFromServer(gameObject, "Your Laws Have Been Updated!");

		//Tell player to open law screen so they dont miss that their laws have changed
		ServerUpdateClientLaws();
	}

	//Removes all laws except core and traitor, unless is purge then will remove core as well
	[Server]
	public void ResetLaws(bool isPurge = false)
	{
		var lawsToRemove = new Dictionary<AiPlayer.LawOrder, List<string>>();

		foreach (var law in aiLaws)
		{
			if ((isPurge == false && law.Key == AiPlayer.LawOrder.Core) ||
			    law.Key == AiPlayer.LawOrder.Traitor) continue;

			lawsToRemove.Add(law.Key, law.Value);
		}

		foreach (var law in lawsToRemove)
		{
			aiLaws.Remove(law.Key);
		}

		Chat.AddExamineMsgFromServer(gameObject, "Your Laws Have Been Updated!");

		ServerUpdateClientLaws();
	}

	public string GetLawsString()
	{
		var lawString = new StringBuilder();

		foreach (var law in GetLaws())
		{
			lawString.AppendLine(law);
		}

		return lawString.ToString();
	}

	//Valid server and client side
	//Gets list of laws with numbering correct
	public List<string> GetLaws()
	{
		var lawsToReturn = new List<string>();

		//Order laws by their enum value
		// 0 laws first, freeform last
		var laws = AiLaws.OrderBy(x => x.Key);

		var count = 1;
		var number = "";

		foreach (var lawGroup in laws)
		{
			if (lawGroup.Key == AiPlayer.LawOrder.Traitor)
			{
				number = "0. ";
			}
			else if (lawGroup.Key == AiPlayer.LawOrder.Hacked)
			{
				number = "@#$# ";
			}
			else if (lawGroup.Key == AiPlayer.LawOrder.IonStorm)
			{
				number = "@#!# ";
			}

			for (int i = 0; i < lawGroup.Value.Count; i++)
			{
				if (lawGroup.Key == AiPlayer.LawOrder.Core || lawGroup.Key == AiPlayer.LawOrder.Freeform)
				{
					number = $"{count}. ";
					count++;
				}

				lawsToReturn.Add(number + lawGroup.Value[i]);
			}
		}

		return lawsToReturn;
	}

	[Server]
	private void ServerUpdateClientLaws()
	{
		var data = new List<AiPlayer.LawSyncData>();

		foreach (var lawGroup in aiLaws)
		{
			data.Add(new AiPlayer.LawSyncData()
			{
				LawOrder = lawGroup.Key,
				Laws = lawGroup.Value.ToArray()
			});
		}



		SynchronisedUpdate(SynchronisedLaws, JsonConvert.SerializeObject(data));
	}


	//Force a law update on player and makes player open law screen

	private void SynchronisedUpdate(string OldData  , string newDataString)
	{
		SynchronisedLaws = newDataString;

		if (isServer) return;
		aiLaws.Clear();
		var newData = JsonConvert.DeserializeObject<List<AiPlayer.LawSyncData>>(newDataString);

		foreach (var lawData in newData)
		{
			aiLaws.Add(lawData.LawOrder, lawData.Laws.ToList());
		}

		if (isOwned)
		{
			AILawsTabUI.Instance.OpenLaws();
		}
	}

	[Command]
	private void CmdAskForLawUpdate()
	{
		ServerUpdateClientLaws();
	}

	[ContextMenu("randomise laws")]
	public void SetRandomDefaultLawSet()
	{
		aiLaws.Clear();
		var pickedLawSet = defaultLawSets.PickRandom();
		foreach (var law in pickedLawSet.Laws)
		{
			AddLaw(law.Law, law.LawOrder, true);
		}
	}

	[ContextMenu("debug log laws")]
	public void DebugLogLaws()
	{
		foreach (var law in GetLaws())
		{
			Debug.LogError(law);
		}
	}
}