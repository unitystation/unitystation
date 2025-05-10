using Core.Admin.Logs;
using UI.Systems.AdminTools.AdminLogs;
using UnityEngine;

namespace UI.Systems.AdminTools.AdminLogs
{
	public class LogInfoUI : MonoBehaviour
	{
		public SubLogInfoUI Text;
		public SubLogInfoUI Core;
		public SubLogInfoUI StoredIn;
		public SubLogInfoUI ControlledBy;
		public SubLogInfoUI Position;

		public LongTermLogEntry.LogItems Info;

		public bool Expanded = false;

		public void SetUp(LongTermLogEntry.LogItems InInfo)
		{
			Info = InInfo;
			if (string.IsNullOrWhiteSpace(Info.Info) == false)
			{
				Text.SetUp(Info);
			}
			else
			{
				Core.SetUp(Info);
			}
		}

		public void Expand()
		{
			StoredIn.SetUp(Info);
			ControlledBy.SetUp(Info);
			Position.SetUp(Info);
			Expanded = true;
		}

	}
}