using System.Collections;
using System.Collections.Generic;
using Core.Utils;
using HealthV2;
using Messages.Server;
using Mirror;
using Systems.Ai;
using UnityEngine;

public interface IPlayerPossessable
{

	public GameObject GameObject { get; }
	public IPlayerPossessable Possessing { get; set; }

	public GameObject PossessingObject { get; set; }

	public Mind PossessingMind { get; set; }

	public IPlayerPossessable PossessedBy { get; set; }

	public MindNIPossessingEvent OnPossessedBy   { get; set; }

	public void InternalOnEnterPlayerControl(GameObject PreviouslyControlling, Mind mind)
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
		if (playerObjectBehavior)
		{
			FollowCameraMessage.Send(GameObject, playerObjectBehavior.gameObject); //TODO Handle within container
		}

		IPlayerControllable input = GameObject.GetComponent<IPlayerControllable>();

		if (GameObject.TryGetComponent<AiMouseInputController>(out var aiMouseInputController))
		{
			input = aiMouseInputController;
		}

		PlayerManager.SetPlayerForControl(GameObject, input);

		PossessAndUnpossessMessage.Send(GameObject, GameObject, PreviouslyControlling);
		var transfers = GameObject.GetComponents<IOnPlayerTransfer>();


		var PS = GameObject.GetComponent<PlayerScript>();
		if (PS)
		{
			PS.SetMind(mind); //TODO unset
		}

		var health = GameObject.GetComponent<LivingHealthMasterBase>();
		if (health != null)
		{
			mind.bodyMobID = health.mobID;
		}

		foreach (var transfer in transfers)
		{
			transfer.OnPlayerTransfer(mind.ControlledBy);
		}

		OnEnterPlayerControl();
	}

	public void OnEnterPlayerControl();

	public bool IsRelatedToObject(GameObject Object)
	{
		if (PossessingObject == Object)
		{
			return true;
		}

		if (Possessing != null && Possessing.IsRelatedToObject(Object))
		{
			return true;
		}

		return false;
	}

	public void BeingPossessedBy(Mind Mind, IPlayerPossessable PlayerPossessable)
	{
		PossessingMind = Mind;
		PossessedBy = PlayerPossessable;
		if (Possessing != null)
		{
			Possessing.BeingPossessedBy(Mind, this);
		}
		OnPossessedBy?.Invoke(Mind,PlayerPossessable);
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
		if (Possessing != null)
		{
			Possessing.GetRelatedBodies(Losing);
		}
		else if (PossessingObject != null)
		{
			Gaining.Add(PossessingObject.NetWorkIdentity());
		}

		PossessingMind.OrNull()?.HandleOwnershipChangeMulti(Losing, Gaining);
		PossessingObject = obj;
		Possessing = obj.GetComponent<IPlayerPossessable>();
		Possessing?.BeingPossessedBy(PossessingMind, this);
	}

	public List<NetworkIdentity> GetRelatedBodies(List<NetworkIdentity> ReturnList)
	{
		ReturnList.Add(GameObject.NetWorkIdentity());
		if (Possessing != null)
		{
			Possessing.GetRelatedBodies(ReturnList);
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

		if (Possessing != null)
		{
			return Possessing.GetDeepestBody();
		}
		else
		{
			if (PossessingObject != null)
			{
				return PossessingObject.NetWorkIdentity();
			}
		}

		return GameObject.NetWorkIdentity();
	}

}
