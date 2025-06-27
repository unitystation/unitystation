using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using AdminTools;
using Managers.SettingsManager;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AdminMindEntry : MonoBehaviour
{
    	private Action<AdminMindEntry> OnClickEvent;
		public TMP_Text displayName = null;
		[SerializeField] private Image bg = null;
		[SerializeField] private GameObject offlineNot = null;
		public Button button;

		public Color selectedColor;
		public Color defaultColor;
		public Color antagTextColor;
		private bool recentClick = false;
		private float secondClickTime = 0.25f;

		public AdminMindEntryData MindData { get; set; }

		/// <summary>
		/// Populates the PlayerEntry button in admin/mentor panels
		/// </summary>
		/// <param name="playerEntryData">The data that will populate the UI</param>
		/// <param name="onClickEvent">What happens when clicked</param>
		/// <param name="disableInteract">Should disable the interaction with the button?</param>
		/// <param name="isForMentor">Is this information for a mentor? (They have less information than admins)</param>
		public void UpdateButton(AdminMindEntryData playerEntryData, Action<AdminMindEntry> onClickEvent,
			bool disableInteract = false, bool isForMentor = false)
		{

			OnClickEvent = onClickEvent;
			MindData = playerEntryData;
			var displayData = new StringBuilder();
			AppendBasicInformation(displayData, playerEntryData);
			AppendAdminMentorStatus(displayData, playerEntryData);
			AppendPersonalInformation(displayData, playerEntryData, isForMentor);
			displayName.text = displayData.ToString();
			displayName.color = Color.white;

			if (disableInteract)
			{
				button.interactable = false;
				bg.color = selectedColor;
			}
			else
			{
				button.interactable = true;
			}

		}

		private void AppendBasicInformation(StringBuilder builder, AdminMindEntryData playerEntryData)
		{
			builder.Append(playerEntryData.CurrentCharacterSettings.Name);
			builder.Append(" - ");
			builder.Append(playerEntryData.CurrentCharacterSettings.Species);
			builder.Append(" - by ");
			builder.Append(playerEntryData.ControlledByID);
		}

		private void AppendAdminMentorStatus(StringBuilder builder, AdminMindEntryData playerEntryData)
		{
			// if (string.IsNullOrWhiteSpace(playerEntryData.roleColour) == false )
			// {
			// 	builder.Append($"<color={playerEntryData.roleColour}>[{playerEntryData.roleSmall}]</color>");
			// }
		}

		private void AppendPersonalInformation(StringBuilder builder, AdminMindEntryData playerEntryData, bool hideSensitiveFields)
		{
			// builder.Append(" ACC: ");
			// builder.Append(playerEntryData.accountName);
			// if (hideSensitiveFields || MiscSettings.Instance.StreamerModeEnabled)
			// {
			// 	return;
			// }
			// builder.Append(" ");
			// builder.Append(playerEntryData.ipAddress);
			// builder.Append(" UUID ");
			// builder.Append(playerEntryData.uid);
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


		public void SelectPlayer()
		{
			bg.color = selectedColor;
		}

		public void DeselectPlayer()
		{
			bg.color = defaultColor;
		}

		private void SecondClickCheck()
		{
			if(recentClick == false) return;
			//TODO Request teleport to mind

			//var player = PlayerList.Instance.GetPlayerByID(MindData.uid);
			//if (player == null || player.Script == null || player.Mind.Body == null) return;
			//if(PlayerManager.LocalPlayerScript.IsDeadOrGhost == false) AGhost.Ghost();
			//GhostOrbit.Instance.CmdServerOrbit(player.Mind.Body.gameObject);
		}

		private IEnumerator ClickCooldown()
		{
			if(recentClick) yield break;
			recentClick = true;
			yield return WaitFor.Seconds(secondClickTime);
			recentClick = false;
		}
}
