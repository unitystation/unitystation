using System;

namespace Systems.Faith.Miracles
{
	public class HealFaithMembers : IFaithMiracle
	{
		public int MiracleCost { get; set; }
		public void DoMiracle()
		{
			foreach (var member in FaithManager.Instance.FaithMembers)
			{
				if (member.IsDeadOrGhost) continue;
				member.playerHealth.FullyHeal();
				Chat.AddExamineMsg(member.gameObject, "");
			}
		}
	}
}