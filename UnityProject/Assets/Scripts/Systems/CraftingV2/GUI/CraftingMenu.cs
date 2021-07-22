using System.Collections.Generic;
using Player;
using UnityEngine;
using UnityEngine.UI;

namespace Systems.CraftingV2.GUI
{
	public class CraftingMenu : MonoBehaviour
	{
		public static CraftingMenu Instance;

		[SerializeField] private List<GameObject> categoryButtonsPrefabs;

		[SerializeField] private GameObject categoriesLayoutGameObject;

		[SerializeField] private GameObject recipesLayoutGameObject;

		private GridLayoutGroup recipesGridLayout;

		private HorizontalLayoutGroup categoriesLayout;

		private CategoryButton chosenCategory;

		#region Lifecycle

		void Awake()
		{
			if (Instance == null)
			{
				InitFields();
				InitCategories();
				return;
			}
			Destroy(gameObject);
		}

		private void InitFields()
		{
			Instance = this;
			recipesGridLayout = recipesLayoutGameObject.GetComponent<GridLayoutGroup>();
			categoriesLayout = categoriesLayoutGameObject.GetComponent<HorizontalLayoutGroup>();
		}

		private void InitCategories()
		{
			foreach (GameObject categoryButtonPrefab in categoryButtonsPrefabs)
			{
				Instantiate(categoryButtonPrefab, categoriesLayoutGameObject.transform);
			}
			SelectCategory(categoriesLayout.GetComponentInChildren<CategoryButton>());
		}

		#endregion

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

		public void Refresh(PlayerCrafting playerCrafting)
		{

		}
	}
}