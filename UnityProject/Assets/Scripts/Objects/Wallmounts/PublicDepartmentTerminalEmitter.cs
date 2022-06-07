using Communications;

namespace Objects.Wallmounts
{
	public class PublicDepartmentTerminalEmitter : SignalEmitter
	{
		public PublicDepartmentTerminal Terminal;

		protected override bool SendSignalLogic()
		{
			return Terminal.CurrentLogin != null && Terminal.isPowered;
		}

		public override void SignalFailed()
		{
			if(Terminal.isPowered == false) return;
			if (Terminal.CurrentLogin == null || Terminal.CurrentLogin.HasAccess(Terminal.terminalRequieredAccess) == false)
			{
				Chat.AddLocalMsgToChat("A huge red X appears on the terminal's screen as it says 'access denied'", gameObject);
				return;
			}
			Chat.AddLocalMsgToChat("A huge ! appears on the terminal's screen as it says 'An error occured.'", gameObject);
		}
	}
}