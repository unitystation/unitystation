using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AdminCommands;
using UnityEngine;
using Mirror;
using Antagonists;
using Systems.Character;
using Systems.Spells;
using HealthV2;
using Initialisation;
using Items.PDA;
using Messages.Server;
using ScriptableObjects.Audio;
using ScriptableObjects.Systems.Spells;
using Systems.Antagonists.Antags;
using UI.Core.Action;
using Changeling;
using Logs;
using static UniversalObjectPhysics;

/// <summary>
/// IC character information (job role, antag info, real name, etc). A body and their ghost link to the same mind
/// SERVER SIDE VALID ONLY, is not sync'd
/// </summary>
public class Mind : NetworkBehaviour, IActionGUI
{
	[SyncVar(hook = nameof(SyncActiveOn))] private uint IDActivelyControlling;

	[SyncVar] public bool IsGhosting;

	[SyncVar(hook = nameof(SyncPossessing))]
	private uint IDPossessing;

	//TODO ondatesiss

	//Antag
	[SyncVar] private bool NetworkedisAntag;

	//Type of Antagonist
	[field: SyncVar] public JobType NetworkedAntagJob { get; private set; }

	public GameObject ControllingObject
	{
		get
		{
			if (IsGhosting) return this.gameObject;
			if (IDActivelyControlling is NetId.Invalid or NetId.Empty) return this.gameObject;

			return CustomNetworkManager.Spawned[IDActivelyControlling].gameObject;
		}
	}

	public GameObject PossessingObject
	{
		get
		{
			var Spawned = SweetExtensions.GetSpawned();
			if (Spawned.TryGetValue(IDPossessing, out var Returning))
			{
				return Returning.gameObject;
			}

			return null;
		}
	}

	public IPlayerPossessable PlayerPossessable
	{
		get
		{
			return PossessingObject.OrNull()?.GetComponent<IPlayerPossessable>();
			//TODO Looking to optimising some time
		}
	}

	public Occupation occupation;

	public PlayerScript ghost { private set; get; }
	public PlayerScript Body => GetDeepestBody()?.GetComponent<PlayerScript>();
	private SpawnedAntag antag;

	public bool IsAntag => CustomNetworkManager.IsServer ? antag != null : NetworkedisAntag;


	public bool DenyCloning;
	public int bodyMobID;
	public FloorSounds StepSound; //Why is this on the mind!!!, Should be on the body
	public FloorSounds SecondaryStepSound;

	private string pdaUplinkCode = "";

	// Current way to check if it's not actually a ghost but a spectator, should set this not have it be the below.

	public PlayerInfo ControlledBy;


	[SyncVar] public CharacterSheet CurrentCharacterSettings;

	public PlayerScript CurrentPlayScript => IsGhosting ? ghost : Body;

	public bool IsSpectator => PossessingObject == null;

	public bool ghostLocked;

	private ObservableCollection<Spell> spells = new ObservableCollection<Spell>();
	public ObservableCollection<Spell> Spells => spells;

	/// <summary>
	/// General purpose properties storage for misc stuff like job-specific flags
	/// </summary>
	private Dictionary<string, object> properties = new Dictionary<string, object>();


	[SyncVar] public bool NonImportantMind = false;

	public bool IsMute
	{
		get
		{
			if (IsMiming) return true;
			var Health = GetDeepestBody().GetComponent<LivingHealthMasterBase>();
			//TODO Problem here what about if you're in an Mech, you should be able to speak but Mech Doesn't have voice?

			if (Health != null)
			{
				return Health.IsMute;
			}

			return IsMiming;
		}
	}


	public bool IsMiming
	{
		get => GetPropertyOrDefault("vowOfSilence", false);
		set => SetProperty("vowOfSilence", value);
	}

	private GhostMove Move;

	// use Create to create a mind.
	public void Awake()
	{
		Move = GetComponent<GhostMove>();
		// add spell to the UI bar as soon as they're added to the spell list
		spells.CollectionChanged += (sender, e) =>
		{
			if (e == null)
			{
				return;
			}

			if (e.NewItems != null)
			{
				foreach (Spell x in e.NewItems)
				{
					UIActionManager.ToggleServer(this.gameObject, x, true);
				}
			}

			if (e.OldItems != null)
			{
				foreach (Spell y in e.OldItems)
				{
					UIActionManager.ToggleServer(this.gameObject, y, false);
				}
			}
		};
	}

