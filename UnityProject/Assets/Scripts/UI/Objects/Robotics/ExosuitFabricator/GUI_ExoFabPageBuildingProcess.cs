﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Objects.Robotics
{
	public class GUI_ExoFabPageBuildingProcess : NetPage
	{
		private bool isAnimating = false;
		public bool IsAnimating { get => isAnimating; }

		[SerializeField]
		private NetLabel buildingLabel = null;

		[SerializeField]
		private NetLabel pleaseWaitLabel = null;

		private string[] pleaseWaitText = { "Please wait until completion . . .",
		"Please wait until completion . .", "Please wait until completion .", "Please wait until completion" };

		public void SetProductLabelProductName(string productName)
		{
			buildingLabel.SetValueServer("Building " + productName);
		}

		public void StartAnimateLabel()
		{
			if (isAnimating == false)
			{
				isAnimating = true;
				StartCoroutine(AnimatingLabel());
			}
		}

		public void StopAnimatingLabel()
		{
			isAnimating = false;
		}

		private IEnumerator AnimatingLabel()
		{
			while (isAnimating)
			{
				foreach (string text in pleaseWaitText)
				{
					pleaseWaitLabel.SetValueServer(text);
					yield return WaitFor.Seconds(0.5f);
				}
			}
		}
	}
}
