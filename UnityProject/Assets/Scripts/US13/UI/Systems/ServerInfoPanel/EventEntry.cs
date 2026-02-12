using TMPro;
using UnityEngine;
using UnityEngine.UI;
using US13.ScriptableObjects.TimedGameEvents;

namespace US13.UI.Systems.ServerInfoPanel
{
	public class EventEntry : MonoBehaviour
	{
		[field: SerializeField] public Image EventImage {get; private set;}
		[field: SerializeField] public TMP_Text EventName {get; private set;}
		[field: SerializeField] public TMP_Text EventDesc {get; private set;}

		public void SetEvent(TimedGameEventSO eventSo)
		{
			EventName.text = eventSo.EventName;
			EventDesc.text = eventSo.EventDesc;
			EventImage.sprite = eventSo.EventIcon;
		}
	}
}