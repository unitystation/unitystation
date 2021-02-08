using System;
using System.Collections.Generic;
using Items.PDA;
using NaughtyAttributes;
using UnityEngine;

namespace Systems.Access
{
	public class AccessHolder: MonoBehaviour
	{
		[SerializeField]
		[ReorderableList]
		[Tooltip("Assign all acceses this object should store to be checked against the relevant objects that require access.")]
		private List<AccessRestrictions> restrictions = default;

		private PDALogic pdaLogic;

		// If PDA, let's get the restrictions from the ID inside PDA.
		public List<AccessRestrictions> Restrictions => pdaLogic != null ? pdaLogic.IDCard.Restrictions : restrictions;

		private void Awake()
		{
			pdaLogic = GetComponent<PDALogic>();
		}

		/// <summary>
		/// Interface to update access restrictions on this access holder.
		/// </summary>
		/// <param name="newRestrictions">Complete list of wanted restrictions</param>
		public void SetRestrictions(List<AccessRestrictions> newRestrictions)
		{
			if (pdaLogic)
			{
				pdaLogic.IDCard.gameObject.GetComponent<AccessHolder>().SetRestrictions(newRestrictions);
				return;
			}

			restrictions = newRestrictions;
		}
	}
}