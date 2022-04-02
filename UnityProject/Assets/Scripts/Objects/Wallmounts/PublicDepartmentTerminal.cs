using Communications;
using Systems.Electricity;

namespace Objects.Wallmounts
{
	public class PublicDepartmentTerminal : SignalEmitter, IAPCPowerable, ICheckedInteractable<HandApply>
	{
		public Department Department; //For displaying what the console's department is for
		public Access terminalRequieredAccess; //only with select access can actually use this

		private float currentVoltage; // for the UI
		private bool isPowered = false; //To disable the terminal if there is no power detected
		private IDCard currentLogin; //the currently logged in user

		public float CurrentVoltage => currentVoltage;
		public IDCard CurrentLogin => currentLogin;

		protected override bool SendSignalLogic()
		{
			return currentLogin != null && isPowered;
		}

		public override void SignalFailed()
		{
			if(isPowered == false) return;
			if (currentLogin == null || currentLogin.HasAccess(terminalRequieredAccess) == false)
			{
				Chat.AddLocalMsgToChat("A huge red X appears on the terminal's screen as it says 'access denied'", gameObject);
				return;
			}
			Chat.AddLocalMsgToChat("A huge ! appears on the terminal's screen as it says 'An error occured.'", gameObject);
		}

		public void PowerNetworkUpdate(float voltage)
		{
			currentVoltage = voltage;
		}

		public void StateUpdate(PowerState state)
		{
			if (state == PowerState.Off)
			{
				isPowered = false;
				return;
			}

			isPowered = true;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return isPowered != false && DefaultWillInteract.Default(interaction, side) != false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			throw new System.NotImplementedException();
		}
	}
}