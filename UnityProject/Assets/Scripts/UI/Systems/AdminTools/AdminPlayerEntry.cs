using System;
using System.Collections;
using System.Text;
using Managers.SettingsManager;
using Player;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class AdminPlayerEntry : MonoBehaviour
	{
		private Action<AdminPlayerEntry> OnClickEvent;
		public Text displayName = null;
		[SerializeField] private Image bg = null;
		//The notification counter on the button
		public GUI_Notification pendingMsgNotification = null;
		/// The reference to the notification counter on the admin chat button (the master one)
		private GUI_Notification parentNotification = null;
		[SerializeField] private GameObject offlineNot = null;
		public Button button;

		public Color selectedColor;
		public Color defaultColor;
		public Color antagTextColor;
		private bool recentClick = false;
		private float secondClickTime = 0.25f;

		public AdminPlayerEntryData PlayerData { get; set; }

		/// <summary>
		/// Populates the PlayerEntry button in admin/mentor panels
		/// </summary>
		/// <param name="playerEntryData">The data that will populate the UI</param>
		/// <param name="onClickEvent">What happens when clicked</param>
		/// <param name="masterNotification">Reference to notification monobehaviour</param>
		/// <param name="disableInteract">Should disable the interaction with the button?</param>
		/// <param name="isForMentor">Is this information for a mentor? (They have less information than admins)</param>
		public void UpdateButton(AdminPlayerEntryData playerEntryData, Action<AdminPlayerEntry> onClickEvent, GUI_Notification masterNotification = null,
			bool disableInteract = false, bool isForMentor = false)
		{
			parentNotification = masterNotification;
			OnClickEvent = onClickEvent;
			PlayerData = playerEntryData;
			var displayData = new StringBuilder();
			AppendBasicInformation(displayData, playerEntryData, isForMentor);
			AppendAdminMentorStatus(displayData, playerEntryData);
			AppendPersonalInformation(displayData, playerEntryData, isForMentor);
			displayName.text = displayData.ToString();
			displayName.color = isForMentor ? antagTextColor : Color.white;
			offlineNot.SetActive(string.IsNullOrEmpty(PlayerData.ipAddress));

			if (disableInteract)
			{
				button.interactable = false;
				bg.color = selectedColor;
			}
			else
			{
				button.interactable = true;
			}

			RefreshNotification();
		}

		private void AppendBasicInformation(StringBuilder builder, AdminPlayerEntryData playerEntryData, bool hideInGameInformation)
		{
			builder.Append(playerEntryData.name);
			if (hideInGameInformation) return;
			builder.Append(" - ");
			builder.Append(playerEntryData.currentJob);
		}

		private void AppendAdminMentorStatus(StringBuilder builder, AdminPlayerEntryData playerEntryData)
		{
			if (playerEntryData.isAdmin)
			{
				builder.Append("<color=red>[A]</color>");

			}

			if (playerEntryData.isMentor)
			{
				builder.Append("<color=#6400ff>[M]</color>");
			}
		}

		private void AppendPersonalInformation(StringBuilder builder, AdminPlayerEntryData playerEntryData, bool hideSensitiveFields)
		{
			builder.Append(" ACC: ");
			builder.Append(playerEntryData.accountName);
			if (hideSensitiveFields || MiscSettings.Instance.StreamerModeEnabled)
			{
				return;
			}
			builder.Append(" ");
			builder.Append(playerEntryData.ipAddress);
			builder.Append(" UUID ");
			builder.Append(playerEntryData.uid);
		}

		public void OnEnable()
		{
			RefreshNotification();
		}

		public void RefreshNotification()
		{
			if (parentNotification == null) return;

			if (parentNotification.notifications.ContainsKey(PlayerData.uid))
			{
				pendingMsgNotification.ClearAll();
				pendingMsgNotification.AddNotification(PlayerData.uid,
					parentNotification.notifications[PlayerData.uid]);
			}
			else
			{
				pendingMsgNotification.ClearAll();
			}
		}

		public void OnClick()
		{
			SecondClickCheck();
			StartCoroutine(ClickCooldown());
			if (OnClickEvent != null)
			{
				OnClickEvent.Invoke(this);
			}
		}

		public void ClearMessageNot()
		{
			if(parentNotification != null) parentNotification.RemoveNotification(PlayerData.uid);
			pendingMsgNotification.ClearAll();
		}

		public void SelectPlayer()
		{
			bg.color = selectedColor;
			ClearMessageNot();
		}

		public void DeselectPlayer()
		{
			bg.color = defaultColor;
		}

		private void SecondClickCheck()
		{
			if(recentClick == false) return;
			var player = PlayerList.Instance.GetPlayerByID(PlayerData.uid);
			if (player == null || player.Script == null || player.Mind.Body == null) return;
			if(PlayerManager.LocalPlayerScript.IsDeadOrGhost == false) AGhost.Ghost();
			GhostOrbit.Instance.CmdServerOrbit(player.Mind.Body.gameObject);
		}

		private IEnumerator ClickCooldown()
		{
			if(recentClick) yield break;
			recentClick = true;
			yield return WaitFor.Seconds(secondClickTime);
			recentClick = false;
		}
	}
}
