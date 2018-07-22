﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class Options : MonoBehaviour
	{
		public Image[] panelImages;
		public Text tipText;
		[Header("Corresponds to Appearence Dropdown positions")]
		public UI_Theme[] themes;


		private void Start()
		{
		}
	}

	[System.Serializable]
	public class UI_Theme{
		public Color newPanelColor;
		public Color newTipTextColor;
	}
}


