using US13.Managers;

namespace US13.Systems.NtOS.Core
{
	public interface INtOSCommandValidation
	{
		public bool CanRun(NtOS_Device device, PlayerInfo playerInfo);
	}
}