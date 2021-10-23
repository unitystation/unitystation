using System.Collections.Generic;
using System.Linq;
using Mirror;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.Clearance
{
	public class BasicClearanceProvider: NetworkBehaviour, IClearanceProvider
	{
		[SerializeField]
		[ReorderableList]
		[Tooltip("Assign all clearances this object should store to be checked against the relevant objects that " +
		         "requires clearance.")]
		protected List<Clearance> clearance = default;

		[SerializeField]
		[ReorderableList]
		[Tooltip("Assign clearances this object should store to be checked against the relevant objects that require " +
		         "clearances when the round is lowpop.")]
		protected List<Clearance> lowPopClearance = default;


		private readonly SyncList<Clearance> syncedClearance = new SyncListClearance();
		private readonly SyncList<Clearance> syncedLowpopClearance = new SyncListClearance();

		public IEnumerable<Clearance> GetClearance()
		{
			//TODO add a way to check for lowpop and return that instead
			return clearance;
		}

		private void Start()
		{
			syncedClearance.Callback += OnClearanceListUpdated;
			syncedLowpopClearance.Callback += OnLowPopClearanceListUpdated;
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			if (clearance.Any())
			{
				ServerSetClearance(clearance);
			}

			if (lowPopClearance.Any())
			{
				ServerSetLowPopClearance(lowPopClearance);
			}
		}

		/// <summary>
		/// Interface to update access definitions on this issued clearance object.
		/// </summary>
		/// <param name="newClearance">Complete list of wanted clearance</param>
		[Server]
		public void ServerSetClearance(IEnumerable<Clearance> newClearance)
		{
			ServerClearClearance();
			//FIXME this looks shitty, every element will trigger the syncing call back. Is there no way to set
			//values directly?
			foreach (var c in newClearance)
			{
				syncedClearance.Add(c);
				netIdentity.isDirty = true;
			}
		}

		/// <summary>
		/// Interface to update access definitions on this issued clearance object for low pop.
		/// </summary>
		/// <param name="newClearance">Complete list of wanted clearance</param>
		[Server]
		public void ServerSetLowPopClearance(IEnumerable<Clearance> newClearance)
		{
			ServerClearLowPopClearance();
			//FIXME this looks shitty, every element will trigger the syncing call back. Is there no way to set
			//values directly?
			foreach (var c in newClearance)
			{
				syncedLowpopClearance.Add(c);
				netIdentity.isDirty = true;
			}
		}

		[Server]
		public void ServerClearClearance()
		{
			syncedClearance.Clear();
			netIdentity.isDirty = true;
		}

		[Server]
		public void ServerClearLowPopClearance()
		{
			syncedLowpopClearance.Clear();
			netIdentity.isDirty = true;
		}

		// ReSharper disable Unity.PerformanceAnalysis
		private void OnClearanceListUpdated(SyncList<Clearance>.Operation op, int index, Clearance oldAccess,
			Clearance newAccess )
		{
			netIdentity.isDirty = true;
			switch (op)
			{
				case SyncList<Clearance>.Operation.OP_ADD:
					syncedClearance.Add(newAccess);
					break;
				case SyncList<Clearance>.Operation.OP_CLEAR:
					syncedClearance.Clear();
					break;
				case SyncList<Clearance>.Operation.OP_INSERT:
					syncedClearance.Insert(index, newAccess);
					break;
				case SyncList<Clearance>.Operation.OP_REMOVEAT:
					syncedClearance.RemoveAt(index);
					break;
				case SyncList<Clearance>.Operation.OP_SET:
					break;
				default:
					Logger.LogError($"Tried to update access sync list with unexpected operation: {op}");
					break;
			}
		}

		// ReSharper disable Unity.PerformanceAnalysis
		private void OnLowPopClearanceListUpdated(SyncList<Clearance>.Operation op, int index, Clearance oldAccess,
			Clearance newAccess )
		{
			netIdentity.isDirty = true;
			switch (op)
			{
				case SyncList<Clearance>.Operation.OP_ADD:
					syncedLowpopClearance.Add(newAccess);
					break;
				case SyncList<Clearance>.Operation.OP_CLEAR:
					syncedLowpopClearance.Clear();
					break;
				case SyncList<Clearance>.Operation.OP_INSERT:
					syncedLowpopClearance.Insert(index, newAccess);
					break;
				case SyncList<Clearance>.Operation.OP_REMOVEAT:
					syncedLowpopClearance.RemoveAt(index);
					break;
				case SyncList<Clearance>.Operation.OP_SET:
					break;
				default:
					Logger.LogError($"Tried to update access sync list with unexpected operation: {op}");
					break;
			}
		}
	}
}