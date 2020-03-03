using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class AdminChatEntry : MonoBehaviour
	{
		[SerializeField] private Text msgText = null;

		public void SetText(string msg)
		{
			msgText.text = msg;
		}
	}
}
