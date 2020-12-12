using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace ScriptableObjects
{
	/// <summary>
	/// Contains a list of all possible ghost roles. This information is available on both server and client.
	/// </summary>
	[CreateAssetMenu(fileName = "GhostRoleList", menuName = "ScriptableObjects/Systems/GhostRoles/GhostRoleList")]
	public class GhostRoleList : ScriptableObject
	{
		[Tooltip("A list of roles that are potentially available to ghosts during a round.")]
		[SerializeField, ReorderableList]
		private List<GhostRoleData> ghostRoles = new List<GhostRoleData>();

		public List<GhostRoleData> GhostRoles => ghostRoles;
	}
}
