using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using Mirror;
using Antagonists;
using Systems.Spells;
using HealthV2;
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
public class Mind : MonoBehaviour
{
	public Occupation occupation;
	public PlayerScript ghost;
	public PlayerScript body;
	private SpawnedAntag antag;
	public bool IsAntag => antag != null;
	public bool IsGhosting;
	public bool DenyCloning;
	public int bodyMobID;
	public FloorSounds StepSound;
	public FloorSounds SecondaryStepSound;
	public ChatModifier inventorySpeechModifiers = ChatModifier.None;
	// Current way to check if it's not actually a ghost but a spectator, should set this not have it be the below.


	public PlayerScript CurrentPlayScript => IsGhosting ? ghost : body;

	public bool IsSpectator => occupation == null || body == null;

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

	// use Create to create a mind.
	public void Awake()
	{
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
					UIActionManager.ToggleServer(this,x, true);
				}
			}

			if (e.OldItems != null)
			{
				foreach (Spell y in e.OldItems)
				{
					UIActionManager.ToggleServer(this, y, false);
				}
			}
		};
	}

	public void SetNewBody(PlayerScript playerScript)
	{
		Spells.Clear();
		ClearOldBody();
		playerScript.mind = this;
		body = playerScript;
		if(antag != null) SetAntag(antag);

		if (playerScript.TryGetComponent<LivingHealthMasterBase>(out var health))
		{
			bodyMobID = health.mobID;
		}

		if (occupation != null)
		{
			foreach (var spellData in occupation.Spells)
			{
				var spellScript = spellData.AddToPlayer(playerScript);
				Spells.Add(spellScript);
			}

			foreach (var pair in occupation.CustomProperties)
			{
				SetProperty(pair.Key, pair.Value);
			}
		}

		StopGhosting();
	}

	private void ClearOldBody()
	{
		if (body)
		{
			ClearActionsMessage.SendTo(body.gameObject);
			body.mind = null;
		}
	}

	/// <summary>
	/// Make this mind a specific spawned antag
	/// </summary>
	public void SetAntag(SpawnedAntag newAntag)
	{
		antag = newAntag;
		ShowObjectives();
		body.OrNull()?.GetComponent<PlayerOnlySyncValues>().OrNull()?.ServerSetAntag(true);
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
		body.OrNull()?.GetComponent<PlayerOnlySyncValues>().OrNull()?.ServerSetAntag(true);
	}

	public GameObject GetCurrentMob()
	{
		if (IsGhosting)
		{
			return ghost.gameObject;
		}
		else
		{
			return body.gameObject;
		}
	}

	public void SetGhost(PlayerScript newGhost)
	{
		ghost = newGhost;
	}

	public void Ghosting(GameObject newGhost)
	{
		IsGhosting = true;
		var PS = newGhost.GetComponent<PlayerScript>();
		PS.mind = this;
		ghost = PS;
	}

	public void StopGhosting()
	{
		IsGhosting = false;
	}

	/// <summary>
	/// Get the cloneable status of the player's mind, relative to the passed mob ID.
	/// </summary>
	public CloneableStatus GetCloneableStatus(int recordMobID)
	{
		if (bodyMobID != recordMobID)
		{  // an old record might still exist even after several body swaps
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
        		if(PDA.IsUplinkCapable == false) continue;

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
