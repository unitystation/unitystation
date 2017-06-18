using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class ControlTabs : MonoBehaviour
	{
		public Button statsTab;
		public Button optionsTab;

		public GameObject panelStats;
		public GameObject panelOptions;

		public Color unselectColor;
		public Color selectedColor;

		private enum WindowSelect
		{
			stats,
			options
		}

		void Start()
		{
			SelectWindow(WindowSelect.stats);
		}

		private void SelectWindow(WindowSelect winSelect)
		{
			switch (winSelect) {
				case WindowSelect.stats:
					statsTab.image.color = selectedColor;
					optionsTab.image.color = unselectColor;
					panelOptions.SetActive(false);
					panelStats.SetActive(true);
					break;
				case WindowSelect.options:
					statsTab.image.color = unselectColor;
					optionsTab.image.color = selectedColor;
					panelOptions.SetActive(true);
					panelStats.SetActive(false);
					break;
			}
		}

		public void Button_Stats(){
			SelectWindow(WindowSelect.stats);
			SoundManager.Play("Click01");
		}

		public void Button_Options(){
			SelectWindow(WindowSelect.options);
			SoundManager.Play("Click01");
		}
	}
}
