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
	public IPlayerPossessable Possessing { get; set; }

	public GameObject PossessingObject { get; set; }

	public Mind PossessingMind { get; set; }

	public IPlayerPossessable PossessedBy { get; set; }

	public MindNIPossessingEvent OnPossessedBy  { get; set; }

	public Action OnActionEnterPlayerControl { get; set; }

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
		if (playerObjectBehavior != null )
		{
			FollowCameraMessage.Send(GameObject, playerObjectBehavior.GetRootObject);
		}

		PossessAndUnpossessMessage.Send(GameObject, GameObject, previouslyControlling);

		var health = GameObject.GetComponent<LivingHealthMasterBase>();
		if (health != null)
		{
			mind.bodyMobID = health.mobID;
		}



		var transfers = GameObject.GetComponents<IOnPlayerTransfer>();

		foreach (var transfer in transfers)
		{
			transfer.OnServerPlayerTransfer(mind.ControlledBy);
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
			dynamicItemStorage.UpdateSlots(	dynamicItemStorage.GetSetData, 	dynamicItemStorage.GetSetData);
		}

		RequestIconsUIActionRefresh.Send();
		OnActionEnterPlayerControl?.Invoke();
	}

	public void InternalOnEnterPlayerControl(GameObject previouslyControlling, Mind mind, bool isServer)
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

		OnEnterPlayerControl( previouslyControlling,  mind,  isServer);
	}

	public void OnEnterPlayerControl(GameObject previouslyControlling, Mind mind, bool isServer);

	public bool IsRelatedToObject(GameObject _object)
	{
		if (PossessingObject == _object)
		{
			return true;
		}

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
		if (Possessing != null)
		{
			Possessing.BeingPossessedBy(mind, this);
		}
		OnPossessedBy?.Invoke(mind,playerPossessable);
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
		if (Possessing != null)
		{
			Possessing.GetRelatedBodies(losing);
		}
		else if (PossessingObject != null)
		{
			gaining.Add(PossessingObject.NetWorkIdentity());
		}

		PossessingMind.OrNull()?.HandleOwnershipChangeMulti(losing, gaining);
		PossessingObject = obj;
		Possessing = obj.GetComponent<IPlayerPossessable>();
		Possessing?.BeingPossessedBy(PossessingMind, this);
	}

	public List<NetworkIdentity> GetRelatedBodies(List<NetworkIdentity> returnList)
	{
		returnList.Add(GameObject.NetWorkIdentity());
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
