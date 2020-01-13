using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdminTools
{
	public class AdminPage : MonoBehaviour
	{
		protected AdminPageRefreshData currentData;

		public virtual void OnEnable()
		{
			RefreshPage();
		}

		public void RefreshPage()
		{
			
		}

		public virtual void OnPageRefresh()
		{

		}
	}

	[Serializable]
	public class AdminPageRefreshData
	{
		//GameMode updates:
		public List<string> availableGameModes = new List<string>();
		public string currentGameMode;
		public bool isSecret;

	}
}