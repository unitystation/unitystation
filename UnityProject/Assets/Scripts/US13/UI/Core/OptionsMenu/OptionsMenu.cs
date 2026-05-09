using System.Collections.Generic;
using DynamicOptions;
using UnityEngine;
using US13.Core.Initialisation;

namespace US13.UI.Core.OptionsMenu
{
	/// <summary>
	/// Main controller for the Options Screen
	/// It is spawned via managers on manager start
	/// and persists across scene changes
	/// </summary>
	public class OptionsMenu : MonoBehaviour, IInitialise
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
			}
			else
			{
				Destroy(gameObject);
			}
		}

		public InitialisationSystems Subsystem => InitialisationSystems.OptionsMenu;

		void IInitialise.Initialise()
		{
			Init();
		}


		void Init()
		{
			var btns = GetComponentsInChildren<OptionsButton>(true);
			optionButtons = new List<OptionsButton>(btns);
			screen.SetActive(false);
			OptionManager.Instance.Initialise();
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
				switch (optionButtons[index].name)
				{
					case ("DisplaySettings"):
						OptionManager.Instance.OptionCollectionDisplay.ResetToDefault();
						return;
					case ("AudioSettings"):
						OptionManager.Instance.OptionCollectionAudio.ResetToDefault();
						return;
					case ("ThemeSettings"):
						OptionManager.Instance.OptionCollectionTheme.ResetToDefault();
						return;
					case ("GameSettings"):
						OptionManager.Instance.OptionCollectionGameplay.ResetToDefault();
						return;
					case ("Misc"):
						OptionManager.Instance.OptionCollectionMisc.ResetToDefault();
						return;
				}
			}
		}
	}
}