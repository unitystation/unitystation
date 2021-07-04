using System.Collections.Generic;
using Items.PDA;
using UnityEngine;

namespace Systems.Clearance
{
	[RequireComponent(typeof(PDALogic))]
	public class PDAClearanceProvider: MonoBehaviour, IClearanceProvider
	{
		private PDALogic pdaLogic;

		private void Awake()
		{
			pdaLogic = GetComponent<PDALogic>();
		}

		public IEnumerable<Clearance> GetClearance()
		{
			return pdaLogic.IDCard.OrNull()?.GetComponent<IClearanceProvider>()?.GetClearance();
		}
	}
}