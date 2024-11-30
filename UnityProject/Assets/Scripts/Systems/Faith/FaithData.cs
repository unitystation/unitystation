using System.Collections.Generic;
using Logs;

namespace Systems.Faith
{
	public class FaithData
	{
		public Faith Faith { get; set; }
		public int Points { get; set; }
		public List<PlayerScript> FaithMembers { get; set; }

		public void AddMember(PlayerScript newMember)
		{
			if (FaithMembers.Contains(newMember)) return;
			FaithMembers.Add(newMember);
			foreach (var property in Faith.FaithProperties)
			{
				property.OnJoinFaith(newMember);
			}
		}

		public void RemoveMember(PlayerScript member)
		{
			foreach (var property in Faith.FaithProperties)
			{
				property.OnLeaveFaith(member);
			}
			FaithMembers.Remove(member);
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
			Loggy.Info("[FaithData/SetupFaith] Setting up faith data for " + Faith.FaithName);
			foreach (var property in Faith.FaithProperties)
			{
				property.Setup(this);
			}
		}
	}
}