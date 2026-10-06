using US13.Core.Chat;
using US13.Managers;
using US13.Objects.Engineering;
using US13.Systems.NtOS.Core;
using Util;

namespace US13.Systems.NtOS.Validation
{
	public class IsApcPowerActive : INtOSCommandValidation
	{
		public APCPoweredDevice ApcPowered;

		public bool CanRun(NtOS_Device device, PlayerInfo callingPlayer)
		{
			if (ApcPowered == null) return true;
			if (ApcPowered.State is PowerState.On or PowerState.OverVoltage) return true;
			Chat.AddExamineMsg(callingPlayer.Script.gameObject, $"The {device.gameObject.ExpensiveName()} does not have power.");
			return false;
		}
	}
}