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

	public Action OnActionControlPlayer { get; set; }

	public Action OnActionPossess { get; set; }

	public void SyncPossessingID(uint previouslyPossessing, uint currentlyPossessing);

	public void PreImplementedSyncPossessingID(uint previouslyPossessing, uint currentlyPossessing)
	{
		var possessing = GetPossessing();
		if (possessing != null)
		{
			if (PossessingMind != null && PossessingMind.IsGhosting == false && CustomNetworkManager.IsServer == false)
			{
				possessing.InternalOnControlPlayer( PossessingMind, CustomNetworkManager.IsServer, this);
			}
		}
	}


	public void ServeInternalOnControlPlayer( Mind mind, bool isServer)
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

		PossessAndUnpossessMessage.Send(GameObject, GameObject, null);

		if (mind != null)
		{
			var transfers = GameObject.GetComponents<IOnControlPlayer>();

			foreach (var transfer in transfers)
			{
				transfer.OnServerPlayerTransfer(mind.ControlledBy);
			}
		}

		OnActionControlPlayer?.Invoke();
	}


	public void ClientInternalOnControlPlayer( Mind mind, bool isServer)
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
		OnActionControlPlayer?.Invoke();
	}

	public void InternalOnLosePossess()
	{
		var MindBackup = PossessingMind;
		PossessingMind = null;

		var Possessed = GetPossessing();
		if (Possessed != null)
		{
			Possessed.InternalOnLosePossess();
		}

		if (MindBackup != null)
		{
			var leaveInterfaces = GameObject.GetComponents<IOnPlayerLosePossess>();
			foreach (var leaveInterface in leaveInterfaces)
			{
				leaveInterface.OnPlayerLosePossession(MindBackup);
			}
		}
	}

	public void InternalOnPossessPlayer(Mind mind, IPlayerPossessable parent)
	{
		var playerScript = GameObject.GetComponent<PlayerScript>();
		if (playerScript)
		{
			playerScript.SetMind(mind);
		}

		if (CustomNetworkManager.IsServer)
		{
			ServerInternalOnPossess(mind, parent);
		}

		OnPossessPlayer(mind, parent);

		var possessing = GetPossessing();
		if (possessing != null)
		{
			possessing.InternalOnPossessPlayer(mind, this);
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
			var transfers = GameObject.GetComponents<IOnPlayerPossess>();

			foreach (var transfer in transfers)
			{
				transfer.OnServerPlayerPossess(mind);
			}
		}

		OnActionPossess?.Invoke();
	}


	public void InternalOnControlPlayer( Mind mind, bool isServer,
		IPlayerPossessable parent)
	{
		if (mind == null) return;

		if (isServer)
		{
			ServeInternalOnControlPlayer( mind, true);
		}

		if (GameObject.GetComponent<NetworkIdentity>().hasAuthority)
		{
			ClientInternalOnControlPlayer( mind, isServer);
		}

		OnControlPlayer(mind, isServer, parent);
		var possessing = GetPossessing();
		if (possessing != null)
		{
			possessing.InternalOnControlPlayer( mind, isServer, this);
		}
	}

	public void InternalOnPlayerLeave(Mind mind)
	{
		var leaveInterfaces = GameObject.GetComponents<IOnPlayerLeaveBody>();
		foreach (var leaveInterface in leaveInterfaces)
		{
			leaveInterface.OnPlayerLeaveBody(mind.ControlledBy);
		}
		PossessAndUnpossessMessage.Send(mind.gameObject, null,GameObject);
		var possessing = GetPossessing();
		if (possessing != null)
		{
			possessing.InternalOnPlayerLeave(mind);
		}

	}

	public void PlayerRejoin()
	{
		var leaveInterfaces = GameObject.GetComponents<IOnPlayerRejoin>();
		foreach (var leaveInterface in leaveInterfaces)
		{
			leaveInterface.OnPlayerRejoin(PossessingMind);
		}

		var possessing = GetPossessing();
		if (possessing != null)
		{
			possessing.PlayerRejoin();
		}
	}


	public void OnPossessPlayer(Mind mind, IPlayerPossessable parent);

	public void OnControlPlayer(Mind mind, bool isServer, IPlayerPossessable parent);

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
			possessing.InternalOnLosePossess();
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
			possessing?.InternalOnPossessPlayer(PossessingMind, this);

			if (PossessingMind != null && PossessingMind.IsGhosting == false)
			{
				possessing?.InternalOnControlPlayer(PossessingMind, CustomNetworkManager.IsServer,
					this);
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