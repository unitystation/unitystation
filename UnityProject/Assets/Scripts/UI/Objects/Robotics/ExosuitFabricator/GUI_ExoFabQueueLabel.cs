using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Objects.Robotics
{
	[Serializable]
	public class GUI_ExoFabQueueLabel : NetUIStringElement
	{
		public override string Value {
			get { return TextComponent.text; }
			set {
				externalChange = true;
				TextComponent.text = value;
				externalChange = false;
			}
		}

		private Text textComponent;

		public Text TextComponent {
			get {
				if (!textComponent)
				{
					textComponent = GetComponent<Text>();
				}
				return textComponent;
			}
		}

		public override void ExecuteServer(ConnectedPlayer subject)
		{
		}
	}
}
