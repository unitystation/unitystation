using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;

namespace UI.Objects
{
	public class GUI_RDProPageBuildingProcess : NetPage
	{
		private bool isAnimating = false;
		public bool IsAnimating => isAnimating;

		[SerializeField]
		private NetText_label buildingLabel = null;

		[SerializeField]
		private NetText_label pleaseWaitLabel = null;

		private string[] pleaseWaitText = { "Please wait until completion . . .",
		"Please wait until completion . .", "Please wait until completion .", "Please wait until completion" };

		public void SetProductLabelProductName(string productName)
		{
			buildingLabel.MasterSetValue("Building " + productName);
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
					pleaseWaitLabel.MasterSetValue(text);
					yield return WaitFor.Seconds(0.5f);
				}
			}
		}
	}
}
