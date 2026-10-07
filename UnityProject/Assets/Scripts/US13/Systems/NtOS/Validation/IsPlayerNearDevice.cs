using UnityEngine;
using US13.Managers;
using US13.Systems.NtOS.Core;
using Util;

namespace US13.Systems.NtOS.Validation
{
	public class IsPlayerNearDevice : INtOSCommandValidation
	{
		public float MinimumDistance = 4f;

		public bool CanRun(NtOS_Device device, PlayerInfo playerInfo)
		{
			if (playerInfo.Mind.isGhosting) return false;
			if (playerInfo.Mind.CurrentPlayScript == null) return false;
			if (Vector2.Distance(playerInfo.Mind.CurrentPlayScript.gameObject.AssumedWorldPosServer(),
				    device.gameObject.AssumedWorldPosServer()) > MinimumDistance) return false;
			return true;
		}
	}
}