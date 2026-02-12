using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using US13.Objects.Machines;
using US13.UI.Core;
using US13.UI.Core.Net.Page;

namespace US13.UI.Objects.Mining.Ore_Redemption_Machine
{
	public class GUI_MaterialsList : NetPage
	{
		[SerializeField] private EmptyItemList materialList = null;
		public MaterialStorageLink materialStorageLink;

		public void UpdateMaterialList()
		{
			_ = SetList();
		}

		private async UniTask SetList()
		{
			var materialRecords = materialStorageLink.usedStorage.MaterialList;
			materialList.Clear();
			materialList.AddItems(materialRecords.Count);
			var i = 0;

			var KeysList = materialRecords.Keys.ToList();

			for (int j = 0; j < KeysList.Count; j++)
			{
				var item = materialList.Entries[i] as GUI_MaterialEntry;
				item?.SetValues(KeysList[j], materialRecords[KeysList[j]], this);
				i++;
				//(Max): This shit fucking sucks major balls. NetUI is ass.
				//At least we have a fun looking update effect from this workaround to make sure that NetUI doesn't shit itself
				//when updating entries.
				await UniTask.WaitForSeconds(0.1f);
			}
		}
	}
}
