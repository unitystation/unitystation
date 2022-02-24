﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UI.Core.NetUI;

namespace UI.Objects.Robotics
{
	[Serializable]
	public class GUI_ExoFabQueueLabel : NetUIStringElement
	{
		public override string Value {
			get => TextComponent.text;
			set {
				externalChange = true;
				TextComponent.text = value;
				externalChange = false;
			}
		}

		public Text TextComponent => textComponent ??= GetComponent<Text>();
		private Text textComponent;
	}
}
