using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.Access
{
	public class AccessCheckable: MonoBehaviour
	{
		[SerializeField]
		[ReorderableList]
		[Tooltip("All access definitions this object checks for")]
		private List<AccessDefinitions> requiredAccess = new List<AccessDefinitions>{AccessDefinitions.NONE};

		[SerializeField]
		[Tooltip("What type of check will be done. \"Any\" means the access requester must have at least one of the " +
		         "definitions, while \"All\" means the requester must have all of them.")]
		private AccessType type = AccessType.Any;

		/// <summary>
		/// Checks if the list of access a requester has coincides with the required access this game object has defined.
		/// </summary>
		/// <param name="requesterAccess">List of AccessDefinitions the requester of access has on them.</param>
		/// <returns>True if the access should be granted</returns>
		public bool HasAccess(List<AccessDefinitions> requesterAccess)
		{
			// If null or no defined access, access is granted immediately
			if (requiredAccess == null ||
			    requiredAccess.Any() == false ||
			    requiredAccess.Equals(new List<AccessDefinitions> {AccessDefinitions.NONE}))
			{
				return true;
			}

			switch (type)
			{
				case AccessType.Any:
					return requesterAccess.Intersect(requiredAccess).Any();
				case AccessType.All:
					return requiredAccess.Except(requesterAccess).Any() == false;
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
		public bool HasAccess(GameObject player)
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
			if (idSlot != null && idSlot.TryGetComponent<AccessHolder>(out var idAccess))
			{
				return HasAccess(idAccess.Access);
			}

			// Nothing worked, let's go with active hand
			var activeHandObject = playerStorage.GetActiveHandSlot().ItemObject;
			if (activeHandObject != null && activeHandObject.TryGetComponent<AccessHolder>(out var handAccess))
			{
				return HasAccess(handAccess.Access);
			}

			return false;
		}

		/// <summary>
		/// Public interface to set access for this object.
		/// </summary>
		/// <param name="newAccess">List of all access that this object will check for.</param>
		public void SetAccess(List<AccessDefinitions> newAccess)
		{
			requiredAccess = newAccess;
		}
	}
}