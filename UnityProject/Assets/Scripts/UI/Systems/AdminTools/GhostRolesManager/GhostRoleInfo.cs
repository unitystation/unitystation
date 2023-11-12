using System.Collections.Generic;

namespace AdminTools
{
	public class GhostRoleInfo
	{
		public int MinPlayers = 2;
		public int MaxPlayers = 4;
		public int RoleKey = -1;
		public int RoleIndex = 0;
		public float Timeout = 0;
		public bool ToRemove = false;
		public bool IsNew = false;
	}

	public class GhostRolesInfo
	{
		public List<GhostRoleInfo> Roles = new List<GhostRoleInfo>();
	}
}