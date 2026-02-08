using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using US13.Actions;
using US13.Core.Lifecycle;
using US13.Core.Utils;
using US13.HealthV2.Living;
using US13.Managers.NetworkManagement;
using US13.Messages.Client;
using US13.Messages.Server;
using US13.Systems.Ai;
using US13.Systems.Inventory;
using Util;

namespace US13.Player
{
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
					if (CustomNetworkManager.IsServer)
					{
						SyncPossessingID(PossessingID, NetId.Empty);
					}

					return null;
				}

				return spawned[PossessingID].gameObject;
			}
		}

		public GameObject ControllingObject //For now the one you are possessing is The deepest one
		{
			get
			{
				if (PossessingID is NetId.Invalid or NetId.Empty) return this.GameObject;
				if (CustomNetworkManager.Spawned.ContainsKey(PossessingID) == false) return this.GameObject;
				var Possessable = CustomNetworkManager.Spawned[PossessingID].GetCommonComponents().IPlayerPossessable;
				return Possessable.ControllingObject;
			}
		}

		public IPlayerPossessable GetPossessing()
		{
			var ob = GetPossessingObject();
			if (ob != null)
			{
				return ob.GetCommonComponents().IPlayerPossessable;
			}

			return null;
		}

		public uint PossessingID { get; }

		public Mind PossessingMind { get; set; }

		public IPlayerPossessable PossessedBy { get; set; }

		[field: SerializeField] public MindNIPossessingEvent OnPossessedBy { get; set; } // = new MindNIPossessingEvent();

		public Action OnActionControlPlayer { get; set; }

		public Action OnActionPossess { get; set; }

		public UnityEvent OnBodyPossesedByPlayer { get; set; }
		public UnityEvent OnBodyUnPossesedByPlayer { get; set; }

		public void SyncPossessingID(uint previouslyPossessing, uint currentlyPossessing);

		public void PreImplementedSyncPossessingID(uint previouslyPossessing, uint currentlyPossessing)
		{
			if (PossessingMind == null)
			{
				var Losing = previouslyPossessing.NetIdToGameObject()?.GetCommonComponents()?.IPlayerPossessable;
				if (Losing != null)
				{
					Losing.InternalOnPlayerLeave(PossessingMind);
				}
			}

			var possessing = GetPossessing();
			if (possessing != null)
			{
				if (PossessingMind != null && PossessingMind.IsGhosting == false)
				{
					possessing.InternalOnControlPlayer(PossessingMind, CustomNetworkManager.IsServer);
				}
			}
			else
			{
				if (PossessingMind != null && PossessingMind.IsGhosting == false) //Has authority to Set control
				{
					InternalOnControlPlayer(PossessingMind, CustomNetworkManager.IsServer);
				}
			}
		}

		#region PossessingAndLosePossessing

		public void SetPossessingObject(GameObject obj)
		{
			var inPossessing = obj.OrNull()?.GetCommonComponents()?.IPlayerPossessable;

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
				possessing.InternalOnPlayerLeave(PossessingMind);
				possessing.PossessedBy = null;
			}
			else if (possessingObject != null)
			{
				gaining.Add(possessingObject.NetWorkIdentity());
			}


			PossessingMind.OrNull()?.HandleOwnershipChangeMulti(losing, gaining);
			SyncPossessingID(PossessingID, obj ? obj.GetComponentCustom<NetworkIdentity>().netId : NetId.Empty);

			if (inPossessing != null)
			{
				inPossessing.InternalOnPossessPlayer(PossessingMind, this);
			}
		}

		public void InternalOnPossessPlayer(Mind mind, IPlayerPossessable parent)
		{
			if (CustomNetworkManager.IsServer)
			{
				ServerInternalOnPossess(mind, parent);
			}

			PossessingMind = mind;
			PossessedBy = parent;

			OnPossessPlayer(mind, parent);

			var possessing = GetPossessing();
			if (possessing != null)
			{
				possessing.InternalOnPossessPlayer(mind, this);
			}

			OnPossessedBy?.Invoke(mind, parent);
			OnBodyPossesedByPlayer?.Invoke();
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

		public void InternalOnLosePossess(Mind losing)
		{
			if (PossessingMind != losing)
			{
				return; //Not the same mind
			}

			var MindBackup = PossessingMind;
			PossessingMind = null;

			var Possessed = GetPossessing();
			if (Possessed != null)
			{
				Possessed.InternalOnLosePossess(losing);
			}

			if (MindBackup != null)
			{
				var leaveInterfaces = GameObject.GetComponents<IOnPlayerLosePossess>();
				foreach (var leaveInterface in leaveInterfaces)
				{
					leaveInterface.OnPlayerLosePossession(MindBackup);
				}
			}
			OnBodyUnPossesedByPlayer?.Invoke();
		}

		public void OnPossessPlayer(Mind mind, IPlayerPossessable parent);

		#endregion


		#region OnControlPlayerORLeaving

		public void InternalOnControlPlayer(Mind mind, bool isServer)
		{
			PossessingMind = mind;

			if (isServer)
			{
				ServeInternalOnControlPlayer(mind, true);
				//SO The problem is it is recursive so it doesn't know which one is the last one!!! AAAAAAAAAA I'm going to bed
			}


			if (isServer == false || mind == PlayerManager.LocalMindScript)
			{
				ClientInternalOnControlPlayer(mind, isServer);
			}

			OnControlPlayer(mind);


			var Possessing = GetPossessing();
			if (Possessing != null)
			{
				Possessing.InternalOnControlPlayer(mind, isServer);
			}
		}

		public void ServeInternalOnControlPlayer(Mind mind, bool isServer)
		{
			if (mind == null) return;
			if (mind?.ControlledBy != null)
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


			ControlAndLoseControlMessage.Send(GameObject, GameObject, null);

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


		public void ClientInternalOnControlPlayer(Mind mind, bool isServer)
		{
			if (mind == null) return;
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
			OnBodyPossesedByPlayer?.Invoke();
		}

		public void InternalOnPlayerLeave(Mind mind)
		{
			if (GameObject == null) return;
			if (CustomNetworkManager.IsServer == false || mind == PlayerManager.LocalMindScript)
			{
				var leaveInterfaces = GameObject.GetComponents<IOnPlayerLeaveBody>();
				foreach (var leaveInterface in leaveInterfaces)
				{
					leaveInterface.OnPlayerLeaveBody(mind?.ControlledBy);
				}
			}

			if (CustomNetworkManager.IsServer)
			{
				ControlAndLoseControlMessage.Send(mind?.gameObject, null, GameObject);
			}

			var possessing = GetPossessing();
			if (possessing != null)
			{
				possessing.InternalOnPlayerLeave(mind);
			}
		}

		public void OnControlPlayer(Mind mind);

		#endregion

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
			else if (PossessingMind != null)
			{
				PossessingMind.SetPossessingObject(null);
			}

			OnBodyPossesedByPlayer?.RemoveAllListeners();
			OnBodyUnPossesedByPlayer?.RemoveAllListeners();
		}
	}
}