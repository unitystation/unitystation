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
		private GameObject pageFromCache;

		public void SetAreYouSurePage(string message, Action proceedAction, GameObject pageFrom = null)
		{
			pageFromCache = pageFrom;

			if (pageFrom != null)
			{
				pageFrom.SetActive(false);
			}

			actionCache = proceedAction;
			areYouSureText.text = message;
			gameObject.SetActive(true);
		}

		public void OnCancel()
		{
			gameObject.SetActive(false);

			if (pageFromCache != null)
			{
				pageFromCache.SetActive(true);
			}
		}

		public void OnProceed()
		{
			if (pageFromCache != null)
			{
				pageFromCache.SetActive(true);
			}

			actionCache.Invoke();
			gameObject.SetActive(false);
		}
	}
}