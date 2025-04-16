using Cysharp.Threading.Tasks;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Machines;

namespace UI.Objects.Cargo
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
			foreach (var material in materialRecords.Keys)
			{
				var item = materialList.Entries[i] as GUI_MaterialEntry;
				item?.SetValues(material, materialRecords[material], this);
				i++;
				//(Max): This shit fucking sucks major balls. NetUI is ass.
				//At least we have a fun looking update effect from this workaround to make sure that NetUI doesn't shit itself
				//when updating entries.
				await UniTask.WaitForSeconds(0.1f);
			}
		}
	}
}
