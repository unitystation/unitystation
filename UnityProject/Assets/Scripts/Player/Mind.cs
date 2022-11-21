using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using Mirror;
using Antagonists;
using Systems.Spells;
using HealthV2;
using Initialisation;
using Items.PDA;
using Messages.Server;
using Player;
using ScriptableObjects.Audio;
using UI.Action;
using ScriptableObjects.Systems.Spells;

/// <summary>
/// IC character information (job role, antag info, real name, etc). A body and their ghost link to the same mind
/// SERVER SIDE VALID ONLY, is not sync'd
/// </summary>
public class Mind : NetworkBehaviour
{
	[SyncVar(hook = nameof(SyncActiveOn))] private uint IDActivelyControlling;

	//Antag
	[SyncVar]
	public bool NetworkedisAntag;

	public GameObject PossessingObject { get; private set; }
	public IPlayerPossessable PlayerPossessable { get; private set; }

	public Occupation occupation;

	public PlayerScript ghost { private set; get; }
	public PlayerScript body => GetDeepestBody().GetComponent<PlayerScript>();
	private SpawnedAntag antag;
	public bool IsAntag => CustomNetworkManager.IsServer ? antag != null : NetworkedisAntag;
	public bool IsGhosting;
	public bool DenyCloning;
	public int bodyMobID;
	public FloorSounds StepSound; //Why is this on the mind!!!, Should be on the body
	public FloorSounds SecondaryStepSound;

	public ChatModifier inventorySpeechModifiers = ChatModifier.None;
	// Current way to check if it's not actually a ghost but a spectator, should set this not have it be the below.

	public PlayerInfo ControlledBy;


	public CharacterSheet CurrentCharacterSettings;

	public PlayerScript CurrentPlayScript => IsGhosting ? ghost : body;

	public bool IsSpectator => PossessingObject == null;

	public bool ghostLocked;

	private ObservableCollection<Spell> spells = new ObservableCollection<Spell>();
	public ObservableCollection<Spell> Spells => spells;

	/// <summary>
	/// General purpose properties storage for misc stuff like job-specific flags
	/// </summary>
	private Dictionary<string, object> properties = new Dictionary<string, object>();

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


	public void ApplyOccupation(Occupation requestedOccupation)
	{
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
	}


