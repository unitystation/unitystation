using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdminTools
{
	public class AdminGlobalAudioButtonTab : MonoBehaviour
	{
		[SerializeField] private GameObject globalSoundWindow = null;

		public void OnClick()
		{
			if (!globalSoundWindow.activeInHierarchy)
			{
				globalSoundWindow.SetActive(true);
			}
			else
			{
				globalSoundWindow.SetActive(false);
			}
		}
	}
}
