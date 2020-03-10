using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdminTools
{
	public class AdminChatButtons : MonoBehaviour
	{
		[SerializeField] private GUI_Notification adminNotification = null;
		[SerializeField] private GUI_Notification playerNotification = null;
		[SerializeField] private GUI_Notification prayerNotification = null;

		void ResetNotifications()
		{
			adminNotification.gameObject.SetActive(false);
			playerNotification.gameObject.SetActive(false);
			prayerNotification.gameObject.SetActive(false);
		}

		private void OnEnable()
		{
			ResetNotifications();
		}
	}
}
