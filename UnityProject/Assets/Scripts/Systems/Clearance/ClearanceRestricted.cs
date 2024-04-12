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

			// If the player has null access, access is denied
			if (clearanceSource?.GetCurrentClearance == null)
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
			if (entity == null) return false;
			var clearanceSource = GrabClearance(entity);
			return clearanceSource == null ? HasClearance(null as IClearanceSource) : HasClearance(GrabClearance(entity));
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

		public static IClearanceSource GrabClearance(GameObject entity)
		{
			return GrabClearance(entity, true) as IClearanceSource;
		}

		public static GameObject GrabClearanceObject(GameObject entity)
		{
			return GrabClearance(entity, false) as GameObject;
		}

		private static object GrabClearance(GameObject entity, bool returnIClearanceSource)
		{
			if (entity == null)
			{
				return null;
			}
			var playerStorage = entity.GetComponent<DynamicItemStorage>();
			if (playerStorage != null)
			{
				List<ItemSlot> slotsToSearch = new List<ItemSlot>();
				slotsToSearch.AddRange(playerStorage.GetHandSlots());
				slotsToSearch.AddRange(playerStorage.GetNamedItemSlots(NamedSlot.id));
				slotsToSearch.AddRange(playerStorage.GetNamedItemSlots(NamedSlot.belt));
				slotsToSearch.AddRange(playerStorage.GetNamedItemSlots(NamedSlot.suitStorage));
				return SearchItemSlotsForClearance(slotsToSearch, returnIClearanceSource);
			}
			var itemStorage = entity.GetComponent<ItemStorage>();
			if (itemStorage != null)
			{
				return SearchItemSlotsForClearance(itemStorage.GetOccupiedSlots(), returnIClearanceSource);
			}
			if (entity.TryGetComponent<IClearanceSource>(out var clearanceSource))
			{
				return returnIClearanceSource ? clearanceSource : entity;
			}
			return null;
		}

		public static object SearchItemSlotsForClearance(List<ItemSlot> slots, bool returnIClearanceSource)
		{
			foreach (var slot in slots)
			{
				if (slot.ItemObject != null && slot.ItemObject.TryGetComponent<IClearanceSource>(out var idObject))
				{
					return returnIClearanceSource ? idObject : slot.ItemObject;
				}
			}
			return null;
		}
	}
}
