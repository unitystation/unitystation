using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UI.Core.NetUI;
using UnityEngine.UI;
using Objects.Wallmounts;
using System.Linq;

namespace UI.Objects.Wallmounts
{
	public enum StringToDepartmentIndex //String to singleton index for departments
	{
		Cargo = 3,
		Engineering = 6,
		Service = 1,
		Medical = 7,
		Command = 8,
		Research = 4,
		Civillian = 0,
		Entertainment = 2,
		Security = 5,
		Synthetic = 9,
	}

	public class GUI_PublicTerminal : NetTab
	{

		[SerializeField]
		private NetPageSwitcher mainSwitcher = null;

		public PublicDepartmentTerminal masterTerminal;

		private bool Urgent = false;

		[SerializeField]
		private NetPage RequestPage = null;
		[SerializeField]
		private NetPage LoginPage = null;
		[SerializeField]
		private NetPage MessagePage = null;
		[SerializeField]
		private NetPage ConstructionPage = null;

		[SerializeField]
		private Dropdown DepartmentDropDown = null;

		[SerializeField]
		private InputField RequestText = null;

		[SerializeField]
		private Toggle UrgentToggle = null;

		[SerializeField]
		public EmptyItemList messages = null;

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

		protected override void InitServer()
		{
			UpdateGUI();
			
			OnTabOpened.AddListener(TabOpened);
			OnTabClosed.AddListener(TabClosed);
		}

		private void Start()
		{
			StartCoroutine(WaitForProvider());
		}

		private void TabClosed(ConnectedPlayer oldPeeper = default)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateGUI);
		}

		private void TabOpened(ConnectedPlayer newPeeper = default)
		{
			Urgent = UrgentToggle.isOn;
			UpdateManager.Add(UpdateGUI,5f);
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			masterTerminal = Provider.GetComponent<PublicDepartmentTerminal>();
			masterTerminal.terminalGUI = this;

			if (masterTerminal.CurrentLogin == null)
			{
				mainSwitcher.SetActivePage(LoginPage);
			}
			else
			{
				mainSwitcher.SetActivePage(RequestPage);
			}

			UpdateGUI();

		}

		public void UpdateGUI()
		{
			if (masterTerminal == null) return;

			TitleLabel.SetValueServer("PUBLIC TERMINAL - " + masterTerminal.Department.DisplayName.Capitalize());

			if(masterTerminal.CurrentLogin == null)
				NameLabel.SetValueServer("Not Signed In.");
			else
				NameLabel.SetValueServer("Signed in as: " + masterTerminal.CurrentLogin.RegisteredName);

			DateTime stationTimeHolder = GameManager.Instance.stationTime;

			string timestring = stationTimeHolder.ToString("HH:mm");
			string voltagestring = masterTerminal.CurrentVoltage + "V";

			TimerLabel.SetValueServer(timestring);
			VoltageLabel.SetValueServer(voltagestring);
		}
		
		public void LogOut()
		{
			mainSwitcher.SetActivePage(LoginPage);
			masterTerminal.ClearID();
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
			if (masterTerminal.CurrentLogin == null)
			{
				mainSwitcher.SetActivePage(LoginPage);
				return;
			}

			mainSwitcher.SetActivePage(MessagePage);

			List<MessageData> messageData = masterTerminal.receivedMessageData;

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

		public void GenerateDropDown()
		{
			var optionData = new List<Dropdown.OptionData>();

			foreach (var department in DepartmentList.Departments)
			{
				optionData.Add(new Dropdown.OptionData
				{
					text = department.DisplayName
				}); 
			}

			DepartmentDropDown.options = optionData;
		}

		public void toggleUrgent()
		{
			Urgent = !Urgent;
		}

		public void SendRequest()
		{
			string message = RequestText.text;

			bool isUrgent = Urgent;

			Department targetDepartment = DepartmentList.Departments.ElementAt<Department>(DepartmentDropDown.value);

			masterTerminal.TransmitRequest(targetDepartment, message, isUrgent);			
		}

	}
}