using ScriptableObjects.TimedGameEvents;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.PreRound
{
	public class EventEntry : MonoBehaviour
	{
		[field: SerializeField] public Image EventImage {get; private set;}
		[field: SerializeField] public TMP_Text EventName {get; private set;}
		[field: SerializeField] public TMP_Text EventDesc {get; private set;}

		public void SetEvent(TimedGameEventSO eventSo)
		{
			EventImage.sprite = eventSo.EventIcon;
			EventName.text = eventSo.EventName;
			EventDesc.text = eventSo.EventDesc;
		}
	}
}