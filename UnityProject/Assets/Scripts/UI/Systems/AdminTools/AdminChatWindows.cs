using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdminTools
{
	public class AdminChatWindows : MonoBehaviour
	{
		public AdminPlayerChat adminPlayerChat = null;
		public MentorPlayerChat mentorPlayerChat = null;
		public AdminToAdminChat adminToAdminChat = null;
		public PlayerPrayerWindow playerPrayerWindow = null;
		public List<AdminPlayersScrollView> playerListViews = new List<AdminPlayersScrollView>();

		public AdminChatWindow SelectedWindow { get; private set; }
		public event Action<AdminChatWindow> WindowChangeEvent;

		void Awake()
		{
			ToggleWindows(AdminChatWindow.None);
		}

		//This is for onclick button events, hence the int param
		public void ToggleWindows(int option)
		{
			ToggleWindows((AdminChatWindow)option);
		}

		void ToggleWindows(AdminChatWindow window)
		{
			adminPlayerChat.gameObject.SetActive(false);
			mentorPlayerChat.gameObject.SetActive(false);
			adminToAdminChat.gameObject.SetActive(false);
			playerPrayerWindow.gameObject.SetActive(false);

			switch (window)
			{
				case AdminChatWindow.AdminPlayerChat:
					adminPlayerChat.gameObject.SetActive(true);
					SelectedWindow = AdminChatWindow.AdminPlayerChat;
					break;
				case AdminChatWindow.MentorPlayerChat:
					mentorPlayerChat.gameObject.SetActive(true);
					SelectedWindow = AdminChatWindow.MentorPlayerChat;
					break;
				case AdminChatWindow.AdminToAdminChat:
					adminToAdminChat.gameObject.SetActive(true);
					SelectedWindow = AdminChatWindow.AdminToAdminChat;
					break;
				case AdminChatWindow.PrayerWindow:
					playerPrayerWindow.gameObject.SetActive(true);
					SelectedWindow = AdminChatWindow.PrayerWindow;
					break;
				default:
					SelectedWindow = AdminChatWindow.None;
					break;
			}

			if (WindowChangeEvent != null)
			{
				WindowChangeEvent.Invoke(SelectedWindow);
			}
		}

		public void ResetAll()
		{
			adminPlayerChat.ClearLogs();
			mentorPlayerChat.ClearLogs();
			adminToAdminChat.ClearLogs();
			playerPrayerWindow.ClearLogs();
		}
	}

	public enum AdminChatWindow
	{
		None,
		AdminPlayerChat,
		AdminToAdminChat,
		MentorPlayerChat,
		PrayerWindow
	}
}