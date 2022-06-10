using System.Collections;
using System;
using UnityEngine;
using UI.Core.NetUI;
using UnityEngine.UI;
using Objects.Wallmounts;
using System.Linq;
using Mirror;

namespace UI.Objects.Wallmounts
{
	public class GUI_PublicTerminal : NetTab
	{

		[SerializeField]
		private NetPageSwitcher mainSwitcher = null;

		public PublicDepartmentTerminal masterTerminal;

		public DepartmentList departmentList;

		[SerializeField]
		private NetPage RequestPage = null;
		[SerializeField]
		private NetPage LoginPage = null;
		[SerializeField]
		private NetPage MessagePage = null;
		[SerializeField]
		private NetPage ConstructionPage = null;
		[SerializeField]
		private NetPage ArchivePage = null;

		[SerializeField]
		private Dropdown DepartmentDropDown = null;

		[SerializeField]
		private InputField RequestText = null;

		[SerializeField]
		private Toggle UrgentToggle = null;

		[SerializeField]
		public EmptyItemList messages = null;
		[SerializeField]
		public EmptyItemList archivedMessages = null;

		[SerializeField]
		private DepartmentList DepartmentList = null;

		[SerializeField]
		private NetLabel TitleLabel = null;
		[SerializeField]
		private NetLabel NameLabel = null;
		[SerializeField]
		private NetLabel VoltageLabel = null;
		[SerializeField]
		private NetLabel TimerLabel = null;

		string message;

		private bool Urgent = false;

		int targetDep;

		private void Start()
		{
			StartCoroutine(WaitForProvider());
		}

		protected override void InitServer()
		{
			UpdateGUI();

			OnTabOpened.AddListener(TabOpened);
			OnTabClosed.AddListener(TabClosed);
		}

		private void TabClosed(PlayerInfo oldPeeper = default)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateGUI);
		}

		private void TabOpened(PlayerInfo newPeeper = default)
		{
			UpdateGUI();
			Urgent = UrgentToggle.isOn;
			UpdateManager.Add(UpdateGUI, 2f);
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			masterTerminal = Provider.GetComponent<PublicDepartmentTerminal>();

			masterTerminal.terminalGUI = this;

			UpdateGUI();

		}

		public void UpdateGUI()
		{
			if (masterTerminal == null) return;

			string displayName = departmentList.Departments.ElementAt<Department>((int)masterTerminal.Department).DisplayName.ToUpper();

			TitleLabel.SetValueServer("PUBLIC TERMINAL - " + displayName);

			if (masterTerminal.CurrentLogin == null)
			{
				NameLabel.SetValueServer("Not Signed In.");
			}
			else
			{
				string playerName = masterTerminal.CurrentLogin.RegisteredName;

				if (playerName == null) NameLabel.SetValueServer("Signed in as: NULL");
				else
					NameLabel.SetValueServer("Signed in as: " + masterTerminal.CurrentLogin.RegisteredName);

				if (mainSwitcher.CurrentPage == LoginPage) mainSwitcher.SetActivePage(RequestPage);
			}
				

			DateTime stationTimeHolder = GameManager.Instance.stationTime;

			string timestring = stationTimeHolder.ToString("HH:mm");
			string voltagestring = masterTerminal.CurrentVoltage + "V";

			TimerLabel.SetValueServer(timestring);
			VoltageLabel.SetValueServer(voltagestring);
		}
		
		public void LogOut()
		{
			masterTerminal.ClearID();
			UpdateGUI();
		}

		public void OpenRequestPage()
		{
			if(masterTerminal.CurrentLogin == null)
			{
				mainSwitcher.SetActivePage(LoginPage);
				return;
			}

			mainSwitcher.SetActivePage(RequestPage);
		}

		public void OpenConstructionPage()
		{
			mainSwitcher.SetActivePage(ConstructionPage);
		}

		public void OpenMessagePage()
		{
			if (CustomNetworkManager.IsServer)
			{
				mainSwitcher.SetActivePage(MessagePage);
			}

			SyncList<MessageData> messageData = masterTerminal.receivedMessageData;

			messages.Clear();

			if (messageData.Count == 0) return;

			messages.AddItems(messageData.Count);

			for (int i = 0; i < messageData.Count; i++)
			{
					GUI_TerminalMessageEntry item = messages.Entries[i] as GUI_TerminalMessageEntry;
					item.TerminalMasterTab = this;
					item.ReInit(messageData[i]);
			}
			
		}

		public void OpenArchivePage()
		{
			if (CustomNetworkManager.IsServer)
			{
				mainSwitcher.SetActivePage(ArchivePage);
			}

			SyncList<MessageData> messageData = masterTerminal.archivedMessageData;

			archivedMessages.Clear();

			if (messageData.Count == 0) return;

			archivedMessages.AddItems(messageData.Count);

			for (int i = 0; i < messageData.Count; i++)
			{
				GUI_TerminalMessageEntry item = archivedMessages.Entries[i] as GUI_TerminalMessageEntry;
				item.TerminalMasterTab = this;
				item.ReInit(messageData[i]);
			}

		}

		public void UpdateDropDown(Dropdown dropdown)
		{
			targetDep = dropdown.value;
		}

		public void toggleUrgent()
		{
			Urgent = !Urgent;
		}

		public void UpdateText(Text _text)
		{
			message = _text.text;
		}

		public void SendRequest()
		{
			UpdateText(RequestText.textComponent);
			UpdateDropDown(DepartmentDropDown);

			bool isUrgent = Urgent;

			masterTerminal.TransmitRequest(targetDep, message, isUrgent);			
		}

	}
}