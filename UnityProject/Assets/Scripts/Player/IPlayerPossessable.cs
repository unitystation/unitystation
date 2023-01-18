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

	public Action OnActionEnterPlayerControl { get; set; }

	public void SyncPossessingID(uint PreviouslyPossessing, uint CurrentlyPossessing);

	public void ImplementationSyncPossessingID(uint PreviouslyPossessing, uint CurrentlyPossessing)
	{
		var spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;
		if (spawned.ContainsKey(PreviouslyPossessing) == false)
		{
			InternalOnEnterPlayerControl(null, PossessingMind, CustomNetworkManager.IsServer, PossessedBy);
		}
		else
		{
			InternalOnEnterPlayerControl(spawned[PreviouslyPossessing].gameObject, PossessingMind,
				CustomNetworkManager.IsServer, PossessedBy);
		}
	}

	public void ServeInternalOnEnterPlayerControl(GameObject previouslyControlling, Mind mind, bool isServer)
	{
		//can observe their new inventory
		var dynamicItemStorage = GameObject.GetComponent<DynamicItemStorage>();
		if (dynamicItemStorage != null)
		{
			dynamicItemStorage.ServerAddObserverPlayer(GameObject);
			PlayerPopulateInventoryUIMessage.Send(dynamicItemStorage,
				GameObject); //TODO should we be using the players body as game object???
		}

		// If the player is inside a container, send a ClosetHandlerMessage.
		// The ClosetHandlerMessage will attach the container to the transfered player.
		var playerObjectBehavior = GameObject.GetComponent<UniversalObjectPhysics>();
		if (playerObjectBehavior != null)
		{
			FollowCameraMessage.Send(GameObject, playerObjectBehavior.GetRootObject);
		}

		PossessAndUnpossessMessage.Send(GameObject, GameObject, previouslyControlling);

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
				transfer.OnServerPlayerTransfer(mind.ControlledBy);
			}
		}

		OnActionEnterPlayerControl?.Invoke();
	}


	public void ClientInternalOnEnterPlayerControl(GameObject previouslyControlling, Mind mind, bool isServer)
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
		OnActionEnterPlayerControl?.Invoke();
	}

	public void InternalOnEnterPlayerControl(GameObject previouslyControlling, Mind mind, bool isServer,
		IPlayerPossessable parent)
	{
		var playerScript = GameObject.GetComponent<PlayerScript>();
		if (playerScript)
		{
			playerScript.SetMind(mind);
		}


		if (isServer)
		{
			ServeInternalOnEnterPlayerControl(previouslyControlling, mind, true);
		}

		if (GameObject.GetComponent<NetworkIdentity>().hasAuthority)
		{
			ClientInternalOnEnterPlayerControl(previouslyControlling, mind, isServer);
		}

		OnEnterPlayerControl(previouslyControlling, mind, isServer, parent);
		var Possessing = GetPossessing();
		if (Possessing != null)
		{
			Possessing.InternalOnEnterPlayerControl(previouslyControlling, mind, isServer, this);
		}
	}

	public void OnEnterPlayerControl(GameObject previouslyControlling, Mind mind, bool isServer,
		IPlayerPossessable parent);

	public bool IsRelatedToObject(GameObject _object)
	{
		if (GetPossessingObject() == _object)
		{
			return true;
		}

		var Possessing = GetPossessing();
		if (Possessing != null && Possessing.IsRelatedToObject(_object))
		{
			return true;
		}

		return false;
	}

	public void BeingPossessedBy(Mind mind, IPlayerPossessable playerPossessable)
	{
		PossessingMind = mind;
		PossessedBy = playerPossessable;
		var Possessing = GetPossessing();
		if (Possessing != null)
		{
			Possessing.BeingPossessedBy(mind, this);
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
		var Possessing = GetPossessing();
		var PossessingObject = GetPossessingObject();
		if (Possessing != null)
		{
			Possessing.GetRelatedBodies(losing);
		}
		else if (PossessingObject != null)
		{
			gaining.Add(PossessingObject.NetWorkIdentity());
		}

		PossessingMind.OrNull()?.HandleOwnershipChangeMulti(losing, gaining);
		SyncPossessingID(PossessingID, obj ? obj.GetComponent<NetworkIdentity>().netId : NetId.Empty);
		if (obj != null)
		{
			Possessing = obj.GetComponent<IPlayerPossessable>();
			Possessing?.BeingPossessedBy(PossessingMind, this);
		}
	}

	public List<NetworkIdentity> GetRelatedBodies(List<NetworkIdentity> returnList)
	{
		returnList.Add(GameObject.NetWorkIdentity());
		var Possessing = GetPossessing();
		var PossessingObject = GetPossessingObject();
		if (Possessing != null)
		{
			Possessing.GetRelatedBodies(returnList);
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
		var Possessing = GetPossessing();
		var PossessingObject = GetPossessingObject();

		if (Possessing != null)
		{
			return Possessing.GetDeepestBody();
		}

		if (PossessingObject != null)
		{
			return PossessingObject.NetWorkIdentity();
		}

		return GameObject.NetWorkIdentity();
	}
}