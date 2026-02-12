using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using US13.Core.Admin.Logs;
using US13.Core.Input_System;
using US13.Managers.NetworkManagement;
using US13.Messages.Client.Admin;
using US13.Player;
using Util;

namespace US13.UI.Systems.AdminTools.AdminLogs
{
	[RequireComponent(typeof(TMP_Text))]
	public class SubLogInfoUI : MonoBehaviour, ISerializationCallbackReceiver, IPointerClickHandler
	{
		[SerializeField] private TMP_Text textComponent;

		public LogInfoUI LogInfoUI;

		public StoredLogEntry.LogItems Info;

		public LogMarker LogMarker;

		public void OnBeforeSerialize()
		{
			if (textComponent == null)
			{
				textComponent = GetComponent<TMP_Text>();
			}
		}

		public void SetUp(StoredLogEntry.LogItems InInfo)
		{
			Info = InInfo;
			switch (LogMarker)
			{
				case LogMarker.Info:
					if (string.IsNullOrWhiteSpace(Info.ObjectName) == false)
					{
						textComponent.text = Info.ObjectName;
					}
					else
					{
						textComponent.text = Info.Info;
					}
					break;

				case LogMarker.Core:
					textComponent.text = Info.ObjectName;
					break;
				case LogMarker.StoredIn:
					textComponent.text = Info.StoredInName;
					break;
				case LogMarker.ControlledBy:
					textComponent.text = Info.PlayerAccountID;
					break;
				case LogMarker.Position:
					textComponent.text = Info.PositionWorld;
					break;
			}

			if (string.IsNullOrWhiteSpace(textComponent.text))
			{
				gameObject.SetActive(false);
			}
			else
			{
				textComponent.text = InsertLineBreaks(textComponent.text, 80);
				gameObject.SetActive(true);
			}
		}

		public void OnAfterDeserialize()
		{
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (LogMarker == LogMarker.Info)
			{
				return;
			}
			else
			{
				if (LogInfoUI.Expanded)
				{
					var Input = "";
					switch (LogMarker)
					{

						case LogMarker.Core:
							Input = Info.Object.ToString();

							if (KeyboardInputManager.IsTabPressed())
							{
								Input = "Obj=" + Input;
							}

							if (SearchModifiersContinue(Input))
							{
								HandleObjectInput(Info.Object);
							}
							break;
						case LogMarker.StoredIn:

							Input = Info.StoredIn.ToString();

							if (KeyboardInputManager.IsTabPressed())
							{
								Input = "StoredIn=" + Input;
							}


							if (SearchModifiersContinue(Input))
							{
								HandleObjectInput(Info.StoredIn);
							}
							break;
						case LogMarker.ControlledBy:

							Input = Info.PlayerAccountID.ToString();

							if (KeyboardInputManager.IsTabPressed())
							{
								Input= "PlayerAccount=" + Input;
							}

							if (SearchModifiersContinue(Input))
							{
								HandleAccountIDInput(Info.PlayerAccountID);
							}
							break;
						case LogMarker.Position:

							Input = Info.PositionWorld.ToString();

							if (KeyboardInputManager.IsTabPressed())
							{
								Input= "Position=" + Input;
							}

							if (SearchModifiersContinue(Input))
							{
								HandlePositionInput(Info.PositionWorld);
							}
							break;
					}
				}
				else
				{
					LogInfoUI.Expand();
				}
			}
		}

		public bool SearchModifiersContinue(string Input)
		{
			//so
			//alt = add to OR search
			//click = TP to ControlledBy?
			//control = Remove from AND search
			//Shift = add to AND search
			//tab Just add to Search


			if (KeyboardInputManager.IsControlPressed())
			{

				UIManager.Instance.AdminLogsWindow.SearchField.text =
					UIManager.Instance.AdminLogsWindow.SearchField.text.ReplaceFirst(" AND " + Input, "");
				return false;
			}

			if (KeyboardInputManager.IsShiftPressed())
			{
				if (string.IsNullOrEmpty(UIManager.Instance.AdminLogsWindow.SearchField.text) == false)
				{
					UIManager.Instance.AdminLogsWindow.SearchField.text += " AND " + Input;
				}
				else
				{
					UIManager.Instance.AdminLogsWindow.SearchField.text = Input;
				}


				return false;
			}

			if (KeyboardInputManager.IsAltActionKeyPressed())
			{
				if (string.IsNullOrEmpty(UIManager.Instance.AdminLogsWindow.SearchField.text) == false)
				{
					UIManager.Instance.AdminLogsWindow.SearchField.text += " OR " + Input;

				}
				else
				{
					UIManager.Instance.AdminLogsWindow.SearchField.text = Input;
				}

				return false;
			}

			return true;
		}

		public void HandleObjectInput(uint netID)
		{
			if (CustomNetworkManager.Spawned.ContainsKey(netID))
			{
				RequestAdminTeleport.Send("", "",
					RequestAdminTeleport.OpperationList.TeleportAdmin,
					PlayerManager.LocalPlayerScript.IsGhost,
					CustomNetworkManager.Spawned[netID].gameObject.AssumedWorldPosServer());
			}
		}

		public void HandlePositionInput(string Position)
		{
			RequestAdminTeleport.Send("", "",
				RequestAdminTeleport.OpperationList.TeleportAdmin,
				PlayerManager.LocalPlayerScript.IsGhost,
				Position.ToVector3());

		}

		public void HandleAccountIDInput(string AccountID)
		{

			RequestAdminTeleport.Send("", AccountID,
				RequestAdminTeleport.OpperationList.AdminToPlayer,
				PlayerManager.LocalPlayerScript.IsGhost,
				Vector3.zero);

		}

		static string InsertLineBreaks(string input, int lineLength)
		{
			if (string.IsNullOrEmpty(input)) return input;

			int insertPosition = lineLength;

			int EmergencyBreak = 0;

			while (insertPosition < input.Length && EmergencyBreak < 1000)
			{
				input = input.Insert(insertPosition, "\n");
				insertPosition += lineLength + 1; // +1 for the inserted '\n'
				EmergencyBreak++;
			}

			return input;
		}
	}
}
