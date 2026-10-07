using US13.Player;

namespace US13.Systems.NtOS.Core
{
	public interface INtOSCommandValidation
	{
		public bool CanRun(NtOS_Device device, PlayerScript playerInfo);
	}
}