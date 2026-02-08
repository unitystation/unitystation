using System.Collections.Generic;
using US13.UI.Systems.AdminTools.ObjectiveManager;

namespace US13.Systems.Antagonists
{
	public class TeamsInfo
	{
		public List<TeamInfo> TeamsInfos = new();
	}

	public class TeamInfo
	{
		public List<TeamMemberInfo> MembersInfo = new List<TeamMemberInfo>();
		public List<ObjectiveInfo> ObjsInfo = new List<ObjectiveInfo>();
		public string Name;
		public int Index = -1;
		public uint ID = 0;
	}

	public class TeamMemberInfo
	{
		public string Id;
		public bool isToRemove = false;
	}
}