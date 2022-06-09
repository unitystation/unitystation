using UnityEngine;
using UI.Core.NetUI;
using Objects.Wallmounts;

namespace UI.Objects.Wallmounts
{
	public class GUI_TerminalMessageEntry : DynamicEntry
	{
		public GUI_PublicTerminal TerminalMasterTab = null;
		public MessageData messageData;

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
			TerminalMasterTab.masterTerminal.receivedMessageData.Remove(messageData);
			TerminalMasterTab.messages.Remove(this.gameObject.name);	
		}

		public void ReInit(MessageData message)
		{
			messageData = message;

			NameAndCategory.SetValueServer(messageData.Sender + ", " + messageData.senderDepartment);

			if (messageData.isUrgent == false)
			{
				UrgencyText.SetValueServer("");
				UrgencyImage.SetValueServer(new Color(255,255,255,0));
			}
	
			MessageText.SetValueServer(messageData.message);
		}
	}
}
