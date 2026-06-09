using System;
using Mirror;
using UnityEngine;

namespace US13.Actions.V2
{
	[Serializable]
	public class CooldownInfo
	{
		public string ActionId;
		public long CooldownEndTicks;

		public CooldownInfo() { }

		public CooldownInfo(string actionId, DateTime cooldownEnd)
		{
			ActionId = actionId;
			CooldownEndTicks = cooldownEnd.Ticks;
		}

		public DateTime GetCooldownEnd()
		{
			return new DateTime(CooldownEndTicks);
		}
	}
}