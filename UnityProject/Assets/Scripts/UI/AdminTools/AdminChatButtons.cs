using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class AdminChatButtons : MonoBehaviour
	{
		public GUI_Notification adminNotification = null;
		public GUI_Notification playerNotification = null;
		public GUI_Notification prayerNotification = null;
		[SerializeField] private AdminChatWindows adminChatWindows = null;
		[SerializeField] private Button adminChatButton = null;
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
			adminChatButton.image.color = unSelectedColor;
			playerChatButton.image.color = unSelectedColor;
			prayerWindowButton.image.color = unSelectedColor;

			switch (selectedWindow)
			{
				case AdminChatWindow.AdminPlayerChat:
					playerChatButton.image.color = selectedColor;
					break;
				case AdminChatWindow.AdminToAdminChat:
					adminChatButton.image.color = selectedColor;
					break;
				case AdminChatWindow.PrayerWindow:
					prayerWindowButton.image.color = selectedColor;
					break;
			}
		}

		public void ClearAllNotifications()
		{
			adminNotification.ClearAll();
			playerNotification.ClearAll();
			prayerNotification.ClearAll();
		}

		/// <summary>
		/// Use for initialization of admin chat notifications when the admin logs in
		/// </summary>
		/// <param name="adminConn"></param>
		public void ServerUpdateAdminNotifications(NetworkConnection adminConn)
		{
			var update = new AdminChatNotificationFullUpdate();

			foreach (var n in adminNotification.notifications)
			{
				update.notificationEntries.Add(new AdminChatNotificationEntry
				{
					Amount = n.Value,
					Key = n.Key,
					TargetWindow = AdminChatWindow.AdminToAdminChat
				});
			}

			foreach (var n in playerNotification.notifications)
			{
				if (PlayerList.Instance.GetByUserID(n.Key) == null
				    || PlayerList.Instance.GetByUserID(n.Key).Connection == null) continue;

				update.notificationEntries.Add(new AdminChatNotificationEntry
				{
					Amount = n.Value,
					Key = n.Key,
					TargetWindow = AdminChatWindow.AdminPlayerChat
				});
			}

			foreach (var n in prayerNotification.notifications)
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
						playerNotification.RemoveNotification(notificationKey);
						if (amt == 0) return;
					}
					//No need to update notification if the player is already selected in admin chat
					if (adminChatWindows.SelectedWindow == AdminChatWindow.AdminPlayerChat)
					{
						if (adminChatWindows.adminPlayerChat.SelectedPlayer != null
						    && adminChatWindows.adminPlayerChat.SelectedPlayer.uid == notificationKey)
						{
							break;
						}
					}
					playerNotification.AddNotification(notificationKey, amt);
					break;
				case AdminChatWindow.AdminToAdminChat:
					if (clearAll)
					{
						adminNotification.RemoveNotification(notificationKey);
						if (amt == 0) return;
					}

					if (adminChatWindows.adminToAdminChat.gameObject.activeInHierarchy) return;

					adminNotification.AddNotification(notificationKey, amt);
					break;
				case AdminChatWindow.PrayerWindow:
					if (clearAll)
					{
						prayerNotification.AddNotification(notificationKey, amt);
					}
					prayerNotification.AddNotification(notificationKey, amt);
					break;
			}
		}
	}
}
