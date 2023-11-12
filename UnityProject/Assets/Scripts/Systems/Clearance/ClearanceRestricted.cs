using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.Clearance
{
	public class ClearanceRestricted: MonoBehaviour
	{
		[SerializeField]
		[ReorderableList]
		[Tooltip("All access definitions this object checks for")]
		private List<Clearance> requiredClearance = new();
		public IEnumerable<Clearance> RequiredClearance => requiredClearance;

		[SerializeField]
		[Tooltip("What type of check will be done. \"Any\" means the access requester must have at least one of the " +
		         "definitions, while \"All\" means the requester must have all of them.")]
		private CheckType type = CheckType.Any;


		/// <summary>
		/// Checks if the list of access a requester has coincides with the required access this game object has defined.
		/// </summary>
		/// <param name="clearanceSource">Object or mob that is requesting clearance on this restricted functionality.</param>
		/// <returns>True if the access should be granted</returns>
		public bool HasClearance(IClearanceSource clearanceSource)
		{
			// If null or no defined access, access is granted immediately
			if (requiredClearance == null ||
			    requiredClearance.Any() == false)
			{
				return true;
			}

			if (requiredClearance.Contains(Clearance.BasicPublicAccess))
			{
				Loggy.LogError($"{this.name} has null Clearance potentially letting anyone access at localPosition {this.transform.localPosition} on {this.gameObject.GetMatrixRoot()}");
				return true;
			}

			if (clearanceSource == null) return false;

			// If the player has null access, access is denied
			if (clearanceSource.GetCurrentClearance == null)
			{
				return false;
			}

			return type switch
			{
				CheckType.Any => clearanceSource.GetCurrentClearance.Intersect(requiredClearance).Any(),
				CheckType.All => requiredClearance.Except(clearanceSource.GetCurrentClearance).Any() == false,
				_ => true
			};
		}

		/// <summary>
		/// Checks if the entity requesting access has access to this restricted functionality.
		/// It will try to get the clearance source from root, then attempt player checks to get the clearance source.
		///
		/// This is expensive, so use sparingly and prefer to use the overload that takes the clearance source directly if you have it!
		/// </summary>
		/// <param name="entity">Game object requesting clearance. Could be a player, a mob, an item, etc</param>
		/// <returns>True if the entity has clearance</returns>
		public bool HasClearance(GameObject entity)
		{
			if (entity == null)
			{
				return false;
			}

			// Is the entity an item or mob with clearance source at root?
			if (entity.TryGetComponent<IClearanceSource>(out var clearanceSource))
			{
				if (HasClearance(clearanceSource)) return true;
			}

			// Is the entity a player with dynamic storage?
			var playerStorage = entity.GetComponent<DynamicItemStorage>();
			if (playerStorage == null)
			{
				return false;
			}

			// Check hand first
			var activeHandObject = playerStorage.GetActiveHandSlot()?.ItemObject;
			if (activeHandObject != null && activeHandObject.TryGetComponent<IClearanceSource>(out var handObject))
			{
				if (HasClearance(handObject)) return true;
			}


			// Try get object in ID slot
			foreach (var slot in playerStorage.GetNamedItemSlots(NamedSlot.id))
			{
				if (slot.ItemObject != null && slot.ItemObject.TryGetComponent<IClearanceSource>(out var idObject))
				{
					return HasClearance(idObject);
				}
			}

			return HasClearance(null as IClearanceSource);

		}

		/// <summary>
		/// Convenience method to attempt an interaction taking into account the clearance restrictions.
		/// </summary>
		/// <param name="clearanceSource">Source from which we will take issued clearances</param>
		/// <param name="success">What will happen if success</param>
		/// <param name="failure">What will happen if the restriction check is a failure</param>
		public void PerformWithClearance(IClearanceSource clearanceSource, Action success, Action failure)
		{
			if (HasClearance(clearanceSource))
			{
				success.Invoke();
				return;
			}

			failure.Invoke();
		}

		/// <summary>
		/// Public interface to set access for this object.
		/// </summary>
		/// <param name="newClearance">List of all access that this object will check for.</param>
		public void SetClearance(List<Clearance> newClearance)
		{
			requiredClearance = newClearance;
		}

		/// <summary>
		/// Public interface to set the check type for this object.
		/// </summary>
		public void SetCheckType(CheckType newType)
		{
			type = newType;
		}
	}
}
