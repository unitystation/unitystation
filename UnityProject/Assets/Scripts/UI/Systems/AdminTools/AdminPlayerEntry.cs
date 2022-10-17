using System;
using System.Collections;
using System.Collections.Generic;
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

		public void UpdateButton(AdminPlayerEntryData playerEntryData, Action<AdminPlayerEntry> onClickEvent, GUI_Notification masterNotification = null,
			bool disableInteract = false, bool hideSensitiveFields = false)
		{
			parentNotification = masterNotification;
			OnClickEvent = onClickEvent;
			PlayerData = playerEntryData;

			if (!hideSensitiveFields)
			{
				displayName.text =
					$"{playerEntryData.name} - {playerEntryData.currentJob}. ACC: {(playerEntryData.isAdmin ? "<color=red>[A]</color>" : "")}{(playerEntryData.isMentor ? "<color=#6400ff>[M]</color>" : "")} {playerEntryData.accountName} {playerEntryData.ipAddress} UUID {playerEntryData.uid}";
			}
			else
			{
				displayName.text = $"{(playerEntryData.isAdmin ? "<color=red>[A]</color>" : "")}{(playerEntryData.isMentor ? "<color=#6400ff>[M]</color>" : "")} {playerEntryData.accountName}";
			}

			if (PlayerData.isAntag && !hideSensitiveFields)
			{
				displayName.color = antagTextColor;
			}
			else
			{
				displayName.color = Color.white;
			}

			if (PlayerData.ipAddress == "")
			{
				offlineNot.SetActive(true);
			}
			else
			{
				offlineNot.SetActive(false);
			}

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
			if (player == null || player.Script == null || player.Mind.body == null) return;
			if(PlayerManager.LocalPlayerScript.IsDeadOrGhost == false) AGhost.Ghost();
			GhostOrbit.Instance.CmdServerOrbit(player.Mind.body.gameObject);
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
