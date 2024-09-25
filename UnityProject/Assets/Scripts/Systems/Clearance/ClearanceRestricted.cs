﻿using System;
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
			return GrabClearance(entity).Any(HasClearance) ;
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

		public static List<IClearanceSource> GrabClearance(GameObject entity)
		{
			return GrabClearance(entity, true).Select(x => (x as IClearanceSource)).ToList();;
		}

		public static List<GameObject> GrabClearanceObject(GameObject entity)
		{
			return GrabClearance(entity, false).Select(x => (x as GameObject)).ToList();
		}

		private static List<object> GrabClearance(GameObject entity, bool returnIClearanceSource)
		{
			var ReturningList = new List<object>();

			if (entity == null)
			{
				return ReturningList;
			}
			var playerStorage = entity.GetComponent<DynamicItemStorage>();
			if (playerStorage != null)
			{
				List<ItemSlot> slotsToSearch = new List<ItemSlot>();
				// Only check the hand in use for a clearance item to avoid blocking other slots.
				// That way players can still hold other PDAs in their hands without it blocking their access until they switch to that hand as their active.
				slotsToSearch.Add(playerStorage.GetActiveHandSlot());
				slotsToSearch.AddRange(playerStorage.GetNamedItemSlots(NamedSlot.id));
				slotsToSearch.AddRange(playerStorage.GetNamedItemSlots(NamedSlot.belt));
				slotsToSearch.AddRange(playerStorage.GetNamedItemSlots(NamedSlot.suitStorage));
				ReturningList.AddRange(SearchItemSlotsForClearance(slotsToSearch, returnIClearanceSource));
			}
			var itemStorage = entity.GetComponent<ItemStorage>();
			if (itemStorage != null)
			{
				ReturningList.AddRange(SearchItemSlotsForClearance(itemStorage.GetOccupiedSlots(), returnIClearanceSource));
			}
			if (entity.TryGetComponent<IClearanceSource>(out var clearanceSource))
			{
				ReturningList.Add(returnIClearanceSource ? clearanceSource : entity);
			}
			return ReturningList;
		}

		public static List<object> SearchItemSlotsForClearance(List<ItemSlot> slots, bool returnIClearanceSource)
		{

			List<object> ClearancesToReturn = new List<object>();

			foreach (var slot in slots)
			{
				if (slot == null) continue;
				if (slot.ItemObject != null && slot.ItemObject.TryGetComponent<IClearanceSource>(out var idObject))
				{
					ClearancesToReturn.Add(returnIClearanceSource ? idObject : slot.ItemObject);
				}
			}
			return ClearancesToReturn;
		}
	}
}
