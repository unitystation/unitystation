using Communications;
using Systems.Electricity;
using UnityEngine;
using UI.Objects.Wallmounts;
using Mirror;
using System.Collections;
using Systems.Clearance;

namespace Objects.Wallmounts.PublicTerminals
{
	public struct MessageData
	{
		public MessageData(string msg, bool urgent, string name, int target, int sender)
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
		public int senderDepartment;
		public int targetDepartment;
	}

	public class PublicDepartmentTerminal : SignalEmitter, IAPCPowerable, ICheckedInteractable<HandApply>
	{
		bool CanTransmit = true; //For serverside cooldown on broadcasting messages, to prevent players from spamming messages

		public enum DepartmentToInt
		{
			Civilian = 0,
			Service = 1,
			Entertainment = 2,
			Cargo = 3,
			Research = 4,
			Security = 5,
			Engineering = 6,
			Medical = 7,
			Command = 8,
			Synthetic = 9,
		}

		public DepartmentList departmentList;

		//For displaying what the console's department is for
		public DepartmentToInt Department;

		public bool AccessRestricted;

		//Access required to send messages at this terminal, requires terminal to be access restricted to work.
		public Clearance terminalRequiredClearance;

		// for the UI
		private float currentVoltage;

		[SyncVar]
		//the currently logged in user, we actually do need to sync this as the requests are being sent from the client so all of them need this.
		private IDCard currentLogin;

		[HideInInspector]
		public GUI_PublicTerminal terminalGUI;

		[HideInInspector]
		public MessageData sendMessageData = new MessageData();

		[HideInInspector]
		public readonly SyncList<MessageData> receivedMessageData = new SyncList<MessageData>();

		[HideInInspector]
		public readonly SyncList<MessageData> archivedMessageData = new SyncList<MessageData>();

		public float CurrentVoltage => currentVoltage;
		public IDCard CurrentLogin => currentLogin;

		public bool isAI = false;

		#region Signals

		protected override bool SendSignalLogic()
		{
			if (isAI && isPowered) return true;
			return CurrentLogin != null && isPowered;
		}

		public override void SignalFailed()
		{
			if (isPowered == false) return;
			if (CurrentLogin == null || CurrentLogin.HasAccess(terminalRequiredClearance) == false)
			{
				Chat.AddLocalMsgToChat("A huge red X appears on the terminal's screen as it says 'access denied'", gameObject);
				return;
			}
			Chat.AddLocalMsgToChat("A huge ! appears on the terminal's screen as it says 'An error occured.'", gameObject);
		}

		[Command(requiresAuthority = false)]
		void CmdSetSentData(MessageData newData, NetworkConnectionToClient sender = null)
		{
			if (CanTransmit == false) return;

			PlayerInfo player = PlayerList.Instance.Get(sender);

			if(player.Script.IsRegisterTileReachable(gameObject.GetComponent<RegisterTile>(), true, 1.5f) == false) return; //Sees if the player is actually next to the terminal.

			//This trims the strings to be a max of 200 characters, shouldn't be able to happen normally but as Gilles once said "Player Inputs are Evil"
			newData.message = newData.message.Length <= 200 ? newData.message : newData.message.Substring(0, 200);

			sendMessageData = newData;

			TrySendSignal();

			StartCoroutine(TransmitterCooldown());
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

		private IEnumerator TransmitterCooldown()
		{
			CanTransmit = false;
			yield return WaitFor.Seconds(1f);
			CanTransmit = true;
		}

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

		public void ClearID()
		{
			currentLogin = null;
		}

		public bool CheckID()
		{
			if (CurrentLogin == null) return false;

			if (AccessRestricted == false) return true;

			if (CurrentLogin.HasAccess(terminalRequiredClearance)) return true;

			return false;
		}

		public void TransmitRequest(int targetDepartment, string message, bool IsUrgent)
		{
			MessageData testSendMessageData = new MessageData(message, IsUrgent, CurrentLogin.RegisteredName, targetDepartment, (int)Department);

			sendMessageData = testSendMessageData;

			if (CustomNetworkManager.Instance._isServer)
				TrySendSignal();
			else
				CmdSetSentData(testSendMessageData);

		}
	}
}