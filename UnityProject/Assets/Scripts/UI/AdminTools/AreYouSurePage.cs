using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class AreYouSurePage : MonoBehaviour
	{
		[SerializeField] private Text areYouSureText = null;
		private Action actionCache;

		public void SetAreYouSurePage(string message, Action proceedAction)
		{
			actionCache = proceedAction;
			areYouSureText.text = message;
			gameObject.SetActive(true);
		}

		public void OnCancel()
		{
			gameObject.SetActive(false);
		}

		public void OnProceed()
		{
			actionCache.Invoke();
			gameObject.SetActive(false);
		}
	}
}