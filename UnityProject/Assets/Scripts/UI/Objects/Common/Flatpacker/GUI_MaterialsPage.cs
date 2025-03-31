using Objects.Machines;
using UI.Core.NetUI;
using UI.Objects;
using UnityEngine;

public class GUI_MaterialsPage : NetPage
{
	[SerializeField] private EmptyItemList materialList = null;

	public void InitMaterialList(MaterialStorage materialStorage)
	{
		var materialRecords = materialStorage.MaterialList;

		materialList.Clear();
		materialList.AddItems(materialRecords.Count);
		var i = 0;
		foreach (var material in materialRecords.Keys)
		{
			GUI_FlatpackerMaterialsEntry item = materialList.Entries[i] as GUI_FlatpackerMaterialsEntry;
			item?.ReInit(material, materialRecords[material]);
			i++;
		}
	}

	public void UpdateMaterialList(MaterialStorage materialStorage)
	{
		InitMaterialList(materialStorage);
	}
}
