using Core.Admin.Logs;
using Logs;
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

		public LogInfo Info;

		public LogMarker LogMarker;

		public void OnBeforeSerialize()
		{
			if (textComponent == null)
			{
				textComponent = GetComponent<TMP_Text>();
			}
		}

		public void SetUp(LogInfo InInfo)
		{
			Info = InInfo;
			switch (LogMarker)
			{
				case LogMarker.Info:
					if (string.IsNullOrWhiteSpace(Info.CoreObjectName) == false)
					{
						textComponent.text = "";
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
					textComponent.text = Info.WasControlledByPlayer.AccountId;
					break;
				case LogMarker.Position:
					textComponent.text = Info.Info;
					break;
			}

			if (string.IsNullOrWhiteSpace(textComponent.text))
			{
				gameObject.SetActive(false);
			}
			else
			{
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
						case LogMarker.Core:
							break;
						case LogMarker.StoredIn:
							break;
						case LogMarker.ControlledBy:
							break;
						case LogMarker.Position:
							break;
					}
					//TODO do logic here
				}
				else
				{
					LogInfoUI.Expand();
				}
			}


		}
	}
}
