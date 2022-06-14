using UnityEngine;
using UI.Core.NetUI;
using Objects.Wallmounts;
using System.Linq;

namespace UI.Objects.Wallmounts
{
	public class GUI_TerminalMessageEntry : DynamicEntry
	{
		public GUI_PublicTerminal TerminalMasterTab = null;
		public MessageData messageData;

		public DepartmentList departmentList;

		public bool IsArchive = false;

		[SerializeField]
		private NetLabel NameAndCategory = null;

		[SerializeField]
		private NetColorChanger UrgencyImage = null;

		[SerializeField]
		private NetLabel UrgencyText = null;

		[SerializeField]
		private NetLabel MessageText = null;

		public void DeleteEntry()
		{
			if (IsArchive == false)
			{
				TerminalMasterTab.masterTerminal.receivedMessageData.Remove(messageData); //Move from messages to archive
				TerminalMasterTab.masterTerminal.archivedMessageData.Add(messageData);
				TerminalMasterTab.messages.Remove(this.gameObject.name);
			}
			else
			{
				TerminalMasterTab.masterTerminal.archivedMessageData.Remove(messageData); //Remove from Archive
				TerminalMasterTab.archivedMessages.Remove(this.gameObject.name);
			}
		}

		public void ReInit(MessageData message)
		{
			messageData = message;

			string displayName = departmentList.Departments.ElementAt<Department>(message.senderDepartment).DisplayName;

			NameAndCategory.SetValueServer(message.Sender + ", " + displayName);

			if (messageData.isUrgent == false)
			{
				UrgencyText.SetValueServer("");
				UrgencyImage.SetValueServer(new Color(255,255,255,0));
			}
	
			MessageText.SetValueServer(messageData.message);
		}
	}
}
