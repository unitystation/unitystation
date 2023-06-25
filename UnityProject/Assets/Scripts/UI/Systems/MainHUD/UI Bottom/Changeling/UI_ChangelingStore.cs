using System.Collections;
using System.Collections.Generic;
using UI.Systems.MainHUD.UI_Bottom;
using UnityEngine;

namespace Changeling
{
	public class UI_ChangelingStore : MonoBehaviour
	{
		[SerializeField]
		private GameObject entryPrefab = null;

		[SerializeField]
		private GameObject contentArea = null;

		[SerializeField]
		private UI_Changeling uiAlien = null;

		private List<EvolveMenuEntry> entryPool = new List<EvolveMenuEntry>();


		public void Refresh()
		{

		}
	}
}