	public void SetNewBody(PlayerScript playerScript)
	{
		//what!!!

		Spells.Clear();
		ClearOldBody();

		if (antag != null) SetAntag(antag);

		if (playerScript.TryGetComponent<LivingHealthMasterBase>(out var health))
		{
			bodyMobID = health.mobID;
		}
		//
		// if (occupation != null)
		// {
		// 	foreach (var spellData in occupation.Spells)
		// 	{
		// 		var spellScript = spellData.AddToPlayer(playerScript);
		// 		Spells.Add(spellScript);
		// 	}
		//
		// 	foreach (var pair in occupation.CustomProperties)
		// 	{
		// 		SetProperty(pair.Key, pair.Value);
		// 	}
		// }

		StopGhosting();
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


	private void ClearOldBody()
	{
		if (body)
		{
			ClearActionsMessage.SendTo(body.gameObject);
			//body.mind = null;
		}
	}

	/// <summary>
	/// Sets the IC name for this player and refreshes the visible name. Name will be kept if respawned.
	/// </summary>
	/// <param name="newName">The new name to give to the player.</param>
	public void SetPermanentName(string newName)
	{
		CurrentCharacterSettings.Name = newName;
		this.name = newName;
	}


	/// <summary>
	/// Make this mind a specific spawned antag
	/// </summary>
	public void SetAntag(SpawnedAntag newAntag)
	{
		antag = newAntag;
		NetworkedisAntag = newAntag != null;
		ShowObjectives();
		GetDeepestBody().GetComponent<PlayerScript>().ActivateAntagAction(NetworkedisAntag);
	}

	public void SetPossessingObject(GameObject obj)
	{
		var InPossessing = obj.OrNull()?.GetComponent<IPlayerPossessable>();
		List<NetworkIdentity> Gaining = new List<NetworkIdentity>();
		if (InPossessing != null)
		{
			InPossessing.GetRelatedBodies(Gaining);
		}
		else if (obj != null)
		{
			Gaining.Add(obj.NetWorkIdentity());
		}


		List<NetworkIdentity> Losing = new List<NetworkIdentity>();
		if (PlayerPossessable != null)
		{
			PlayerPossessable.GetRelatedBodies(Losing);
		}
		else if (PossessingObject != null)
		{
			Gaining.Add(PossessingObject.NetWorkIdentity());
		}

		HandleOwnershipChangeMulti(Losing, Gaining);

		PossessingObject = obj;
		PlayerPossessable = obj.GetComponent<IPlayerPossessable>();
		PlayerPossessable?.BeingPossessedBy(this, null);

		SyncActiveOn(IDActivelyControlling, GetDeepestBody().netId);
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
		GetDeepestBody().GetComponent<PlayerScript>().ActivateAntagAction(NetworkedisAntag);
	}

	public GameObject GetCurrentMob()
	{
		return GetDeepestBody().gameObject;
	}

	public void SetGhost(PlayerScript newGhost)
	{
		ghost = newGhost;
		newGhost.SetMind(this);
	}


	public void Ghost()
	{
		var Body = GetDeepestBody();
		Move.ForcePositionClient(Body.transform.position);
		IsGhosting = true;
		SyncActiveOn(IDActivelyControlling, GetDeepestBody().netId);
	}

	public void StopGhosting()
	{
		IsGhosting = false;
		if (GetDeepestBody().netId == this.netId)
		{
			IsGhosting = true; //Basically is not able to possess anything
		}

		SyncActiveOn(IDActivelyControlling, GetDeepestBody().netId);
	}

	public void SyncActiveOn(uint oldID, uint newID)
	{
		IDActivelyControlling = newID;

		LoadManager.RegisterActionDelayed(() => { HandleActiveOnChange(oldID, newID); },
			2); //This is to handle The game object being spawned in and data being provided before Owner message
		//s sent owner, This means the game object it's told it's in charge of is not actually in charge of Until later on in that frame is Dumb,
		//Plus this handles server player funnies with the same thing Just stretched over another frame so that's why it's 2
	}

	public void HandleActiveOnChange(uint oldID, uint newID)
	{
		var spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;
		if (spawned.ContainsKey(newID))
		{
			if (ControlledBy != null) //TODO Remove
			{
				ControlledBy.GameObject = spawned[newID].gameObject;
			}

			IPlayerPossessable oldPossessable = null;
			if (spawned.ContainsKey(oldID))
			{
				oldPossessable = spawned[oldID].GetComponent<IPlayerPossessable>();
			}

			var Possessable = spawned[newID].GetComponent<IPlayerPossessable>();
			if (Possessable != null)
			{
				Possessable.InternalOnEnterPlayerControl(oldPossessable?.GameObject, this,
					CustomNetworkManager.IsServer);
			}
			else
			{
				//TODO For objects
			}
		}

		//here
	}

	public void AccountLeavingMind(PlayerInfo Account)
	{
		Account.SetMind(null);
		//Remove account from being observer of ghost and stuff
		var RelatedBodies = GetRelatedBodies();
		foreach (var Body in RelatedBodies)
		{
			PlayerSpawn.TransferOwnershipToConnection(Account, Body, null);
		}
	}

	public void AccountEnteringMind(PlayerInfo Account)
	{
		Account.SetMind(this);

		var RelatedBodies = GetRelatedBodies();
		foreach (var Body in RelatedBodies)
		{
			PlayerSpawn.TransferOwnershipToConnection(Account, null, Body);
		}

		SyncActiveOn(IDActivelyControlling, IDActivelyControlling);
	}

	public void ReLog()
	{
		if (ControlledBy?.Connection == null)
		{
			Logger.LogError("oh god!, Somehow there's no connection to client when ReLog Code has Been called");
			return;
		}

		PlayerSpawn.TransferAccountToSpawnedMind(ControlledBy, this);


		var RelatedBodies = GetRelatedBodies();
		foreach (var Body in RelatedBodies)
		{
			PlayerSpawn.TransferOwnershipToConnection(ControlledBy, null, Body);
		}
	}

	public void HandleOwnershipChangeMulti(List<NetworkIdentity> Losing, List<NetworkIdentity> Gaining)
	{
		if (ControlledBy != null)
		{
			foreach (var Lost in Losing)
			{
				PlayerSpawn.TransferOwnershipToConnection(ControlledBy, Lost, null);
			}

			foreach (var Gained in Gaining)
			{
				PlayerSpawn.TransferOwnershipToConnection(ControlledBy, null, Gained);
			}
		}
	}


	//Gets all Bodies that are related to this mind,  Mind-> Brain-> Body
	public List<NetworkIdentity> GetRelatedBodies()
	{
		var ReturnList = new List<NetworkIdentity>();
		ReturnList.Add(this.netIdentity);

		if (PlayerPossessable != null)
		{
			PlayerPossessable.GetRelatedBodies(ReturnList);
		}
		else
		{
			if (PossessingObject != null)
			{
				ReturnList.Add(PossessingObject.NetWorkIdentity());
			}
		}

		return ReturnList;
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

	/// <summary>
	/// Show the the player their current objectives if they have any
	/// </summary>
	public void ShowObjectives()
	{
		if (IsAntag == false) return;
		var playerMob = GetCurrentMob();

		//Send Objectives
		Chat.AddExamineMsgFromServer(playerMob, antag.GetObjectivesForPlayer());

		if (playerMob.TryGetComponent<PlayerScript>(out var body) == false) return;
		if (antag.Antagonist.AntagJobType == JobType.TRAITOR || antag.Antagonist.AntagJobType == JobType.SYNDICATE)
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
			}
		}
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