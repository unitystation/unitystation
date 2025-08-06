using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace UI.Systems.PreRound
{
	public class PreRoundLoadingArea : MonoBehaviour
	{
		[SerializeField] private Scrollbar loadingBar = null;
		[SerializeField] private TMP_Text loadingTitle = null;
		[SerializeField] private TMP_Text loadingSubject = null;
		[SerializeField] private TMP_Text loadingTooLongWarning = null;
		[SerializeField] private TMP_Text loadingTextDetailed = null;

		private int loadingTooLongWarningCount = 0;

		private void OnEnable()
		{
			UpdateManager.Add(UpdateMe, 1f);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			loadingTooLongWarningCount = 0;
		}

		private void UpdateMe()
		{
			loadingTooLongWarningCount++;
			if (loadingTooLongWarningCount > 8)
			{
				loadingTooLongWarning?.gameObject.SetActive(true);
			}
			else
			{
				loadingTooLongWarning?.gameObject.SetActive(false);
			}
		}

		public void UpdateLoadingBar(string title, string subject, float loadedAmt)
		{
			if (title == "" && subject == "") return;
			if (loadedAmt >= 1f)
			{
				loadingBar.size = 1f;
				gameObject.SetActive(false);
			}
			if (gameObject.activeSelf == false)
			{
				gameObject.SetActive(true);
			}
			loadingTitle.text = title;
			loadingSubject.text = subject;
			loadingBar.size = loadedAmt;
		}
	}
}