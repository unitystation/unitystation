using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class AdminChatButtons : MonoBehaviour
	{
		public GUI_Notification adminNotification = null;
		public GUI_Notification mentorNotification = null;
		public GUI_Notification playerNotification = null;
		public GUI_Notification prayerNotification = null;
		[SerializeField] private AdminChatWindows adminChatWindows = null;
		[SerializeField] private Button adminChatButton = null;
		[SerializeField] private Button mentorChatButton = null;
		[SerializeField] private Button playerChatButton = null;
		[SerializeField] private Button prayerWindowButton = null;
		// Ignore default color warning
#pragma warning disable CS0649
		[SerializeField] private Color selectedColor;
		[SerializeField] private Color unSelectedColor;
		// Ignore default color warning
#pragma warning restore CS0649

		private void OnEnable()
		{
			adminChatWindows.WindowChangeEvent += OnAdminChatWindowChange;
			ToggleButtons(AdminChatWindow.None);
		}

		private void OnDisable()
		{
			adminChatWindows.WindowChangeEvent -= OnAdminChatWindowChange;
		}

		void OnAdminChatWindowChange(AdminChatWindow selectedWindow)
		{
			ToggleButtons(selectedWindow);
		}

		void ToggleButtons(AdminChatWindow selectedWindow)
		{
			if(adminChatButton != null)
				adminChatButton.image.color = unSelectedColor;
			if(mentorChatButton != null)
				mentorChatButton.image.color = unSelectedColor;
			if(playerChatButton != null)
				playerChatButton.image.color = unSelectedColor;
			if(prayerWindowButton != null)
				prayerWindowButton.image.color = unSelectedColor;

			switch (selectedWindow)
			{
				case AdminChatWindow.AdminPlayerChat:
					if(playerChatButton != null)
						playerChatButton.image.color = selectedColor;
					break;
				case AdminChatWindow.MentorPlayerChat:
					if(mentorChatButton != null)
						mentorChatButton.image.color = selectedColor;
					break;
				case AdminChatWindow.AdminToAdminChat:
					if(adminChatButton != null)
						adminChatButton.image.color = selectedColor;
					break;
				case AdminChatWindow.PrayerWindow:
					if(prayerWindowButton != null)
						prayerWindowButton.image.color = selectedColor;
					break;
			}
		}

		public void ClearAllNotifications()
		{
			adminNotification?.ClearAll();
			mentorNotification?.ClearAll();
			playerNotification?.ClearAll();
			prayerNotification?.ClearAll();
		}

		/// <summary>
		/// Use for initialization of admin chat notifications when the admin logs in
		/// </summary>
		/// <param name="adminConn"></param>
		public void ServerUpdateAdminNotifications(NetworkConnection adminConn)
		{
			var update = new AdminChatNotificationFullUpdate();

			if(adminNotification != null){
				foreach (var n in adminNotification?.notifications)
				{
					update.notificationEntries.Add(new AdminChatNotificationEntry
					{
						Amount = n.Value,
						Key = n.Key,
						TargetWindow = AdminChatWindow.AdminToAdminChat
					});
				}
			}

			if(playerNotification != null){
			foreach (var n in playerNotification?.notifications)
			{
				if (PlayerList.Instance.GetByUserID(n.Key) == null
				    || PlayerList.Instance.GetByUserID(n.Key).Connection == null) continue;

				update.notificationEntries.Add(new AdminChatNotificationEntry
				{
					Amount = n.Value,
					Key = n.Key,
					TargetWindow = AdminChatWindow.AdminPlayerChat
				});
			}}

			if(mentorNotification != null){
			foreach (var n in mentorNotification?.notifications)
			{
				if (PlayerList.Instance.GetByUserID(n.Key) == null
				    || PlayerList.Instance.GetByUserID(n.Key).Connection == null) continue;

				update.notificationEntries.Add(new AdminChatNotificationEntry
				{
					Amount = n.Value,
					Key = n.Key,
					TargetWindow = AdminChatWindow.MentorPlayerChat
				});
			}}

			if(prayerNotification != null) {
			foreach (var n in prayerNotification?.notifications)
			{
				if (PlayerList.Instance.GetByUserID(n.Key) == null
				    || PlayerList.Instance.GetByUserID(n.Key).Connection == null) continue;

				update.notificationEntries.Add(new AdminChatNotificationEntry
				{
					Amount = n.Value,
					Key = n.Key,
					TargetWindow = AdminChatWindow.PrayerWindow
				});
			}
			}

			AdminChatNotifications.Send(adminConn, update);
		}

		public void ClientUpdateNotifications(string notificationKey, AdminChatWindow targetWindow,
			int amt, bool clearAll)
		{
			switch (targetWindow)
			{
				case AdminChatWindow.AdminPlayerChat:
					if (clearAll)
					{
						playerNotification?.RemoveNotification(notificationKey);
						if (amt == 0) return;
					}
					//No need to update notification if the player is already selected in admin chat
					if (adminChatWindows.SelectedWindow == AdminChatWindow.AdminPlayerChat)
					{
						if (adminChatWindows.adminPlayerChat?.SelectedPlayer != null
						    && adminChatWindows.adminPlayerChat?.SelectedPlayer.uid == notificationKey)
						{
							break;
						}
					}
					playerNotification?.AddNotification(notificationKey, amt);
					break;
				case AdminChatWindow.MentorPlayerChat:
					if (clearAll)
					{
						mentorNotification?.RemoveNotification(notificationKey);
						if (amt == 0) return;
					}
					//No need to update notification if the player is already selected in admin chat
					if (adminChatWindows.SelectedWindow == AdminChatWindow.MentorPlayerChat)
					{
						if (adminChatWindows.mentorPlayerChat?.SelectedPlayer != null
						    && adminChatWindows.mentorPlayerChat?.SelectedPlayer.uid == notificationKey)
						{
							break;
						}
					}
					mentorNotification?.AddNotification(notificationKey, amt);
					break;
				case AdminChatWindow.AdminToAdminChat:
					if (clearAll)
					{
						adminNotification?.RemoveNotification(notificationKey);
						if (amt == 0) return;
					}

					if (adminChatWindows.adminToAdminChat?.gameObject.activeInHierarchy == true) return;

					adminNotification?.AddNotification(notificationKey, amt);
					break;
				case AdminChatWindow.PrayerWindow:
					if (clearAll)
					{
						prayerNotification?.RemoveNotification(notificationKey);
						if(amt==0) return;
					}
					prayerNotification?.AddNotification(notificationKey, amt);
					break;
			}
		}
	}
}
