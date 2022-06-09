using Communications;
using Systems.Electricity;
using UnityEngine;
using UI.Objects.Wallmounts;
using System.Collections.Generic;
using Mirror;

namespace Objects.Wallmounts
{
	public struct MessageData
	{
		public MessageData(string msg, bool urgent, string name, string target, string sender)
		{
			message = msg;
			isUrgent = urgent;
			Sender = name;
			senderDepartment = sender;
			targetDepartment = target;
		}

		public string message;
		public bool isUrgent;
		public string Sender;
		public string senderDepartment;
		public string targetDepartment;
	}
	
	public class PublicDepartmentTerminal : SignalEmitter, IAPCPowerable, ICheckedInteractable<HandApply>
	{
		public Department Department; //For displaying what the console's department is for
		public Access terminalRequieredAccess; //only with select access can actually use this

		private float currentVoltage; // for the UI
		public bool isPowered = false; //To disable the terminal if there is no power detected

		[SyncVar]
		private IDCard currentLogin; //the currently logged in user, we actually do need to sync this as the requests are being sent from the client so all of them need this.

		[HideInInspector]
		public GUI_PublicTerminal terminalGUI;

		[HideInInspector]
		public MessageData sendMessageData = new MessageData();

		[HideInInspector]
		public readonly SyncList<MessageData> receivedMessageData = new SyncList<MessageData>();

		public float CurrentVoltage => currentVoltage;
		public IDCard CurrentLogin => currentLogin;

		#region Signals

		protected override bool SendSignalLogic()
		{
			return CurrentLogin != null && isPowered;
		}

		public override void SignalFailed()
		{
			if (isPowered == false) return;
			if (CurrentLogin == null || CurrentLogin.HasAccess(terminalRequieredAccess) == false)
			{
				Chat.AddLocalMsgToChat("A huge red X appears on the terminal's screen as it says 'access denied'", gameObject);
				return;
			}
			Chat.AddLocalMsgToChat("A huge ! appears on the terminal's screen as it says 'An error occured.'", gameObject);
		}

		[Command(requiresAuthority = false)]
		void CmdSetSentData(MessageData newData)
		{
			sendMessageData = newData;

			TrySendSignal();
		}

		#endregion

		#region Power
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

		#endregion

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (isPowered == false || DefaultWillInteract.Default(interaction, side) == false)
				return false;
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Id))
				return true;
			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandSlot.Item.TryGetComponent<IDCard>(out var id))
			{

				currentLogin = id;

				var interact = "The console accepts your ID!";
				if (CheckID() == false)
				{
					interact = "It seems to reject your ID!";
					currentLogin = null;
				}

				if (terminalGUI != null)
				{
					terminalGUI.UpdateGUI();
					terminalGUI.OpenRequestPage();
				}

				Chat.AddActionMsgToChat(interaction.Performer, $"You swipe your ID through the supply console's ID slot. " + interact,
				$"{interaction.Performer.ExpensiveName()} swiped their ID through the supply console's ID slot");
			}
		}

		[Command]
		public void ClearID()
		{
			currentLogin = null;
		}

		public bool CheckID()
		{
			if (CurrentLogin == null) return false;

			if (CurrentLogin.HasAccess(terminalRequieredAccess)) return true;

			ClearID();

			return false;
		}

		public void TransmitRequest(string targetDepartment, string message, bool IsUrgent)
		{
			MessageData testSendMessageData = new MessageData(message, IsUrgent, CurrentLogin.RegisteredName, targetDepartment, Department.DisplayName);

			sendMessageData = testSendMessageData;

			if (CustomNetworkManager.Instance._isServer)
				TrySendSignal();
			else
				CmdSetSentData(testSendMessageData);

		}
	}
}