using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Core.NetUI
{
	/// Simple button, has no special value
	[RequireComponent(typeof(Button))]
	[Serializable]
	public class NetButton : NetUIStringElement, IPointerEnterHandler, IPointerExitHandler
	{

		public bool AddInRunTime = true;

		private Button Button;

		public UnityEvent ServerMethod;

		public UnityEvent OnMouseEnter;
		public UnityEvent OnMouseExit;

		public override void ExecuteServer(PlayerInfo subject)
		{
			ServerMethod.Invoke();
		}

		public void Awake()
		{
			Button = this.GetComponent<Button>();
			if (AddInRunTime)
			{
				Button.onClick = new Button.ButtonClickedEvent();
				Button.onClick.AddListener(ExecuteClient);
			}
		}


		public void OnPointerEnter(PointerEventData eventData)
		{
			OnMouseEnter?.Invoke();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			OnMouseExit?.Invoke();
		}
	}
}
