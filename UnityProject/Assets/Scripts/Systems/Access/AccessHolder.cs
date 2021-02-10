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
		private List<AccessDefinitions> access = default;

		[SerializeField]
		[ReorderableList]
		[Tooltip("Assign accesses this object should store to be checked against the relevant objects that require access when the round is lowpop.")]
		private List<AccessDefinitions> minimalAccess = default;

		private PDALogic pdaLogic;

		public List<AccessDefinitions> Access
		{
			get
			{
				// If PDA, let's get the Access from the ID inside PDA.
				if (pdaLogic != null)
				{
					return pdaLogic.Access;
				}

				//TODO check for lowpop here
				// if (lowpop) return minimalAccess;
				return access;
			}
		}

		private void Awake()
		{
			pdaLogic = GetComponent<PDALogic>();
		}

		/// <summary>
		/// Interface to update access restrictions on this access holder.
		/// </summary>
		/// <param name="newAccess">Complete list of wanted access</param>
		public void SetAccess(List<AccessDefinitions> newAccess)
		{
			if (pdaLogic)
			{
				pdaLogic.IDCard.gameObject.GetComponent<AccessHolder>().SetAccess(newAccess);
				return;
			}

			access = newAccess;
			//TODO handle this better maybe. This means any custom access would override the lowpop list.
			minimalAccess = newAccess;
		}
	}
}