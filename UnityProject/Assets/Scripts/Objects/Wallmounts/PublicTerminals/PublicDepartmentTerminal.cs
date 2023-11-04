using System;
using Communications;
using Systems.Electricity;
using UnityEngine;
using UI.Objects.Wallmounts;
using Mirror;
using System.Collections;
using System.Linq;
using Systems.Clearance;
using UnityEngine.Serialization;

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
		[field: SerializeField]
		public DepartmentToInt Department {get; private set;}
		
		// for the UI
		private float currentVoltage;

		[SyncVar]
		//the currently logged in user, we actually do need to sync this as the requests are being sent from the client so all of them need this.
		private IDCard currentLogin;

		public GUI_PublicTerminal TerminalGUI { get; set; }

		public MessageData SendMessageData { get; private set; }
		public SyncList<MessageData> ReceivedMessageData { get; } = new();
		public SyncList<MessageData> ArchivedMessageData { get; } = new();

		public float CurrentVoltage => currentVoltage;
		public IDCard CurrentLogin => currentLogin;

		[field: SerializeField, FormerlySerializedAs("isAI")]
		public bool IsAI { get; set; } = false;
		private bool canTransmit = true; //For serverside cooldown on broadcasting messages, to prevent players from spamming messages
		private ClearanceRestricted restricted;

		private void Awake()
		{
			restricted = GetComponent<ClearanceRestricted>();
		}

		#region Signals

		protected override bool SendSignalLogic()
		{
			if (IsAI && isPowered) return true;
			return CurrentLogin != null && isPowered;
		}

		public override void SignalFailed()
		{
			if (isPowered == false) return;
			if (CurrentLogin == null || restricted.HasClearance(currentLogin.ClearanceSource) == false)
			{
				Chat.AddActionMsgToChat(gameObject, "A huge red X appears on the terminal's screen as it says 'access denied'");
				return;
			}
			Chat.AddActionMsgToChat(gameObject, "A huge ! appears on the terminal's screen as it says 'An error occured.'");
		}

		[Command(requiresAuthority = false)]
		void CmdSetSentData(MessageData newData, NetworkConnectionToClient sender = null)
		{
			if (canTransmit == false) return;

			PlayerInfo player = PlayerList.Instance.Get(sender);

			if(player.Script.IsRegisterTileReachable(gameObject.GetComponent<RegisterTile>(), true, 1.5f) == false) return; //Sees if the player is actually next to the terminal.

			//This trims the strings to be a max of 200 characters, shouldn't be able to happen normally but as Gilles once said "Player Inputs are Evil"
			newData.message = newData.message.Length <= 200 ? newData.message : newData.message.Substring(0, 200);

			SendMessageData = newData;

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
			canTransmit = false;
			yield return WaitFor.Seconds(1f);
			canTransmit = true;
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
			if (interaction.HandSlot.Item.TryGetComponent<IDCard>(out var id) == false) return;

			currentLogin = id;

			var interact = "The console accepts your ID!";
			if (CheckID() == false)
			{
				interact = "It seems to reject your ID!";
				currentLogin = null;
			}

			if (TerminalGUI != null)
			{
				TerminalGUI.UpdateGUI();
				TerminalGUI.OpenRequestPage();
			}

			Chat.AddActionMsgToChat(interaction.Performer, $"You swipe your ID through the supply console's ID slot. " + interact,
				$"{interaction.Performer.ExpensiveName()} swiped their ID through the supply console's ID slot");
		}

		public void ClearID()
		{
			currentLogin = null;
		}

		private bool CheckID()
		{
			if (restricted.RequiredClearance.Any() == false) return true;
			if (CurrentLogin == null) return false;
			var idClearance = CurrentLogin.ClearanceSource;
			return restricted.HasClearance(idClearance);
		}

		public void TransmitRequest(int targetDepartment, string message, bool isUrgent)
		{
			MessageData testSendMessageData = new MessageData(message, isUrgent, CurrentLogin.RegisteredName, targetDepartment, (int)Department);

			SendMessageData = testSendMessageData;

			if (CustomNetworkManager.Instance._isServer)
				TrySendSignal();
			else
				CmdSetSentData(testSendMessageData);

		}
	}
}