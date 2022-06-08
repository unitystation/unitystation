using Communications;
using Systems.Electricity;
using UnityEngine;
using UI.Objects.Wallmounts;
using System.Collections.Generic;

namespace Objects.Wallmounts
{
	public struct MessageData
	{
		public string message;
		public bool isUrgent;
		public IDCard Sender;
		public Department senderDepartment;
		public Department targetDepartment;
	}
	
	public class PublicDepartmentTerminal : MonoBehaviour, IAPCPowerable, ICheckedInteractable<HandApply>
	{
		public Department Department; //For displaying what the console's department is for
		public Access terminalRequieredAccess; //only with select access can actually use this

		private float currentVoltage; // for the UI
		public bool isPowered = false; //To disable the terminal if there is no power detected
		private IDCard currentLogin; //the currently logged in user

		[HideInInspector]
		public GUI_PublicTerminal terminalGUI;

		[SerializeField]
		private SignalEmitter Emitter;

		public MessageData sendMessageData = new MessageData();
		public List<MessageData> receivedMessageData = new List<MessageData>();

		public float CurrentVoltage => currentVoltage;
		public IDCard CurrentLogin => currentLogin;

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
			if(isPowered == false || DefaultWillInteract.Default(interaction, side) == false)
				return false;	
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Id))
				return true;
			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandSlot.Item.TryGetComponent<IDCard>(out var id))
			{
				if (terminalGUI != null)
				{
					terminalGUI.UpdateGUI();
					terminalGUI.OpenRequestPage();
				}

				currentLogin = id;

				var interact = "The console accepts your ID!";
				if (CheckID() == false)
				{
					interact = "It seems to reject your ID!";
					currentLogin = null;
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

			if (CurrentLogin.Occupation.AllowedAccess.Contains(terminalRequieredAccess)) return true;

			return false;
		}

		public void TransmitRequest(Department targetDepartment, string message, bool IsUrgent)
		{
			sendMessageData = new MessageData
			{
				targetDepartment = targetDepartment,
				senderDepartment = Department,
				Sender = CurrentLogin,
				message = message,
				isUrgent = IsUrgent,
			};
	
			Emitter.TrySendSignal();
		}
	}
}