using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class AdminChatButtons : MonoBehaviour
	{
		[SerializeField] private GUI_Notification adminNotification = null;
		[SerializeField] private GUI_Notification playerNotification = null;
		[SerializeField] private GUI_Notification prayerNotification = null;
		[SerializeField] private AdminChatWindows adminChatWindows = null;
		[SerializeField] private Button adminChatButton = null;
		[SerializeField] private Button playerChatButton = null;
		[SerializeField] private Button prayerWindowButton = null;
		[SerializeField] private Color selectedColor;
		[SerializeField] private Color unSelectedColor;

		void ResetNotifications()
		{
			adminNotification.gameObject.SetActive(false);
			playerNotification.gameObject.SetActive(false);
			prayerNotification.gameObject.SetActive(false);
		}

		private void OnEnable()
		{
			ResetNotifications();
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
				update.notificationEntries.Add(new AdminChatNotificationEntry
				{
					Amount = n.Value,
					Key = n.Key,
					TargetWindow = AdminChatWindow.AdminPlayerChat
				});
			}

			foreach (var n in prayerNotification.notifications)
			{
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

		}

	}
}
