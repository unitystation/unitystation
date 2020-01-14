using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class KickBanEntryPage : MonoBehaviour
	{
		[SerializeField] private GameObject kickPage;
		[SerializeField] private GameObject banPage;

		[SerializeField] private Text kickTitle;
		[SerializeField] private InputField kickReasonField;

		[SerializeField] private Text banTitle;
		[SerializeField] private InputField banReasonField;
		[SerializeField] private InputField minutesField;

		public void SetPage(bool isBan, AdminPlayerEntryData playerToKick)
		{
			if (!isBan)
			{
				kickPage.SetActive(true);
				kickTitle.text = $"Kick Player: {playerToKick.name}";
				kickReasonField.text = "";
				kickReasonField.Select();
			}
			else
			{
				banPage.SetActive(true);
				banTitle.text = $"Ban Player: {playerToKick.name}";
				banReasonField.text = "";
				minutesField.text = "";
			}

			gameObject.SetActive(true);
		}

		public void OnDoKick()
		{
			if (string.IsNullOrEmpty(kickReasonField.text))
			{
				Logger.LogError("Kick reason field needs to be completed!", Category.Admin);
				return;
			}

			ClosePage();
		}

		public void OnDoBan()
		{
			if (string.IsNullOrEmpty(banReasonField.text))
			{
				Logger.LogError("Ban reason field needs to be completed!", Category.Admin);
				return;
			}

			if (string.IsNullOrEmpty(minutesField.text))
			{
				Logger.LogError("Duration field needs to be completed!", Category.Admin);
				return;
			}


			ClosePage();
		}

		public void ClosePage()
		{
			gameObject.SetActive(false);
			kickPage.SetActive(false);
			banPage.SetActive(false);
		}
	}
}