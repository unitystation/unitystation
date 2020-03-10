using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class AdminHelpChat : MonoBehaviour
	{
		[SerializeField] private InputField chatInputField = null;
		[SerializeField] private Transform content = null;
		[SerializeField] private Transform thresholdMarker = null;

		public Transform ThresholdMarker => thresholdMarker;
		public Transform Content => content;

		public void CloseWindow()
		{
			gameObject.SetActive(false);
		}
	}
}