using Core.Admin.Logs;
using Logs;
using Messages.Client.Admin;
using TMPro;
using UI.Systems.AdminTools.AdminLogs;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Systems.AdminTools.AdminLogs
{
	[RequireComponent(typeof(TMP_Text))]
	public class SubLogInfoUI : MonoBehaviour, ISerializationCallbackReceiver, IPointerClickHandler
	{
		[SerializeField] private TMP_Text textComponent;

		public LogInfoUI LogInfoUI;

		public LongTermLogEntry.LogItems Info;

		public LogMarker LogMarker;

		public void OnBeforeSerialize()
		{
			if (textComponent == null)
			{
				textComponent = GetComponent<TMP_Text>();
			}
		}

		public void SetUp(LongTermLogEntry.LogItems InInfo)
		{
			Info = InInfo;
			switch (LogMarker)
			{
				case LogMarker.Info:
					if (string.IsNullOrWhiteSpace(Info.CoreObjectName) == false)
					{
						textComponent.text = Info.CoreObjectName;
					}
					else
					{
						textComponent.text = Info.Info;
					}
					break;

				case LogMarker.Core:
					textComponent.text = Info.CoreObjectName;
					break;
				case LogMarker.StoredIn:
					textComponent.text = Info.WasStoredInObjectName;
					break;
				case LogMarker.ControlledBy:
					textComponent.text = Info.WasControlledByPlayerAccountId;
					break;
				case LogMarker.Position:
					textComponent.text = Info.WasAtPositionWorld;
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
					switch (LogMarker)
					{
						//so
						//alt = add to OR search
						//click = TP to ControlledBy?
						//control = Remove from AND search
						//Shift = add to AND search
						case LogMarker.Core:
							if (SearchModifiersContinue(Info.CoreObject.ToString()))
							{
								HandleObjectInput(Info.CoreObject);
							}
							break;
						case LogMarker.StoredIn:
							if (SearchModifiersContinue(Info.WasStoredInObject.ToString()))
							{
								HandleObjectInput(Info.WasStoredInObject);
							}
							break;
						case LogMarker.ControlledBy:
							if (SearchModifiersContinue(Info.WasControlledByPlayerAccountId))
							{
								HandleAccountIDInput(Info.WasControlledByPlayerAccountId);
							}
							break;
						case LogMarker.Position:
							if (SearchModifiersContinue(Info.WasAtPositionWorld.ToString()))
							{
								HandlePositionInput(Info.WasAtPositionWorld);
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
			if (KeyboardInputManager.IsControlPressed())
			{
				Loggy.Error("Remove From AND Search");
				return false;
			}

			if (KeyboardInputManager.IsShiftPressed())
			{
				Loggy.Error("add to AND Search");
				return false;
			}

			if (KeyboardInputManager.IsAltActionKeyPressed())
			{
				Loggy.Error("add to OR Search");
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

			while (insertPosition < input.Length)
			{
				input = input.Insert(insertPosition, "\n");
				insertPosition += lineLength + 1; // +1 for the inserted '\n'
			}

			return input;
		}
	}
}
