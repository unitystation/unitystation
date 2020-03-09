using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unitystation.Options
{
	/// <summary>
	/// Main controller for the Options Screen
	/// It is spawned via managers on manager start
	/// and persists across scene changes
	/// </summary>
	public class OptionsMenu : MonoBehaviour
	{
		public static OptionsMenu Instance;
		[SerializeField]
		private GameObject screen = null;
		//All the nav buttons in the left column
		private List<OptionsButton> optionButtons = new List<OptionsButton>();

		void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
				// DontDestroyOnLoad(gameObject); //Commented out because it only works for root or components of root (Warning)
				Init();
			}
			else
			{
				Destroy(gameObject);
			}
		}

		void Init()
		{
			var btns = GetComponentsInChildren<OptionsButton>(true);
			optionButtons = new List<OptionsButton>(btns);
			screen.SetActive(false);
		}

		public void ToggleButtonOn(OptionsButton button)
		{
			foreach (OptionsButton b in optionButtons)
			{
				if (b == button)
				{
					b.Toggle(true);
				}
				else
				{
					b.Toggle(false);
				}
			}
		}

		/// <summary>
		/// Open the Options Menu
		/// </summary>
		public void Open()
		{
			ToggleButtonOn(optionButtons[0]);
			screen.SetActive(true);
		}

		/// <summary>
		/// Close the Options Menu
		/// </summary>
		public void Close()
		{
			screen.SetActive(false);
		}

		/// <summary>
		/// Used to reset options to default
		/// </summary>
		public void Reset()
		{
			var index = optionButtons.FindIndex(x => x.IsActive == true);
			if (index != -1)
			{
				optionButtons[index].ResetDefaults();
			}
		}
	}
}