using System;
using System.Collections.Generic;
using System.Linq;
using Items.PDA;
using Mirror;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.Access
{
	public class AccessHolder: NetworkBehaviour
	{
		[SerializeField]
		[ReorderableList]
		[Tooltip("Assign all acceses this object should store to be checked against the relevant objects that require access.")]
		private List<AccessDefinitions> access = default;

		[SerializeField]
		[ReorderableList]
		[Tooltip("Assign accesses this object should store to be checked against the relevant objects that require access when the round is lowpop.")]
		private List<AccessDefinitions> minimalAccess = default;

		private PDALogic pdaLogic;

		private readonly SyncList<AccessDefinitions> syncedAccess = new SyncListAccess();
		private readonly SyncList<AccessDefinitions> syncedMinimalAccess = new SyncListAccess();

		public List<AccessDefinitions> Access
		{
			get
			{
				// If PDA, let's get the Access from the ID inside PDA.
				if (pdaLogic != null)
				{
					return pdaLogic.Access;
				}

				//TODO check for lowpop here
				// if (lowpop) return minimalAccess;
				return access;
			}
		}

		private void Awake()
		{
			pdaLogic = GetComponent<PDALogic>();
		}

		private void Start()
		{
			syncedAccess.Callback += OnAccessListUpdated;
			syncedMinimalAccess.Callback += OnMinimalAccessListUpdated;
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			if (access.Any())
			{
				ServerSetAccess(access);
			}

			if (minimalAccess.Any())
			{
				ServerSetMinimalAccess(minimalAccess);
			}
		}

		// ReSharper disable Unity.PerformanceAnalysis
		private void OnAccessListUpdated(SyncList<AccessDefinitions>.Operation op, int index, AccessDefinitions oldAccess,
			AccessDefinitions newAccess )
		{
			switch (op)
			{
				case SyncList<AccessDefinitions>.Operation.OP_ADD:
					access.Add(newAccess);
					minimalAccess.Add(newAccess);
					break;
				case SyncList<AccessDefinitions>.Operation.OP_CLEAR:
					access.Clear();
					minimalAccess.Clear();
					break;
				case SyncList<AccessDefinitions>.Operation.OP_INSERT:
					access.Insert(index, newAccess);
					break;
				case SyncList<AccessDefinitions>.Operation.OP_REMOVEAT:
					access.RemoveAt(index);
					break;
				case SyncList<AccessDefinitions>.Operation.OP_SET:
					break;
				default:
					Logger.LogError($"Tried to update access sync list with unexpected operation: {op}");
					break;
			}
		}

		private void OnMinimalAccessListUpdated(SyncList<AccessDefinitions>.Operation op, int index, AccessDefinitions oldAccess,
			AccessDefinitions newAccess )
		{
			switch (op)
			{
				case SyncList<AccessDefinitions>.Operation.OP_ADD:
					minimalAccess.Add(newAccess);
					break;
				case SyncList<AccessDefinitions>.Operation.OP_CLEAR:
					minimalAccess.Clear();
					break;
				case SyncList<AccessDefinitions>.Operation.OP_INSERT:
					minimalAccess.Insert(index, newAccess);
					break;
				case SyncList<AccessDefinitions>.Operation.OP_REMOVEAT:
					minimalAccess.RemoveAt(index);
					break;
				case SyncList<AccessDefinitions>.Operation.OP_SET:
					break;
				default:
					Logger.LogError($"Tried to update access sync list with unexpected operation: {op}");
					break;
			}
		}

		/// <summary>
		/// Interface to update access definitions on this access holder.
		/// </summary>
		/// <param name="newAccess">Complete list of wanted access</param>
		[Server]
		public void ServerSetAccess(List<AccessDefinitions> newAccess)
		{
			if (pdaLogic)
			{
				pdaLogic.IDCard.gameObject.GetComponent<AccessHolder>().ServerSetAccess(newAccess);
				return;
			}
			syncedAccess.Clear();
			//FIXME this looks shitty, every element will trigger the syncing call back. Is there no way to set
			//values directly?
			foreach (var na in newAccess)
			{
				syncedAccess.Add(na);
			}
		}

		/// <summary>
		/// Interface to update minimal access for this access holder
		/// </summary>
		/// <param name="newAccess"></param>
		[Server]
		public void ServerSetMinimalAccess(List<AccessDefinitions> newAccess)
		{
			if (pdaLogic)
			{
				pdaLogic.IDCard.gameObject.GetComponent<AccessHolder>().ServerSetMinimalAccess(newAccess);
			}

			syncedMinimalAccess.Clear();
			//FIXME this looks shitty, every element will trigger the syncing call back. Is there no way to set
			//values directly?
			foreach (var a in newAccess)
			{
				syncedMinimalAccess.Add(a);
			}
		}

		[Server]
		public void ServerClearAccess()
		{
			syncedAccess.Clear();
		}

		[Server]
		public void ServerClearMinimalAccess()
		{
			syncedMinimalAccess.Clear();
		}
	}
}