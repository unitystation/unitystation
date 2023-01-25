using System;
using System.Collections.Generic;
using Core.Utils;
using HealthV2;
using Messages.Server;
using Mirror;
using Systems.Ai;
using UI.Core.Action;
using UnityEngine;

public interface IPlayerPossessable
{
	public GameObject GameObject { get; }

	private GameObject GetPossessingObject()
	{
		if (PossessingID is NetId.Empty or NetId.Invalid)
		{
			return null;
		}
		else
		{
			var spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;
			if (spawned.ContainsKey(PossessingID) == false)
			{
				Logger.LogError(
					$"Destroyed Possessing  While PossessingID Still references it fixing, Please work out how it got a Destroyed ID {PossessingMind.OrNull()?.name}");
				SyncPossessingID(PossessingID, NetId.Empty);
				return null;
			}

			return spawned[PossessingID].gameObject;
		}
	}

	public IPlayerPossessable GetPossessing()
	{
		var ob = GetPossessingObject();
		if (ob != null)
		{
			return ob.GetComponent<IPlayerPossessable>();
		}

		return null;
	}

	public uint PossessingID { get; }

	public Mind PossessingMind { get; set; }

	public IPlayerPossessable PossessedBy { get; set; }

	public MindNIPossessingEvent OnPossessedBy { get; set; }

	public Action OnActionEnterPlayer { get; set; }

	public Action OnActionEnterControl { get; set; }

	public void SyncPossessingID(uint previouslyPossessing, uint currentlyPossessing);

	public void PreImplementedSyncPossessingID(uint previouslyPossessing, uint currentlyPossessing)
	{
		var spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;

		var possessing = GetPossessing();
		if (possessing != null)
		{
			if (PossessingMind != null && PossessingMind.IsGhosting == false && CustomNetworkManager.IsServer == false)
			{
				if (spawned.ContainsKey(previouslyPossessing)) //TODO Test client
				{
					possessing.InternalOnEnter(spawned[previouslyPossessing].gameObject, PossessingMind, CustomNetworkManager.IsServer, this);
				}
				else
				{
					possessing.InternalOnEnter(null, PossessingMind, CustomNetworkManager.IsServer, this);
				}
			}
		}




	}


	public void ServeInternalOnEnter(GameObject previouslyControlling, Mind mind, bool isServer)
	{
		if (mind.ControlledBy != null)
		{
			mind.ControlledBy.GameObject = GameObject; //TODO Remove
		}

		//can observe their new inventory
		var dynamicItemStorage = GameObject.GetComponent<DynamicItemStorage>();
		if (dynamicItemStorage != null)
		{
			dynamicItemStorage.ServerAddObserverPlayer(GameObject);
			PlayerPopulateInventoryUIMessage.Send(dynamicItemStorage,
				GameObject); //TODO should we be using the players body as game object???
		}

		PossessAndUnpossessMessage.Send(GameObject, GameObject, previouslyControlling);

		if (mind != null)
		{
			var transfers = GameObject.GetComponents<IOnPlayerTransfer>();

			foreach (var transfer in transfers)
			{
				transfer.OnServerPlayerTransfer(mind.ControlledBy);
			}
		}

		OnActionEnterPlayer?.Invoke();
	}


	public void ClientInternalOnEnter(GameObject previouslyControlling, Mind mind, bool isServer)
	{
		var input = GameObject.GetComponent<IPlayerControllable>();

		if (GameObject.TryGetComponent<AiMouseInputController>(out var aiMouseInputController))
		{
			input = aiMouseInputController;
		}

		PlayerManager.SetPlayerForControl(GameObject, input);
		var dynamicItemStorage = GameObject.GetComponent<DynamicItemStorage>();
		if (dynamicItemStorage != null)
		{
			dynamicItemStorage.UpdateSlots(dynamicItemStorage.GetSetData, dynamicItemStorage.GetSetData);
		}

		UIActionManager.ClearAllActionsClient();
		RequestIconsUIActionRefresh.Send();
		OnActionEnterPlayer?.Invoke();
	}

	public void InternalOnLoseControl()
	{
		PossessingMind = null;
		var Possessed = GetPossessing();
		if (Possessed != null)
		{
			Possessed.InternalOnLoseControl();
		}
	}

	public void InternalOnGainControlOf(Mind mind, IPlayerPossessable parent)
	{
		var playerScript = GameObject.GetComponent<PlayerScript>();
		if (playerScript)
		{
			playerScript.SetMind(mind);
		}

		if ( CustomNetworkManager.IsServer )
		{
			ServerInternalOnPossess(mind, parent);
		}

		OnEnterPossess(mind, parent);

		var possessing = GetPossessing();
		if (possessing != null)
		{
			possessing.InternalOnGainControlOf(mind, this);
		}
	}

