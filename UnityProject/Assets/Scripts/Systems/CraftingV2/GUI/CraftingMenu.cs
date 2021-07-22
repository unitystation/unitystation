using Player;
using UnityEngine;

namespace Systems.CraftingV2.GUI
{
	public class CraftingMenu : MonoBehaviour
	{
		public static CraftingMenu Instance;
		// All the nav buttons in the left column
		private CategoryButton[] categoryButtons;
		private CategoryButton chosenCategory;

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

		private void SelectCategory(CategoryButton categoryButton)
		{
			categoryButton.OnPressed();
			chosenCategory = categoryButton;
		}

		private void DeselectCategory(CategoryButton categoryButton)
		{
			categoryButton.OnUnpressed();
		}

		public void ChangeCategory(CategoryButton categoryButton)
		{
			DeselectCategory(chosenCategory);
			SelectCategory(categoryButton);
		}

		/// <summary>
		/// Open the Crafting Menu
		/// </summary>
		public void Open(PlayerCrafting playerCrafting)
		{
			Init(playerCrafting);
			Refresh(playerCrafting);
			this.SetActive(true);
		}

		/// <summary>
		/// Close the Crafting Menu
		/// </summary>
		public void Close()
		{
			this.SetActive(false);
		}

		public void Init(PlayerCrafting playerCrafting)
		{
			/*
			if (categoryButtons == null)
			{
				categoryButtons = new CategoryButton[playerCrafting.KnownRecipesByCategory.Count];
			}

			for (int i = 0; i < categoryButtons.Length; i++)
			{
				categoryButtons[i] = new CategoryButton(i.ToString());
			}
			*/
			//SelectCategory(categoryButtons[0]);
		}

		public void Refresh(PlayerCrafting playerCrafting)
		{

		}
	}
}