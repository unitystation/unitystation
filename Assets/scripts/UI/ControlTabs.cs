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

		private static ControlTabs controlTabs;

		public static ControlTabs Instance
		{
			get
			{
				if (!controlTabs)
				{
					controlTabs = FindObjectOfType<ControlTabs>();
				}

				return controlTabs;
			}
		}

		void Start()
		{
			SelectWindow(WindowSelect.stats);
			objectWindowExists = false;
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


		/// <summary>
		/// Displays the Tile List tab and moves the Options and More tabs out of the way
		/// </summary>
		public static void ShowObjectsWindow(List<GameObject> tiles)
		{
			if(Instance.objectWindowExists) {
				return;
			}

			foreach (GameObject tile in tiles) {
				UITileList.addObjectToPanel(tile);
			}

			float width = Instance.objectsTab.GetComponent<RectTransform>().rect.width;
			RectTransform optionsRect = Instance.optionsTab.GetComponent<RectTransform>();
			RectTransform moreRect = Instance.moreTab.GetComponent<RectTransform>();

			//Slide over the other two tabs
			optionsRect.localPosition += Vector3.right * (width / 2f);
			moreRect.localPosition += Vector3.right * (width / 2f);

			Instance.objectsTab.gameObject.SetActive(true);
			Instance.Button_Objects();
			Instance.objectWindowExists = true;
		}

		/// <summary>
		/// Hides the Tile List tab and moves the Options and More tabs back to their original positions
		/// </summary>
		public static void HideObjectsWindow()
		{
			if (!Instance.objectWindowExists)
			{
				return;
			}

			float width = Instance.objectsTab.GetComponent<RectTransform>().rect.width;
			RectTransform optionsRect = Instance.optionsTab.GetComponent<RectTransform>();
			RectTransform moreRect = Instance.moreTab.GetComponent<RectTransform>();

			//Slide back the other two tabs
			optionsRect.localPosition += Vector3.left * (width / 2f);
			moreRect.localPosition += Vector3.left * (width / 2f);

			Instance.objectsTab.gameObject.SetActive(false);
			Instance.Button_Stats();
			Instance.objectWindowExists = false;
		}
	}
}
