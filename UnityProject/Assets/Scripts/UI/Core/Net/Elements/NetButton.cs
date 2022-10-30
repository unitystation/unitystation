using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Core.NetUI
{
	/// Simple button, has no special value
	[RequireComponent(typeof(Button))]
	[Serializable]
	public class NetButton : NetUIStringElement
	{

		private Button Button;

		public UnityEvent ServerMethod;

		public override void ExecuteServer(PlayerInfo subject)
		{
			ServerMethod.Invoke();
		}

		public void Awake()
		{
			Button = this.GetComponent<Button>();
			Button.onClick = new Button.ButtonClickedEvent();
			Button.onClick.AddListener(ExecuteClient);
		}



	}
}
