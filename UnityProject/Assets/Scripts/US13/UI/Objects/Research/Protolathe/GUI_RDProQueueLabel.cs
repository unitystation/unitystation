using System;
using UnityEngine.UI;
using US13.UI.Core.Net.Elements;

namespace US13.UI.Objects.Research.Protolathe
{
	[Serializable]
	public class GUI_RDProQueueLabel : NetUIStringElement
	{
		public override string Value {
			get => TextComponent.text;
			protected set {
				externalChange = true;
				TextComponent.text = value;
				externalChange = false;
			}
		}

		public Text TextComponent => textComponent ??= GetComponent<Text>();
		private Text textComponent;
	}
}
