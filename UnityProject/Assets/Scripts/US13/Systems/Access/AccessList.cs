using System.Collections.Generic;
using UnityEngine;

namespace US13.Systems.Access
{
	/// <summary>
	/// Used to sort accesses by type
	/// </summary>
	[CreateAssetMenu(fileName = "AccessList", menuName = "ScriptableObjects/AccessList")]
	public class AccessList : ScriptableObject
	{
		[SerializeField]
		private List<Clearance.Clearance> clearances = null;
		public List<Clearance.Clearance> Clearances => clearances;
	}
}
