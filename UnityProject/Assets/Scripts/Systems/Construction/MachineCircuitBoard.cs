using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Machines
{
	/// <summary>
	/// Allows an object to function as a circuitboard for a computer, being placed into a computer frame and
	/// causing a particular computer to be spawned on completion.
	/// </summary>
	public class MachineCircuitBoard : MonoBehaviour
	{
		[Tooltip("Machine parts scriptableobject; what gameobject to spawn, what parts needed")]
		[SerializeField]
		private MachineParts machineParts = null;

		/// <summary>
		/// What machine parts are needed
		/// </summary>
		public MachineParts MachinePartsUsed => machineParts;

		public void SetMachineParts(MachineParts MachineParts)
		{
			machineParts = MachineParts;
		}
	}
}
