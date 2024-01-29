using System.Collections.Generic;
using Logs;

namespace Systems.Faith
{
	public class FaithData
	{
		public Faith Faith { get; set; }
		public int Points { get; set; }
		public List<PlayerScript> FaithMembers { get; set; }
		public List<PlayerScript> FaithLeaders { get; set; }

		public void AddMember(PlayerScript newMember)
		{
			RemoveMember(newMember);
			FaithMembers.Add(newMember);
			foreach (var property in Faith.FaithProperties)
			{
				property.OnJoinFaith(newMember);
			}
		}

		public void RemoveMember(PlayerScript member)
		{
			FaithMembers.Remove(member);
			foreach (var property in Faith.FaithProperties)
			{
				property.OnLeaveFaith(member);
			}
		}

		public void RemoveAllMembers()
		{
			foreach (var member in FaithMembers)
			{
				RemoveMember(member);
			}
		}

		public void SetupFaith()
		{
			Loggy.Log("[FaithData/SetupFaith] Setting up faith data for " + Faith.FaithName);
			foreach (var property in Faith.FaithProperties)
			{
				property.Setup(this);
			}
		}
	}
}