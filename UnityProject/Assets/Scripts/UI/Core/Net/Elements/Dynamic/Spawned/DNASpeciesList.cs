using System.Collections;
using System.Collections.Generic;
using UI.Core.NetUI;
using UI.Objects.Medical;
using UnityEngine;

public class DNASpeciesList : EmptyItemList
{
	public DNASpeciesElement AddElement(PlayerHealthData PlayerHealthData)
	{
		var NewElement  = AddItem() as DNASpeciesElement;
		NewElement.SetValues(PlayerHealthData);
		return NewElement;
	}
}
