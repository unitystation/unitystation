using UnityEngine;
using US13.Core.Chat;
using US13.Player;
using US13.Systems.NtOS.Core;
using Util;

namespace US13.Systems.NtOS.Validation
{
	public class IsPlayerNearDevice : INtOSCommandValidation
	{
		public float MinimumDistance = 4f;

		public bool CanRun(NtOS_Device device, PlayerScript playerInfo)
		{
			if (playerInfo == null) return false;
			if (playerInfo.GameObject == null) return false;
			if (Vector2.Distance(playerInfo.gameObject.AssumedWorldPosServer(), device.gameObject.AssumedWorldPosServer()) > MinimumDistance)
			{
				Chat.AddExamineMsg(playerInfo.GameObject, "You are too far away from this device.");
				return false;
			}
			return true;
		}
	}
}