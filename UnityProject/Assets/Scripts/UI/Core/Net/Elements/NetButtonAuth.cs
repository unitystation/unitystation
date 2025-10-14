using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Core.NetUI
{
	/// <summary>
	/// Button that pass player that pressed this button
	/// Useful for testing player ID access
	/// </summary>
	[RequireComponent(typeof(Button))]
	[Serializable]
	public class NetButtonAuth : NetUIStringElement
	{
		public bool AddInRunTime = false;
		private Button Button;
		public ConnectedPlayerEvent ServerMethod;

		public void Awake()
		{
			Button = this.GetComponent<Button>();
			if (AddInRunTime)
			{
				Button.onClick = new Button.ButtonClickedEvent();
				Button.onClick.AddListener(ExecuteClient);
			}
		}

		public override void ExecuteServer(PlayerInfo subject)
		{
			ServerMethod.Invoke(subject);
		}
	}
}
