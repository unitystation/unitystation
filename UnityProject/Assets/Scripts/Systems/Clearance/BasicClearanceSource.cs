using System.Collections.Generic;
using System.Linq;
using Mirror;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.Clearance
{
	/// <summary>
	/// Component to make an object a basic clearance source, like an ID card for example.
	/// Simply add this component to the object and set the different clearance levels for normal population and low population.
	/// </summary>
	public class BasicClearanceSource: NetworkBehaviour, IClearanceSource
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


		[SyncVar]
		public bool ClearanceDisabled = false;

		private readonly SyncList<Clearance> syncedClearance = new SyncListClearance();
		private readonly SyncList<Clearance> syncedLowpopClearance = new SyncListClearance();

		public IEnumerable<Clearance> IssuedClearance
		{
			get
			{
				if (ClearanceDisabled)
				{
					return Enumerable.Empty<Clearance>();
				}
				return syncedClearance;
			}
		}

		public IEnumerable<Clearance> LowPopIssuedClearance
		{
			get
			{
				if (ClearanceDisabled)
				{
					return Enumerable.Empty<Clearance>();
				}
				return syncedLowpopClearance;
			}
		}

		public override void OnStartServer()
		{
			ServerSetClearance(clearance);
			ServerSetLowPopClearance(lowPopClearance);
		}

		/// <summary>
		/// Update clearance list on this source by adding a single one.
		/// </summary>
		/// <param name="newClearance"></param>
		[Server]
		public void ServerAddClearance(Clearance newClearance)
		{
			if (syncedClearance.Contains(newClearance))
			{
				return;
			}

			syncedClearance.Add(newClearance);
			netIdentity.isDirty = true;
		}

		/// <summary>
		/// Update clearance list on this source by setting its value to a new one.
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
				ServerAddClearance(c);
			}
		}

		/// <summary>
		/// Update low pop clearance list on this source by adding a single one.
		/// </summary>
		/// <param name="newClearance"></param>
		[Server]
		public void ServerAddLowPopClearance(Clearance newClearance)
		{
			if (syncedLowpopClearance.Contains(newClearance))
			{
				return;
			}

			syncedLowpopClearance.Add(newClearance);
			netIdentity.isDirty = true;
		}

		/// <summary>
		/// Update low pop clearance list on this source by setting its value to a new one.
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
				ServerAddLowPopClearance(c);
			}
		}

		[Server]
		public void ServerRemoveClearance(Clearance forRemoval)
		{
			syncedClearance.Remove(forRemoval);
			netIdentity.isDirty = true;
		}

		[Server]
		public void ServerRemoveLowPopClearance(Clearance forRemoval)
		{
			syncedLowpopClearance.Remove(forRemoval);
			netIdentity.isDirty = true;
		}

		/// <summary>
		/// Clears the current clearance list.
		/// </summary>
		[Server]
		public void ServerClearClearance()
		{
			syncedClearance.Clear();
			netIdentity.isDirty = true;
		}

		/// <summary>
		/// Clears the current low pop clearance list.
		/// </summary>
		[Server]
		public void ServerClearLowPopClearance()
		{
			syncedLowpopClearance.Clear();
			netIdentity.isDirty = true;
		}
	}
}