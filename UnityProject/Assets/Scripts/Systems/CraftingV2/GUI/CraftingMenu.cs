using System.Collections.Generic;
using HealthV2;
using Initialisation;
using Player;
using UnityEngine;

namespace Systems.CraftingV2.GUI
{
	public class CraftingMenu : MonoBehaviour, IInitialise
	{
		public static CraftingMenu Instance;
		//All the nav buttons in the left column
		private CategoryButton[] categoryButtons;

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

		public InitialisationSystems Subsystem { get; }

		void IInitialise.Initialise()
		{
			Init();
		}


		void Init()
		{
			categoryButtons = GetComponentsInChildren<CategoryButton>(true);
			this.SetActive(false);
		}

		public void ChooseCategory(CategoryButton button)
		{
			foreach (CategoryButton categoryButton in categoryButtons)
			{
				if (categoryButton == button)
				{
					categoryButton.Toggle(true);
				}
				else
				{
					categoryButton.Toggle(false);
				}
			}
		}

		/// <summary>
		/// Open the Crafting Menu
		/// </summary>
		public void Open(PlayerCrafting playerCrafting)
		{
//			ChooseCategory(categoryButtons[0]);
			this.SetActive(true);
		}

		/// <summary>
		/// Close the Crafting Menu
		/// </summary>
		public void Close()
		{
			this.SetActive(false);
		}
	}
}