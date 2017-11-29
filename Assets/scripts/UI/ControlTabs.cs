using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class ControlTabs : MonoBehaviour
	{
		public Button statsTab;
		public Button objectsTab;
		public Button optionsTab;
		public Button moreTab;

		public GameObject panelStats;
		public GameObject panelObjects;
		public GameObject panelOptions;
		public GameObject panelMore;

		public Color unselectColor;
		public Color selectedColor;

		private bool objectWindowExists;

		private enum WindowSelect
		{
			stats,
			objects,
			options,
			more
		}

		void Start()
		{
			SelectWindow(WindowSelect.stats);
			this.objectWindowExists = false;
		}

		private void SelectWindow(WindowSelect winSelect)
		{
			switch (winSelect) {
				case WindowSelect.stats:
					UnselectAll();
					statsTab.image.color = selectedColor;
					panelStats.SetActive(true);
					break;
				case WindowSelect.objects:
					UnselectAll();
					objectsTab.image.color = selectedColor;
					panelObjects.SetActive(true);
					break;
				case WindowSelect.options:
					UnselectAll();
					optionsTab.image.color = selectedColor;
					panelOptions.SetActive(true);
					break;
				case WindowSelect.more:
					UnselectAll();
					moreTab.image.color = selectedColor;
					panelMore.SetActive(true);
					break;
			}
		}

		private void UnselectAll()
		{
			statsTab.image.color = unselectColor;
			objectsTab.image.color = unselectColor;
			optionsTab.image.color = unselectColor;
			moreTab.image.color = unselectColor;
			panelStats.SetActive(false);
			panelObjects.SetActive(false);
			panelOptions.SetActive(false);
			panelMore.SetActive(false);
		}

		public void Button_Stats()
		{
			SelectWindow(WindowSelect.stats);
			SoundManager.Play("Click01");
		}

		public void Button_Objects()
		{
			SelectWindow(WindowSelect.objects);
			SoundManager.Play("Click01");
		}

		public void Button_Options()
		{ 
			SelectWindow(WindowSelect.options);
			SoundManager.Play("Click01");
		}

		public void Button_More()
		{
			SelectWindow(WindowSelect.more);
			SoundManager.Play("Click01");
		}

		public void ShowObjectsWindow()
		{
			if(this.objectWindowExists) {
				return;
			}

			float width = objectsTab.GetComponent<RectTransform>().rect.width;
			RectTransform optionsRect = optionsTab.GetComponent<RectTransform>();
			RectTransform moreRect = moreTab.GetComponent<RectTransform>();

			//Slide over the other two tabs
			optionsRect.localPosition += Vector3.right * (width / 2f);
			moreRect.localPosition += Vector3.right * (width / 2f);

			objectsTab.gameObject.SetActive(true);
			Button_Objects();
			this.objectWindowExists = true;
		}

		public void HideObjectsWindow()
		{
			if (!this.objectWindowExists)
			{
				return;
			}

			float width = objectsTab.GetComponent<RectTransform>().rect.width;
			RectTransform optionsRect = optionsTab.GetComponent<RectTransform>();
			RectTransform moreRect = moreTab.GetComponent<RectTransform>();

			//Slide back the other two tabs
			optionsRect.localPosition += Vector3.left * (width / 2f);
			moreRect.localPosition += Vector3.left * (width / 2f);

			objectsTab.gameObject.SetActive(false);
			Button_Stats();
			this.objectWindowExists = false;
		}
	}
}
