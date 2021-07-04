using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.Clearance
{
	public class ClearanceCheckable: MonoBehaviour
	{
		[SerializeField]
		[ReorderableList]
		[Tooltip("All access definitions this object checks for")]
		private List<Clearance> requiredClearance = new List<Clearance>();

		[SerializeField]
		[Tooltip("What type of check will be done. \"Any\" means the access requester must have at least one of the " +
		         "definitions, while \"All\" means the requester must have all of them.")]
		private CheckType type = CheckType.Any;

		/// <summary>
		/// Checks if the list of access a requester has coincides with the required access this game object has defined.
		/// </summary>
		/// <param name="requesterClearance">List of AccessDefinitions the requester of access has on them.</param>
		/// <returns>True if the access should be granted</returns>
		public bool HasClearance(IEnumerable<Clearance> requesterClearance)
		{
			// If null or no defined access, access is granted immediately
			if (requiredClearance == null ||
			    requiredClearance.Any() == false)
			{
				return true;
			}

			switch (type)
			{
				case CheckType.Any:
					return requesterClearance.Intersect(requiredClearance).Any();
				case CheckType.All:
					return requiredClearance.Except(requesterClearance).Any() == false;
				default:
					return true;
			}
		}

		/// <summary>
		/// Checks if the player requesting access has access to this particular game object.
		/// It will iterate through their ID and then PDA in both the ID slot and Active hand to determine it.
		/// </summary>
		/// <param name="player">Game object that represents this player</param>
		/// <returns>True if the player has access.</returns>
		public bool HasClearance(GameObject player)
		{
			if (player == null)
			{
				return false;
			}

			var playerStorage = player.GetComponent<ItemStorage>();
			if (playerStorage == null)
			{
				return false;
			}

			// Try get object in ID slot
			var idSlot = playerStorage.OrNull()?.GetNamedItemSlot(NamedSlot.id).ItemObject;
			if (idSlot != null && idSlot.TryGetComponent<IClearanceProvider>(out var idObject))
			{
				return HasClearance(idObject.GetClearance());
			}

			// Nothing worked, let's go with active hand
			var activeHandObject = playerStorage.GetActiveHandSlot().ItemObject;
			if (activeHandObject != null && activeHandObject.TryGetComponent<IClearanceProvider>(out var handObject))
			{
				return HasClearance(handObject.GetClearance());
			}

			return false;
		}

		/// <summary>
		/// Public interface to set access for this object.
		/// </summary>
		/// <param name="newClearance">List of all access that this object will check for.</param>
		public void SetClearance(List<Clearance> newClearance)
		{
			requiredClearance = newClearance;
		}
	}

}