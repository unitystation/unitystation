using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI.Systems.AdminTools
{
	public class LogUI : MonoBehaviour
	{
		[SerializeField] private TMP_Text text;

		public TMP_Text Text
		{
			get => text;
			set => text = value;
		}
	}
}