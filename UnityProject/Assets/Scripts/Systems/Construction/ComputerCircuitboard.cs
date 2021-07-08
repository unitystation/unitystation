using UnityEngine;

namespace Items.Construction
{
	/// <summary>
	/// Allows an object to function as a circuitboard for a computer, being placed into a computer frame and
	/// causing a particular computer to be spawned on completion.
	/// </summary>
	public class ComputerCircuitboard : MonoBehaviour
	{
		[Tooltip("Computer which should be spawned when this circuitboard's frame is constructed.")]
		[SerializeField]
		private GameObject computerToSpawn = null;

		/// <summary>
		/// Computer which should be spawned when this circuitboard's frame is constructed
		/// </summary>
		public GameObject ComputerToSpawn => computerToSpawn;
	}
}
