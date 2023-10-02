using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Antagonists;
using Logs;

namespace ScriptableObjects
{
	/// <summary>
	/// Contains a list of all possible ghost roles. This information is available on both server and client.
	/// </summary>
	[CreateAssetMenu(fileName = "GhostRoleList", menuName = "ScriptableObjects/Systems/GhostRoles/GhostRoleList")]
	public class GhostRoleList: SingletonScriptableObject<GhostRoleList>
	{
		[Tooltip("A list of roles that are potentially available to ghosts during a round.")]
		[SerializeField, ReorderableList]
		private List<GhostRoleData> ghostRoles = new List<GhostRoleData>();

		public List<GhostRoleData> GhostRoles => ghostRoles;

		public short GetIndex(GhostRoleData ghostRole)
		{
			return (short)ghostRoles.IndexOf(ghostRole);
		}

		public GhostRoleData FromIndex(short index)
		{
			if (index < 0 || index > ghostRoles.Count - 1)
			{
				Loggy.LogErrorFormat("AntagData: no Objective found at index {0}", Category.Antags, index);
				return null;
			}

			return ghostRoles[index];
		}
	}
}
