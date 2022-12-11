using UnityEngine;
using UI.Core.NetUI;
using Objects.Wallmounts;
using System.Linq;
using Objects.Wallmounts.PublicTerminals;

namespace UI.Objects.Wallmounts
{
	public class GUI_TerminalMessageEntry : DynamicEntry
	{
		public GUI_PublicTerminal TerminalMasterTab = null;
		public MessageData messageData;

		public DepartmentList departmentList;

		public bool IsArchive = false;

		[SerializeField]
		private NetText_label NameAndCategory = null;

		[SerializeField]
		private NetColorChanger UrgencyImage = null;

		[SerializeField]
		private NetText_label UrgencyText = null;

		[SerializeField]
		private NetText_label MessageText = null;

		public void DeleteEntry()
		{
			if (IsArchive == false)
			{
				TerminalMasterTab.masterTerminal.ReceivedMessageData.Remove(messageData); //Move from messages to archive
				TerminalMasterTab.masterTerminal.ArchivedMessageData.Add(messageData);
				TerminalMasterTab.messages.Remove(this.gameObject.name);
			}
			else
			{
				TerminalMasterTab.masterTerminal.ArchivedMessageData.Remove(messageData); //Remove from Archive
				TerminalMasterTab.archivedMessages.Remove(this.gameObject.name);
			}
		}

		public void ReInit(MessageData message)
		{
			messageData = message;

			string displayName = departmentList.Departments.ElementAt<Department>(message.senderDepartment).DisplayName;

			NameAndCategory.MasterSetValue(message.Sender + ", " + displayName);

			if (messageData.isUrgent == false)
			{
				UrgencyText.MasterSetValue("");
				UrgencyImage.MasterSetValue(new Color(255,255,255,0));
			}

			MessageText.MasterSetValue(messageData.message);
		}
	}
}