	public void ServerInternalOnPossess(Mind mind, IPlayerPossessable parent)
	{
		var health = GameObject.GetComponent<LivingHealthMasterBase>();
		if (health != null && mind != null)
		{
			mind.bodyMobID = health.mobID;
		}

		if (mind != null)
		{
			var transfers = GameObject.GetComponents<IOnPlayerTransfer>();

			foreach (var transfer in transfers)
			{
				transfer.OnServerPlayerTransfer(mind.ControlledBy); //TODO Differentiate
			}
		}

		OnActionEnterControl?.Invoke();
	}


	public void InternalOnEnter(GameObject previouslyControlling, Mind mind, bool isServer, IPlayerPossessable parent)
	{
		if (mind == null) return;

		if (isServer)
		{
			ServeInternalOnEnter(previouslyControlling, mind, true);
		}

		if (GameObject.GetComponent<NetworkIdentity>().hasAuthority)
		{
			ClientInternalOnEnter(previouslyControlling, mind, isServer);
		}

		OnEnterPlayer(previouslyControlling, mind, isServer, parent);
		var possessing = GetPossessing();
		if (possessing != null)
		{
			possessing.InternalOnEnter(previouslyControlling, mind, isServer, this);
		}
	}

	public void OnEnterPossess(Mind mind, IPlayerPossessable parent);

	public void OnEnterPlayer(GameObject previouslyControlling, Mind mind, bool isServer, IPlayerPossessable parent);

	public bool IsRelatedToObject(GameObject _object)
	{
		if (GetPossessingObject() == _object)
		{
			return true;
		}


		var possessing = GetPossessing();
		if (possessing != null && possessing.IsRelatedToObject(_object))
		{
			return true;
		}

		return false;
	}

	public void BeingPossessedBy(Mind mind, IPlayerPossessable playerPossessable)
	{
		PossessingMind = mind;
		PossessedBy = playerPossessable;
		var possessing = GetPossessing();
		if (possessing != null)
		{
			possessing.BeingPossessedBy(mind, this);
		}

		OnPossessedBy?.Invoke(mind, playerPossessable);
	}

	public void SetPossessingObject(GameObject obj)
	{
		var inPossessing = obj.OrNull()?.GetComponent<IPlayerPossessable>();
		var gaining = new List<NetworkIdentity>();
		if (inPossessing != null)
		{
			inPossessing.GetRelatedBodies(gaining);
		}
		else if (obj != null)
		{
			gaining.Add(obj.NetWorkIdentity());
		}


		var losing = new List<NetworkIdentity>();
		var possessing = GetPossessing();
		var possessingObject = GetPossessingObject();
		if (possessing != null)
		{
			possessing.GetRelatedBodies(losing);
			possessing.InternalOnLoseControl();
			possessing.PossessedBy = null;
		}
		else if (possessingObject != null)
		{
			gaining.Add(possessingObject.NetWorkIdentity());
		}

		PossessingMind.OrNull()?.HandleOwnershipChangeMulti(losing, gaining);
		SyncPossessingID(PossessingID, obj ? obj.GetComponent<NetworkIdentity>().netId : NetId.Empty);

		if (obj != null)
		{
			possessing = obj.GetComponent<IPlayerPossessable>();
			possessing?.InternalOnGainControlOf(PossessingMind, this);

			if (PossessingMind != null && PossessingMind.IsGhosting == false)
			{
				possessing?.InternalOnEnter(possessingObject, PossessingMind, CustomNetworkManager.IsServer, this);
			}
		}
	}

	public List<NetworkIdentity> GetRelatedBodies(List<NetworkIdentity> returnList)
	{
		returnList.Add(GameObject.NetWorkIdentity());
		var possessing = GetPossessing();
		var possessingObject = GetPossessingObject();
		if (possessing != null)
		{
			possessing.GetRelatedBodies(returnList);
		}
		else
		{
			if (possessingObject != null)
			{
				returnList.Add(possessingObject.NetWorkIdentity());
			}
		}

		return returnList;
	}

	public NetworkIdentity GetDeepestBody()
	{
		var possessing = GetPossessing();
		var possessingObject = GetPossessingObject();

		if (possessing != null)
		{
			return possessing.GetDeepestBody();
		}

		if (possessingObject != null)
		{
			return possessingObject.NetWorkIdentity();
		}

		return GameObject.NetWorkIdentity();
	}

	public void OnDestroy();

	public void PreImplementedOnDestroy()
	{
		if (PossessedBy != null)
		{
			PossessedBy.SetPossessingObject(null);
		}

		if (PossessingMind != null)
		{
		}
	}
}