	public void Start()
	{
		if (NonImportantMind)
		{
			UpdateManager.Add(CheckNonImportantMind, 30f);
		}
	}

	public void OnDestroy()
	{
		if (NonImportantMind)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckNonImportantMind);
		}
	}


	[Command]
	public void CmdRequestPossess(uint ID)
	{

		if (AdminCommandsManager.IsAdmin(this.connectionToClient, out var _))
		{
			SetPossessingObject(CustomNetworkManager.Spawned[ID].gameObject);
		}


	}


	public void CheckNonImportantMind()
	{
		var Deepestbody = GetDeepestBody();
		if (Deepestbody.gameObject == this.gameObject && ControlledBy == null)
		{
			_ = Despawn.ServerSingle(this.gameObject);
		}
	}

	public void ApplyOccupation(Occupation requestedOccupation)
	{
		if (requestedOccupation == null) return;
		this.occupation = requestedOccupation;
		foreach (var spellData in occupation.Spells)
		{
			var spellScript = spellData.AddToPlayer(this);
			Spells.Add(spellScript);
		}

		foreach (var pair in occupation.CustomProperties)
		{
			SetProperty(pair.Key, pair.Value);
		}

		if (occupation.JobType == JobType.AI)
		{
			SetPermanentName(CurrentCharacterSettings.AiName ?? "H.A.L.E");
		}
	}


	public bool IsRelatedToObject(GameObject Object)
	{
		if (this.gameObject == Object)
		{
			return true;
		}

		if (this.PossessingObject == Object)
		{
			return true;
		}

		if (PlayerPossessable != null && PlayerPossessable.IsRelatedToObject(Object))
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Sets the IC name for this player and refreshes the visible name. Name will be kept if respawned.
	/// </summary>
	/// <param name="newName">The new name to give to the player.</param>
	public void SetPermanentName(string newName)
	{
		if (CurrentCharacterSettings != null)
		{
			CurrentCharacterSettings.Name = newName;
		}

		this.name = newName;
	}


	/// <summary>
	/// Make this mind a specific spawned antag
	/// </summary>
	public void SetAntag(SpawnedAntag newAntag)
	{
		antag = newAntag;
		NetworkedisAntag = newAntag != null;
		NetworkedAntagJob = newAntag.Antagonist.AntagJobType;

		ShowObjectives();
		ActivateAntagAction(NetworkedisAntag);
	}

	public void ActivateAntagAction(bool state)
	{
		UIActionManager.ToggleServer(gameObject, this, state);
	}


	public void SetPossessingObject(GameObject obj)
	{
		Ghost();
		var InPossessing = obj.OrNull()?.GetComponent<IPlayerPossessable>();
		List<NetworkIdentity> gaining = new List<NetworkIdentity>();
		if (InPossessing != null)
		{
			InPossessing.GetRelatedBodies(gaining);
		}
		else if (obj != null)
		{
			gaining.Add(obj.NetWorkIdentity());
		}

		List<NetworkIdentity> Losing = new List<NetworkIdentity>();
		if (PlayerPossessable != null)
		{
			PlayerPossessable.GetRelatedBodies(Losing);
		}
		else if (PossessingObject != null)
		{
			Losing.Add(PossessingObject.NetWorkIdentity());
		}

		HandleOwnershipChangeMulti(Losing, gaining);

		var intID = this.netId;
		if (obj != null)
		{
			intID = obj.NetId();
		}

		SyncPossessing(IDPossessing, intID);
	}

	public void InternalSetControllingObject(GameObject obj)
	{
		if (obj == this.gameObject)
		{
			IsGhosting = true;
		}
		else
		{
			IsGhosting = false;
		}

		if (PlayerPossessable != null)
		{
			PlayerPossessable.InternalOnPlayerLeave(this);
		}

		SyncActiveOn(IDActivelyControlling, obj.NetId());

		if (ControlledBy != null)
		{
			if (PlayerPossessable != null)
			{
				ControlledBy.GameObject = PlayerPossessable.GetDeepestBody().gameObject; //TODO Better system
			}
			else
			{
				ControlledBy.GameObject = PossessingObject; //TODO Better system
			}
		}
	}

	public void AddObjectiveToAntag(Objective objectiveToAdd)
	{
		//TODO : Notify the player that a new objective has been added automatically.
		var list = new List<Objective>();
		antag.Objectives.CopyTo<Objective>(list);
		list.Add(objectiveToAdd);
		antag.Objectives = list;
	}

	/// <summary>
	/// Remove the antag status from this mind
	/// </summary>
	public void RemoveAntag()
	{
		antag = null;
		NetworkedisAntag = antag != null;
		NetworkedAntagJob = JobType.NULL;

		ActivateAntagAction(NetworkedisAntag);
	}

	public GameObject GetCurrentMob()
	{
		return GetDeepestBody().gameObject;
	}

	public void SetGhost(PlayerScript newGhost)
	{
		ghost = newGhost;
	}


	[Command]
	public void CmdAGhost()
	{
		if (AdminCommandsManager.IsAdmin(connectionToClient, out _))
		{
			if (IsGhosting)
			{
				StopGhosting();
			}
			else
			{
				Ghost();
			}
		}
	}

	public void Ghost()
	{
		var Body = GetDeepestBody();
		if (CurrentPlayScript != null)
		{
			CurrentPlayScript.OnBodyUnPossesedByPlayer?.Invoke();
		}
		Move.ForcePositionClient(Body.gameObject.AssumedWorldPosServer(), Smooth: false);
		IsGhosting = true;
		InternalSetControllingObject(GetDeepestBody().gameObject);
	}

	/// <summary>
	/// Spawn the ghost for this player and tell the client to switch input / camera to it
	/// </summary>
	[Command]
	public void CmdSpawnPlayerGhost()
	{
		ServerSpawnPlayerGhost();
	}

	[Server]
	public void ServerSpawnPlayerGhost(bool skipCheck = false)
	{
		//Only force to ghost if the mind belongs in to that body
		if (skipCheck)
		{
			Ghost();
			return;
		}

		var deepest = GetDeepestBody();

		var deepestPlayer = deepest.GetComponent<PlayerScript>();

		var livingHealth = deepest.GetComponent<LivingHealthMasterBase>();

		if (livingHealth != null)
		{
			if (deepest.GetComponent<LivingHealthMasterBase>().IsDead && deepestPlayer.IsGhost == false)
			{
				Ghost();
			}
		}
		else
		{
			Ghost();
		}
	}

	/// <summary>
	/// Asks the server to let the client rejoin into a logged off character.
	/// </summary>
	///
	[Command]
	public void CmdGhostCheck() // specific check for if you want value returned
	{
		GhostEnterBody();
	}

	//TODO
	//Right clicking when just a Brain Causing errors

	[Server]
	public void GhostEnterBody()
	{
		if (IsSpectator) return;

		if (ghostLocked) return;

		StopGhosting();
	}

	public void StopGhosting()
	{
		IsGhosting = false;
		if (GetDeepestBody().netId == this.netId)
		{
			IsGhosting = true; //Basically is not able to possess anything
			return;
		}

		InternalSetControllingObject(PossessingObject.gameObject);
	}

	private void SyncActiveOn(uint oldID, uint newID)
	{
		IDActivelyControlling = newID;
		if (isClient)
		{
			LoadManager.RegisterActionDelayed(() => { HandleActiveOnChange(oldID, newID); },
				2);
		}
	}

	private void SyncPossessing(uint oldID, uint newID)
	{
		IDPossessing = newID;
		LoadManager.RegisterActionDelayed(() => { HandlePossessingChange(oldID, newID); },
			2);
		//This is to handle The game object being spawned in and data being provided before Owner message
		//s sent owner, This means the game object it's told it's in charge of is not actually in charge of Until later on in that frame is Dumb,
		//Plus this handles server player funnies with the same thing Just stretched over another frame so that's why it's 2
	}

	private void HandlePossessingChange(uint oldID, uint newID)
	{
		var spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;

		if (spawned.ContainsKey(oldID))
		{
			var oldPossessable = spawned[oldID].GetComponent<IPlayerPossessable>();
			oldPossessable?.InternalOnLosePossess();
		}

		if (spawned.ContainsKey(newID))
		{
			var Possessable = spawned[newID].GetComponent<IPlayerPossessable>();
			Possessable?.InternalOnPossessPlayer(this, null);
		}
	}


	private void HandleActiveOnChange(uint oldID, uint newID)
	{
		var spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;
		if (spawned.ContainsKey(newID) == false) return;

		if (ControlledBy != null) //TODO Remove
		{
			ControlledBy.GameObject = spawned[newID].gameObject;
		}

		var possessable = spawned[newID].GetComponent<IPlayerPossessable>();

		if (possessable != null)
		{
			possessable.InternalOnControlPlayer(this, isServer);
		}
		else
		{
			if (isServer && spawned[newID].gameObject == gameObject) //Has ghosted
			{
				PossessAndUnpossessMessage.Send(this.gameObject, gameObject, null);
			}

			//TODO For objects
		}
	}

	public void AccountLeavingMind(PlayerInfo account)
	{
		account.SetMind(null);
		// Remove account from being observer of ghost and stuff
		var relatedBodies = GetRelatedBodies();
		foreach (var body in relatedBodies)
		{
			PlayerSpawn.TransferOwnershipFromToConnection(account, body, null);
		}
	}

	public void AccountEnteringMind(PlayerInfo account)
	{
		account.SetMind(this);

		var RelatedBodies = GetRelatedBodies();
		foreach (var Body in RelatedBodies)
		{
			PlayerSpawn.TransferOwnershipFromToConnection(ControlledBy, null, Body);
		}

		UpdateMind.SendTo(ControlledBy?.Connection, this);

		if (PlayerPossessable != null)
		{
			PlayerPossessable.PlayerRejoin();
		}

		SyncPossessing(IDPossessing, IDPossessing);
		SyncActiveOn(IDActivelyControlling, IDActivelyControlling);
	}

	public void ReLog()
	{
		if (ControlledBy?.Connection == null)
		{
			Loggy.LogError("oh god!, Somehow there's no connection to client when ReLog Code has Been called");
			return;
		}

		PlayerSpawn.TransferAccountToSpawnedMind(ControlledBy, this);


	}

	public void HandleOwnershipChangeMulti(List<NetworkIdentity> Losing, List<NetworkIdentity> Gaining)
	{
		if (ControlledBy == null) return;
		foreach (var Lost in Losing)
		{
			PlayerSpawn.TransferOwnershipFromToConnection(ControlledBy, Lost, null);

			var PlayerPositionable = Lost.GetComponent<IPlayerPossessable>();
			if (PlayerPositionable != null)
			{
				PlayerPositionable.InternalOnLosePossess();
			}
		}

		foreach (var Gained in Gaining)
		{
			PlayerSpawn.TransferOwnershipFromToConnection(ControlledBy, null, Gained);
		}
	}


	//Gets all Bodies that are related to this mind,  Mind-> Brain-> Body
	public List<NetworkIdentity> GetRelatedBodies()
	{
		var returnList = new List<NetworkIdentity>();
		returnList.Add(this.netIdentity);

		if (PlayerPossessable != null)
		{
			PlayerPossessable.GetRelatedBodies(returnList);
		}
		else
		{
			if (PossessingObject != null)
			{
				returnList.Add(PossessingObject.NetWorkIdentity());
			}
		}

		return returnList;
	}


	public NetworkIdentity GetDeepestBody()
	{
		if (IsGhosting)
		{
			return this.netIdentity;
		}


		if (PlayerPossessable != null)
		{
			return PlayerPossessable.GetDeepestBody();
		}
		else
		{
			if (PossessingObject != null)
			{
				return PossessingObject.NetWorkIdentity();
			}
		}

		return this.netIdentity;
	}


	/// <summary>
	/// Get the cloneable status of the player's mind, relative to the passed mob ID.
	/// </summary>
	public CloneableStatus GetCloneableStatus(int recordMobID)
	{
		if (bodyMobID != recordMobID)
		{
			// an old record might still exist even after several body swaps
			return CloneableStatus.OldRecord;
		}

		if (DenyCloning)
		{
			return CloneableStatus.DenyingCloning;
		}

		var currentMob = GetCurrentMob();
		if (IsGhosting == false)
		{
			var livingHealthBehaviour = currentMob.GetComponent<LivingHealthMasterBase>();
			if (livingHealthBehaviour.IsDead == false)
			{
				return CloneableStatus.StillAlive;
			}
		}

		if (IsOnline() == false)
		{
			return CloneableStatus.Offline;
		}

		return CloneableStatus.Cloneable;
	}

	public bool IsOnline()
	{
		NetworkConnection connection = GetCurrentMob().GetComponent<NetworkIdentity>().connectionToClient;
		return PlayerList.Instance.Has(connection);
	}

	[SerializeField] private ActionData actionData = null; //Antagonist show objectives button
	public ActionData ActionData => actionData;

	public void CallActionClient()
	{
		CmdAskforAntagObjectives();
	}

	[Command]
	public void CmdAskforAntagObjectives()
	{
		ShowObjectives();
	}

	/// <summary>
	/// Show the the player their current objectives if they have any
	/// </summary>
	public void ShowObjectives()
	{
		if (IsAntag == false) return;
		var playerMob = GetCurrentMob();

		//Send Objectives
		Chat.AddExamineMsgFromServer(playerMob, antag.GetObjectivesForPlayer());

		if (CodeWordManager.Instance.CodeWordRoles.Contains(NetworkedAntagJob) == true)
		{
			string codeWordsString = "Code Words:";
			for (int i = 0; i < CodeWordManager.WORD_COUNT; i++)
			{
				codeWordsString += $"\n-{CodeWordManager.Instance.Words[i]}";
			}

			codeWordsString += "\n\nResponses:";
			for (int i = 0; i < CodeWordManager.WORD_COUNT; i++)
			{
				codeWordsString += $"\n-{CodeWordManager.Instance.Responses[i]}";
			}

			Chat.AddExamineMsgFromServer(playerMob, codeWordsString);
		}

		if (playerMob.TryGetComponent<PlayerScript>(out var body) == false) return;

		if (CodeWordManager.Instance.CodeWordRoles.Contains(NetworkedAntagJob) == true || antag.Antagonist is BloodBrother == true)
		{
			if (body.OrNull()?.DynamicItemStorage == null) return;
			var playerInventory = body.DynamicItemStorage.GetItemSlots();
			foreach (var item in playerInventory)
			{
				if (item.IsEmpty) continue;
				if (item.ItemObject.TryGetComponent<PDALogic>(out var PDA) == false) continue;
				if (PDA.IsUplinkCapable == false) continue;

				//Send Uplink code
				Chat.AddExamineMsgFromServer(playerMob, $"PDA uplink code retrieved: {PDA.UplinkUnlockCode}");
				pdaUplinkCode = PDA.UplinkUnlockCode;
				//TODO Store same place as objectives it's Dumb being here,
				//Means you can View the code of Any PDA If you're an antagonist
			}
		}
	}

	public string GetObjectives()
	{
		var objectives = "";
		if (IsAntag == false) return "";
		var playerMob = GetCurrentMob();

		//Send Objectives
		objectives += antag.GetObjectivesForPlayer();

		if (playerMob.TryGetComponent<PlayerScript>(out var body) == false) return "";
		if (antag.Antagonist.AntagJobType == JobType.TRAITOR || antag.Antagonist.AntagJobType == JobType.SYNDICATE || antag.Antagonist is BloodBrother)
		{
			string codeWordsString = "\nCode Words:";
			for (int i = 0; i < CodeWordManager.WORD_COUNT; i++)
			{
				codeWordsString += $"\n-{CodeWordManager.Instance.Words[i]}";
			}

			codeWordsString += "\nResponses:";
			for (int i = 0; i < CodeWordManager.WORD_COUNT; i++)
			{
				codeWordsString += $"\n-{CodeWordManager.Instance.Responses[i]}";
			}

			objectives += codeWordsString;

			objectives += $"\nPDA uplink code retrieved:{pdaUplinkCode}";
		}
		return objectives;
	}

	/// <summary>
	/// Simply returns what antag the player is, if any
	/// </summary>
	public SpawnedAntag GetAntag()
	{
		return antag;
	}

	/// <summary>
	/// Returns true if the given mind is of the given Antagonist type.
	/// </summary>
	/// <typeparam name="T">The type of antagonist to check against</typeparam>
	public bool IsOfAntag<T>() where T : Antagonist
	{
		if (IsAntag == false) return false;

		return antag.Antagonist is T;
	}

	public void AddSpell(Spell spell)
	{
		if (spells.Contains(spell))
		{
			return;
		}

		spells.Add(spell);
	}

	public void RemoveSpell(Spell spell)
	{
		if (spells.Contains(spell))
		{
			spells.Remove(spell);
		}
	}

	public Spell GetSpellInstance(SpellData spellData)
	{
		foreach (Spell spell in Spells)
		{
			if (spell.SpellData == spellData)
			{
				return spell;
			}
		}

		return default;
	}

	public bool HasSpell(SpellData spellData)
	{
		return GetSpellInstance(spellData) != null;
	}

	public void SetProperty(string key, object value)
	{
		if (properties.ContainsKey(key))
		{
			properties[key] = value;
		}
		else
		{
			properties.Add(key, value);
		}
	}

	public T GetPropertyOrDefault<T>(string key, T defaultValue)
	{
		return properties.GetOrDefault(key, defaultValue) is T typedProperty ? typedProperty : defaultValue;
	}
}