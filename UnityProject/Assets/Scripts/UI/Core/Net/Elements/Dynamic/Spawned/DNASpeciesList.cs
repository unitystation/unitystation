using System.Collections;
using System.Collections.Generic;
using UI.Core.NetUI;
using UI.Objects.Medical;
using UnityEngine;

public class DNASpeciesList : EmptyItemList
{
	public DNASpeciesElement AddElement(PlayerHealthData PlayerHealthData, GUI_DNAConsole GUI_DNAConsole)
	{
		var NewElement  = AddItem() as DNASpeciesElement;
		NewElement.SetValues(PlayerHealthData, GUI_DNAConsole);
		return NewElement;
	}
}
